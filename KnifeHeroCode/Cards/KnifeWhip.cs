using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
namespace KnifeHero.KnifeHeroCode.Cards;

/* Knife Whip — Hallie's design: a card that spends itself down the more you swing it.
   "Deal 8 damage. Put a shiv in your discard and decrease the damage this card does by 1."
   The shiv it drops is the base-game Shiv token (not our Throwing Shiv card). The damage reduction is
   permanent for this card instance for the rest of combat (UpgradeValueBy(-1)), so each swing
   trades a point of whip damage for a thrown knife in the pile — managing the decay is the play.
   Human-sourced mechanic (Hallie); placeholder art via KnifeHeroCard. */
public sealed class KnifeWhip() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IBlade
{
    public override string PortraitPath => "knife_whip.png".CardImagePath();
    public override string CustomPortraitPath => "knife_whip.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    /* THE WHIP SHATTERS ON ARMOUR, AND THE SHARDS ARE KNIVES.
       Every point of this card's damage that lands on BLOCK becomes a Shiv in your discard — and this
       card loses that much damage, permanently, for the rest of the fight.

       So the whip only wears out when it hits something hard. Against bare flesh it never decays at all.
       Against a shielded enemy it comes apart in your hands and you're left holding a fistful of knives.
       Armour doesn't stop you; it ARMS you. */
    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (cardSource != this || dealer != Owner.Creature) return;
        int shards = result.BlockedDamage;
        if (shards <= 0) return;

        for (int i = 0; i < shards; i++)
        {
            var shiv = CombatState.CreateCard<Shiv>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Discard, Owner);
        }
        DynamicVars.Damage.UpgradeValueBy(-shards);
    }
}
