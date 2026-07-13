using System.Linq;
using System.Threading.Tasks;
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
        QueerMod.Queer(card, Owner);
        Flash();
        await Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) _mintedThisTurn = false;
        return Task.CompletedTask;
    }

    /* ── WHAT IS CAST OUT COMES BACK OTHER ──────────────────────────────────────────────────────
       Exhaust a Strike or a Defend and it does not die. It returns to your draw pile, queer.

       This used to live on the QUEER curse — an Innate, Eternal, Unplayable card that sat in your hand
       forever. Hallie cut it (2026-07-12): *"we don't have anything that mints it or uses it or gets rid
       of it."* She's right. It was a permanent −1 to hand size, in a deck where hand space is the only
       real currency, and it bought you a mechanic you could not see, could not aim, and could not remove.
       The tax was real and the card was inert.

       So the thesis moves here, where the rest of the Queer engine already lives, and the hand slot goes
       back to the player:

           what you MAKE      → the first Attack you create each turn comes out queer.   (above)
           what is CAST OUT   → the normative you throw away comes back queer.           (here)

       Every other character deletes its Strikes and Defends to reach a lean core. The Gay Blade can't —
       it queers them. **The cast-out normative doesn't leave. It comes back other.** And now it can do
       that more than once: QueerMod accumulates, so a Strike you keep exhausting keeps coming back MORE
       other — Queer 1, then Queer 2, then Queer 3. The world can fail to erase you as many times as it
       likes. */
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner != Owner) return;

        if (!card.Tags.Contains(CardTag.Strike) && !card.Tags.Contains(CardTag.Defend)) return;

        await CardPileCmd.Add(card, PileType.Draw);
        QueerMod.Queer(card, Owner);
        Flash();
    }


    /* WHEN A PRIDE IS EXHAUSTED, A SPENT BASIC COMES BACK AS A SWITCH BLADE.
       (Hallie, post-playtest, 2026-07-12.)

       This is the loop's engine, and moving it here is what let the Switch Blade stop being a novel.
       Before, the Switch Blade itself had to recruit from the discard — so the card had to say four
       things. Now it says one thing per path, and the RELIC closes the circle:

           swing a Top Chop  →  it exhausts  →  a spent Strike in your discard becomes a Switch Blade
           swing a Bottom    →  it exhausts  →  a spent Defend does the same

       Your prides die and your basics come back sharpened. Nothing is wasted, and the deck feeds itself
       out of the pile of things it already used. */
}
