using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Extensions;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Enchantments;

/* PRIDE ENCHANTMENTS — the flag, unbundled.

   A Pride used to be a two-state card (held clause + swung clause + cash-out — see PrideCard.cs).
   Hallie's 2026-07-24 revelation: the HELD clause is the whole juice, and the base game already has a
   system for "bolt a persistent effect onto a card" — Enchantments, the thing the Questing-Beast-style
   bosses use to make your cards cost more / exhaust / only-one-per-turn. So a Pride becomes a card that
   ENCHANTS one of your Attacks with its held effect, then leaves.

   Why the enchant system and not the QueerMod rider we already have: enchantments come with native UI for
   free — an icon on the card, hover tips, the effect printed on the card face (HasExtraCardText), a
   gold/red glow, and a land-VFX. That is the point: it atomizes the learning curve, because the card
   SHOWS what it now does. Queering stays its own separate thing (the relic's random rider); this is its
   deliberate, chosen cousin.

   ── SCOPE ──────────────────────────────────────────────────────────────────────────────────────────
   COMBAT-SCOPED. We enchant the in-combat card instance and never its DeckVersion, so the flag lasts the
   fight and is gone next combat. (Goopy DOES write through to DeckVersion to persist — we deliberately
   don't.) No ClearEnchantment needed: combat cards are regenerated from the deck each fight.

   IN-HAND ONLY. The held effect fires only while the enchanted card is in your hand (gate on
   Card.Pile == Hand), exactly like the old PrideCard.WhileFlown. And a Pride is unplayable with no Attack
   in hand — which is the fun: sometimes you fly the flag on a sub-optimal blade because that's what
   you're holding. */
public abstract class PrideEnchantment : CustomEnchantmentModel
{
    // A Pride flies on an Attack. (Same restriction Sharp uses.)
    public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

    // Print the held effect on the card face + show its number.
    public override bool HasExtraCardText => true;
    public override bool ShowAmount => true;

    /* THE UPGRADE AXIS. Hallie, 2026-07-24: "the upgrade effect for these powers will often add Retain."
       An un-upgraded Pride flies its flag only while the enchanted Attack happens to be in your hand;
       upgraded, it grants Retain so the flag STAYS in hand and ticks every turn. Persisted (StS2 can save
       mid-combat): on load, ModifyCard() re-runs OnEnchant and re-adds the keyword. */
    [SavedProperty(SerializationCondition.SaveIfNotTypeDefault)]
    public bool GrantsRetain { get; set; }

    protected override void OnEnchant()
    {
        if (GrantsRetain) Card.AddKeyword(CardKeyword.Retain);
    }

    // Each flag glosses its own keyword/power here (e.g. Bound → Doom). Base folds in a Retain tip when
    // this flag grants it (the card face auto-glosses Retain once it's on the card; this is the panel tip).
    protected virtual IEnumerable<IHoverTip> FlagTips => System.Array.Empty<IHoverTip>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>(FlagTips);
            if (GrantsRetain) tips.Add(HoverTipFactory.FromKeyword(CardKeyword.Retain));
            return tips;
        }
    }

    /// True if this card is currently flying a Pride flag. Payoffs (Knife Block, Solidarity, Gay Wrath)
    /// count these alongside IPride / Queer — an enchanted Strike IS a flag in your hand.
    public static bool IsFlag(CardModel card) => card.Enchantment is PrideEnchantment;

    // The held clause only ticks while the enchanted card is in hand, on YOUR turn.
    protected bool FlyingInHand(CombatSide side) =>
        side == CombatSide.Player && Card.Owner != null && Card.Pile?.Type == PileType.Hand;

    /* ── THE APPLIER HELPERS ─────────────────────────────────────────────────────────────────────────
       Every Pride is a Power you play to enchant one of your Attacks. Shared here so all Prides target
       the same way: any Attack you own in the combat — HAND, DRAW, or DISCARD — so a bad hand never
       strands the flag (Hallie, 2026-07-24: "more flexibility of what attack you apply it to"). The
       held effect still only fires while the enchanted card is in your HAND, so pre-enchanting a card in
       draw/discard is a deliberate set-up, not a free ride. */
    private static readonly PileType[] Owned = { PileType.Hand, PileType.Draw, PileType.Discard };

    public static bool HasEnchantableAttack(Player player, CardModel source) =>
        CardPile.GetCards(player, Owned).Any(c => c.Type == CardType.Attack && c != source);

    public static async Task<CardModel?> ChooseAttack(PlayerChoiceContext ctx, Player player, CardModel source)
    {
        var attacks = CardPile.GetCards(player, Owned).Where(c => c.Type == CardType.Attack && c != source).ToList();
        if (attacks.Count == 0) return null;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        return (await CardSelectCmd.FromSimpleGrid(ctx, attacks, player, prefs)).FirstOrDefault();
    }

    /* THE CASH-OUT. Apply flag T to `card`, and — if the applier was upgraded — grant Retain so the flag
       stays in hand and flies every turn. The one place every Pride enchants; a new Pride calls this and
       inherits the whole contract (retain-on-upgrade, the correct ModifyCard re-run) for free. Returns the
       applied enchant so a caller can tweak it further. */
    public static T? Bestow<T>(CardModel card, decimal amount, bool grantRetain) where T : PrideEnchantment
    {
        var e = CardCmd.Enchant<T>(card, amount);
        if (e != null && grantRetain)
        {
            e.GrantsRetain = true;
            e.ModifyCard();   // re-runs OnEnchant → adds Retain to the enchanted Attack
        }
        return e;
    }
}

/* PROUD — Gay Pride's flag. While the enchanted Attack is in your hand, gain {Amount} Visibility at end
   of turn. (Gay Pride's old HELD clause, verbatim — see GayPride.cs. Granted via PowerCmd directly, NOT
   through Stealth's found-you path, so Dead Name does not intercept it: this is visibility you chose.) */
public sealed class Proud : PrideEnchantment
{
    protected override string? CustomIconPath => "proud.png".EnchantmentImagePath();

    /* THE PAYOFF. The enchanted blade carries your Visibility as bite: bonus damage equal to your current
       Visibility. This is why the flag climbing Visibility each turn is a GOOD thing and not just the
       found-clock ticking up — being out and loud makes this knife hit harder. (Gay Pride 1.0 was "deal
       damage equal to your Visibility"; that payoff now rides the card you enchant.) Same native hook
       Sharp uses, so the card previews the larger number. */
    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        if (!props.IsPoweredAttack()) return 0m;
        return Card.Owner?.Creature.GetPower<Visibility>()?.Amount ?? 0m;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!FlyingInHand(side)) return;
        await PowerCmd.Apply<Visibility>(choiceContext, Card.Owner!.Creature, Amount,
            Card.Owner.Creature, Card, false);
    }
}

/* SLAY — Necrobinder Pride's flag. On retain (end of your turn, while the enchanted Attack is in your
   hand), apply Doom to a random enemy equal to the number of Pride-enchanted cards in your hand (this one
   counts itself). Doom kills an enemy whose HP is at or below its stacks at end of turn — so a wide hand
   of flags marks a random enemy for death. (The name slays two ways: it kills, and it *slays*. — Hallie,
   2026-07-24.) */
public sealed class Slay : PrideEnchantment
{
    protected override string? CustomIconPath => "slay.png".EnchantmentImagePath();

    // The amount is dynamic (the live flag count), so no fixed number to show; the card text states it.
    public override bool ShowAmount => false;

    protected override IEnumerable<IHoverTip> FlagTips =>
        new List<IHoverTip> { HoverTipFactory.FromPower<DoomPower>() };

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!FlyingInHand(side)) return;

        int flags = CardPile.GetCards(Card.Owner!, PileType.Hand).Count(IsFlag);
        if (flags <= 0) return;

        // ⚠ Route CombatState through Creature, NOT Card.CombatState — the shipped build dropped
        // CardModel.CombatState (throws MissingMethodException → hangs the turn). KnifeHeroCard shims this,
        // but our Card here is a raw CardModel, so we must use the live path ourselves. See KnifeHeroCard.
        var combat = Card.Owner.Creature.CombatState;
        if (combat == null) return;
        var enemies = combat.HittableEnemies.ToList();
        if (enemies.Count == 0) return;

        var target = Card.Owner.RunState.Rng.CombatCardGeneration.NextItem(enemies);
        await PowerCmd.Apply<DoomPower>(choiceContext, target, flags, Card.Owner.Creature, Card, false);
    }
}

/* SHADY — Silent Pride's flag. While the enchanted Attack is in your hand, put a Shiv on TOP of your
   draw pile at the end of your turn (so you draw it next turn). And when you play the enchanted Attack,
   draw a card. The silent knife that keeps the knives coming. (Hallie, 2026-07-24 — kept simple over
   "draw per shiv played"; name is easily changed.)

   ⚠ The Shiv goes to the DRAW PILE, not the hand: a card added to hand at end of turn is flushed by the
   turn-end discard (Shivs don't Retain — the trap that bit Silent Pride 1.0). The draw pile is safe. */
public sealed class Shady : PrideEnchantment
{
    protected override string? CustomIconPath => "shady.png".EnchantmentImagePath();
    public override bool ShowAmount => false;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!FlyingInHand(side)) return;
        var combat = Card.Owner!.Creature.CombatState;
        if (combat == null) return;
        var shiv = combat.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Shiv>(Card.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Draw, Card.Owner, CardPilePosition.Top);
    }

    // When the enchanted Attack is played, draw a card. (The enchant's own OnPlay fires for its host.)
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card.Owner == null) return;
        await CardPileCmd.Draw(choiceContext, 1m, Card.Owner);
    }
}
