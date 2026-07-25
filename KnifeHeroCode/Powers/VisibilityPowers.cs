using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

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

/* DEAD NAME — whenever you would gain Visibility, take a Dazed instead.

   They don't get to know what to call you. So they never learn where you are — Visibility never rises, Stealth
   never degrades, and you can hide for the whole fight. The price is that your deck fills up with
   something that isn't you: clutter you have to carry instead of being found.

   Read by Stealth.AfterDamageReceived, which is the only place Visibility is ever granted. */
public sealed class DeadNamePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // Called by Stealth in place of gaining Visibility.
    public async Task RefuseTheName()
    {
        var dazed = Owner.CombatState.CreateCard<Dazed>(Owner.Player);
        await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Discard, Owner.Player);
    }
}

/* THE CLOSET — gain `Amount` Stealth at the start of each turn. The passive hide-engine. */
public sealed class TheClosetPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player) return;
        await PowerCmd.Apply<Stealth>(choiceContext, Owner, Amount, Owner, null, false);
    }
}

/* BISEXUAL LIGHTNING — at the start of each turn, deal `Amount` to each of 2 random enemies (always 2). */
public sealed class BisexualLightningPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner.Player) return;
        var enemies = Owner.CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0) return;
        var rng = Owner.Player.RunState.Rng.CombatCardGeneration;
        for (int i = 0; i < 2; i++)
            await CreatureCmd.Damage(choiceContext, rng.NextItem(enemies), Amount,
                ValueProp.Unpowered, Owner, null);
    }
}

/* ASSASSIN — your Attacks deal `Amount` bonus damage per point of Stealth (base 2). Same additive hook
   Vigor uses; reads live Stealth each swing. */
public sealed class AssassinPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (Owner != dealer || !props.IsPoweredAttack()) return 0m;
        int stealth = (int)(Owner.GetPower<Stealth>()?.Amount ?? 0m);
        return Amount * stealth;
    }
}
