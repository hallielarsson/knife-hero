using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Abstracts;

namespace KnifeHero.KnifeHeroCode.Relics;

/* THE GAY BLADE'S SIGNATURE RELIC — Hallie, 2026-07-12.
   ⚠ NAME IS A PLACEHOLDER. Hers to mint.

     Whenever you CREATE an Attack, Queer it.

   THE ENGINE IT CLOSES
   The Queer curse (Queer.cs) already queers the normative when it's cast out — exhaust a Strike or a
   Defend and it comes back as itself plus a rider. That handles what the world *throws away*.

   This handles what you *make*. And this deck makes an enormous amount: every Shiv from Knife Whip
   (armour shattering into knives), from Silent Pride, from Superfan, from Pickpocket, from Look What I
   Found Down Here. Every one of them now arrives already queer.

   So the two halves of the thesis finally meet:
     • what is cast out comes back OTHER  (the Queer curse)
     • what you make is ALREADY other     (this relic)

   You don't have to wait to be exhausted into queerness. Everything you make is queer because you made
   it. **Diversity is strength; thin by becoming, not subtraction.**

   Mechanically it means the shiv engine and the queer engine are the same engine, which is what makes
   the "go wide" build work: a fistful of shivs is now a fistful of *riders*.

   ⁉ FLAGGED — right now every queering attaches the same rider (QueerRiderMod: poison on hit). Hallie's
   design is that a Queered card gains **one existing card quality at random** — Sharp, Sly, Clone, Swap,
   Thorns, "essentially all existing card qualities." That rider POOL is the next piece of work and it's
   the piece that makes this relic sing: right now every shiv comes out the same, and it should come out
   *different*. Divergence by source is the whole point. */
public sealed class EverythingIMakeIsQueer : KnifeHeroRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    private bool _mintedThisTurn;

    /* ONCE PER TURN (Hallie, 2026-07-12: "otherwise minting queerness is not a special thing").
       This deck CREATES an enormous number of attacks — every shiv from Knife Whip, Silent Pride,
       Superfan, Pickpocket. If all of them came out queer, queerness would be wallpaper. One per turn
       makes it a mint: you get to watch for it, and you get to *aim* it, because you choose what you
       make first. */
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (_mintedThisTurn || creator != Owner || card.Type != CardType.Attack) return;
        _mintedThisTurn = true;
        Queer.Apply(card, Owner);
        Flash();
        await Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) _mintedThisTurn = false;
        return Task.CompletedTask;
    }

    /* ── THE RELIC NO LONGER UN-EXHAUSTS YOUR BASICS ────────────────────────────────────────────
       (Hallie, playtest: *"Does the relic still un-exhaust attacks and defends? Because it probably
       shouldn't."*)

       It did, and it shouldn't. Exhausting a Strike or a Defend used to send it back to your draw pile,
       queered.

       I first wrote this up as "the relic was refunding the Switch Blade's cost." Hallie: *"Not cost —
       exhausting a card isn't a straight cost, esp. a basic."* She's right and it matters, because the
       actual problem is the opposite one and it's bigger.

       **Exhausting a basic is a BENEFIT.** Thinning is one of the deepest levers in the genre: every
       Strike you remove makes every good card you own more likely to show up. The Switch Blade doesn't
       *charge* you a basic — it *upgrades* one, out of the deck and into a blade. That's the whole
       pleasure of it.

       And this relic silently forbade it. Every basic you exhausted came straight back, so **the Gay
       Blade could never thin, ever, by any route.** A relic that quietly cancels a core strategic lever
       isn't a gift, it's a trap — and it's an invisible one, because nothing fails. Your deck just stays
       fat forever and you never learn why.

       ── BUT DON'T JUST DELETE THE UPSIDE ───────────────────────────────────────────────────────
       Hallie, immediately: *"so also nerfing its benefit."* Yes — cutting the clause takes half the
       relic's text with it, and this is a STARTER relic. It cannot be "sometimes nothing."

       So the benefit stays and only the *destination* changes. Exhaust a Strike or a Defend and it does
       not come back — but **what it was passes into something else.** A card in your draw pile is
       queered.

       This is strictly better than what it replaced, and it's better because it says the truer thing:

           the old clause  → what you cast out returns, unchanged except for a rider
           this one        → what you cast out is GONE, and it changes what's left behind

       You get to thin. You get the queering. And you don't have to be given back to be worth something —
       **what you let go of still makes the rest of you different.** */
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner != Owner) return;
        if (!card.Tags.Contains(CardTag.Strike) && !card.Tags.Contains(CardTag.Defend)) return;

        // The basic stays exhausted — you keep the thinning. It just doesn't leave quietly.
        var draw = CardPile.GetCards(Owner, PileType.Draw).ToList();
        if (draw.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        Queer.Apply(rng.NextItem(draw), Owner);
        Flash();
    }
}
