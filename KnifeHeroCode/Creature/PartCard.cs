using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var lesson in Owner.Creature.Powers.Where(p => p is Lesson).ToList())
            await PowerCmd.ModifyAmount(choiceContext, lesson, -LessonsToMend, Owner.Creature, this);

        await PowerCmd.Apply<Wholeness>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await CreatureCmd.SetMaxHp(Owner.Creature, Owner.Creature.MaxHp + 2);
        await CreatureCmd.Heal(Owner.Creature, 2m, false);

        await OnMended(choiceContext);
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

/* IPart — anything that is a piece of you that is not yet whole. Read by Grief, which counts them.
   A Scar is one of these too: it is a part of you, and it never became whole. */
public interface IPart { }

/* IScar — a part that rotted. It is permanent, it makes you dangerous, and it keeps the grief.
   Read by the Mourner's payoffs. */
public interface IScar : IPart { }

/* IMendedPart — a part that became whole. It stops counting toward Grief, and it scales on Wholeness:
   the more of you is whole, the better every whole part works. */
public interface IMendedPart { }
