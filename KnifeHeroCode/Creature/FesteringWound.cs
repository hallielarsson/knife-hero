using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Festering Wound — what a Throbbing Heart rots into if you don't redeem it in time. A Curse:
   unplayable, and at the end of each turn it's in your hand you take 3 grief damage. Unlike the
   Heart it's NOT Eternal — you can pay to purge it, the gold cost of letting a part go bad. */
public sealed class FesteringWound() : CreatureCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Unplayable };

    public override bool HasTurnEndInHandEffect => true;

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await TakeGriefDamage(choiceContext, 3);
    }
}
