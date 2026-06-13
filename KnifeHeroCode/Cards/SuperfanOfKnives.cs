using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Superfan of Knives — Hallie's design. Deal 4 to all enemies, then make a Shiv for each enemy hit
   and drop them all in your hand. The Shivs themselves hit all enemies and vanish at end of turn
   (Ethereal), so it's a knife storm that scales with how many enemies there are. */
public sealed class SuperfanOfKnives() : KnifeHeroCard(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(4m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = CombatState.HittableEnemies.Count();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        for (int i = 0; i < count; i++)
        {
            var shiv = CombatState.CreateCard<Shiv>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Hand, addedByPlayer: true);
        }
    }
}
