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

/* Femme Flechette — a Retain Blade you forge with Fancy Footwork's defend use. Where Butch forges
   sharper offense, Femme forges sharper RETALIATION: while it's in your hand, when an enemy attack
   damages you it takes Retaliate damage straight back — its own teeth, no Thorns power, so nothing
   accumulates across turns. The retaliate is the stat that grows: each time you Fancy it (re-forge a
   copy you already hold) it goes up by 1. One copy per run-of-combat; stays cheap, no exhaust. */
public sealed class FemmeFlechette() : KnifeHeroCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy), IBlade, IFlagBlade
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(5m, ValueProp.Move) };

    private int _retaliate = 3;   // grows +1 per re-forge (à la the Regent's forge)

    // While in hand: an enemy attack that damages you takes Retaliate damage straight back.
    public override async Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner?.Creature && dealer != null && Pile?.Type == PileType.Hand && props.IsPoweredAttack())
            await CreatureCmd.Damage(choiceContext, dealer, _retaliate,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim, Owner.Creature, null);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // Re-forged (you "got the flag" again): the retaliate sharpens by 1 — the thing that scales.
    protected override void OnUpgrade() => _retaliate += 1;
}
