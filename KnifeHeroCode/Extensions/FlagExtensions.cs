using System.Linq;
using KnifeHero.KnifeHeroCode.Monsters;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace KnifeHero.KnifeHeroCode.Extensions;

public static class FlagExtensions
{
    /* The Gay Blade's Flag count — one place, so "the flags are pets" stays true everywhere.
       A Flag is EITHER an IFlag power (Stealth, Extremely Online — counted by stacks) OR a Top/Bottom
       guardian standing on your side (1 each). Used by Stonewall, Rainbow Strike, etc. Other pets
       (Labrys, Pride Golem, Kunaii) are NOT flags — only Top and Bottom. */
    public static int FlagCount(this Creature creature)
    {
        int powerFlags = (int)creature.Powers.Where(p => p is IFlag).Sum(p => p.Amount);
        int petFlags = creature.CombatState.Creatures
            .Count(c => c.Side == creature.Side && (c.Monster is TopPet || c.Monster is BottomPet));
        return powerFlags + petFlags;
    }
}
