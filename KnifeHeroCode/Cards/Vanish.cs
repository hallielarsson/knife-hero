using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Vanish — a simple Flag-granter (gain Stealth) so the Flag system is testable.
   Placeholder; flags will come from many cards later. */
public sealed class Vanish() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // UPGRADE: 3 Stealth instead of 2.
    public override int MaxUpgradeLevel => 1;
    private decimal StealthGain => IsUpgraded ? 3m : 2m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Stealth>(choiceContext, Owner.Creature, StealthGain, Owner.Creature, this, false);
    }
}
