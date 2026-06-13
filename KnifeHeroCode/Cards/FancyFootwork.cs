using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Fancy Footwork — the flex, and the engine that makes your blades. How you USE it decides which
   blade you forge:
     - PLAY it as an attack  -> deal 6, then a TOP blade joins your hand (Retain: +1 dmg while held).
     - HOLD it to end of turn -> gain 3 Block, then a BOTTOM blade joins your hand (Retain: +3 Block
       at end of turn while held).
   The blades stick around (Retain), so leaning a pole stacks held blades — managing the loop is the
   game. Human-sourced mechanic (Hallie). */
public sealed class FancyFootwork() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new BlockVar(3m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        var top = CombatState.CreateCard<Top>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(top, PileType.Hand, addedByPlayer: true);
    }

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        var bottom = CombatState.CreateCard<Bottom>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(bottom, PileType.Hand, addedByPlayer: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
