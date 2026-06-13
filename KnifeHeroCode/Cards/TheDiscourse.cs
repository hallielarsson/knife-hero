using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Extensions;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* The Discourse — Hallie's design. A Status card: "If this is in your hand at the end of your
   turn, gain 1 less energy next turn. Cost 1: Exhaust it." The feed clutter you spend energy to
   clear, or eat the energy hit. Extends CustomCardModel DIRECTLY (not KnifeHeroCard) on purpose:
   no [Pool] attribute, so it can never appear as a card reward — it only enters play when
   something generates it (Extremely Online shuffles it in). Human-sourced mechanic (Hallie). */
public sealed class TheDiscourse : CustomCardModel
{
    // crash-safe placeholder art — restated here since we don't inherit KnifeHeroCard's defaults
    public override string CustomPortraitPath => "card.png".BigCardImagePath();
    public override string PortraitPath => "card.png".CardImagePath();
    public override string BetaPortraitPath => "card.png".CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Exhaust };

    public override bool HasTurnEndInHandEffect => true;

    public TheDiscourse() : base(1, CardType.Status, CardRarity.Status, TargetType.None) { }

    // Playing it just pays 1 energy; the Exhaust keyword clears it. No on-play effect.
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<Discoursed>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
