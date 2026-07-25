using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   STABBY — ⟨1⟩ Attack. Retain. A Blade.

     Deal {Damage} damage. You may digest a Strike or Defend in your hand: it Exhausts, and this becomes
     more Top (from a Strike) or more Bottom (from a Defend).

   ── THE DIGEST NOW FEEDS AN ENCHANT (Hallie, 2026-07-24) ───────────────────────────────────────
   Stabby still eats your deck, but instead of just gaining Vigor/Block and upgrading, digesting a basic
   ENCHANTS STABBY ITSELF — a Strike makes it Top (Vigor a turn while held), a Defend makes it Bottom
   (Block a turn). One enchant slot, so Stabby commits to a lean on the first bite and DEEPENS it on each
   later one (same-type digests stack the enchant's amount; the opposite type can't overwrite it). The
   knife that eats your starting deck becomes a Top or a Bottom, sharpened by everything it swallowed.

   Digesting is still not a cost — you thin your deck AND feed the blade. See PrideEnchantment.cs (Top/
   Bottom). Retain keeps it in hand so its flag flies. */
public sealed class FancyFootwork() : KnifeHeroCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy), IBlade
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    private static bool IsBasic(CardModel c) =>
        c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await Digest(choiceContext);
    }

    /* THE DIGEST. Optional (Cancelable) — Stabby has Retain, so you can decline and keep the basic. */
    private async Task Digest(PlayerChoiceContext choiceContext)
    {
        if (!CardPile.GetCards(Owner, PileType.Hand).Any(c => c != this && IsBasic(c))) return;

        var prefs = new CardSelectorPrefs(new LocString("gameplay_ui", "CHOOSE_CARD_HEADER"), 1) { Cancelable = true };
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, c => c != this && IsBasic(c), this))
            .FirstOrDefault();
        if (chosen == null) return;

        bool wasStrike = chosen.Tags.Contains(CardTag.Strike);
        decimal worth = Worth(chosen, wasStrike);
        await CardCmd.Exhaust(choiceContext, chosen, causedByEthereal: false);

        // Enchant Stabby with what it ate. Same-type stacks the amount; the other type can't overwrite it.
        if (wasStrike) CardCmd.Enchant<Top>(this, worth);
        else CardCmd.Enchant<Bottom>(this, worth);
    }

    private static decimal Worth(CardModel card, bool wasStrike) =>
        card.DynamicVars.TryGetValue(wasStrike ? "Damage" : "Block", out var v) ? v.BaseValue : 0m;
}
