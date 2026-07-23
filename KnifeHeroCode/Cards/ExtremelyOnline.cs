using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Extremely Online — Hallie's design. A Power, cost 0: gain 2 energy now, +2 energy every turn, and
   add 1 Dazed to your draw pile. Endless plugged-in energy, paid for in feed clutter. Upgrade drops
   the Dazed. */
public sealed class ExtremelyOnline() : KnifeHeroCard(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    /* Already free, so the upgrade buys you OUT of the downside instead of the cost: an upgraded
       Extremely Online doesn't add the Dazed. Online without the clutter — the only real upgrade there is. */
    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ExtremelyOnlinePower>(choiceContext, Owner.Creature, 2m, Owner.Creature, this, false); // +2 energy/turn
        await PlayerCmd.GainEnergy(2m, Owner);                                                        // and 2 right now

        if (IsUpgraded) return;

        var clutter = CombatState.CreateCard<Dazed>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(clutter, PileType.Draw, null, CardPilePosition.Random);
    }
}
