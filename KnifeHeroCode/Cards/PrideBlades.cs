using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE PRIDE BLADES — built from Hallie's margin notes (rescued into GAY_BLADE_2.0.md before the old
   Power versions were deleted). Each one is a flag you can FLY or SWING:

     HELD  — it sits in your hand doing a passive thing. It costs you a hand slot. You're flying it.
     SWUNG — it cashes out and leaves, and you get your hand back.

   They are named for the Slay the Spire characters, and each one steals that character's engine — but
   as a thing you *carry*, not a power you *have*. Silent pays you for discarding. Ironclad pays you for
   exhausting. Watcher pays you for looking. Regent eats the others.

   And they all feed PridesPlayed, which is what Stonewall and Pride Parade count. Every flag you swing
   makes the wall higher. */


/* IRONCLAD PRIDE — the exhaust engine, carried.
     HELD:  deal 3 damage to a random enemy whenever you exhaust a card.
     SWUNG: deal 10 and apply Vulnerable.

   The mirror of Silent Pride: Silent wants you throwing cards away, Ironclad wants you burning them.
   Hold both and every card that leaves your hand pays you twice. */
public sealed class IroncladPride() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    /* All three numbers are DynamicVars, not private consts — so the card TEXT reads them and can't
       lie after an upgrade. (A hardcoded "3" in the loc string stays 3 forever, even when the code says
       4. That's a card that lies to the player, and it's invisible until someone upgrades one.) */
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar>
        {
            new DamageVar(10m, ValueProp.Move),
            new IntVar("Burn", 3m),
            new IntVar("Vuln", 2m),
        };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Burn"].UpgradeValueBy(1m);
        DynamicVars["Vuln"].UpgradeValueBy(1m);
    }

    private decimal BurnDamage => DynamicVars["Burn"].BaseValue;
    private decimal VulnerableAmount => DynamicVars["Vuln"].BaseValue;

    /* HELD — the flag BANKS the burn and lets it all go at once, at the end of your turn.
       (Hallie, post-playtest 2026-07-12: "Ironclad Pride should bank the exhaust attacks for the end of
       turn.") She's right: it used to fire a separate little 3-damage hit for every single card you
       exhausted, which in a deck that exhausts constantly meant a stuttering machine-gun of tiny attacks
       that you couldn't read and couldn't plan around.

       Banking it makes it ONE number you watch climb — you can see the pyre building — and it lands as a
       single hit you can actually aim your turn around. Same damage, legible. */
    private int _banked;

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (Pile?.Type == PileType.Hand && card.Owner == Owner) _banked++;
        return Task.CompletedTask;
    }

    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        if (_banked <= 0) return;
        int burn = _banked; _banked = 0;

        var enemy = CombatState.HittableEnemies.FirstOrDefault();
        if (enemy == null) return;
        await DamageCmd.Attack(BurnDamage * burn).FromCard(this).Targeting(enemy)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, VulnerableAmount,
            Owner.Creature, this, false);
    }
}


/* WATCHER PRIDE — the sight.
     HELD:  at the start of your turn, draw 2 and discard 1.
     SWUNG: draw 3.

   Hallie's note replaced the old Watcher outright ("draw two cards and discard one at the beginning of
   your turn"), because the old one GRANTED RETAIN to a random card — and in a deck where hand space is
   the currency, handing out Retain for free is not a gift, it's making a random card sticky. This one
   is a real Watcher: you see more, and you throw away what you don't want. It also feeds Silent Pride,
   which pays you for discarding. */
public sealed class WatcherPride() : PrideCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Draw", 3m) };

    protected override void OnUpgrade() => DynamicVars["Draw"].UpgradeValueBy(1m);

    private decimal DrawOnSwing => DynamicVars["Draw"].BaseValue;

    // HELD — you're looking. Draw two, keep one.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Pile?.Type != PileType.Hand || player != Owner) return;
        await CardPileCmd.Draw(choiceContext, 2m, Owner);

        var prefs = new CardSelectorPrefs(
            new MegaCrit.Sts2.Core.Localization.LocString("gameplay_ui", "CHOOSE_CARD_DISCARD_HEADER"), 1);
        var chosen = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
        foreach (var c in chosen)
            await CardCmd.Discard(choiceContext, c);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DrawOnSwing, Owner);
    }
}


/* REGENT PRIDE — the one that eats its own court.
     COST: exhausts another Pride blade from your hand when you play it. It cannot be played otherwise.
     HELD: nothing. It is not a flag you fly; it is a thing you feed.
     SWUNG: each turn after, deal 6 to an enemy and gain 6 Block, for the rest of the fight.

   Hallie: "Regent works as is as long as it applies to Pride Blades instead of Pets."

   So the Regent is the payoff for having built a court of flags — and the price is one of them. It's the
   only card in the deck that *consumes* a Pride, which means it's in direct tension with Stonewall and
   Pride Parade (which want you swinging them) and with Knife Block (which wants you holding them). You
   have to decide what your prides are FOR. */
public sealed class RegentPride() : PrideCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    /* UPGRADE: INNATE — you start the fight holding it.
       (There is no permanent cost-reduction API in this engine; EnergyCost.AddThisCombat is combat-
       scoped and wrong for an upgrade. Innate is the right upgrade for an expensive Power anyway:
       the problem with a 2-cost Power is never its cost, it's that you draw it on turn 4.) */
    public override int MaxUpgradeLevel => 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword> { CardKeyword.Innate } : new List<CardKeyword>();

    // Unplayable unless there's another Pride in hand to sacrifice. The court feeds the crown.
    protected override bool IsPlayable =>
        CardPile.GetCards(Owner, PileType.Hand).Any(c => c is IPride && c != this);

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var court = CardPile.GetCards(Owner, PileType.Hand)
            .FirstOrDefault(c => c is IPride && c != this);
        if (court != null)
            await CardCmd.Exhaust(choiceContext, court, causedByEthereal: false);

        await PowerCmd.Apply<RegentPridePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}


/* DYKE PRIDE — the labrys.
     HELD:  the next hit that would take your HP is voided, and the blade banks that much damage.
     SWUNG: deal 6, plus everything it has drunk.

   The parry. It stands in front of you and drinks a blow, and it gets heavier for it. Hold it and it's a
   shield that's slowly turning into a sword; swing it and you give back everything they gave you.

   It is the only Pride that WANTS you to be hit, which makes it the natural partner for Stealth (where
   getting hit is cheap) and for Honeypot (where getting hit is the plan). */
public sealed class DykePride() : PrideCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    // HELD — drink the blow. Void the HP loss, and keep what it was worth.
    public override decimal ModifyHpLostBeforeOsty(MegaCrit.Sts2.Core.Entities.Creatures.Creature target,
        decimal amount, ValueProp props, MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer,
        CardModel? cardSource)
    {
        if (Pile?.Type != PileType.Hand || target != Owner.Creature || amount <= 0m) return amount;
        DynamicVars.Damage.UpgradeValueBy(amount);   // the axe gets heavier
        return 0m;                                    // and you don't feel a thing
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}

/* BOTH IS GOOD — ⟨2⟩ Power, Rare.
   The first Pride you play each turn also fires its held effect. See BothIsGoodPower. */
public sealed class BothIsGood() : KnifeHeroCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
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
        await PowerCmd.Apply<BothIsGoodPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
