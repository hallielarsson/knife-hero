using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   STABBY — ⟨1⟩ Attack. Retain. A Blade.

     Deal {Damage} damage. You may enchant a Strike or Defend in your hand with Top (from a Strike) or
     Bottom (from a Defend).

   Stabby doesn't EAT your basics any more — it queers them. The Strike or Defend stays in your hand and
   in your deck, now flying a flag: while it's held at end of turn it gives you {Flag} Vigor (Top) or
   {Flag} Block (Bottom). Your boring cards become the engine instead of becoming fuel.

   ── WHY THE REWRITE (2026-08-01) ────────────────────────────────────────────────────────────────
   The digest version hung the game two ways, both fixed here:

   1. CardCmd.Enchant THROWS when the target already carries an enchantment and the enchant isn't
      IsStackable — and EnchantmentModel.IsStackable defaults to false. So the SECOND digest onto an
      already-flagged Stabby threw inside the async play and stalled the turn. Top and Bottom are now
      IsStackable (see PrideEnchantment.cs), and we still gate every application on CanEnchant so a
      card wearing somebody else's flag is simply not offered.

   2. CardSelectorPrefs.Cancelable is honored ONLY by the out-of-combat deck screens
      (NDeckCardSelectScreen and friends). The in-combat hand selector ignores it outright and enables
      its confirm button only once _selectedCards.Count >= MinSelect — so "you may decline" with
      MinSelect 1 was a promise the UI could not keep, and declining meant sitting there forever. The
      optional choice is now spelled MinSelect 0 / MaxSelect 1, which the hand selector DOES understand:
      confirm is live with nothing picked, so skipping is a real button. */
public sealed class FancyFootwork() : KnifeHeroCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy), IBlade
{
    public override string PortraitPath => "fancy_footwork.png".CardImagePath();
    public override string CustomPortraitPath => "fancy_footwork.png".BigCardImagePath();

    /* The flag's size. Deliberately not on the upgrade axis — Stabby upgrades its damage, and the flag
       stays a flat, readable 2 no matter how many blades are flying it. */
    private const decimal FlagAmount = 2m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    // Stabby grants either flag, so it glosses both: hover it and you see what Top and Bottom do.
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        PrideEnchantment.TipFor<Top>(FlagAmount).Concat(PrideEnchantment.TipFor<Bottom>(FlagAmount));

    private static bool IsStrike(CardModel c) => c.Tags.Contains(CardTag.Strike);
    private static bool IsDefend(CardModel c) => c.Tags.Contains(CardTag.Defend);

    /* Can this basic actually take its flag? Gates on the enchant system's own CanEnchant, which rejects
       a card already wearing a different enchantment. Without this, Enchant() throws mid-play. */
    private static bool CanFlag(CardModel c) =>
        (IsStrike(c) && ModelDb.Enchantment<Top>().CanEnchant(c)) ||
        (IsDefend(c) && ModelDb.Enchantment<Bottom>().CanEnchant(c));

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await Flag(choiceContext);
    }

    /* THE FLAGGING. Optional: MinSelect 0 / MaxSelect 1 gives the hand selector a live confirm button
       with nothing chosen, so declining is one click and never a stall. (Do NOT reach for Cancelable —
       the combat hand selector ignores it. See the header note.) */
    private async Task Flag(PlayerChoiceContext choiceContext)
    {
        if (!CardPile.GetCards(Owner, PileType.Hand).Any(c => c != this && CanFlag(c))) return;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, 1);
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, c => c != this && CanFlag(c), this))
            .FirstOrDefault();
        if (chosen == null) return;

        // A Strike flies Top, a Defend flies Bottom. Flag it a second time and the flag deepens.
        if (IsStrike(chosen)) CardCmd.Enchant<Top>(chosen, FlagAmount);
        else CardCmd.Enchant<Bottom>(chosen, FlagAmount);
    }
}
