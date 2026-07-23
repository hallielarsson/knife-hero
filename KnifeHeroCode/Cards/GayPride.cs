using System.Collections.Generic;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* GAY PRIDE — ⟨2⟩ (⟨1⟩ upgraded) Attack. Retain. A Pride.
     HELD:  gain 1 Visibility at end of turn.
     SWUNG: deal damage equal to your Visibility to ALL enemies.

   The Visibility payoff. Every other card treats Visibility as a countdown to being found; this one is the reason to
   let it climb. Fly it and it makes you louder every turn; swing it and every point of that noise lands
   on everything in the room.

   ⚠ The held effect grants Visibility via PowerCmd directly and NOT through Stealth's found-you path, so
   DeadNamePower does NOT intercept it. That's deliberate: Dead Name refuses the visibility of being *found*,
   and this is visibility you chose. See Stealth.cs — the two interception points there are the only ones. */
public sealed class GayPride() : PrideCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override int MaxUpgradeLevel => 1;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<Visibility>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal visibility = Owner.Creature.GetPower<Visibility>()?.Amount ?? 0m;
        if (visibility <= 0m) return;

        await DamageCmd.Attack(visibility).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}
