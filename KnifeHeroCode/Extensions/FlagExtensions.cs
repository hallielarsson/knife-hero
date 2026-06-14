using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Cards;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.Extensions;

public static class FlagExtensions
{
    /* One copy per flag-blade. Forging a blade you already carry (in hand, draw, or discard) doesn't
       stack a duplicate — it UPGRADES the copy you have. So getting that flag again sharpens it.
       Used by Fancy Footwork when it generates Butch Blade / Femme Flechette. */
    public static async Task AddOrUpgradeFlagBlade<T>(this CombatState combat, Player owner) where T : CardModel
    {
        var existing = CardPile.GetCards(owner, PileType.Hand, PileType.Draw, PileType.Discard)
            .FirstOrDefault(c => c is T);
        if (existing != null)
        {
            CardCmd.Upgrade(existing);
            return;
        }
        var card = combat.CreateCard<T>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
    }

    /* The Gay Blade's Flag count — one place, so "the flags are pets/swords" stays true everywhere.
       A Flag is: an IFlag power (Stealth, Extremely Online, the Pride flags — counted by stacks),
       OR any pet on your side (the flags are pets), OR a retained flag-blade in your hand
       (Top/Bottom and the pride swords — the flags are swords). Used by Stonewall, Rainbow Strike. */
    public static int FlagCount(this Creature creature)
    {
        int powerFlags = (int)creature.Powers.Where(p => p is IFlag).Sum(p => p.Amount);
        int petFlags = creature.CombatState.Creatures.Count(c => c.IsPet && c.Side == creature.Side);
        int bladeFlags = creature.Player != null
            ? CardPile.GetCards(creature.Player, PileType.Hand).Count(c => c is IFlagBlade)
            : 0;
        return powerFlags + petFlags + bladeFlags;
    }
}
