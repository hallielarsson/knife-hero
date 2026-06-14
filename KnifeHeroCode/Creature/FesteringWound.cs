using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Festering Wound — what a Throbbing Heart rots into if you don't redeem it in time. A Curse:
   unplayable, and at the end of each turn it's in your hand you take 2 grief damage. NOT Eternal —
   you can pay gold to purge it.

   But the rot is also a weapon. While it's in your hand, your attacks deal +1 damage — and because
   every Wound in hand is its own hook listener, the bonus stacks: carry your wounds and you grow
   deadlier as you fall apart. The tragic build-around. Hold them for the edge, or let them go. */
public sealed class FesteringWound() : CreatureCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Unplayable };

    // The rot, weaponized: +1 to your attacks per Festering Wound in hand (each contributes its share).
    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && dealer == Owner?.Creature && Pile?.Type == PileType.Hand)
            return 1m;
        return 0m;
    }

    public override bool HasTurnEndInHandEffect => true;

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await TakeGriefDamage(choiceContext, 2);
    }
}
