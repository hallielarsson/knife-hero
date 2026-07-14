using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* TOP CHOP — ⟨1⟩ Attack. Retain. A Pride blade. The mirror of Bottom Blade.

     RETAINED: gain 4 Vigor.
     SWUNG:    deal {Damage} damage, THEN gain {Vigor} Vigor.

   ── IT IS ITS OWN CARD NOW ─────────────────────────────────────────────────────────────────────
   (Hallie, 2026-07-13: *"Top chop and bottom blade become their own cards."*)

   It used to be a Token forged by the Switch Blade, with a "forge level" that the Switch Blade pumped —
   a whole second upgrade economy bolted onto a card that already had one. Stabby doesn't forge anything
   anymore; it eats. So these are just cards: you find them, you take them, you upgrade them like
   everything else. **One upgrade economy. The card says what it does.**

   ⚠ THE ORDER MATTERS AND IT IS DELIBERATE: the damage lands FIRST, then the Vigor. Base-game Vigor only
   buffs the **next** attack and is consumed by it — so granting it after the swing means Top Chop does
   NOT buff its own hit. It buffs whatever you swing next. **The blade sharpens the one after it.**

   Held it is a steady 4 Vigor a turn, which is a lot, and it costs you the hand slot to fly it. Swung it
   is damage plus a bigger sharpening. Hold it while the wall goes up; swing it when you know what's next. */
public sealed class TopChop() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    /* No Exhaust. The blade doesn't die when you swing it — it goes to your discard and you draw it
       again. **A blade is a thing you keep.** */
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(5m, ValueProp.Move), new IntVar("Vigor", 4m) };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);          // 5 → 8, mirroring Bottom Blade
        DynamicVars["Vigor"].UpgradeValueBy(2m);        // 4 → 6
    }

    private const decimal Kept = 4m;   // flat, never scales — no reward for hoarding

    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, Kept, Owner.Creature, this, false);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // Damage FIRST — so the Vigor below lands on your NEXT attack, not this one.
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature,
            DynamicVars["Vigor"].BaseValue, Owner.Creature, this, false);
    }
}
