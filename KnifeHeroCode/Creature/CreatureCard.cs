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

    // API DRIFT SHIM (see KnifeHeroCard): the shipped build dropped CardModel.CombatState; the base
    // getter throws MissingMethodException and hangs the turn. Route through Creature.CombatState.
    // NOTE: Creature.CombatState is ICombatState?, not the concrete CombatState class — typed
    // accordingly (2026-07-11 API re-verify against .decompiled).
    public new MegaCrit.Sts2.Core.Combat.ICombatState? CombatState => Owner.Creature.CombatState;

    /* Straight HP damage. Lessons deliberately do NOT cancel it: that rule drained the very Lessons you
       need to bank to mend a part, so neither currency could ever accumulate. */
    protected async Task TakeGriefDamage(PlayerChoiceContext choiceContext, int amount)
    {
        if (amount > 0)
            await CreatureCmd.Damage(choiceContext, Owner.Creature, amount, ValueProp.Unpowered, Owner.Creature, this);
    }
}

/* IBook — marker for a card you "read" (Marginalia keys off it).
   A marker interface and not a CardTag because CardTag/CardKeyword are closed engine enums a mod
   cannot extend. Same reason as IBlade/IFlag/IPride. */
public interface IBook { }
