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
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE HEAT CARDS. Visibility is the clock on Stealth (see Stealth.cs) and is pure downside on its own. These
   are the three answers to it: run from it (Smoke Bomb, Shadow Dodge), refuse it (Dead Name), or feed it
   (Honeypot — the build where being found is the plan). */

/* HONEYPOT — ⟨2⟩ Power. Gain Thorns equal to your Visibility + {Bonus}. Turns Visibility from a punishment into a
   resource, which is what makes the loud build exist. */
public sealed class Honeypot() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "honeypot.png".CardImagePath();
    public override string CustomPortraitPath => "honeypot.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Thorns", 1m) };

    public override int MaxUpgradeLevel => 1;
    protected override void OnUpgrade() => DynamicVars["Thorns"].UpgradeValueBy(1m);   // 1 -> 2

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HoneypotPower>(choiceContext, Owner.Creature, DynamicVars["Thorns"].BaseValue,
            Owner.Creature, this, false);
    }
}

/* SMOKE BOMB — ⟨3⟩ (⟨2⟩ upgraded) Skill, Rare, Exhaust. Exhaust EVERY status card in your whole deck —
   hand, draw, AND discard. A one-shot cleanse, and unique: nothing else in the deck reaches your whole
   deck like this. Works on any Status, not just Visibility. */
public sealed class SmokeBomb() : KnifeHeroCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => "smoke_bomb.png".CardImagePath();
    public override string CustomPortraitPath => "smoke_bomb.png".BigCardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };   // a one-shot sweep

    public override int MaxUpgradeLevel => 1;
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);   // 3 -> 2

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var statuses = CardPile.GetCards(Owner, PileType.Hand, PileType.Draw, PileType.Discard)
            .Where(c => c.Type == CardType.Status && c != this).ToList();
        foreach (var s in statuses)
            await CardCmd.Exhaust(choiceContext, s, causedByEthereal: false);
    }
}

/* SHADOW DODGE — ⟨1⟩ Skill. Gain {Block} Block. Lose 1 Visibility. The cheap, repeatable cool-down. */
public sealed class ShadowDodge() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "shadow_dodge.png".CardImagePath();
    public override string CustomPortraitPath => "shadow_dodge.png".BigCardImagePath();

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        var status = CardPile.GetCards(Owner, PileType.Hand).FirstOrDefault(c => c.Type == CardType.Status && c != this);
        if (status != null) await CardCmd.Exhaust(choiceContext, status, causedByEthereal: false);
    }
}

/* GO TO GROUND — ⟨1⟩ Skill. Gain {Block} Block. Gain 1 Stealth.
   ⁉ NAME IS A PLACEHOLDER. Hallie called it "Stealth", but that name belongs to the power, and a card
   and a power sharing a display name makes the tooltips fight. Hers to mint. */
public sealed class GoToGround() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "go_to_ground.png".CardImagePath();
    public override string CustomPortraitPath => "go_to_ground.png".BigCardImagePath();

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
    public override string PortraitPath => "look_what_i_found_down_here.png".CardImagePath();
    public override string CustomPortraitPath => "look_what_i_found_down_here.png".BigCardImagePath();

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
    public override string PortraitPath => "day_of_invisibility.png".CardImagePath();
    public override string CustomPortraitPath => "day_of_invisibility.png".BigCardImagePath();

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
    public override string PortraitPath => "pickpocket.png".CardImagePath();
    public override string CustomPortraitPath => "pickpocket.png".BigCardImagePath();

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
    public override string PortraitPath => "dead_name.png".CardImagePath();
    public override string CustomPortraitPath => "dead_name.png".BigCardImagePath();

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);   // 2 -> 1

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
    public override string PortraitPath => "into_the_streets.png".CardImagePath();
    public override string CustomPortraitPath => "into_the_streets.png".BigCardImagePath();

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(10m, ValueProp.Move), new IntVar("Vis", 3m) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await Visibility.Add(choiceContext, Owner, (int)DynamicVars["Vis"].BaseValue, PileType.Hand);   // chosen → hand
    }
}

/* THE CLOSET — ⟨1⟩ Power. Gain 1 Stealth at the start of each turn. The passive hide-engine. */
public sealed class TheCloset() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "the_closet.png".CardImagePath();
    public override string CustomPortraitPath => "the_closet.png".BigCardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TheClosetPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* FLANK — ⟨0⟩ Skill. Lose all Stealth; gain {Per} Vigor per Stealth lost, plus {Bonus}.
   A Skill, so it clears Stealth by hand (the Attack-only auto-break doesn't fire) — and reads the bank
   before clearing it. */
public sealed class Flank() : KnifeHeroCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "flank.png".CardImagePath();
    public override string CustomPortraitPath => "flank.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Per", 2m), new IntVar("Bonus", 1m) };

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var stealth = Owner.Creature.GetPower<Stealth>();
        int lost = (int)(stealth?.Amount ?? 0m);
        if (stealth != null) await PowerCmd.Remove(stealth);

        decimal vigor = DynamicVars["Per"].BaseValue * lost + DynamicVars["Bonus"].BaseValue;
        if (vigor > 0m)
            await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, vigor, Owner.Creature, this, false);
    }
}

/* SNEAK ATTACK — ⟨1⟩ Attack. Lose all Stealth, deal {Per} per Stealth lost, then gain 1 Stealth.
   Read the bonus in OnPlay (before the swing's own auto-break clears Stealth), and re-seed the 1 in
   AfterCardPlayedLate so it survives that break — the same trick the Fade queer-rider uses. */
public sealed class SneakAttack() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override string PortraitPath => "sneak_attack.png".CardImagePath();
    public override string CustomPortraitPath => "sneak_attack.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Per", 3m) };

    protected override void OnUpgrade() => DynamicVars["Per"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int lost = (int)(Owner.Creature.GetPower<Stealth>()?.Amount ?? 0m);
        if (lost > 0)
            await DamageCmd.Attack((int)DynamicVars["Per"].BaseValue * lost).FromCard(this)
                .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        await PowerCmd.Apply<Stealth>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* ASSASSIN — ⟨2⟩ (1 upgraded) Power. Your Attacks deal 2 bonus damage per Stealth. The Stealth build's
   payoff: hide, stack the bank, then every swing scales with how hidden you are. */
public sealed class Assassin() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "assassin.png".CardImagePath();
    public override string CustomPortraitPath => "assassin.png".BigCardImagePath();

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);   // 2 -> 1

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AssassinPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, this, false);
    }
}

/* SMOKE BOMB, BUT ITS KNIVES — ⟨1⟩ Skill. Add {Shivs} Shivs to your hand; at the end of your turn,
   exhaust every Shiv in hand and gain 1 Stealth for each (SmokeKnivesPower). Cover made of blades. */
public sealed class SmokeBombKnives() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "smoke_bomb_knives.png".CardImagePath();
    public override string CustomPortraitPath => "smoke_bomb_knives.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Shivs", 4m) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await Shiv.CreateInHand(Owner, (int)DynamicVars["Shivs"].BaseValue, CombatState);
        await PowerCmd.Apply<SmokeKnivesPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
