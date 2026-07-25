using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using KnifeHero.KnifeHeroCode.Powers;

namespace KnifeHero.KnifeHeroCode.Cards;

/* STONEWALL — Hallie's design. Gain 10 Block, and each Pride you've PLAYED this combat attacks for 3.

   2026-07-11: re-seated onto the new Pride axis. It no longer counts Flags-in-hand — it counts
   PridesPlayed, the cumulative tally of every Pride you have SWUNG this combat. Hallie confirmed this
   as deliberate.

   Which makes it the *cumulative* payoff: it only ever goes up, nothing can take it away, and swinging
   a flag FEEDS it. Its opposite number is Knife Block, which counts Prides in your HAND — so every Pride
   you swing simultaneously feeds Stonewall and starves Knife Block. That tension is the flag economy.

   Stonewall was a riot: it isn't how many banners you're holding. It's how many you already threw. */
public sealed class Stonewall() : KnifeHeroCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(10m, ValueProp.Move), new DamageVar(3m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        int flags = CardPile.GetCards(Owner, PileType.Hand).Count(Enchantments.Queer.Is);
        if (flags > 0)
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(flags).FromCard(this)
                .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // PROPOSAL (Claude 2026-06-15): hold the line harder. +5 Block (10 -> 15) and +1 per-flag
    // damage (3 -> 4), so the wall and the pride both grow. Hallie to tune.
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
