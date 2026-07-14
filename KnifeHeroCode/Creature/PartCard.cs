using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* ── THE PARTS — the Creature's body ────────────────────────────────────────────────────────────
   A Part is a card that arrives as a Curse: Eternal + Retain, so it sits in your hand and cannot be
   removed at a shop. Each is a one-time fork, kept for the rest of the run:

     MEND IT    — spend Lessons. It becomes a working limb (+1 Wholeness). Grief drops by one.
     LET IT ROT — miss the clock and it festers into a Scar. Permanent, buffs your attacks, and does
                  NOT clear the grief — a scar keeps counting toward Grief forever (and at double
                  weight; see MendedBody.BrokenCount).

   Grief is a derived readout — the count of parts of you that are not whole — not a resource you can
   gain or spend. At turn start you lose HP equal to it. HP is run-level, so every fork here is priced
   across the whole run. Design writeup: THE_CREATURE/THE_PARTS.md. */
public abstract class PartCard(int cost, CardType type, CardRarity rarity, TargetType targetType)
    : CreatureCard(cost, type, rarity, targetType), IPart
{
    // Lessons needed to mend this organ. Bigger organs cost more.
    protected virtual int LessonsToMend => 2;

    /* Turns held before it rots. ⚠ BALANCE: 3, not 4. Grief only climbs by scarring, so at a 4-turn
       clock the Mourner gained ~1 Grief every fourth turn and Wallow/Keening had nothing to read. */
    protected virtual int TurnsToFester => 3;

    private int _turnsLeft = -1;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Eternal, CardKeyword.Retain };

    // What it becomes if you mend it.
    protected abstract CardModel Mended();

    // What it becomes if you don't. Default is the generic Festering Wound; an organ may override.
    protected virtual CardModel Scarred() => CombatState.CreateCard<FesteringWound>(Owner);

    protected override bool IsPlayable => LessonAmount() >= LessonsToMend;

    /* ⚠ The fester timer lives at TURN START, never turn end. A card with HasTurnEndInHandEffect
       DISCARDS ITSELF afterwards, unconditionally, ignoring Retain — see PrideCard.cs for the autopsy.
       AfterPlayerTurnStart does not move the card. */
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Pile?.Type != PileType.Hand) return;

        if (_turnsLeft < 0) _turnsLeft = TurnsToFester;   // the clock starts when you first hold it
        _turnsLeft--;

        if (_turnsLeft <= 0)
            await CardCmd.Transform(this, Scarred());
    }

    private bool _mending;

    /* THE MEND. Spend the Lessons; Wholeness lands immediately. The CARD transforms at END OF TURN.

       ⚠⚠ NEVER TRANSFORM A CARD THAT IS CURRENTLY BEING PLAYED. Not in OnPlay, not in AfterCardPlayed —
       both leave the Godot node stranded on screen, floating forever, because the model is replaced and
       the node has nothing to follow. This bug has been rediscovered three times in this repo. Transform
       only a card sitting quietly in a pile. So the mend's rewards land now and the transform waits for
       BeforeSideTurnEnd, by which point the card has resolved into the discard pile.

       ⚠ BALANCE: the mend must NOT grant max HP. It used to (+2, permanently, per mend), which was fine
       when an organ was mended once per run — then MendedBody.TheAppetiteReturns started handing out
       parts continuously and it became an unbounded max-HP ratchet (+9.5 HP/fight average, up to +34,
       measured over 300 fights). All Creature sustain lives in MendedBody.AfterCombatVictory now. */
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var lesson in Owner.Creature.Powers.Where(p => p is Lesson).ToList())
            await PowerCmd.ModifyAmount(choiceContext, lesson, -LessonsToMend, Owner.Creature, this);

        await PowerCmd.Apply<Wholeness>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);

        await OnMended(choiceContext);
        _mending = true;   // transform at end of turn, from the discard. Never here — see above.
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !_mending) return;
        _mending = false;
        await CardCmd.Transform(this, Mended());
    }

    // Hook for an organ that does something extra on becoming whole. Most don't.
    protected virtual Task OnMended(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    // Called by Let It Rot — the only way to fester an organ on purpose.
    internal async Task RotNow(PlayerChoiceContext choiceContext)
    {
        await CardCmd.Transform(this, Scarred());
    }

    protected int LessonAmount() =>
        (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
}

/* THE ONE LIST of organs a body can be built from. ⚠ Three callers pull parts (The Charnel House,
   The Appetite, MendedBody) — keep this the single source. A duplicated list means a new organ is
   silently unreachable from two of the three, with nothing failing and nothing warning. */
public static class Parts
{
    public static CardModel Random(Player owner)
    {
        var rng = owner.RunState.Rng.CombatCardGeneration;
        var combat = owner.Creature.CombatState;
        var organs = new List<System.Func<CardModel>>
        {
            () => combat.CreateCard<ThrobbingHeart>(owner),
            () => combat.CreateCard<TheThroat>(owner),
            () => combat.CreateCard<TheLeg>(owner),
            () => combat.CreateCard<TheGut>(owner),
        };
        return rng.NextItem(organs)();
    }

    /* Is any part still MENDABLE? (Gates MendedBody.TheAppetiteReturns.)
       ⚠ Scars deliberately do NOT count — a scar can never be made whole, so if it counted here, a
       player who let everything rot would stop being handed new parts and the character would switch
       itself off. Note the type test is PartCard, not IPart: IScar is an IPart, PartCard is not. */
    public static bool AnyBroken(Player owner) =>
        CardPile.GetCards(owner, PileType.Draw, PileType.Hand, PileType.Discard).Any(c => c is PartCard);
}

// IPart — a piece of you that is not whole. Counted by Grief. A Scar is one of these.
public interface IPart { }

// IScar — a part that rotted. Permanent, buffs attacks, and keeps counting toward Grief (at 2×).
public interface IScar : IPart { }

// IMendedPart — a part made whole. Stops counting toward Grief; counted by Wholeness.
public interface IMendedPart { }
