using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Cards;

/* TOP CHOP — Hallie's design, simplified 2026-07-12. Forged by PLAYING a Switch Blade.

     Retain. Gain (2 × forge level) Vigor.

   The Top gives you ATTACK, the Bottom gives you BLOCK. That's the whole pair, and it's the cleanest
   this engine has been: a Switch Blade becomes a **Top** if you *play* it and a **Bottom** if you *hold*
   it. The card is a switch.

   RETAIN IS THE POINT (Hallie, 2026-07-12: "Both gain Retain — you were right"). You carry the blade
   until the turn you actually want the power, because Vigor is worth most on the turn you spend it. And
   re-forging a blade you're holding raises its level — and the level IS the payout. So it's the deck's
   question again: bank it, or cash it?

   ⁉ FLAGGED — "U x 2 vigor" is ambiguous about the base. Built as **2 × (forgeLevel + 1)**, so a fresh
   Top gives 2 Vigor, a once-re-forged one gives 4, twice 6. (A literal level × 2 would give 0 on the
   first forge.) One line to change if that's wrong. */
public sealed class TopChop() : PrideCard(1, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    // "Upgrade ∞" — re-forging a held blade raises its level, and the level is the payout.
    public override int MaxUpgradeLevel => 99;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain, CardKeyword.Exhaust };

    private decimal Payout => 2m * (CurrentUpgradeLevel + 1);

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, Payout, Owner.Creature, this, false);
    }
}
