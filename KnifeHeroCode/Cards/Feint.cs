using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Cards;

/* FEINT — ⟨0⟩ Skill. Apply 1 Weak. Gain 1 Stealth.
   In the starting deck (Hallie, 2026-07-12).

   Free, so it always fits. It's the card that teaches the hidden build in the first fight: you make them
   swing at where you were, and slip out of the way. Weak on them, cover for you, no energy spent.

   Note it does NOT deal damage — so it doesn't break your own Stealth. That's the whole point of it, and
   it's the first thing a player will learn about the mechanic without being told.

   ⁉ FLAGGED — Hallie specified "one card that gives 1 stealth and 1 weak for 0" but didn't name it.
   "Feint" is a placeholder; hers to mint. */
public sealed class Feint() : KnifeHeroCard(0, CardType.Skill, CardRarity.Basic, TargetType.AnyEnemy)
{
    // Already free, so the upgrade buys cover rather than cost: 1 Stealth → 2.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Weak", 1m), new IntVar("Stealth", 1m) };

    protected override void OnUpgrade() => DynamicVars["Stealth"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target,
            DynamicVars["Weak"].BaseValue, Owner.Creature, this, false);
        await PowerCmd.Apply<Stealth>(choiceContext, Owner.Creature,
            DynamicVars["Stealth"].BaseValue, Owner.Creature, this, false);
    }
}
