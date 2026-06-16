using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Closeted — being-in-the-closet as a *maintained posture*, not a reaction.
   Decided 2026-06-15 via the whetstone (see WHETSTONE-the-closet.md). The deeper question —
   "can a Power even prevent damage, or must the closet become a card you hold?" — resolved:
   a Power CAN prevent, but only as a PRE-PAID charge (the engine's BufferPower proves the
   pattern), never as a mid-blow async choice. So the closet collects rent each turn:

     - The moment you play an Attack, you blow your cover: all Stealth is stripped and this
       watcher removes itself. (Acting out exposes you — the original rule, kept.)

     - At the start of YOUR turn, the closet asks for rent: discard one card from hand to
       gain a Buffer charge (the next instance of HP loss this turn is voided). If your hand
       is empty — nothing to feed the dark — the closet breaks: this removes itself and the
       light finds you (a Dazed enters your hand).

   Lives on The Closet only (NOT on Stealth generally), so Vanish-granted Stealth stays robust.
   Not an IFlag: a hidden condition, not a pride flag (Rainbow Strike / Corporate Sponsored
   Pride don't count it). Numbers (charges per discard, Dazed count) are Hallie's to mint. */
public sealed class Closeted : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // Acting out blows your cover.
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        var stealth = Owner.GetPower<Stealth>();
        if (stealth != null) await PowerCmd.Remove(stealth);   // cover blown — lose all Stealth
        await PowerCmd.Remove(this);
    }

    // The closet collects rent at the start of your turn: a card buys a prevention charge;
    // an empty hand breaks the posture and lets the light in (Dazed).
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var hand = CardPile.GetCards(Owner.Player, PileType.Hand).ToList();
        if (hand.Count > 0)
        {
            // PROPOSAL (Claude, whetstone 2026-06-15): feed the closet the first card in hand.
            // A targeted player-choice discard would be richer (CardSelectCmd.FromHandForDiscard);
            // auto-discarding the first card keeps the turn-start flow non-blocking. Hallie may
            // swap to a chosen discard and tune the BufferPower charge count.
            Flash();
            await CardCmd.Discard(choiceContext, hand[0]);
            await PowerCmd.Apply<BufferPower>(Owner, 1m, Owner, null, false);   // 1 HP-loss instance voided
        }
        else
        {
            // No rent to pay — the closet breaks and the light finds you.
            await PowerCmd.Remove(this);
            var dazed = Owner.CombatState.CreateCard<Dazed>(Owner.Player);
            await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Hand, addedByPlayer: false);
        }
    }
}
