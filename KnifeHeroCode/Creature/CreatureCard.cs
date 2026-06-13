using BaseLib.Abstracts;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Base for The Creature's cards — its own pool, so these never mix into the Gay Blade's rewards.
   Shares the mod's placeholder card art (card.png) until the Creature gets its own. */
[Pool(typeof(TheCreatureCardPool))]
public abstract class CreatureCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => "card.png".BigCardImagePath();
    public override string PortraitPath => "card.png".CardImagePath();
    public override string BetaPortraitPath => "card.png".CardImagePath();
}

/* Book — The Creature's card classifier (same marker-interface pattern as IBlade/IFlag, since
   CardTag/CardKeyword are closed engine enums). A Book is a card you "read"; nothing on its own,
   but Marginalia and other cards key off it. */
public interface IBook { }
