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

using MegaCrit.Sts2.Core.Models.Powers;

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
    public override bool HasTurnEndInHandEffect => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    // It's a knife. It cuts. (Damage grows with every campfire upgrade, same as the blades it forges.)
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    /* PLAY IT → it's a TOP. Cut, forge a Top Chop (or sharpen the one you carry), take 4 Vigor, Exhaust. */
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        await CombatState.AddOrUpgradeFlagBlade<TopChop>(Owner);
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, TopChop.OnForge,
            Owner.Creature, this, false);
    }

    /* HOLD IT → it's a BOTTOM. Forge a Bottom Blade, take 4 Block now, Exhaust.
       (Post-playtest with Lori: the old end-of-turn Block gain is GONE. The forge itself pays you —
       that's how the card stops being a novel. It says one thing per path and nothing else.) */
    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CombatState.AddOrUpgradeFlagBlade<BottomBlade>(Owner);
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(BottomBlade.OnForge, ValueProp.Move), null);
        await CardCmd.Exhaust(choiceContext, this, causedByEthereal: false);
    }
}
