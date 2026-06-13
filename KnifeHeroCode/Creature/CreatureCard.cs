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

    /* Grief damage — Hallie's design. Reaching back into your Exhaust (refusing to let go) hurts.
       But Lessons cancel grief: "Whenever you would take grief damage, lose a Lesson instead."
       Each Lesson absorbs 1 point; only the remainder lands on your HP. So you Learn (read Books)
       in order to afford staying with your dead — and Lessons are also a damage resource elsewhere,
       so spending them to buffer grief is a real choice. */
    protected async Task TakeGriefDamage(PlayerChoiceContext choiceContext, int amount)
    {
        var lesson = Owner.Creature.Powers.FirstOrDefault(p => p is Lesson);
        int lessons = (int)(lesson?.Amount ?? 0m);
        int absorbed = lessons < amount ? lessons : amount;
        if (absorbed > 0)
            await PowerCmd.Apply<Lesson>(Owner.Creature, -absorbed, Owner.Creature, this, false);
        int remaining = amount - absorbed;
        if (remaining > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature, remaining, ValueProp.Unpowered, Owner.Creature, this);
    }
}

/* Book — The Creature's card classifier (same marker-interface pattern as IBlade/IFlag, since
   CardTag/CardKeyword are closed engine enums). A Book is a card you "read"; nothing on its own,
   but Marginalia and other cards key off it. */
public interface IBook { }
