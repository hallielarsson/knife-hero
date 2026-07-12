using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* TOP CHOP — Hallie's design (Gay Blade 2.0 sheet).

   FLY IT:   at end of turn, gain 4 (+2U) Block.
   SWING IT: deal 6 (+U). Exhaust.

   The plainest possible statement of the Pride mechanic, and deliberately so — this is the card that
   teaches it. Hold it and it defends you, every turn, forever, for free. Swing it and you get a good
   hit and it's gone. The Block it gives you while flown is not a one-off: it is rent, paid to you, for
   as long as you're willing to carry the thing.

   Which means the question is never "is 6 damage worth 4 Block" — it's "how many more turns is this
   fight going to last." Fly it early, swing it late. That's the whole deck in one card. */
public sealed class TopChop() : PrideCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    // "Upgrade ∞" on Hallie's sheet — re-forging it with a Switch Blade raises the retain level, and the
    // retain level is both the passive's size and the size of the payout when you finally swing it.
    public override int MaxUpgradeLevel => 99;

    // THE RETURN STROKE: Top Chop is the TOP. It cashes out into a Strike (+1 more per retain level).
    protected override CardModel? Becomes() => CombatState.CreateCard<GayBladeStrike>(Owner);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new BlockVar(4m, ValueProp.Move) };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }

    // FLOWN — the flag pays rent.
    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }

    // SWUNG — cash out, and get your hand back.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        // No Exhaust: it BECOMES a Strike (see PrideCard.Becomes — fires post-resolution, never here).
    }
}
