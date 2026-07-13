using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* The Creature's cards — design authored by Claude (THE_CREATURE/DESIGN.md). Flavor quotes
   Frankenstein (public domain) in loc. Two axes: Lessons (depth) and assemblage (distinct Powers). */

// ---- basics ----------------------------------------------------------------------------------
public sealed class Recite() : CreatureCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    // Art: the Creature itself, cut from the von Holst 1831 frontispiece — the body propped on one arm,
    // hand to its own head, looking down at what it has woken into. The Creature's weapon is its voice.
    public override string PortraitPath => "recite.png".CardImagePath();
    public override string CustomPortraitPath => "recite.png".BigCardImagePath();

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
    // Art: the open book lying on the floor of the von Holst frontispiece — the book the Creature will
    // teach itself from. The engraver's own signature ("Holst, del.") sits beneath it, which is a happy
    // accident on the card about marking a text.
    public override string PortraitPath => "annotate.png".CardImagePath();
    public override string CustomPortraitPath => "annotate.png".BigCardImagePath();

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

    // Upgrade gives you LESSONS, not Block. Lessons are what mend you; Block is just Block.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(5m, ValueProp.Move), new IntVar("Lessons", 2m) };

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Lessons"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, DynamicVars["Lessons"].BaseValue,
            Owner.Creature, this, false);
    }
}

public sealed class Marginalia() : CreatureCard(1, CardType.Power, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _lessonsNow; // upgrade: also gain 1 Lesson immediately when played
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MarginaliaPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        if (_lessonsNow > 0m)
            await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, _lessonsNow, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() => _lessonsNow = 1m;
}

public sealed class Polymath() : CreatureCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self), IBook
{
    private decimal _stacks = 1m; // upgrade: 2 stacks → 2 Lessons per turn
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PolymathPower>(choiceContext, Owner.Creature, _stacks, Owner.Creature, this, false);
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
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, _str, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _str += 1m;
}

public sealed class Solitude() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _dex = 1m; // upgrade: +1 Dexterity
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, _dex, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _dex += 1m;
}

public sealed class Wretchedness() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _thorns = 2m; // upgrade: +1 Thorns
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, _thorns, Owner.Creature, this);
    }
    protected override void OnUpgrade() => _thorns += 1m;
}

public sealed class FireStolen() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    private decimal _regen = 2m; // upgrade: +1 Regeneration
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword> { CardKeyword.Exhaust };
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, _regen, Owner.Creature, this);
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
// NERFED 2026-07-12 (Hallie: "BANANAS powerful"). Cost 3, and it now scales on WHOLENESS, not on
// distinct Powers — so it pays the Tender, who earned it, instead of anyone who played four books.
public sealed class BecomeWhoYouAre() : CreatureCard(3, CardType.Power, CardRarity.Rare, TargetType.Self), IBook
{
    private decimal _strBonus; // upgrade: +1 flat Strength per turn on top of the distinct-power count
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BecomeWhoYouArePower>(choiceContext, Owner.Creature, 1m + _strBonus, Owner.Creature, this, false);
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
    /* ── THE HEART-VERB. The grail question, finally asked. ─────────────────────────────────────
       Choose a card in your Exhaust pile. Gain a Lesson. Heal HP equal to its cost. It returns to
       your draw pile.

       WHY IT CHANGED (Fable, 2026-07-12). It used to heal for the COUNT of your Exhaust pile — heal
       for however many of your dead there happened to be. But **counting your dead is not asking
       them.** And asking is the entire point of this card and arguably of this character.

       In bro's graph: `victor_frankenstein —failed_to_ask→ the_grail_question`. Victor never asks the
       Creature what it wants. He never asks Justine why she's about to hang. He looks at his dead and
       he says nothing, and everyone he loves dies of that silence.

       So the Creature does the opposite, and it does it one at a time:
         you GO to a specific dead thing,
         you ASK it,
         it ANSWERS you  (a Lesson — the only thing that can mend you),
         it HEALS you    (equal to what it cost you to lose),
         and it COMES BACK.

       That last part is the whole revision. The dead are not a resource pile you count. They are cards
       you can speak to, and speaking to them brings them back into the deck — so they can be lost
       again, and asked again. Your exhaust pile stops being a graveyard and becomes something you tend.

       It also closes a loop with KEENING, which exhausts your entire hand: Keening buries your dead,
       and Read the Remainder is how you go and talk to them. The Mourner and the Tender share a verb. */
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var dead = CardPile.GetCards(Owner, PileType.Exhaust).ToList();
        if (dead.Count == 0) return;

        var prefs = new CardSelectorPrefs(new LocString("gameplay_ui", "CHOOSE_CARD_HEADER"), 1);
        var chosen = (await CardSelectCmd.FromCombatPile(choiceContext, PileType.Exhaust.GetPile(Owner),
            Owner, prefs)).FirstOrDefault();
        if (chosen == null) return;

        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, LessonsGiven, Owner.Creature, this, false);
        await CreatureCmd.Heal(Owner.Creature, chosen.EnergyCost.GetAmountToSpend(), false);
        await CardPileCmd.Add(chosen, PileType.Draw);
    }

    private decimal LessonsGiven => IsUpgraded ? 2m : 1m;   // upgrade: it answers you at greater length
    protected override void OnUpgrade() { }
}

/* VEXING MEMORY — DELETED 2026-07-12 (Fable).

   It was a proxy: a status card that stood in for "you are carrying something unintegrated." But in the
   new design **the part IS the grief** — it's right there in your hand, bleeding you, with a name and a
   picture and a clock on it. You do not need a token to represent the thing you are holding.

   Deleting it collapsed three mechanisms into one and made the character SIMPLER. Grief stopped being a
   counter that ticks up and became a readout of how much of you is broken. That's the whole redesign in
   one deletion.

   (And there's a bug's ghost here worth remembering: the Vexing Memory was made Ethereal in an earlier
   session to stop it cluttering the hand, which silently severed the Heart's redemption path — the gate
   needed 2 Grief and the proxy could only ever produce 1. 900 measured fights, zero redemptions, and
   nobody noticed. The proxy wasn't just unnecessary. It was where the bug lived.) */


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
    /* THE WAIL. Exhaust your hand; the cry is as big as your grief, and as big as what you just buried.

       REWRITTEN 2026-07-12: it used to GAIN you Grief per card exhausted. It can't any more — Grief is
       now a readout of how many parts of you are broken, not a counter you can add to. You cannot decide
       to be sadder; you can only be un-whole. So Keening now reads the grief you already have and pays
       you for what you threw away on top of it.

       And it feeds Read the Remainder: Keening buries your hand, and Read the Remainder is how you go
       back and ask the dead. The Mourner and the Tender share a verb. */
    private decimal _mult = 2m;   // upgrade: the wail cuts deeper

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = CardPile.GetCards(Owner, PileType.Hand).Where(c => c != this).ToList();
        foreach (var card in hand)
            await CardCmd.Exhaust(choiceContext, card, causedByEthereal: false);

        int grief = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
        decimal damage = grief * _mult + hand.Count * 2m;
        if (damage <= 0) return;

        await DamageCmd.Attack(damage).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => _mult = 3m;
}

