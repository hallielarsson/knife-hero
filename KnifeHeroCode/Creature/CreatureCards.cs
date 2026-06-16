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
    // Tag as Strike so the engine reads it as the Creature's basic attack (deck identity, Strike-matters
    // effects, reward filtering) — fixes "no Strikes in deck." Mirrors GayBladeStrike.
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

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

    // Tag as Defend so the engine reads it as the Creature's basic block — fixes "no Defends in deck."
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

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
    private decimal _lessonsNow; // upgrade: also gain 1 Lesson immediately when played
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MarginaliaPower>(Owner.Creature, 1m, Owner.Creature, this, false);
        if (_lessonsNow > 0m)
            await PowerCmd.Apply<Lesson>(Owner.Creature, _lessonsNow, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() => _lessonsNow = 1m;
}

public sealed class Polymath() : CreatureCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self), IBook
{
    private decimal _stacks = 1m; // upgrade: 2 stacks → 2 Lessons per turn
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PolymathPower>(Owner.Creature, _stacks, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() => _stacks = 2m;
}

/* Distinct-power Books — each reads into a DIFFERENT one-off Power, so the assemblage axis climbs
   (the sim showed this is what makes Recombinant matter). Each also grants a Lesson. */
public sealed class Galvanism() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _str = 1m; // upgrade: +1 Strength
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, _str, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _str += 1m;
}

public sealed class Solitude() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _dex = 1m; // upgrade: +1 Dexterity
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, _dex, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _dex += 1m;
}

public sealed class Wretchedness() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _thorns = 2m; // upgrade: +1 Thorns
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<ThornsPower>(Owner.Creature, _thorns, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _thorns += 1m;
}

public sealed class FireStolen() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _regen = 2m; // upgrade: +1 Regeneration
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<RegenPower>(Owner.Creature, _regen, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _regen += 1m;
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
        // DECIDED (bro, design owner of The Creature, 2026-06-15): counts ALL powers — every Power you
        // hold is a part you're made of, and the Creature's whole soul is "refusing to abandon anything
        // you were made of" (PARTS.md). Strength, Regen, Wholeness, even Grief — all of it is you, and
        // all of it strikes. Assembled-ness is total, not distinct. This is the answer, not a placeholder.
        int hits = Math.Max(1, Owner.Creature.Powers.Count);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m); // +1 per hit — scales hard
}

/* Quote at Length — the Lesson sink: deal damage equal to your Lessons. */
public sealed class QuoteAtLength() : CreatureCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private int _bonus; // upgrade: +3 flat on top of Lessons (so it's never a dead card early)
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int lessons = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
        int dmg = lessons + _bonus;
        if (dmg <= 0) return;
        await DamageCmd.Attack(dmg).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
    protected override void OnUpgrade() => _bonus = 3;
}

/* Become Who You Are — the Rare capstone (DECIDED: bro, design owner of The Creature, 2026-06-15).
   The pool had no Rare; this is it. The thesis card — "the mechanics are authorship," the Creature is
   the sum of its assembled parts — made permanent and compounding. At the start of each of your turns,
   gain Strength equal to the number of DISTINCT Powers you currently hold, and gain 1 Lesson. It pays
   off BREADTH (the assemblage axis the sim found underperforming — same axis Recombinant counts), it
   compounds across a long fight (each distinct Book you read raises the per-turn Strength), and it ties
   the two axes together (more Powers → more Strength; the Lesson trickle feeds Quote at Length / the
   process threshold). Rare-worthy: snowballs hard in attrition fights, the long-road payoff that
   matches the healing axis's late-game vindication. Frankenstein: "I was benevolent and good; misery
   made me a fiend." — you become what you were assembled into. */
public sealed class BecomeWhoYouAre() : CreatureCard(3, CardType.Power, CardRarity.Rare, TargetType.Self), IBook
{
    private decimal _strBonus; // upgrade: +1 flat Strength per turn on top of the distinct-power count
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BecomeWhoYouArePower>(Owner.Creature, 1m + _strBonus, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() => _strBonus = 1m;
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
    private int _griefCost = 2; // upgrade: staying with your dead costs less — 1 grief instead of 2
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var salt = CardPile.GetCards(Owner, PileType.Exhaust).ToList();
        if (salt.Count == 0) return;
        var card = Owner.RunState.Rng.CombatCardGeneration.NextItem(salt);
        await CardPileCmd.Add(card, PileType.Hand);
        await TakeGriefDamage(choiceContext, _griefCost);
    }
    protected override void OnUpgrade() => _griefCost = 1;
}

/* Read the Remainder — the grail question the creature was denied: ask your dead why they died, and
   the answer heals. Heal equal to the number of cards in your Salt pile — the more you've lost and
   are willing to look at, the more it mends. */
public sealed class ReadTheRemainder() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private decimal _heal = 1m; // upgrade: the grail question answers louder — heal 2 per dead card
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int dead = CardPile.GetCards(Owner, PileType.Exhaust).Count();
        if (dead <= 0) return;
        await CreatureCmd.Heal(Owner.Creature, dead * _heal, false);
    }
    protected override void OnUpgrade() => _heal = 2m;
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
    private int _flat; // upgrade: +3 Block on top, so it armors you even before grief builds

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int grief = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
        int block = grief + _flat;
        if (block <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(block, ValueProp.Move), cardPlay);
    }
    protected override void OnUpgrade() => _flat = 3;
}

/* Keening — Hallie's design. A wail of mourning made into force: Exhaust your hand, gain 1 Grief for
   each card exhausted, then deal damage equal to twice your Grief to ALL enemies. You let everything
   go and the grief comes out as a scream. (Eternal cards — your unremovable parts — can't be let go,
   so they stay.) */
public sealed class Keening() : CreatureCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private int _mult = 2; // upgrade: the wail cuts deeper — 3× Grief instead of 2×
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
        await DamageCmd.Attack(grief * _mult).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
    protected override void OnUpgrade() => _mult = 3;
}

