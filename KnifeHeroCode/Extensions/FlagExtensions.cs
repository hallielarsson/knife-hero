using System.Linq;
using KnifeHero.KnifeHeroCode.Cards;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KnifeHero.KnifeHeroCode.Extensions;

public static class FlagExtensions
{
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
