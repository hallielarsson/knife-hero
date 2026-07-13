using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* BOTTOM BLADE — forged by HOLDING a Switch Blade to end of turn. The mirror of Top Chop.

     ON FORGE:  +4 Block immediately (applied at the forge site in FancyFootwork).
     KEPT:      +2 Block at the end of every turn you hold it. **Flat — never scales.**
     SWUNG:     Deal damage AND gain 2 Block per forge level. Exhaust.

   The Top sharpens your next attack; the Bottom puts up the wall. Same shape, same split: holding is a
   flat trickle that never improves, swinging is where the forging pays. Carry it until it's heavy, then
   swing it — and when it exhausts, the relic turns a spent Defend in your discard back into a Switch
   Blade. */
public sealed class BottomBlade() : PrideCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    public override int MaxUpgradeLevel => 99;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain, CardKeyword.Exhaust };

    public const decimal OnForge = 4m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new BlockVar(2m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    private decimal Swung => 2m * (CurrentUpgradeLevel + 1);

    // KEPT — the flat Block trickle. Never scales.
    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(Swung, ValueProp.Move), cardPlay);
    }
}
