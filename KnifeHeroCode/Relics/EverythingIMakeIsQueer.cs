using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
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
        if (CardModifier.DirectModifiers(card).Any(m => m is QueerRider or QueerRiderMod)) return;

        _mintedThisTurn = true;
        CardModifier.AddModifier(card, QueerRider.Random(Owner));
        Flash();
        await Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) _mintedThisTurn = false;
        return Task.CompletedTask;
    }
}
