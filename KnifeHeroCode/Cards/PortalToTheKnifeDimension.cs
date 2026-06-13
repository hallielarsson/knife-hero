using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Portal to the Knife Dimension — Hallie's design. A Power, cost 3: every turn it puts a copy of a
   random Blade from your deck into your hand, with Exhaust + Ethereal (free, but use it this turn).
   The big knife-engine payoff. Human-sourced mechanic (Hallie); placeholder art via KnifeHeroCard. */
public sealed class PortalToTheKnifeDimension() : KnifeHeroCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PortalToTheKnifeDimensionPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
