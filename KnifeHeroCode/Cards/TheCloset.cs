using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* The Closet — Hallie's design, reworked into a *maintained posture* (whetstone 2026-06-15,
   see Closeted.cs / WHETSTONE-the-closet.md). Gain 3 Stealth and become Closeted:
     - At the start of each turn the closet collects rent — discard a card to void the next
       instance of HP loss (a Buffer charge); an empty hand breaks the closet for a Dazed.
     - Playing an Attack blows your cover: you lose all Stealth and the posture ends.
   High defense, zero offense; staying hidden has an upkeep cost. Human-sourced mechanic
   (Hallie); placeholder art via KnifeHeroCard. */
/* THE CLOSET — ⟨1⟩ Gain 3 Stealth. You cannot play Attacks this turn.

   REWRITTEN 2026-07-12 (Hallie, post-playtest: "The Closet is incoherent now").
   She's right. It used to say "next Attack played → lose all Stealth" — but attacking ALREADY breaks
   your Stealth now, so the drawback was a restatement of the rules. The card said nothing.

   So make the drawback real, and make it the thing the card is actually about: **in the closet, you are
   safe, and you cannot do anything.** You get the deepest cover in the deck and you spend the turn not
   acting. That's the trade, and it's the only card in the game that makes it explicitly.

   It's also the strongest Stealth-bank builder, which means it wants Backstab, Sneak Attack, or Look
   What I Found Down Here on the turn AFTER — you hide, and then you come out.

   (Implementation note: there is no engine hook that can forbid *playing* a card from a Power — only
   CardModel.IsPlayable, which is per-card. So the closet zeroes your attack damage instead, which is the
   same thing and reads better anyway: you can swing all you like. You will not hit anyone.) */
public sealed class TheCloset() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private decimal StealthGain => IsUpgraded ? 4m : 3m;
    public override int MaxUpgradeLevel => 1;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Stealth>(choiceContext, Owner.Creature, StealthGain, Owner.Creature, this, false);
        await PowerCmd.Apply<InTheCloset>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
