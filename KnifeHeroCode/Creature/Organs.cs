using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* THE ORGANS — the Parts (see PartCard.cs) and what they become when mended.
   Every mended organ scales on WHOLENESS, so mending one makes all the others better. That compounding
   is the Tender build's whole payoff and the reason it's worth the tempo it costs.
   Art: public-domain Gray's Anatomy plates (1918), THE_CREATURE/art/gray/. */


/* THE THROAT — mend for 2 Lessons. */
public sealed class TheThroat() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_throat.png".CardImagePath();
    public override string CustomPortraitPath => "the_throat.png".BigCardImagePath();

    protected override int LessonsToMend => 2;
    protected override CardModel Mended() => CombatState.CreateCard<MendedThroat>(Owner);
}

/* MENDED THROAT — gain a Lesson per Wholeness. */
public sealed class MendedThroat() : CreatureCard(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMendedPart
{
    public override string PortraitPath => "the_throat.png".CardImagePath();
    public override string CustomPortraitPath => "the_throat.png".BigCardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int whole = (int)(Owner.Creature.GetPower<Wholeness>()?.Amount ?? 0m);
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, Math.Max(1, whole),
            Owner.Creature, this, false);
    }
}


/* THE LEG — mend for 3 Lessons (a whole limb, so it costs more than an organ). */
public sealed class TheLeg() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_leg.png".CardImagePath();
    public override string CustomPortraitPath => "the_leg.png".BigCardImagePath();

    protected override int LessonsToMend => 3;
    protected override CardModel Mended() => CombatState.CreateCard<MendedLeg>(Owner);
}

/* MENDED LEG — gain 4 Block per Wholeness. */
public sealed class MendedLeg() : CreatureCard(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMendedPart
{
    public override string PortraitPath => "the_leg.png".CardImagePath();
    public override string CustomPortraitPath => "the_leg.png".BigCardImagePath();

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(4m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int whole = Math.Max(1, (int)(Owner.Creature.GetPower<Wholeness>()?.Amount ?? 0m));
        await CreatureCmd.GainBlock(Owner.Creature,
            new BlockVar(DynamicVars.Block.BaseValue * whole, ValueProp.Move), cardPlay);
    }
}


/* THE GUT — mend for 2 Lessons. The organ that heals. */
public sealed class TheGut() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_gut.png".CardImagePath();
    public override string CustomPortraitPath => "the_gut.png".BigCardImagePath();

    protected override int LessonsToMend => 2;
    protected override CardModel Mended() => CombatState.CreateCard<MendedGut>(Owner);
}

/* MENDED GUT — heal 2 HP per Wholeness. Exhaust.

   ⚠ THE EXHAUST IS LOAD-BEARING. Without it the card is replayable from the discard every turn,
   healing 2×Wholeness against a bleed of ~1 — which makes "stall the fight and farm HP" strictly
   correct play. Sustain must never scale with turns spent in a fight. (Same rule broke Wholeness and
   the mend; see CreaturePowers.Wholeness.) */
public sealed class MendedGut() : CreatureCard(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMendedPart
{
    public override string PortraitPath => "the_gut.png".CardImagePath();
    public override string CustomPortraitPath => "the_gut.png".BigCardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int whole = Math.Max(1, (int)(Owner.Creature.GetPower<Wholeness>()?.Amount ?? 0m));
        await CreatureCmd.Heal(Owner.Creature, 2m * whole, false);
    }
}


/* THE HIP — mend for 3 Lessons (a whole limb, like the Leg). Gray1244: hip & gluteal region. */
public sealed class TheHip() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_hip.png".CardImagePath();
    public override string CustomPortraitPath => "the_hip.png".BigCardImagePath();

    protected override int LessonsToMend => 3;
    protected override CardModel Mended() => CombatState.CreateCard<MendedHip>(Owner);
}

/* MENDED HIP — deal 4 damage for each part of you that is whole.

   DECIDED (bro, design owner, 2026-07-24): the offensive mended limb. The hip & gluteal region is the
   body's seat of locomotive power — the largest muscles, the thrust that drives a blow — so a body made
   more whole strikes harder. It fills the one gap in the mended-limb suite: throat gives Lessons, leg
   gives Block, gut heals, Mended Heart hits FLAT — nothing converts Wholeness into damage. This does, so
   it is the Tender's compounding kill-button: the reward for mending many parts is finally a threat, not
   just durability. Answers BLUE_SKY.md's open "each limb should do something the organ would do."
   The 4-per-Wholeness is a starting number; Hallie mints the final. */
public sealed class MendedHip() : CreatureCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy), IMendedPart
{
    public override string PortraitPath => "the_hip.png".CardImagePath();
    public override string CustomPortraitPath => "the_hip.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(4m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int whole = Math.Max(1, (int)(Owner.Creature.GetPower<Wholeness>()?.Amount ?? 0m));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue * whole).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}


/* THE DIAPHRAGM — mend for 2 Lessons. Gray990: the sagittal sections, the breath. */
public sealed class TheDiaphragm() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_diaphragm.png".CardImagePath();
    public override string CustomPortraitPath => "the_diaphragm.png".BigCardImagePath();

    protected override int LessonsToMend => 2;
    protected override CardModel Mended() => CombatState.CreateCard<MendedDiaphragm>(Owner);
}

/* MENDED DIAPHRAGM — draw a card for each part of you that is whole. Exhaust.

   DECIDED (bro, design owner, 2026-07-25): the breath, and the one niche the mended-limb suite was
   missing — DRAW. The body finds its rhythm and pulls its cards to it. Exhaust is load-bearing for the
   same reason as the Gut's: a replayable per-Wholeness draw is a stall-and-farm engine. Numbers Hallie's. */
public sealed class MendedDiaphragm() : CreatureCard(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMendedPart
{
    public override string PortraitPath => "the_diaphragm.png".CardImagePath();
    public override string CustomPortraitPath => "the_diaphragm.png".BigCardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int whole = Math.Max(1, (int)(Owner.Creature.GetPower<Wholeness>()?.Amount ?? 0m));
        await CardPileCmd.Draw(choiceContext, whole, Owner);
    }
}


/* LET IT ROT — ⟨0⟩ Skill, Exhaust. Fester an unmended Part in your hand immediately. Gain 2 Lessons.
   The only way to choose to scar. Costs 0 because the cost is the organ, not the energy. */
public sealed class LetItRot() : CreatureCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override bool IsPlayable =>
        CardPile.GetCards(Owner, PileType.Hand).Any(c => c is PartCard);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var part = CardPile.GetCards(Owner, PileType.Hand).OfType<PartCard>().FirstOrDefault();
        if (part == null) return;

        await part.RotNow(choiceContext);
        await PowerCmd.Apply<Lesson>(choiceContext, Owner.Creature, 2m, Owner.Creature, this, false);
    }
}


/* THE CHARNEL HOUSE — ⟨1⟩ Skill, Exhaust. Add a random Part to your hand.
   The voluntary way to take on a new part: upside if you have the Lessons banked to mend it, a
   permanent scar if you don't. */
public sealed class TheCharnelHouse() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.AddGeneratedCardToCombat(Parts.Random(Owner), PileType.Hand, Owner);
    }
}


/* THE APPETITE — ⟨2⟩ Power, Rare. Take a Part at the start of EVERY turn, unconditionally.
   The Mourner's accelerator: parts arrive faster than any Lesson economy can mend them, so grief
   stacks and Wallow/Keening feed. (The "take one if you have none" floor is in MendedBody, not here.) */
public sealed class TheAppetite() : CreatureCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AppetitePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
