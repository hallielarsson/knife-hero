using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

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

    /* Grief damage is just damage. The old "Lessons cancel grief" rule was draining the very Lessons
       you need to bank to reach the processing threshold (3 Lessons) — the two currencies fought, so
       you could never accumulate either. Removed per Hallie's playtest. Now grief hurts your HP
       straight (the monster's pain), and Lessons are free to pile toward healing. */
    protected async Task TakeGriefDamage(PlayerChoiceContext choiceContext, int amount)
    {
        if (amount > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature, amount, ValueProp.Unpowered, Owner.Creature, this);
    }
}

/* Book — The Creature's card classifier (same marker-interface pattern as IBlade/IFlag, since
   CardTag/CardKeyword are closed engine enums). A Book is a card you "read"; nothing on its own,
   but Marginalia and other cards key off it. */
public interface IBook { }
