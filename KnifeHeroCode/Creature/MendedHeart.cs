using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Mended Heart — what a redeemed Throbbing Heart grows into (THE_CREATURE/HEALING.md, the open
   keystone). The part made whole: stable, no Vexing Memory, no festering, no Eternal weight. A good,
   reliable card that is yours — a cheap attack that also nudges your healing. The body, healed in one
   place.

   PROPOSAL (Claude, Pathetic Governor 2026-06-15): HEALING.md promised "a Mended Heart (a boon, not a
   curse) ... a solid Strike-equivalent that's yours, that also nudges Wholeness" but no code existed.
   This implements the felt core: deal 6, then heal 1 per Wholeness you've earned (healing begets
   healing — the amplification HEALING.md names). Final numbers are Hallie's to mint. */
// Token rarity: only ever created by transforming a mended Throbbing Heart, never offered as a reward.
public sealed class MendedHeart() : CreatureCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        // Healing amplified by Wholeness — the slow road compounds (HEALING.md). Heal 1 per Wholeness.
        int whole = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Wholeness)?.Amount ?? 0m);
        if (whole > 0)
            await CreatureCmd.Heal(Owner.Creature, whole, false);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
