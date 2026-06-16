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
public sealed class TheCloset() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Stealth>(Owner.Creature, 3m, Owner.Creature, this, false);
        await PowerCmd.Apply<Closeted>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
