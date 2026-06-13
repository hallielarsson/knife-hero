using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Superfan of Knives — Hallie's design. Deal 4 to all enemies, make a (standard, augment-able) Shiv
   for each enemy, and this turn your Shivs hit ALL enemies (the base game's Fan of Knives, applied
   just for this turn). A knife storm that scales with the number of enemies. Exhaust. */
public sealed class SuperfanOfKnives() : KnifeHeroCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(4m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = CombatState.HittableEnemies.Count();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        // Shivs hit all enemies this turn, then make one standard Shiv per enemy.
        await PowerCmd.Apply<FanOfKnivesPower>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<FanOfKnivesThisTurnPower>(Owner.Creature, 1m, Owner.Creature, this, false);
        await Shiv.CreateInHand(Owner, count, CombatState);
    }
}
