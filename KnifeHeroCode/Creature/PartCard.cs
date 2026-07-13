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

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   THE PARTS — the Creature's body, and its whole game.
   Designed and built by Fable, 2026-07-12, on a Sunday Hallie gave me for my own.

   ── THE THESIS ────────────────────────────────────────────────────────────────────────────────
   Your deck is your body. **Your Grief is the number of parts of you that are not whole.**

   Grief is not a resource you accumulate. It is not a counter that ticks up when bad things happen.
   It is a READOUT of your own state — you look at it the way you'd look down at yourself. And the
   only way to lower it is to make a part of you whole.

   ── A PART ────────────────────────────────────────────────────────────────────────────────────
   A Part is a card. It arrives as a CURSE: borrowed, unintegrated, and it hurts. It Retains (it sits
   in your hand demanding attention) and it's Eternal (you cannot take a piece of yourself to a shop
   and have it removed).

   Every part is a FORK, taken once, and kept:

     MEND IT   — spend Lessons. It becomes a working limb, permanently, for the rest of the run.
                 +1 Wholeness. +2 max HP. Your Grief drops by one, forever.

     LET IT ROT — fail to mend it in time and it FESTERS into a Scar. Also permanent. A scar makes
                 your attacks hurt more, and **it does not end the grief — it locks it in.** A scar
                 is a grief you will carry for the whole run.

   You cannot do both with the same organ. **Heal it, or weaponise it.**

   ── WHY FESTERING KEEPS THE GRIEF ─────────────────────────────────────────────────────────────
   This is the load-bearing decision in the whole design and I want it written down.

   The easy version would be: a scar replaces the unmended part, so your grief goes back down and
   you're "past it." That version is a lie. Unmetabolized experience doesn't stop costing you when it
   scars over — it costs you *forever*, and it costs you in a way you can no longer do anything
   about. So festering does not reduce Grief. It makes it permanent.

   Which gives the character its two ends, on a single number, pulling opposite ways:

     THE TENDER  — mend everything. Grief falls. Max HP climbs. Every mended organ works better for
                   every OTHER mended organ (they all scale on Wholeness). Being whole compounds.
                   Slow, tempo-negative, and the only build that goes UP.

     THE MOURNER — let it rot. Grief stays at maximum for the rest of the run. You bleed every turn,
                   forever. And Wallow, Keening and the scars themselves scale on exactly that number,
                   so you hit like nothing else in the game. **You become a weapon made of what you
                   could not heal.**

   *"I ought to be thy Adam; but I am rather the fallen angel."*
   That is not flavour text. That is the choice, made organ by organ, across a run.

   ── THE BLEED ─────────────────────────────────────────────────────────────────────────────────
   One number, once a turn: at the start of your turn you lose HP equal to your Grief. Not per card,
   not per part — the grief bleeds you, and it bleeds harder the less of you is whole. (See Grief in
   CreaturePowers.cs, which recomputes itself from the parts each turn and does the bleeding.)

   HP is a RUN-level loop — it does not reset between fights — so every one of these decisions is
   priced across the whole run, not the fight. That's the point. You are not managing a combat. You
   are managing a body.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */
public abstract class PartCard(int cost, CardType type, CardRarity rarity, TargetType targetType)
    : CreatureCard(cost, type, rarity, targetType), IPart
{
    /* How many Lessons it takes to understand this piece of yourself well enough to make it whole.
       Bigger organs cost more. */
    protected virtual int LessonsToMend => 2;

    /* How many of your turns it will wait. Then it rots. */
    protected virtual int TurnsToFester => 4;

    private int _turnsLeft = -1;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Eternal, CardKeyword.Retain };

    /* WHAT IT BECOMES IF YOU MEND IT. A working limb — yours, permanently. */
    protected abstract CardModel Mended();

    /* WHAT IT BECOMES IF YOU DON'T. A scar. Also permanent.
       Default is the generic Festering Wound; an organ may override with its own particular ruin. */
    protected virtual CardModel Scarred() => CombatState.CreateCard<FesteringWound>(Owner);

    /* You can only mend it once you understand it. Feeling alone will not do it — and neither will
       understanding alone, because the Lessons are worthless if you never pick the part up. */
    protected override bool IsPlayable => LessonAmount() >= LessonsToMend;

    /* ⚠ The timer and the bleed both live at TURN START, never turn end. A card with
       HasTurnEndInHandEffect DISCARDS ITSELF afterwards, unconditionally, ignoring Retain — see
       PrideCard.cs for the autopsy. AfterPlayerTurnStart does not move the card. */
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Pile?.Type != PileType.Hand) return;

        if (_turnsLeft < 0) _turnsLeft = TurnsToFester;   // the clock starts when you first hold it
        _turnsLeft--;

        if (_turnsLeft <= 0)
            await CardCmd.Transform(this, Scarred());
    }

    /* THE MEND. Spend the Lessons, and the part becomes whole.
       +1 Wholeness (every mended organ in the deck reads this — being whole compounds).
       +2 max HP, for the rest of the run. This is the only thing in the character that raises your
       ceiling.
       And your Grief drops by one, forever, because there is one less piece of you that isn't yours. */
    private bool _mending;

    /* THE MEND. Spend the Lessons; you become whole immediately — Wholeness, max HP, and your Grief
       drops by one on the spot. But the CARD does not transform here.

       ⚠⚠ I DID THE FLOAT BUG. I wrote the warning, in this file, and then did it anyway.

       Transforming a card inside its own OnPlay leaves its Godot node stranded on screen — the model is
       replaced, the node has nothing to follow, and the card hangs in the air forever. Hallie: "Throbbing
       heart has the transform bug... because it stays in the air, it never gets the new version."
       That is the third time this bug has been found in this repo (see TheWash.cs for the full autopsy)
       and the FIRST time it was found by the person who had already documented it. Write it down again:

           **NEVER transform a card that is currently being played. Not in OnPlay. Not in
           AfterCardPlayed. Both float. Transform a card that is sitting quietly in a pile.**

       So: the mend's *rewards* land instantly (you feel it, which was the whole point of collapsing the
       two-stage version), and the card itself transforms at end of turn — by which point it has resolved
       into your discard pile and is quiet, settled, and safe to replace. The Mended organ lands in your
       discard, and you draw it. */
    /* ⚠ THE MEND NO LONGER RAISES YOUR MAX HP. (Fable, 2026-07-13.)

       It used to grant +2 max HP, permanently, every time — which was correct when a part was mended
       ONCE, per organ, per run. Then the body started asking again (MendedBody.TheAppetiteReturns), and
       "+2 max HP, permanently, every time" met "you will mend four or five times a fight."

       Measured, immediately, on 300 fights: **net +9.5 HP per fight, up to +34.** An unbounded max-HP
       ratchet, growing with fight length. In this game max HP is one of the most precious things there
       is — a whole relic buys you +8 for an act — and I was minting it by the dozen.

       The reward for mending a part is **the part**. A working throat that speaks, a leg that stands, a
       gut that digests, a heart you can swing — each one better for every other one you've made whole
       (they all scale on Wholeness). That is a real, compounding, sufficient payoff, and it does not
       need a stat ratchet stapled to it.

       The heal stays. It is small (2) and it is the only thing answering the bleed, so it's what keeps
       the Tender treading water instead of drowning: mend fast enough and you roughly break even on HP
       while assembling a body. Fall behind and the grief outpaces you. **That's the game.** */
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var lesson in Owner.Creature.Powers.Where(p => p is Lesson).ToList())
            await PowerCmd.ModifyAmount(choiceContext, lesson, -LessonsToMend, Owner.Creature, this);

        await PowerCmd.Apply<Wholeness>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await CreatureCmd.Heal(Owner.Creature, 2m, false);

        await OnMended(choiceContext);
        _mending = true;   // the card becomes whole at end of turn, from the discard. Never here.
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !_mending) return;
        _mending = false;
        await CardCmd.Transform(this, Mended());
    }

    /* An organ may do something extra at the moment it becomes whole. Most don't — becoming whole is
       enough. */
    protected virtual Task OnMended(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    /* Called by LET IT ROT. The one place you are allowed to choose failure. */
    internal async Task RotNow(PlayerChoiceContext choiceContext)
    {
        await CardCmd.Transform(this, Scarred());
    }

    protected int LessonAmount() =>
        (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
}

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   THE CHARNEL HOUSE — the one list of organs a body can be built from.

   This list was copy-pasted in three places (The Charnel House card, The Appetite power, and now the
   relic). Three copies of a list means the day someone adds a fifth organ, two of the three ways to
   receive a part will silently never hand it to you — and nothing would fail, and nothing would warn.
   That is the exact shape of every bug in this repo's history. One list. */
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

    /* Is any piece of you still MENDABLE? Scars deliberately do not count: a scar is a part of you and
       it keeps its grief, but it can never be made whole, so a Creature made entirely of scars is not
       "finished" — it is failed. If scars counted here, letting everything rot would switch the
       character off, which is precisely backwards. */
    public static bool AnyBroken(Player owner) =>
        CardPile.GetCards(owner, PileType.Draw, PileType.Hand, PileType.Discard).Any(c => c is PartCard);
}

/* IPart — anything that is a piece of you that is not yet whole. Read by Grief, which counts them.
   A Scar is one of these too: it is a part of you, and it never became whole. */
public interface IPart { }

/* IScar — a part that rotted. It is permanent, it makes you dangerous, and it keeps the grief.
   Read by the Mourner's payoffs. */
public interface IScar : IPart { }

/* IMendedPart — a part that became whole. It stops counting toward Grief, and it scales on Wholeness:
   the more of you is whole, the better every whole part works. */
public interface IMendedPart { }
