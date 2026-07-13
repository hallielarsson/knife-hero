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

/* TOP CHOP — forged by PLAYING a Switch Blade. (Hallie, post-playtest with Lori, 2026-07-12.)

     ON FORGE:  +4 Vigor immediately (applied at the forge site in FancyFootwork).
     KEPT:      +2 Vigor at the end of every turn you hold it. **Flat — never scales.**
     SWUNG:     Deal damage, THEN gain 2 Vigor per forge level. Exhaust.

   ⚠ THE ORDER MATTERS AND IT IS DELIBERATE: the damage lands FIRST, then the Vigor. Base-game Vigor only
   buffs the **next** attack and is consumed by it — so granting it after the swing means the Top Chop
   does NOT buff its own hit. It buffs whatever you swing next. The blade sharpens the one after it.

   THE KEPT/SWUNG SPLIT IS THE DESIGN. Holding it is a flat, honest trickle that never improves, so there
   is no reward for hoarding. Swinging it is where the forging pays. The blade doesn't want to be carried
   forever — it wants to be carried until it's heavy, and then swung.

   And swinging Exhausts it, which is a **Pride dying**, which is what the relic watches for: a spent
   Strike in your discard comes back as a Switch Blade. Your prides die and your basics come back
   sharpened. */
public sealed class TopChop() : PrideCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override int MaxUpgradeLevel => 99;

    /* NO EXHAUST (Hallie, 2026-07-13). The blade doesn't die when you swing it — it goes to your
       discard, and you draw it again, and it's still as sharp as you forged it. A blade is a thing you
       KEEP. Re-forge it to make it heavier; swing it as often as you like. */
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    public const decimal OnForge = 4m;   // read by FancyFootwork at the forge site

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    private const decimal Kept = 2m;                          // flat, forever
    private decimal Swung => 2m * (CurrentUpgradeLevel + 1);  // this is the half that scales

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

        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, Swung, Owner.Creature, this, false);
    }
}
