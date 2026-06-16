using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Pin — a shiv that staples the enemy in place. "Deal 4; if it damages, apply Weak + Vulnerable."
   Part of the shiv build (SHIV_MODIFIER_ENGINE_SPEC.md). It carries CardTag.Shiv, so the turn-wide
   shiv modifiers (Poison Coating, Explosive Tip) light it up too.
   Hallie: do the proposal, reap in playtest. Effect is her stated one; the // PROPOSAL bits are
   the cost, the Weak/Vulnerable amounts, and the rarity. */
public sealed class Pin() : KnifeHeroCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IBlade
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Shiv };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(4m, ValueProp.Move) };   // Hallie's "deal 4"

    // // PROPOSAL: 2 Weak + 2 Vulnerable on a damaging hit. Hallie tunes by feel.
    private const decimal WeakAmount = 2m;
    private const decimal VulnerableAmount = 2m;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int hpBefore = cardPlay.Target.CurrentHp;
        int blockBefore = cardPlay.Target.Block;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        // "If it damages" — any HP or Block damage landed (the pin bit into something).
        bool damaged = cardPlay.Target.CurrentHp < hpBefore || cardPlay.Target.Block < blockBefore;
        if (damaged)
        {
            await PowerCmd.Apply<WeakPower>(cardPlay.Target, WeakAmount, Owner.Creature, this, false);
            await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, VulnerableAmount, Owner.Creature, this, false);
        }
    }

    // // PROPOSAL: upgrade adds 2 damage (4 -> 6). Hallie tunes.
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
