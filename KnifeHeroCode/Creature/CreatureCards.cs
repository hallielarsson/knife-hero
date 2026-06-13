using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* The Creature's cards — design authored by Claude (THE_CREATURE/DESIGN.md). Flavor quotes
   Frankenstein (public domain) in loc. Two axes: Lessons (depth) and assemblage (distinct Powers). */

// ---- basics ----------------------------------------------------------------------------------
public sealed class Recite() : CreatureCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class Annotate() : CreatureCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(5m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

// ---- Books (read for Lessons + Powers) -------------------------------------------------------
public sealed class OpenBook() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 2m, Owner.Creature, this, false);
    }
}

public sealed class Marginalia() : CreatureCard(1, CardType.Power, CardRarity.Common, TargetType.Self), IBook
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MarginaliaPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

public sealed class Polymath() : CreatureCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self), IBook
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PolymathPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* Distinct-power Books — each reads into a DIFFERENT one-off Power, so the assemblage axis climbs
   (the sim showed this is what makes Recombinant matter). Each also grants a Lesson. */
public sealed class Galvanism() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1m, Owner.Creature, this);
    }
}

public sealed class Solitude() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, 1m, Owner.Creature, this);
    }
}

public sealed class Wretchedness() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<ThornsPower>(Owner.Creature, 2m, Owner.Creature, this);
    }
}

public sealed class FireStolen() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<RegenPower>(Owner.Creature, 2m, Owner.Creature, this);
    }
}

// ---- payoffs ---------------------------------------------------------------------------------
/* Recombinant — the assemblage payoff: hit once per distinct Power you have. */
public sealed class Recombinant() : CreatureCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(3m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int hits = Math.Max(1, Owner.Creature.Powers.Count);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}

/* Quote at Length — the Lesson sink: deal damage equal to your Lessons. */
public sealed class QuoteAtLength() : CreatureCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int lessons = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
        if (lessons <= 0) return;
        await DamageCmd.Attack(lessons).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}
