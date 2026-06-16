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


    // Damage per Flag. Upgradeable so the rainbow brightens with the deck.
    // PROPOSAL (Claude 2026-06-15): base 2, +1 on upgrade (every flag hits harder). Hallie to tune.
    private int _perFlag = 2;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int flags = Owner.Creature.FlagCount();
        await DamageCmd.Attack(_perFlag * flags).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => _perFlag += 1;
}
