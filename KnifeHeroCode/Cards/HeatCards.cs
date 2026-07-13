using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE HEAT CARDS — Hallie's design, 2026-07-12.

   Heat is the clock on Stealth: it makes hits bigger AND strips your cover faster (see Stealth.cs).
   Left alone it is pure downside — a countdown to being found. These cards are what you do about it,
   and they pull in three different directions:

     • RUN FROM IT   — Smoke Bomb (clear it), Shadow Dodge (shave it)
     • HIDE FROM IT  — Dead Name (refuse it entirely; take clutter instead)
     • **FEED IT**   — Honeypot. You are the bait. The more they know where you are, the more it costs
                       them to come and get you.

   Honeypot is the one that turns the whole thing inside out. Heat stops being a punishment and becomes
   a resource, and suddenly you *want* to be found. That's a build. */

/* HONEYPOT — ⟨2⟩ Power. Gain Thorns equal to your Heat + 2.
   The bait. You let them find you, and you make finding you expensive. */
public sealed class Honeypot() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // UPGRADE: the bait is sweeter. +4 over your Heat instead of +2.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Bonus", 2m) };

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(2m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int heat = (int)(Owner.Creature.GetPower<Heat>()?.Amount ?? 0m);
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature,
            heat + DynamicVars["Bonus"].BaseValue, Owner.Creature, this, false);
    }
}

/* SMOKE BOMB — ⟨2⟩ Skill, Exhaust. Lose all Heat.
   You break line of sight and they lose you completely. It costs you the card. */
public sealed class SmokeBomb() : KnifeHeroCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // UPGRADE: it doesn't Exhaust. You get to disappear more than once.
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword>() : new List<CardKeyword> { CardKeyword.Exhaust };

    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var heat = Owner.Creature.GetPower<Heat>();
        if (heat != null) await PowerCmd.Remove(heat);
    }
}

/* SHADOW DODGE — ⟨1⟩ Skill, Common. Gain 6 (9) Block. Lose 1 Heat.
   The cheap, repeatable cool-down. Block AND you get a little of your cover back. */
public sealed class ShadowDodge() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var heat = Owner.Creature.GetPower<Heat>();
        if (heat != null) await PowerCmd.ModifyAmount(choiceContext, heat, -1m, Owner.Creature, this);
    }
}

/* GO TO GROUND — ⟨1⟩ Skill, Common. Gain 6 (9) Block. Gain 1 Stealth.
   ⁉ FLAGGED: Hallie called this card "Stealth", but that name already belongs to the POWER, and a card
   and a power sharing a display name is a real UX problem (the tooltip and the card would fight). Named
   "Go to Ground" as a placeholder — Hallie's to mint. The mechanic is exactly as specified. */
public sealed class GoToGround() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<Stealth>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* LOOK WHAT I FOUND DOWN HERE — ⟨1⟩ Skill, Common. Gain 6 (9) Block. Convert all Stealth into Shivs.
   The cash-out. Your cover becomes knives. You were rummaging around down there the whole time. */
public sealed class LookWhatIFoundDownHere() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var stealth = Owner.Creature.GetPower<Stealth>();
        int n = (int)(stealth?.Amount ?? 0m);
        if (n <= 0) return;

        await PowerCmd.Remove(stealth);
        await Shiv.CreateInHand(Owner, n, CombatState);
    }
}

/* DAY OF INVISIBILITY — ⟨1⟩ Skill, Exhaust. Your attacks this turn do not break your Stealth.
   The one turn you get to be a ghost with a knife. Normally striking someone shows them where you are;
   this turn it doesn't. */
public sealed class DayOfInvisibility() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // UPGRADE: it doesn't Exhaust. Every turn can be the day.
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword>() : new List<CardKeyword> { CardKeyword.Exhaust };

    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Unseen>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* PICKPOCKET — ⟨1⟩ Power. The first time you deal damage each turn, gain a Shiv.
   ⁉ FLAGGED: Hallie didn't give a cost. Priced at 1 — it's a slow, per-turn trickle, not a burst.
   Change freely. */
public sealed class Pickpocket() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // UPGRADE: two knives out of their pocket, not one. (The power lifts `Amount` shivs.)
        await PowerCmd.Apply<PickpocketPower>(choiceContext, Owner.Creature, IsUpgraded ? 2m : 1m,
            Owner.Creature, this, false);
    }
}

/* DEAD NAME — ⟨2⟩ Power. Whenever you would gain Heat, put a Dazed in your discard instead.
   They don't get to know what to call you. The cost is that your deck fills with something that isn't
   you — clutter you have to carry around instead of being found.

   Mechanically it is the hard counter to the Stealth clock: with Dead Name out, Heat never rises, so
   Stealth never degrades, and you can hide forever — as long as you can stand the junk in your deck.
   (And it is anti-synergy with Honeypot, deliberately: you cannot both refuse the Heat and farm it.) */
public sealed class DeadName() : KnifeHeroCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    /* UPGRADE: INNATE — you start the fight holding it.
       (There is no permanent cost-reduction API in this engine; EnergyCost.AddThisCombat is combat-
       scoped and wrong for an upgrade. Innate is the right upgrade for an expensive Power anyway:
       the problem with a 2-cost Power is never its cost, it's that you draw it on turn 4.) */
    public override int MaxUpgradeLevel => 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword> { CardKeyword.Innate } : new List<CardKeyword>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DeadNamePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
