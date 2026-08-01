using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* VISIBILITY — the found-clock, made physical. (2.0, 2026-07-25: replaces the old Visibility Power.)

   A status card. Being seen puts these in your deck; carrying them is the double edge:
     • IN HAND — each one raises the cap on your Stealth (Stealth.cs reads Visibility.CountInHand).
     • DRAWN   — strips a Stealth, fullstop (AfterCardDrawn).
     • PLAYED  — costs 1 and just leaves your hand (to discard). You can pay to clear it, but it cycles.

   The loud build LEANS in and reads them (Honeypot, Dashing Strike, Proud). The clean build EXHAUSTS
   them — Smoke Bomb / Shadow Dodge delete STATUS cards in general, permanently. */
public sealed class Visibility() : KnifeHeroCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override string PortraitPath => "visibility.png".CardImagePath();
    public override string CustomPortraitPath => "visibility.png".BigCardImagePath();

    // Playable: 1 energy and it just leaves your hand (played cards go to discard). No other effect.
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;

    // Drawing it strips a Stealth. fullstop.
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        var stealth = Owner.Creature.GetPower<Stealth>();
        if (stealth != null) await PowerCmd.ModifyAmount(choiceContext, stealth, -1m, Owner.Creature, this);
    }

    // ── the API the rest of the deck reads/writes (all counting is IN HAND) ──

    /// How visible you are right now = Visibility cards in your hand. Raises the Stealth cap; feeds payoffs.
    public static int CountInHand(Player player) =>
        CardPile.GetCards(player, PileType.Hand).Count(c => c is Visibility);

    /// Gain n Visibility. FOUND ones go to the draw pile (they loom, and strip Stealth when drawn); CHOSEN
    /// ones go to hand (immediate — you wanted them). Caller picks the pile.
    public static async Task Add(PlayerChoiceContext choiceContext, Player player, int n, PileType pile)
    {
        for (int i = 0; i < n; i++)
        {
            var card = player.Creature.CombatState.CreateCard<Visibility>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, pile, player);
        }
    }
}
