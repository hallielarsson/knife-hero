using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Corporate Sponsored Pride — cash in a Flag for Energy. Remove a Flag; if you did, gain 2 Energy. */
public sealed class CorporateSponsoredPride() : KnifeHeroCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var flag = Owner.Creature.Powers.FirstOrDefault(p => p is IFlag);
        if (flag != null)
        {
            await PowerCmd.Decrement(flag);
            await PlayerCmd.GainEnergy(2m, Owner);
        }
    }
}
