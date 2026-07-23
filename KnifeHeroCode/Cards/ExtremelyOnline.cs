using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Extremely Online — Hallie's design. A Power, cost 0: gain 2 energy now, gain a Flag that grants
   +2 energy every turn, and shuffle 1 Discourse into your draw pile. Endless plugged-in energy,
   paid for in feed clutter. Because the power is a Flag, you can later cash it down (Corporate
   Sponsored Pride) or let it ride. Human-sourced mechanic (Hallie); placeholder art via KnifeHeroCard. */
public sealed class ExtremelyOnline() : KnifeHeroCard(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    /* Already free, so the upgrade buys you OUT of the downside instead of out of the cost: an upgraded
       Extremely Online doesn't shuffle a Discourse into your draw pile. You get to be online without the
       discourse, which is the only real upgrade there is. */
    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ExtremelyOnlinePower>(choiceContext, Owner.Creature, 2m, Owner.Creature, this, false); // +2 energy/turn (a Flag)
        await PlayerCmd.GainEnergy(2m, Owner);                                                        // and 2 right now

        if (IsUpgraded) return;

        var clutter = CombatState.CreateCard<TheDiscourse>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(clutter, PileType.Draw, null, CardPilePosition.Random);
    }
}
