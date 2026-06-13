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

/* Throwing Knife — Hallie's design: "Deal 6 damage. If this deals HP damage, Exhaust it.
   If it doesn't, return it to your hand." A knife that sticks (in flesh) or bounces back (off
   Block) so you can throw it again. We detect HP damage by comparing the target's CurrentHp
   before and after the hit; landing on Block returns the card to hand instead of exhausting.
   Human-sourced mechanic (Hallie); placeholder art via KnifeHeroCard. */
public sealed class ThrowingKnife() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IBlade
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int hpBefore = cardPlay.Target.CurrentHp;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        bool dealtHpDamage = cardPlay.Target.CurrentHp < hpBefore;
        if (dealtHpDamage)
            await CardCmd.Exhaust(choiceContext, this, causedByEthereal: false);   // it stuck — gone
        else
            await CardPileCmd.Add(this, PileType.Hand);                             // bounced off Block — throw again
    }
}
