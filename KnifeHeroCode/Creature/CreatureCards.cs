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

/* The Creature's cards. Two axes: Lessons (depth) and assemblage (distinct Powers). See
   THE_CREATURE/DESIGN.md. */

// ---- basics ----------------------------------------------------------------------------------
/* Recite — the Creature's Strike. Deal 6. */
public sealed class Recite() : CreatureCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override string PortraitPath => "recite.png".CardImagePath();
    public override string CustomPortraitPath => "recite.png".BigCardImagePath();

    // ⚠ Must carry CardTag.Strike or the engine reports "no Strikes in deck" (deck identity,
    // Strike-matters effects, reward filtering all key off the tag, not the name).
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

/* Annotate — the Creature's Defend. Gain 5 Block. */
public sealed class Annotate() : CreatureCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override string PortraitPath => "annotate.png".CardImagePath();
    public override string CustomPortraitPath => "annotate.png".BigCardImagePath();

    public override bool GainsBlock => true;

    // ⚠ Must carry CardTag.Defend — see Recite.
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
/* Open Book — gain 5 Block and 2 Lessons. */
public sealed class OpenBook() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IBook
{
    public override bool GainsBlock => true;

    // Upgrade favours Lessons over Block — Lessons are what mend you.
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

/* Distinct-power Books — each grants a Lesson plus a DIFFERENT one-off Power, so the assemblage axis
   (which Recombinant and Wholeness read) actually climbs. */
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
/* Recombinant — the assemblage payoff: hit 3 damage once per Power you hold. */
public sealed class Recombinant() : CreatureCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(3m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // ALL powers, not distinct-buffs-only — debuffs (Grief) and readouts (Wholeness) count too.
        // Deliberate; assembled-ness is total. Don't "fix" this into a filtered count.
        int hits = Math.Max(1, Owner.Creature.Powers.Count);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m); // +1 per hit — scales hard
}

/* Quote at Length — the Lesson sink: deal damage equal to your Lessons. */
public sealed class QuoteAtLength() : CreatureCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private int _bonus; // upgrade: +3 flat on top of Lessons, so it's never dead early
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

/* Become Who You Are — the Rare capstone. Each turn, gain Strength equal to your Wholeness.
   ⚠ BALANCE: it scales on WHOLENESS deliberately, not on distinct-Power count. Counting Powers opened
   at +3 Strength/turn for free (Grief, Wholeness and Lesson are almost always on you) and was
   game-breaking. Wholeness starts at 0 and has to be earned a mend at a time. Cost 3 for the same
   reason. See BecomeWhoYouArePower. */
public sealed class BecomeWhoYouAre() : CreatureCard(3, CardType.Power, CardRarity.Rare, TargetType.Self), IBook
{
    private decimal _strBonus; // upgrade: +1 flat Strength per turn on top of Wholeness
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BecomeWhoYouArePower>(choiceContext, Owner.Creature, 1m + _strBonus, Owner.Creature, this, false);
    }
    protected override void OnUpgrade() => _strBonus = 1m;
}

// ---- the Exhaust pile ("Salt") ---------------------------------------------------------------

/* Don't Look Away — take a random card from your Exhaust pile back into your hand. It costs 2 damage:
   pulling a card back from the dead is priced. */
public sealed class DontLookAway() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private int _griefCost = 2; // upgrade: 1 instead of 2
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

/* Read the Remainder — choose a card in your Exhaust pile. Gain a Lesson, heal HP equal to its cost,
   and it returns to your draw pile. The Lesson sink's counterpart: it recycles as well as heals, which
   is why the heal is small and per-card rather than scaling on pile size. */
public sealed class ReadTheRemainder() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
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

    private decimal LessonsGiven => IsUpgraded ? 2m : 1m;
    protected override void OnUpgrade() { }
}

/* Wallow — gain Block equal to your Grief. The Mourner's defence: Grief bleeds you every turn, and
   this is the one card that pays you for carrying it. */
public sealed class Wallow() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    private int _flat; // upgrade: +3 flat, so it isn't dead at Grief 0

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int grief = (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
        int block = grief + _flat;
        if (block <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(block, ValueProp.Move), cardPlay);
    }
    protected override void OnUpgrade() => _flat = 3;
}

/* Keening — Exhaust your hand, then hit ALL enemies for (2 × your Grief) + 2 per card exhausted.

   ⚠ It READS Grief; it cannot GRANT it. Grief is a derived readout of how many parts of you are broken
   (see MendedBody.Recount) — nothing may add to it directly, or the readout and the deck disagree. */
public sealed class Keening() : CreatureCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private decimal _mult = 2m;   // upgrade: 3× Grief

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

