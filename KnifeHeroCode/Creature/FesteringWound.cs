using System.Collections.Generic;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Festering Wound — the generic Scar: what a Part rots into. Unplayable Curse. While it is in your
   hand your attacks deal +1 (each Wound is its own hook listener, so they stack).
   It keeps counting toward Grief forever (IScar : IPart, at double weight — see MendedBody). */
public sealed class FesteringWound() : CreatureCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None), IScar
{
    public override string PortraitPath => "festering_wound.png".CardImagePath();
    public override string CustomPortraitPath => "festering_wound.png".BigCardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Unplayable };

    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && dealer == Owner?.Creature && Pile?.Type == PileType.Hand)
            return 1m;
        return 0m;
    }

    /* ⚠ No self-bleed here on purpose. All bleeding is Grief's, once a turn, on one number the player
       can read. A second per-turn drain hidden on the card would be invisible and unbalanceable. */
}
