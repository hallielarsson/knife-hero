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
    }

    /* ── THE RETURN STROKE — what this card BECOMES when you swing it ────────────────────────────────
       Override `Becomes()` to declare it. Null (default) = the card just leaves.

       WHY IT LIVES HERE AND NOT ON THE RELIC (Hallie, 2026-07-11): "otherwise if we have other cards like
       this we have to put it ALL in the relic." Exactly — a relic holding every transform becomes a
       god-object switch statement that every new card must be registered in, and it grows forever. So the
       CARD declares its own becoming, and the base class only handles the TIMING. The relic keeps the one
       job that is genuinely its own: turning Strikes and Defends into Switch Blades, because the *basics*
       must not know that the Gay Blade's relic exists.

       ⚠ TIMING IS LOAD-BEARING: this fires in AfterCardPlayed, NEVER in OnPlay. Transforming a card while
       it is still resolving is the "Rapier stuck floating after play" bug — the engine cannot dispose a
       card mid-resolution. See FOOTWORK_SPEC.md, which called this exact shot and left the recipe.

       THE EXTRA COPIES are the entropy pump. Re-forging a held blade raises its CurrentUpgradeLevel (the
       "retain level"), and swinging it spawns one EXTRA basic per level into your discard. So banking a
       blade doesn't just grow its passive — it grows the *payout*, and the payout is deck bloat. That's the
       wash: cash out big and you flood your own deck with basics. Queering is what makes the bloat good. */
    protected virtual CardModel? Becomes() => null;

    protected virtual int ExtraCopiesOnSwing => CurrentUpgradeLevel;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        var becoming = Becomes();
        if (becoming == null) return;

        for (int i = 0; i < ExtraCopiesOnSwing; i++)
        {
            var extra = Becomes();
            if (extra != null)
                await CardPileCmd.AddGeneratedCardToCombat(extra, PileType.Discard, Owner);
        }

        // See CardTransformExtensions.TransformAndSettle: transforming a just-played card (this is
        // always called from AfterCardPlayed, never OnPlay - see the TIMING note above) leaves the
        // replacement stranded in the Play pile unless we move it ourselves.
        await choiceContext.TransformAndSettle(this, becoming);
    }
}
