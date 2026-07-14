using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Extensions;
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
   Hallie: OK. Once more. THIS TIME. Lets get rid of it and just call iy "Stabby". On play, it still CAN absorb a strike or defend.
   If it does so, you gain block or vigor equal to the amount gained and it upgrades once. On retain, it gives you U + 2 block, on attack it does 8 + U damage and the above absorb, option.
   That's it. Thats the whole tweet. Top chop and bottom blade become their own cards. No Exhaust, no nothing, better block on bottom blade, better templating on the instructions?

   SWITCH BLADE — ⟨1⟩ Attack.

     Deal 6 (9) damage. Exhaust a Strike or Defend in your hand and forge a Pride Blade.

       a Strike → a TOP CHOP
       a Defend → a BOTTOM BLADE

   That's the card. It cuts, it eats one of your basics, and it hands you the blade that basic wanted to
   become. It does not exhaust itself. It does not make more of itself.

   ── WHY IT TOOK SO LONG (Hallie, 2026-07-13: "it's time to bite the bullet") ───────────────────
   Every earlier version tried to be the whole engine on one card — damage, forge on play, forge a
   *different* thing if held, recruit a replacement out of the discard, exhaust itself. Four clauses, two
   branches, a paragraph of text. Clever, and unreadable. She told me so three times in three different
   ways before I heard it.

   The card is a **switch**. So the switch is *which basic you feed it*, and that is the only decision on
   the card. It reads in one breath and the joke survives intact.

   ── AND YOUR BASICS ARE NOT SPENT ─────────────────────────────────────────────────────────────
   Look what this does with the relic. Exhausting a Strike or a Defend makes the relic return it to your
   draw pile, **queer** — and a different queer every time.

   **You put your basics through the wash. They come out other. And you get a knife.**

   That is the whole thesis of the character, running as one motion, on one card. Your basics are not
   dead weight to be deleted. They are the medium you sculpt.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */
public sealed class FancyFootwork() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    // No Exhaust, no Retain. It's a tool, and you keep it.
    public override IEnumerable<CardKeyword> CanonicalKeywords => new List<CardKeyword>();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    private static bool IsBasic(CardModel c) =>
        c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        // The forge is a bonus, not a requirement — the card still cuts if you have no basic to feed it.
        if (!CardPile.GetCards(Owner, PileType.Hand).Any(c => c != this && IsBasic(c))) return;

        var prefs = new CardSelectorPrefs(new LocString("gameplay_ui", "CHOOSE_CARD_HEADER"), 1);
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs,
            c => c != this && IsBasic(c), this)).FirstOrDefault();
        if (chosen == null) return;

        bool wasStrike = chosen.Tags.Contains(CardTag.Strike);

        // Exhausting it is what hands it to the relic — which hands it back to you queer.
        await CardCmd.Exhaust(choiceContext, chosen, causedByEthereal: false);

        if (wasStrike) await CombatState.AddOrUpgradeFlagBlade<TopChop>(Owner);
        else           await CombatState.AddOrUpgradeFlagBlade<BottomBlade>(Owner);
    }
}
