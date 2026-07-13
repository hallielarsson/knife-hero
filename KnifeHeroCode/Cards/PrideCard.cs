using System.Collections.Generic;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Extensions;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.Cards;

/* IPride — the marker. A Pride is a card you can FLY or SWING.

   This exists so the payoffs can count Prides, and they count them on TWO DIFFERENT AXES, deliberately
   (Hallie, confirmed):
     • PLAYED this combat  → Stonewall, Pride Parade.  Cumulative, stable, the wash can't threaten it.
     • IN YOUR HAND        → Knife Block.              Tactical, and the wash DOES threaten it.

   Why a marker interface and not a CardTag/CardKeyword: both of those are closed enums baked into the
   game and a mod cannot add a value. Same reason IBlade and IFlag are interfaces. Cost: invisible to the
   player unless we print it. To make any card a Pride, extend PrideCard below. */
public interface IPride { }

/* PRIDE — the mechanic, not just a tag. (Hallie, 2026-07-11: "most of the things that have held vs play
   are Prides — that's sort of the Pride mechanic.")

   A Pride is a two-state object:
     HELD  — it sits in your hand and does a passive thing. You are flying the flag.
     PLAYED — it cashes out and leaves. You are swinging it.
   You cannot do both with the same card at the same time, and **swinging it gives you your hand back.**
   That's the character's central decision, and it is why hand space is the deck's real currency.

   ─────────────────────────────────────────────────────────────────────────────────────────────────
   ⚠⚠ DO NOT IMPLEMENT THE HELD EFFECT WITH `HasTurnEndInHandEffect` / `OnTurnEndInHand`. ⚠⚠

   `CardModel.OnTurnEndInHandWrapper` (see .decompiled/) does this, unconditionally, after your effect:

       if (Keywords.Contains(Ethereal))  Exhaust(this);
       else                              CardPileCmd.Add(this, PileType.Discard);

   It NEVER checks Retain. The engine treats turn-end-in-hand as a mechanism for cards that LEAVE (The
   Discourse, Vexing Memory, Festering Wound). So a Retain card whose held-effect fires at turn end
   **throws itself away every single turn**, and it does so SILENTLY — no error, the card just quietly
   stops being in your hand. This cost us the Creature's entire central loop today; it went undetected
   through 900 measured fights and was only caught by actually playing one and watching the card vanish.

   The correct hook is `BeforeSideTurnEnd`, which fires at end of turn and does NOT move the card. Cards
   in hand are live hook listeners — Butch Blade's "while in hand, your attacks deal +1" already relies
   on exactly this (it overrides ModifyDamageAdditive and gates on `Pile?.Type == PileType.Hand`).
   ─────────────────────────────────────────────────────────────────────────────────────────────────

   SENTINELS NOTE (the design's origin — Lori's ask): a Sentinels deck crossed between Unity and Wraith
   — "things you have out and in play doing things", plus gadgets and stealth. Sentinels has a PLAY AREA.
   Slay the Spire does not. So **the retained hand IS the play area**, and a held Pride is not a card you
   haven't spent — it is a gadget you have DEPLOYED. Hand size is how many things you can have out. */
public abstract class PrideCard(int cost, CardType type, CardRarity rarity, TargetType targetType)
    : KnifeHeroCard(cost, type, rarity, targetType), IPride
{
    // A Pride you're flying stays flown. Subclasses may add more keywords by overriding and calling base.
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    /* THE HELD CLAUSE — "if this is in your hand at end of turn…"
       Fires only while the card is in your hand, and does not discard it. Override this instead of
       OnTurnEndInHand. See the warning above; it is not stylistic. */
    protected virtual Task WhileFlown(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    /* BOTH IS GOOD (the power) reaches in here. The whole Pride mechanic is "fly it OR swing it, never
       both" — so the one card that lets you do both has to be able to fire the held clause on demand,
       from outside, on a card that has just been played. This is that door, and it exists for exactly
       one caller. */
    internal Task FlyItAnyway(PlayerChoiceContext choiceContext) => WhileFlown(choiceContext);

    public sealed override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        if (Owner == null || Pile?.Type != PileType.Hand) return;
        await WhileFlown(choiceContext);
    }

    /* THE PLAYED CLAUSE — override this, not OnPlay.
       OnPlay is sealed here so that EVERY Pride counts itself when swung. If a Pride had to remember to
       tick the counter itself, one of them would eventually forget, and Stonewall would silently
       under-count forever. The base class cannot forget. */
    protected abstract Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await OnSwung(choiceContext, cardPlay);
        // Your pride, put into the world. It only ever goes up. (Stonewall / Pride Parade read this.)
        await PowerCmd.Apply<PridesPlayed>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, true);
        await CashOut();   // put the basic(s) back in the deck. ADD, never transform. See below.
    }

    /* ── THE CASH-OUT — what swinging this puts back into your deck ─────────────────────────────────
       Override `Begets()` to declare it. Null (default) = nothing.

       ⚠⚠ WE DO NOT TRANSFORM THE PLAYED CARD. WE **ADD** A NEW ONE. ⚠⚠

       This looks like a small distinction and it is the whole ballgame. Adding a card to the Discard
       pile is always safe. TRANSFORMING a card that is currently being played is not, and it broke this
       project twice in one evening:
         1. Transform in `OnPlay`          → "Rapier stuck floating after play" (FOOTWORK_SPEC.md).
         2. Transform in `AfterCardPlayed` → STILL FLOATS. A glowing card hung frozen in the middle of
            Hallie's screen mid-playtest. That hook fires while the card is still in PileType.Play and
            still mid-animation.
         3. And `OnPlayWrapper`'s cleanup is scoped to the ORIGINAL card, so the replacement is stranded
            in Play and **silently deleted from the deck** on every single trigger.
       There is no later hook to escape to: a played card reaches Discard via `CardPileCmd.Add`, which
       does not fire `Hook.AfterCardDiscarded`.

       So the played card just resolves and leaves, exactly like every other card in the game, and we
       put a NEW basic in the discard pile beside it. Same outcome for the player. Zero engine risk.
       (The relic does its transform on a card sitting quietly in your HAND at turn start — see
       TheWash.cs. That is the only safe place to transform anything.)

       THE EXTRA COPIES are the entropy pump: re-forging a held blade raises its CurrentUpgradeLevel
       (the "retain level"), and swinging it begets one EXTRA basic per level. Banking a blade grows its
       passive AND grows the payout — and the payout is deck bloat. That's the wash. Cash out big and you
       flood your own deck with basics. Queering is what makes the bloat good. */
    protected virtual CardModel? Begets() => null;

    protected virtual int ExtraCopiesOnSwing => CurrentUpgradeLevel;

    private async Task CashOut()
    {
        for (int i = 0; i <= ExtraCopiesOnSwing; i++)
        {
            var basic = Begets();
            if (basic != null)
                await CardPileCmd.AddGeneratedCardToCombat(basic, PileType.Discard, Owner);
        }
    }
}
