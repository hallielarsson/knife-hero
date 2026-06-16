using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Mended Heart — what a redeemed Throbbing Heart grows into (THE_CREATURE/HEALING.md, the open
   keystone). The part made whole: stable, no Vexing Memory, no festering, no Eternal weight. The
   "solid Strike-equivalent that's yours" HEALING.md asked for — a clean, reliable attack. The body,
   healed in one place.

   PROPOSAL (Claude, Pathetic Governor 2026-06-15): the Wholeness HEALING is deliberately NOT here.
   An earlier draft healed 1-per-Wholeness on play, but Mended Heart is re-playable (DontLookAway can
   prehend it from Salt, etc.), so a free scaling heal stapled to a repeatable attack was a runaway
   healing feedback loop. Fixed/grieved 2026-06-15: the healing moved to the Wholeness power as an
   unpumpable once-per-turn-start trickle. Mended Heart is now just the good, dependable strike.
   Final numbers are Hallie's to mint. */
// Token rarity: only ever created by transforming a mended Throbbing Heart, never offered as a reward.
public sealed class MendedHeart() : CreatureCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
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
