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
    // Art slug is switch_blade.png — per ART_MAPPING this card is slated to become "Switch Blade"
    // (Hallie's rename, still gated). The drawing is ready, so wire it now; the rename rides her pass.
    public override string PortraitPath => "switch_blade.png".CardImagePath();
    public override string CustomPortraitPath => "switch_blade.png".BigCardImagePath();

    public override bool GainsBlock => true;
    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new BlockVar(4m, ValueProp.Move) };
    
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        // Play it → it's a TOP. Forge a Top Chop, or sharpen the one you're already carrying.
        await CombatState.AddOrUpgradeFlagBlade<TopChop>(Owner);
        await RecruitFromDiscard();
    }

    /* THE ENGINE FEEDS ITSELF (Hallie, 2026-07-12).
       Whichever way you use a Switch Blade, it reaches into your DISCARD and turns a Strike or a Defend
       there into another Switch Blade. So the loop no longer depends on the relic to keep turning — the
       relic seeds it, and then the blades recruit their own replacements out of the pile of spent basics.

       Your discard is not a graveyard. It's the raw stock.

       ⚠ The transform happens to a card in the DISCARD, which is quiet and settled — NOT to a card being
       played. Transforming a card mid-play is what stranded a glowing card on Hallie's screen and, worse,
       silently deleted cards from the deck. See TheWash.cs for the full autopsy. Do not move this. */
    private async Task RecruitFromDiscard()
    {
        var basic = CardPile.GetCards(Owner, PileType.Discard)
            .FirstOrDefault(c => c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend));
        if (basic == null) return;
        await CardCmd.Transform(basic, CombatState.CreateCard<FancyFootwork>(Owner));
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);

        // Hold it → it's a BOTTOM. Forge a Bottom Blade, or sharpen the one you're carrying.
        await CombatState.AddOrUpgradeFlagBlade<BottomBlade>(Owner);
        await RecruitFromDiscard();
        await CardCmd.Exhaust(choiceContext, this, causedByEthereal: false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
