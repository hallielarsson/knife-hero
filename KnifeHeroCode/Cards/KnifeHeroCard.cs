using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace KnifeHero.KnifeHeroCode.Cards;

[Pool(typeof(KnifeHeroCardPool))]
public abstract class KnifeHeroCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    // CRASH-SAFE DEFAULT: every card falls back to the shared placeholder art (card.png),
    // so a WIP / "unaligned" card with no drawing yet won't 404 the game. When a card is
    // finished, override these three to its own "<id>.png" (big: 1000x760, small: 250x190,
    // full-art: 606x852 / 250x350).
    public override string CustomPortraitPath => "card.png".BigCardImagePath();
    public override string PortraitPath => "card.png".CardImagePath();
    public override string BetaPortraitPath => "card.png".CardImagePath();
}