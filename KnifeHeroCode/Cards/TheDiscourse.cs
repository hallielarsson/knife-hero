using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* The Discourse — Hallie's design. A Status card: "If this is in your hand at the end of your turn,
   gain 1 less energy next turn. Cost 1: Exhaust it." The feed clutter you spend energy to clear, or
   eat the energy hit. It extends KnifeHeroCard so it carries the mod's [Pool] attribute (BaseLib
   REQUIRES every registered card to name a pool — a non-pooled card crashes the game at startup).
   It still never appears as a reward because CardRarity.Status is never rolled in card-choice rewards;
   it only enters play when something generates it (Extremely Online shuffles it in). */
public sealed class TheDiscourse() : KnifeHeroCard(1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    public override bool HasTurnEndInHandEffect => true;

    // Playing it just pays 1 energy; the Exhaust keyword clears it. No on-play effect.
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<Discoursed>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
