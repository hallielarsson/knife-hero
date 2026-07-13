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

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   THE ORGANS — the parts you are made of, and the parts you can become.
   Fable, 2026-07-12.

   Each one arrives as a curse and asks you the same question: **do I make this whole, or do I let it
   rot and carry the scar?** Heal it, or weaponise it. Once, per organ, for the whole run.

   ── AND THE MENDED ORGANS SCALE ON EACH OTHER ─────────────────────────────────────────────────
   Every whole part reads your WHOLENESS — the number of parts of you that are whole. So the second
   organ you mend makes the first one better, and the third makes both better, and so on.

   **Being whole compounds.** That is the Tender's entire payoff and the reason the slow build is worth
   the tempo it costs: a Creature with five whole organs isn't five times as good, it's five organs each
   working five times as hard. A body is not a pile of parts. It's parts that help each other.

   ── ART ───────────────────────────────────────────────────────────────────────────────────────
   Public-domain Gray's Anatomy plates (1918), from THE_CREATURE/art/gray/. A body assembled from
   borrowed parts, drawn by someone else, printed in a book. Which is what the Creature is.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */


/* ── THE THROAT ─────────────────────────────────────────────────────────────────────────────────
   Gray1210 — the neck dissected: sternocleidomastoid, carotid, the facial nerve.

   The organ of SPEECH, and so the organ of asking. The Creature's whole tragedy is that it can speak
   beautifully and no one will listen; its whole method is that it learns by reading. A throat is how
   you say what you learned.

   Mend it and you can ask. Let it rot and you carry a scar where your voice was. */
public sealed class TheThroat() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_throat.png".CardImagePath();
    public override string CustomPortraitPath => "the_throat.png".BigCardImagePath();

    protected override int LessonsToMend => 2;
    protected override CardModel Mended() => CombatState.CreateCard<MendedThroat>(Owner);
}

/* MENDED THROAT — you can speak. Gain a Lesson for every part of you that is whole.
   The more of you there is, the more you have to say. */
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


/* ── THE LEG ────────────────────────────────────────────────────────────────────────────────────
   Gray1247 — the whole leg: sciatic nerve to foot, popliteal and tibial arteries. The tallest plate
   in the set, and the only one that is a whole limb rather than an organ.

   The Creature outruns everything. It crosses the Alps, it crosses the ice, it is never caught — and
   it is never *not being chased*. A leg is how you keep going.

   Mend it and you can stand. Let it rot and you are lame for the rest of the run. */
public sealed class TheLeg() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_leg.png".CardImagePath();
    public override string CustomPortraitPath => "the_leg.png".BigCardImagePath();

    protected override int LessonsToMend => 3;   // a whole limb costs more to understand
    protected override CardModel Mended() => CombatState.CreateCard<MendedLeg>(Owner);
}

/* MENDED LEG — you can stand. Gain 4 Block for every part of you that is whole. */
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


/* ── THE GUT ────────────────────────────────────────────────────────────────────────────────────
   Gray989 — the abdominal viscera: liver, stomach, pancreas, omentum.

   The organ of METABOLISM. Which is not a metaphor here: the Creature's entire loop is information
   metabolism — take a thing in, feel it, integrate it, or carry it as scar. The gut is the part of you
   that turns what you swallowed into what you are.

   So it is the organ that HEALS, and it heals in proportion to how much of you already works.

   Mend it and you can digest. Let it rot and nothing you take in will ever nourish you. */
public sealed class TheGut() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "the_gut.png".CardImagePath();
    public override string CustomPortraitPath => "the_gut.png".BigCardImagePath();

    protected override int LessonsToMend => 2;
    protected override CardModel Mended() => CombatState.CreateCard<MendedGut>(Owner);
}

/* MENDED GUT — you can digest. Heal 2 HP for every part of you that is whole.
   The only sustained healing the Creature has, and it only works if you are already working. */
public sealed class MendedGut() : CreatureCard(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMendedPart
{
    public override string PortraitPath => "the_gut.png".CardImagePath();
    public override string CustomPortraitPath => "the_gut.png".BigCardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int whole = Math.Max(1, (int)(Owner.Creature.GetPower<Wholeness>()?.Amount ?? 0m));
        await CreatureCmd.Heal(Owner.Creature, 2m * whole, false);
    }
}


/* ── LET IT ROT ─────────────────────────────────────────────────────────────────────────────────
   ⟨0⟩ Skill. Choose an unmended Part in your hand. It festers immediately. Gain 2 Lessons.

   THE DEAL. This is the card the whole character is for, and it is the only card in the game that lets
   you *choose* to fail.

   You are carrying four broken organs and Lessons enough for one. Something is going to rot. This card
   says: **pick which**, and take the understanding you get from watching it happen.

   For the Mourner, it's the accelerator — scar yourself on purpose, keep the grief at maximum, and let
   Wallow and Keening feed on it.
   For the Tender, it is worse than that: it is the card you play when you have to sacrifice one part of
   yourself to save another. You get the two Lessons. You mend the heart. And the throat rots while you
   do it, and it stays rotted for the rest of the run.

   Free, because the cost is not energy. */
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


/* ── THE CHARNEL HOUSE ──────────────────────────────────────────────────────────────────────────
   ⟨1⟩ Skill, Exhaust. Add a random Part to your hand.

   HOW YOU GET MORE OF YOURSELF. Victor collected from "the dissecting room and the slaughter-house."
   The Creature is made of stolen parts, so of course it steals more.

   And this is the whole game in one card, because a Part is not a gift — it is a **bet**:

     mend it   → +1 Wholeness, +2 max HP, forever. Every other whole organ gets better.
     fail it   → a scar. Permanent. It bleeds you every turn for the rest of the run.

   You take the part because you *might* become more. That is exactly why Victor did it, and it is
   exactly what it cost him. The Creature does not get to be innocent of its maker's appetite — it has
   the same one. It wants to be more, and it will rob a grave to do it.

   Take it when you have Lessons in the bank. Take it when you don't and you are choosing the scar. */
public sealed class TheCharnelHouse() : CreatureCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var organs = new System.Func<CardModel>[]
        {
            () => CombatState.CreateCard<TheThroat>(Owner),
            () => CombatState.CreateCard<TheLeg>(Owner),
            () => CombatState.CreateCard<TheGut>(Owner),
            () => CombatState.CreateCard<ThrobbingHeart>(Owner),
        };
        var part = rng.NextItem(organs.ToList())();
        await CardPileCmd.AddGeneratedCardToCombat(part, PileType.Hand, Owner);
    }
}


/* ── THE APPETITE ───────────────────────────────────────────────────────────────────────────────
   ⟨2⟩ Power, Rare. At the start of your turn, if you are carrying no broken Part, take one.

   **You can never be done.** (Hallie, post-playtest: "I feel like I'm not making a ton of interesting
   decisions mid-fight by the first or second boss.")

   She's right, and the reason is that the Creature's whole game is the fork — mend it or let it rot —
   and once you've mended your heart there is nothing left to decide. You just play cards. A body with
   nothing broken in it is not this character; it's a deck.

   So this is the build-around for BOTH archetypes, and it's the same card for both, which is exactly
   right: the Tender takes it because every new organ is another +2 max HP and another point of Wholeness
   compounding into all the others. The Mourner takes it because every new organ is another scar waiting
   to happen, and scars are what they're built out of.

   Either way, the deal is the same: **the appetite never stops, and you have to keep answering it.**

   It is Victor's appetite. He could have stopped at one. */
public sealed class TheAppetite() : CreatureCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AppetitePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
