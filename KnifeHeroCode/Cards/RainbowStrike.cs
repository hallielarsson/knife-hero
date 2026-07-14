using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Rainbow Strike — deal 2 damage for every Flag you're flying (sum of your Flag stacks). */
public sealed class RainbowStrike() : KnifeHeroCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "rainbow_strike.png".CardImagePath();
    public override string CustomPortraitPath => "rainbow_strike.png".BigCardImagePath();


    // Damage per Flag — a DynamicVar, so the card text prints {Per} and stays true after upgrade.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Per", 2m) };

    protected override void OnUpgrade() => DynamicVars["Per"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int flags = Owner.Creature.FlagCount();
        await DamageCmd.Attack(DynamicVars["Per"].BaseValue * flags).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}
