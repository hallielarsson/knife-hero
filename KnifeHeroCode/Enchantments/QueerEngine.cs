using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Enchantments;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   THE QUEER ENGINE — queering IS enchanting. (Hallie, 2026-07-24.)

   The old QueerMod rider system is gone. To "queer" a card is now to apply a RANDOM enchantment from a
   small pool — which is exactly what the EverythingIMakeIsQueer relic's flagged TODO always pointed at
   ("a Queered card gains one existing card quality at random — Sharp, Sly, Thorns…"). The pool is mostly
   real base-game enchantments plus two of ours; each brings its own hover tip and icon, so nothing has to
   print effect text on the card face.

   `Is(card)` = the card carries any enchantment. That is the single axis the whole deck counts — Pride
   flags and queer cards are one thing now: enchanted cards. See the flag-economy counters. */
public static class Queer
{
    private readonly record struct Option(int Weight, Func<CardModel, bool> Can, Action<CardModel> Enchant);

    private static Option Opt<T>(int weight, decimal amount) where T : EnchantmentModel =>
        new(weight, c => ModelDb.Enchantment<T>().CanEnchant(c), c => CardCmd.Enchant<T>(c, amount));

    /* THE POOL. Simple names; Clone is rare. Base-game enchants live in Core.Models.Enchantments; Thorns
       and Clone are ours (below). Each option's Can() is the enchant's own CanEnchant, so a queering only
       ever lands something legal for that card (Sharp/Inky→attacks, Nimble→block cards). */
    private static readonly List<Option> Pool = new()
    {
        Opt<Sharp>(3, 3m),        // +3 damage (attacks)
        Opt<Nimble>(3, 4m),       // +4 block (block cards)
        Opt<Swift>(2, 1m),        // draw a card when played
        Opt<Steady>(2, 1m),       // Retain
        Opt<Inky>(2, 1m),         // +damage & Weak (attacks)
        Opt<Thorns>(2, 2m),       // gain Thorns when played
        Opt<Clone>(1, 1m),        // play it → add a copy to hand   ⭐ rare
    };

    /// True if the card is queer — i.e. it carries any enchantment. THE axis the flag economy counts.
    public static bool Is(CardModel card) => card.Enchantment != null;

    /* Queer a card: enchant it with a random legal pool member, and show it. One slot per card, so a card
       that's already enchanted is left alone (you don't re-queer what's already other). */
    public static void Apply(CardModel card, Player player)
    {
        if (card.Enchantment != null) return;

        var rng = player.RunState.Rng.CombatCardGeneration;
        var options = Pool.Where(o => o.Can(card)).ToList();
        if (options.Count == 0) return;

        // Weight by repetition, then pick uniformly (the rng exposes NextItem, not a ranged int).
        var weighted = options.SelectMany(o => Enumerable.Repeat(o, o.Weight)).ToList();
        var chosen = rng.NextItem(weighted);

        chosen.Enchant(card);
        ShowVisual(card);
    }

    // The clear visual: the enchant badge lands on the card with the game's own enchant VFX. Cosmetic, so
    // it can never throw — a missing node must not take down a combat.
    private static void ShowVisual(CardModel card)
    {
        try { NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(NCardEnchantVfx.Create(card)); }
        catch (Exception e) { MainFile.Logger.Error($"Queer VFX failed on {card.Id}: {e}"); }
    }
}

/* THORNS — a queer enchant: when you play this card, gain {Amount} Thorns. (No base-game Thorns enchant
   exists, so it's ours.) Hover only — no card-face text. */
public sealed class Thorns : CustomEnchantmentModel
{
    protected override string? CustomIconPath => "thorns.png".EnchantmentImagePath();
    public override bool ShowAmount => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new List<IHoverTip> { HoverTipFactory.FromPower<ThornsPower>() };

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card.Owner == null) return;
        await PowerCmd.Apply<ThornsPower>(choiceContext, Card.Owner.Creature, Amount, Card.Owner.Creature, Card, false);
    }
}

/* CLONE — the rare queer enchant: when you play this card, add a copy of it to your hand. The copy is
   stripped of the enchantment so it doesn't clone forever. Hover only. */
public sealed class Clone : CustomEnchantmentModel
{
    protected override string? CustomIconPath => "clone.png".EnchantmentImagePath();

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card.Owner == null) return;
        var copy = Card.CreateClone();
        CardCmd.ClearEnchantment(copy);   // the copy is NOT queer — no infinite clones
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Card.Owner);
    }
}
