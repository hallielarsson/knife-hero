using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Butch Blade — a Retain Blade you forge with Fancy Footwork's attack use. While it's in your hand,
   your attacks deal +1 damage; played, it's a strong strike. One copy per run-of-combat (Fancy
   re-forges UPGRADE it, never duplicate) — that singleton limit is the balance, so it stays cheap
   and doesn't exhaust. */
public sealed class ButchBlade() : KnifeHeroCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy), IBlade, IFlagBlade
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    // +1 to your attacks while Butch Blade is in your hand (every card in hand is a hook listener).
    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && dealer == Owner?.Creature && Pile?.Type == PileType.Hand)
            return 1m;
        return 0m;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // Re-forged (you "got the flag" again): the blade sharpens by 1 (à la the Regent's forge).
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}
