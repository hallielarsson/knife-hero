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

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        // drop a standard Shiv into the discard pile
        var shiv = CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Shiv>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Discard, addedByPlayer: true));



	//CLAUDE: (this is hallie) I'm thinking that WHAT IF, it does direct damage to flesh and shatters for each point of damage to armor?
        // the whip wears down: this card permanently does 1 less for the rest of combat
        DynamicVars.Damage.UpgradeValueBy(-1m);
    }
}
