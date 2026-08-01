using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Vanish — a simple Flag-granter (gain Stealth) so the Flag system is testable.
   Placeholder; flags will come from many cards later. */
public sealed class Vanish() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "vanish.png".CardImagePath();
    public override string CustomPortraitPath => "vanish.png".BigCardImagePath();

    public override int MaxUpgradeLevel => 1;

    // {Stealth} as a DynamicVar, so the text prints the real number after upgrade instead of a frozen 2.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Stealth", 2m) };

    protected override void OnUpgrade() => DynamicVars["Stealth"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Stealth>(choiceContext, Owner.Creature,
            DynamicVars["Stealth"].BaseValue, Owner.Creature, this, false);
    }
}
