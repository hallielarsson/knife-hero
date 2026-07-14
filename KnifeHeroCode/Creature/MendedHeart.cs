using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Extensions;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Mended Heart — a mended Throbbing Heart. A clean 8-damage attack. Token rarity: only ever created
   by the mend, never offered as a reward.

   ⚠ It deliberately does NOT heal. It is replayable (Don't Look Away can pull it back from Exhaust), so
   a heal stapled here is a runaway loop. All Creature sustain lives in MendedBody.AfterCombatVictory. */
public sealed class MendedHeart() : CreatureCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy), IMendedPart
{
    public override string PortraitPath => "mended_heart.png".CardImagePath();
    public override string CustomPortraitPath => "mended_heart.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
