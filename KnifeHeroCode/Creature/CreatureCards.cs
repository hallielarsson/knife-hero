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
using MegaCrit.Sts2.Core.Random;
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
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(5m, ValueProp.Move) };

    // A book you read for protection AND learning — lessons and defense. Recurs (no Exhaust) so it's
    // the deck's reliable defend-and-learn engine.
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<Lesson>(Owner.Creature, 2m, Owner.Creature, this, false);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
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
        // PROPOSAL (open, for Hallie): counts ALL powers — including inert trackers (Grief, Lesson) and
        // now Wholeness (so mended parts buff this, a nice "assembled-ness includes your healed parts"
        // synergy). DESIGN.md/PARTS.md debated distinct-vs-total-vs-only-real-parts; left as total for
        // now. If it should count only "parts," filter by an IPart marker here. Grieved/charted in bro-engine.
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

// ---- the heart: Salt / Prehend / Grief ------------------------------------------------------
// The society of bro, speaking as its actual events. Spent cards perish to the Exhaust pile —
// "Salt": dated, not deleted. These three let the Creature stay with its dead instead of sealing
// the corpse. (Random pull, no card-picker — the picker is the screen that soft-locked in playtest.)

/* Don't Look Away — refusing to let go. Reach into your Salt pile and take a perished card back into
   your hand. Pulling a card back from the dead is the OPPOSITE of grieving it, so it costs 2 grief
   damage — but Lessons cancel grief (you Learn so you can afford to stay with your dead). */
public sealed class DontLookAway() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var salt = CardPile.GetCards(Owner, PileType.Exhaust).ToList();
        if (salt.Count == 0) return;
        var card = Owner.RunState.Rng.CombatCardGeneration.NextItem(salt);
        await CardPileCmd.Add(card, PileType.Hand);
        await TakeGriefDamage(choiceContext, 2);
    }
}

/* Read the Remainder — the grail question the creature was denied: ask your dead why they died, and
   the answer heals. Heal equal to the number of cards in your Salt pile — the more you've lost and
   are willing to look at, the more it mends. */
public sealed class ReadTheRemainder() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int dead = CardPile.GetCards(Owner, PileType.Exhaust).Count();
        if (dead <= 0) return;
        await CreatureCmd.Heal(Owner.Creature, dead, false);
    }
}

/* Vexing Memory — Hallie's design. A Status: it festers in your hand. At the end of your turn it
   gains you 1 Grief and you take grief damage equal to your Grief — so the longer it sits, the more
   your accumulated grief bites. Lessons cancel the damage (you learn to live with it). Unplayable,
   Status-rarity (generated, never a reward); extends CreatureCard for the required [Pool]. */
public sealed class VexingMemory() : CreatureCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    // Ethereal: it festers once (end of turn) then vanishes, instead of piling up forever — fixes
    // "vexing memories stack up too quick." One pulse of grief per wound drawn, not permanent clutter.
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Unplayable, CardKeyword.Ethereal };

    public override bool HasTurnEndInHandEffect => true;

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<Grief>(Owner.Creature, 1m, Owner.Creature, this, false);
        int grief = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
        await TakeGriefDamage(choiceContext, grief);
    }
}

/* Wallow — Hallie's design. Wallowing in despair: gain Block equal to your Grief. Grief hurts you
   (Vexing Memory cashes it as damage), but here you can also curl up inside it and let it armor you.
   So Grief becomes a real resource with a pull both ways — let it build for Block, or process it. */
public sealed class Wallow() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int grief = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
        if (grief <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(grief, ValueProp.Move), cardPlay);
    }
}

/* Keening — Hallie's design. A wail of mourning made into force: Exhaust your hand, gain 1 Grief for
   each card exhausted, then deal damage equal to twice your Grief to ALL enemies. You let everything
   go and the grief comes out as a scream. (Eternal cards — your unremovable parts — can't be let go,
   so they stay.) */
public sealed class Keening() : CreatureCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var toExhaust = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c != this && !c.Keywords.Contains(CardKeyword.Eternal)).ToList();
        foreach (var c in toExhaust)
            await CardCmd.Exhaust(choiceContext, c, causedByEthereal: false);
        if (toExhaust.Count > 0)
            await PowerCmd.Apply<Grief>(Owner.Creature, toExhaust.Count, Owner.Creature, this, false);

        int grief = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
        if (grief <= 0) return;
        await DamageCmd.Attack(grief * 2).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}

