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

/* THE HEAT CARDS. Visibility is the clock on Stealth (see Stealth.cs) and is pure downside on its own. These
   are the three answers to it: run from it (Smoke Bomb, Shadow Dodge), refuse it (Dead Name), or feed it
   (Honeypot — the build where being found is the plan). */

/* HONEYPOT — ⟨2⟩ Power. Gain Thorns equal to your Visibility + {Bonus}. Turns Visibility from a punishment into a
   resource, which is what makes the loud build exist. */
public sealed class Honeypot() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Bonus", 2m) };

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(2m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int visibility = (int)(Owner.Creature.GetPower<Visibility>()?.Amount ?? 0m);
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature,
            visibility + DynamicVars["Bonus"].BaseValue, Owner.Creature, this, false);
    }
}

/* SMOKE BOMB — ⟨2⟩ Skill, Exhaust. Lose all Visibility. Upgrade: no Exhaust. */
public sealed class SmokeBomb() : KnifeHeroCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword>() : new List<CardKeyword> { CardKeyword.Exhaust };

    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var visibility = Owner.Creature.GetPower<Visibility>();
        if (visibility != null) await PowerCmd.Remove(visibility);
    }
}

/* SHADOW DODGE — ⟨1⟩ Skill. Gain {Block} Block. Lose 1 Visibility. The cheap, repeatable cool-down. */
public sealed class ShadowDodge() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var visibility = Owner.Creature.GetPower<Visibility>();
        if (visibility != null) await PowerCmd.ModifyAmount(choiceContext, visibility, -1m, Owner.Creature, this);
    }
}

/* GO TO GROUND — ⟨1⟩ Skill. Gain {Block} Block. Gain 1 Stealth.
   ⁉ NAME IS A PLACEHOLDER. Hallie called it "Stealth", but that name belongs to the power, and a card
   and a power sharing a display name makes the tooltips fight. Hers to mint. */
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

/* LOOK WHAT I FOUND DOWN HERE — ⟨1⟩ Skill. Gain {Block} Block. Convert all Stealth into Shivs.
   One of the three Stealth cash-outs (with Backstab and Sneak Attack). */
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

/* DAY OF INVISIBILITY — ⟨1⟩ Skill, Exhaust. Applies Unseen: your attacks this turn don't break Stealth.
   Upgrade: no Exhaust. */
public sealed class DayOfInvisibility() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword>() : new List<CardKeyword> { CardKeyword.Exhaust };

    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Unseen>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* PICKPOCKET — ⟨1⟩ Power. The first time you deal damage each turn, gain a Shiv (2 upgraded).
   ⁉ COST UNSPECIFIED by Hallie; priced at 1 as a per-turn trickle. Change freely. */
public sealed class Pickpocket() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PickpocketPower>(choiceContext, Owner.Creature, IsUpgraded ? 2m : 1m,
            Owner.Creature, this, false);
    }
}

/* DEAD NAME — ⟨2⟩ Power. Whenever you would gain Visibility, take a Dazed in your discard instead.
   The hard counter to the Stealth clock: Visibility never rises, so Stealth never degrades — paid for in deck
   clutter. Deliberately anti-synergistic with Honeypot: you cannot both refuse Visibility and farm it. */
public sealed class DeadName() : KnifeHeroCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    /* UPGRADE: INNATE. No permanent cost-reduction API exists in this engine — EnergyCost.AddThisCombat
       is combat-scoped and wrong for an upgrade. */
    public override int MaxUpgradeLevel => 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword> { CardKeyword.Innate } : new List<CardKeyword>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DeadNamePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* INTO THE STREETS — ⟨1⟩ Skill. Gain {Block} Block and {Vis} Visibility. Chosen visibility: like
   Gay Pride, DeadName does NOT intercept it (that only refuses the Visibility of being *found*). Armour
   for the loud build — feeds Honeypot, Dashing Strike, and the Visibility payoffs. */
public sealed class IntoTheStreets() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(10m, ValueProp.Move), new IntVar("Vis", 3m) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<Visibility>(choiceContext, Owner.Creature, DynamicVars["Vis"].BaseValue,
            Owner.Creature, this, false);
    }
}
