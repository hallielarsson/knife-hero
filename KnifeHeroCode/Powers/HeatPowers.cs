using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace KnifeHero.KnifeHeroCode.Powers;

/* UNSEEN — from Day of Invisibility. This turn, your Attacks don't break Stealth.
   Read by Stealth.AfterCardPlayed, which is the only place that check lives. */
public sealed class Unseen : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        System.Collections.Generic.IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player) await PowerCmd.Remove(this);
    }
}

/* PICKPOCKET — the first time you deal damage each turn, gain `Amount` Shivs. */
public sealed class PickpocketPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _lifted;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, MegaCrit.Sts2.Core.ValueProps.ValueProp props, Creature target,
        MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (_lifted || dealer != Owner || result.TotalDamage <= 0m) return;
        _lifted = true;
        await Shiv.CreateInHand(Owner.Player, (int)Amount, Owner.CombatState);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner.Player) _lifted = false;
        return Task.CompletedTask;
    }
}

/* DEAD NAME — whenever you would gain Heat, take a Dazed instead.

   They don't get to know what to call you. So they never learn where you are — Heat never rises, Stealth
   never degrades, and you can hide for the whole fight. The price is that your deck fills up with
   something that isn't you: clutter you have to carry instead of being found.

   Read by Stealth.AfterDamageReceived, which is the only place Heat is ever granted. */
public sealed class DeadNamePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // Called by Stealth in place of gaining Heat.
    public async Task RefuseTheName()
    {
        var dazed = Owner.CombatState.CreateCard<Dazed>(Owner.Player);
        await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Discard, Owner.Player);
    }
}

/* REGENT PRIDE — the ongoing. Each turn, deal 6 and gain 6 Block.
   Bought with the life of another Pride blade. */
public sealed class RegentPridePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player) return;
        var enemy = Owner.CombatState.HittableEnemies.FirstOrDefault();
        if (enemy != null)
            await CreatureCmd.Damage(choiceContext, enemy, 6m * Amount,
                MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, Owner, null);
        await CreatureCmd.GainBlock(Owner,
            new MegaCrit.Sts2.Core.Localization.DynamicVars.BlockVar(6m * Amount,
                MegaCrit.Sts2.Core.ValueProps.ValueProp.Move), null);
    }
}

/* DOUBLE SHOT — from Finger Guns (Bisexual Pride). The next Attack you play is played twice.
   Consumed by the attack that uses it. */
public sealed class DoubleShot : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(MegaCrit.Sts2.Core.Models.CardModel card,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? target, int playCount)
    {
        if (card.Owner != Owner.Player || card.Type != CardType.Attack) return playCount;
        return playCount + 1;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type != CardType.Attack) return;
        await PowerCmd.Decrement(this);   // spent on the attack it doubled
    }
}

/* BOTH IS GOOD — Hallie, 2026-07-12.
   The first Pride you play each turn also fires its held effect.

   The entire Pride mechanic is a fork: **fly it, or swing it. Not both.** Hold the flag and it pays you
   a little every turn; swing it and it pays you a lot, once, and it's gone. Every Pride in the deck is
   built around making you choose.

   This is the card that says: both is good.

   And it isn't just a value bump, it's a *reframe* — with this out, a Pride is no longer a decision, it's
   a package. Silent Pride swung gives you the shiv AND the block. Finger Guns swung doubles your next
   attack AND fires twice on its way out. Watcher swung draws 3 AND draws 2/discards 1. The cards stop
   pulling against themselves.

   Once per turn, deliberately — it's the difference between "you get to have both" and "the whole
   mechanic was a lie." */
public sealed class BothIsGoodPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _usedThisTurn;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (_usedThisTurn) return;
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card is not KnifeHero.KnifeHeroCode.Cards.PrideCard pride) return;

        _usedThisTurn = true;
        await pride.FlyItAnyway(context);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner.Player) _usedThisTurn = false;
        return Task.CompletedTask;
    }
}
