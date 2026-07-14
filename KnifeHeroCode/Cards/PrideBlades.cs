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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE PRIDE BLADES — Prides named for the Slay the Spire characters, each carrying that character's
   engine as a thing you hold rather than a power you have. See PrideCard.cs for the fly/swing mechanic
   and GAY_BLADE_2.0.md for the design. */


/* IRONCLAD PRIDE — the exhaust engine.
     HELD:  bank 3 damage per card you exhaust; it all lands at end of turn as one hit.
     SWUNG: deal {Damage} and apply Vulnerable.

   ⚠ The banking is deliberate. It used to fire a separate 3-damage hit per exhausted card, which in a
   deck that exhausts constantly was an unreadable machine-gun of tiny attacks. Same damage; legible. */
public sealed class IroncladPride() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    /* ⚠ Every number a card PRINTS must be a DynamicVar, never a private const — a hardcoded number in
       the loc string stays frozen after upgrade and the card lies to the player, invisibly. */
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

/* WATCHER PRIDE — ⟨1⟩ Skill. Retain.
     HELD:  at the start of your turn, add a Fuel to your hand. (Fuel ⟨0⟩: gain 1 Energy, draw 1. Exhaust.)
     SWUNG: draw {Draw}.

   A held Pride costs you tempo (a hand slot), so the held effect gives tempo back. An earlier "draw 2,
   discard 1" gave nothing net and was not worth the slot.

   ⚠ THE HELD EFFECT FIRES AT TURN START, NOT TURN END. A card added to hand by WhileFlown
   (BeforeSideTurnEnd) is immediately discarded by the end-of-turn flush unless it Retains — and Fuel
   doesn't. The card would silently do nothing. Same trap as Silent Pride's Shiv. */
public sealed class WatcherPride() : PrideCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Draw", 3m) };

    protected override void OnUpgrade() => DynamicVars["Draw"].UpgradeValueBy(1m);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Pile?.Type != PileType.Hand || player != Owner) return;

        var fuel = CombatState.CreateCard<Fuel>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(fuel, PileType.Hand, Owner);
    }

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);
    }
}



/* REGENT PRIDE — ⟨2⟩ Attack, Rare. Retain.
     COST:  exhausts another Pride from your hand. Unplayable without one.
     SWUNG: deal {Damage} — and every turn after, deal 6 and gain 6 Block for the rest of the fight.

   The only card that CONSUMES a Pride, so it pulls against Stonewall / Pride Parade (which want you
   swinging them) and Knife Block (which wants you holding them). */
public sealed class RegentPride() : PrideCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    /* UPGRADE: INNATE. There is no permanent cost-reduction API in this engine — EnergyCost.AddThisCombat
       is combat-scoped and wrong for an upgrade. */
    public override int MaxUpgradeLevel => 1;

    /* ⚠ RETAIN MUST BE RE-ADDED HERE. Overriding CanonicalKeywords replaces PrideCard's list, and
       without Retain this is the one Pride in the deck you cannot hold — silently, with nothing failing.
       (It shipped that way once, copy-pasted from DeadName/BothIsGood, which are not PrideCards.) */
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded
            ? new List<CardKeyword> { CardKeyword.Retain, CardKeyword.Innate }
            : new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(10m, ValueProp.Move) };

    protected override bool IsPlayable =>
        CardPile.GetCards(Owner, PileType.Hand).Any(c => c is IPride && c != this);

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        var court = CardPile.GetCards(Owner, PileType.Hand)
            .FirstOrDefault(c => c is IPride && c != this);
        if (court != null)
            await CardCmd.Exhaust(choiceContext, court, causedByEthereal: false);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        await PowerCmd.Apply<RegentPridePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}


/* DYKE PRIDE — the labrys, the parry.
     HELD:  HP loss is voided, and the blade banks that much extra damage.
     SWUNG: deal {Damage} — 6, plus everything it has drunk.

   The only Pride that wants you to be hit, so it pairs with Stealth and Honeypot. */
public sealed class DykePride() : PrideCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    // HELD — void the HP loss, bank what it was worth as permanent damage on this card.
    public override decimal ModifyHpLostBeforeOsty(MegaCrit.Sts2.Core.Entities.Creatures.Creature target,
        decimal amount, ValueProp props, MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer,
        CardModel? cardSource)
    {
        if (Pile?.Type != PileType.Hand || target != Owner.Creature || amount <= 0m) return amount;
        DynamicVars.Damage.UpgradeValueBy(amount);
        return 0m;
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
    /* UPGRADE: INNATE. No permanent cost-reduction API exists in this engine — EnergyCost.AddThisCombat
       is combat-scoped and wrong for an upgrade. (Not a PrideCard, so no Retain to preserve.) */
    public override int MaxUpgradeLevel => 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? new List<CardKeyword> { CardKeyword.Innate } : new List<CardKeyword>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BothIsGoodPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
