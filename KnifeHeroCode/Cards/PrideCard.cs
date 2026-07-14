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

/* IPride — marker so payoffs can count Prides on two deliberately different axes:
     • PLAYED this combat (PridesPlayed power) → Stonewall, Pride Parade. Cumulative; only goes up.
     • IN YOUR HAND (counted live)             → Knife Block. Tactical; swinging a Pride removes it.
   So swinging a Pride feeds one payoff and starves the other. That tension is the flag economy.

   Marker interface, not a CardTag/CardKeyword, because both are closed engine enums a mod cannot
   extend. Cost: invisible to the player unless we print it. */
public interface IPride { }

/* PRIDE — a two-state card:
     HELD (in hand at end of turn) — a passive effect. You're flying the flag; it costs a hand slot.
     SWUNG (played)                — it cashes out and leaves, and you get the hand slot back.
   Hand space is the deck's real currency: a held Pride is a deployed gadget, not an unspent card.

   ─────────────────────────────────────────────────────────────────────────────────────────────────
   ⚠⚠ DO NOT IMPLEMENT A HELD EFFECT WITH `HasTurnEndInHandEffect` / `OnTurnEndInHand`. ⚠⚠

   `CardModel.OnTurnEndInHandWrapper` does this unconditionally, after your effect, and NEVER checks
   Retain:

       if (Keywords.Contains(Ethereal))  Exhaust(this);
       else                              CardPileCmd.Add(this, PileType.Discard);

   The engine treats turn-end-in-hand as a mechanism for cards that LEAVE. So a Retain card with a
   turn-end effect throws itself away every turn, SILENTLY — no error, it just stops being in hand.
   This went undetected through 900 measured fights.

   Use `BeforeSideTurnEnd`: it fires at end of turn and does NOT move the card. Cards in hand are live
   hook listeners; gate on `Pile?.Type == PileType.Hand`.
   ───────────────────────────────────────────────────────────────────────────────────────────────── */
public abstract class PrideCard(int cost, CardType type, CardRarity rarity, TargetType targetType)
    : KnifeHeroCard(cost, type, rarity, targetType), IPride
{
    /* ⚠ Retain is what makes a Pride flyable. A subclass that overrides CanonicalKeywords MUST re-add
       it — dropping it silently makes that Pride the one card in the deck you cannot hold. */
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    // THE HELD CLAUSE. Override this, never OnTurnEndInHand — see the warning above.
    protected virtual Task WhileFlown(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    // Door for BothIsGoodPower, its only caller: fire the held clause on a card that was just played.
    internal Task FlyItAnyway(PlayerChoiceContext choiceContext) => WhileFlown(choiceContext);

    public sealed override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        if (Owner == null || Pile?.Type != PileType.Hand) return;
        await WhileFlown(choiceContext);
    }

    // THE PLAYED CLAUSE — override this, not OnPlay. OnPlay is sealed so no Pride can forget to tick
    // PridesPlayed, which Stonewall and Pride Parade read.
    protected abstract Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await OnSwung(choiceContext, cardPlay);
        await PowerCmd.Apply<PridesPlayed>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, true);
        await CashOut();
    }

    /* THE CASH-OUT — what swinging this puts back in your deck. Override Begets(); null = nothing.
       Extra copies scale with CurrentUpgradeLevel, so a re-forged blade pays out more basics on swing —
       deck bloat is the price of banking one.

       ⚠⚠ WE **ADD** A NEW CARD. WE DO NOT TRANSFORM THE PLAYED ONE. ⚠⚠

       Transforming a card that is currently being played strands its Godot node on screen — it floats,
       frozen, forever. True in OnPlay AND in AfterCardPlayed (that hook fires while the card is still in
       PileType.Play, mid-animation). Worse: OnPlayWrapper's cleanup is scoped to the ORIGINAL card, so
       the replacement is stranded in Play and silently deleted from the deck on every trigger. And there
       is no later hook to escape to — a played card reaches Discard via CardPileCmd.Add, which does not
       fire Hook.AfterCardDiscarded.

       Adding to the Discard pile is always safe. Only ever transform a card sitting quietly in a pile. */
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
