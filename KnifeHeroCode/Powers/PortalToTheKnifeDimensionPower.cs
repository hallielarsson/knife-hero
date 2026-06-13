using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Cards;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Random;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Portal to the Knife Dimension (the power) — at the start of each turn, reach into your deck,
   pull a copy of a random Blade (anything IBlade), and drop it in your hand with Exhaust + Ethereal
   so it's a use-it-this-turn-or-lose-it free knife. A persistent free-blade engine. Looks at the
   draw AND discard piles ("your deck" in combat). Counter-stacks: each stack = one extra blade/turn. */
public sealed class PortalToTheKnifeDimensionPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var blades = CardPile.GetCards(player, PileType.Draw, PileType.Discard)
            .Where(c => c is IBlade)
            .ToList();
        if (blades.Count == 0) return;

        var rng = player.RunState.Rng.CombatCardGeneration;
        for (int i = 0; i < Amount; i++)
        {
            var clone = rng.NextItem(blades).CreateClone();
            CardCmd.ApplyKeyword(clone, CardKeyword.Exhaust, CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, addedByPlayer: true);
        }
    }
}
