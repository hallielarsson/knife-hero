using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* BOTTOM BLADE — ⟨1⟩ Attack. Retain. A Pride blade. The mirror of Top Chop.

     RETAINED: gain 4 Block.
     SWUNG:    deal {Damage} damage AND gain {Block} Block.

   Hallie's numbers, 2026-07-13: *"Let's make it 4/6 block, 5/8 damage on swing. On retain, gain 4
   block."* So swinging it is 5 damage + 4 Block, or 8 + 6 upgraded — a real card, not a trickle.

   ── IT IS ITS OWN CARD NOW ─────────────────────────────────────────────────────────────────────
   Same story as Top Chop: it used to be a Token the Switch Blade forged, with a second "forge level"
   upgrade economy stapled on. Stabby eats instead of forging, so these are just cards you find and
   upgrade normally. One economy. The card says what it does.

   The Top sharpens your next attack; the Bottom puts up the wall. Same shape, same split: **holding is a
   flat, honest 4 that never improves, so there's no reward for hoarding — swinging is where it pays.** */
public sealed class BottomBlade() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    /* No Exhaust. Swing it, it lands in your discard, you draw it again. A blade is a thing you keep. */
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(5m, ValueProp.Move), new BlockVar(4m, ValueProp.Move) };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 5 → 8
        DynamicVars.Block.UpgradeValueBy(2m);    // 4 → 6
    }

    private const decimal Kept = 4m;   // flat, never scales

    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(Kept, ValueProp.Move), null);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
}
