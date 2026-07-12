using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;


//We need to revisit these. 
//Each one of these should be a Retained Blade instead of a power, even if it's a power that PRODUCES the blane. 
//
//Silent -- on retain, gain 3 block when you discard a card and Inflicts Weak what it hits

//Flags for cards then, instead, count Pride cards in hand or deck. 
//
//IronClad pride inflicts (level) vulnerable. On retain, deals 3 damage when you exhaust a card.
//
//
/* Pride flags themed after the StS characters — each a passive Flag (IFlag, so it counts for
   Stonewall / Rainbow Strike). Silent = shivs, Ironclad = heal. */


//Regent works as is as long as it applies to Pride Blades instead of Pets
//
//Watcher Pride -- draw two cards and discard one at the beginning of your turn.
//
//
/* Silent Pride — the Silent's shiv engine: at the start of each turn, put a Shiv in your discard. */
public sealed class SilentPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        for (int i = 0; i < (int)Amount; i++)
        {
            var shiv = Owner.CombatState.CreateCard<Shiv>(player);
            await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Discard, null);
        }
    }
}

/* Ironclad Pride — Burning Blood as a pride flag: heal 5 at the end of combat if it's out. */
public sealed class IroncladPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        await CreatureCmd.Heal(Owner, 5m * Amount, false);
    }
}



/* Regent Pride — the ruler that feeds on its own court: it costs another Pride (a pet sacrificed on
   play), and in return, each turn, deal 6 damage to an enemy and gain 6 Block. */
public sealed class RegentPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        var enemy = Owner.CombatState.HittableEnemies.FirstOrDefault();
        if (enemy != null)
            await CreatureCmd.Damage(choiceContext, enemy, 6m * Amount, ValueProp.Unpowered, Owner, null);
        await CreatureCmd.GainBlock(Owner, new BlockVar(6m * Amount, ValueProp.Move), null);
    }
}

/* Watcher Pride — the Watcher's retention: at the start of each turn, add Retain to a random card in
   your hand (it won't be discarded at end of turn). */
public sealed class WatcherPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return Task.CompletedTask;
        var hand = CardPile.GetCards(player, PileType.Hand).ToList();
        var rng = player.RunState.Rng.CombatCardGeneration;
        for (int i = 0; i < (int)Amount && hand.Count > 0; i++)
        {
            var card = rng.NextItem(hand);
            CardCmd.ApplyKeyword(card, CardKeyword.Retain);
            hand.Remove(card);
        }
        return Task.CompletedTask;
    }
}

/* Defect Pride — the Defect's engine: your Power cards cost 1 less, and you draw a card whenever you
   play a Power. */
public sealed class DefectPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner?.Creature != Owner || card.Type != CardType.Power) return false;
        modifiedCost = originalCost - Amount;
        if (modifiedCost < 0m) modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type != CardType.Power) return;
        await CardPileCmd.Draw(context, Amount, Owner.Player);
    }
}
