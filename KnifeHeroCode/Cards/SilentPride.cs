using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* SILENT PRIDE — ⟨1⟩ Attack. Retain. A Pride blade.

     HELD:  at the start of your turn, add a Shiv to your hand.
     SWUNG: deal {Damage} damage. Draw a card.

   (Hallie, 2026-07-13: *"The text on this has the discard thing. I think we need to do the shiv one. If
   it starts in your hand, add 1 shiv to your hand. When you attack, draw a card. That's it. That's the
   whole tweet."*)

   ── WHAT IT WAS, AND WHY IT WASN'T WORKING ─────────────────────────────────────────────────────
   It was carrying three riders — gain 3 Block whenever you discard, deal 8 + apply Weak + drop a Shiv in
   your discard, and permanently lose 1 damage each swing. A discard engine, a decay mechanic, and a
   debuff, on a common. It read like a paragraph and it depended on a discard economy the deck doesn't
   really have.

   Now it is the SHIV blade, and it says one thing per state:

     fly it   → a knife a turn, into your hand, where you can throw it
     swing it → it cuts, and it hands you the next card

   And the two states finally *want the same build* instead of fighting each other. Held, it feeds the go-
   wide shiv deck (and the relic queers the first Attack you create each turn — that's this Shiv, every
   turn, free). Swung, it draws, so it keeps the chain going. **The Silent doesn't make noise. It makes
   knives.**

   ⚠ TURN START, not end of turn. A Shiv added by WhileFlown (BeforeSideTurnEnd) would land in your hand
   and be discarded by the flush a moment later — Shivs don't Retain. Same trap as Watcher Pride's Fuel.
   The card has to hand you the knife when you can still throw it. */
public sealed class SilentPride() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    // HELD — a knife a turn, into your hand.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Pile?.Type != PileType.Hand || player != Owner) return;

        var shiv = CombatState.CreateCard<Shiv>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Hand, Owner);
    }

    // SWUNG — it cuts, and it hands you the next card.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
    }
}
