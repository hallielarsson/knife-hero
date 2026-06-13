using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Monsters;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Pride Golem — Hallie's design: sacrifice your pride to forge one big body. "Destroy all Flags and
   other Pets you have. Create a pet with 2X HP" where X = the total Flags + pets you destroyed.
   "Whenever this pet takes damage, it deals that much damage to the attacker" (PrideGolemThorns).
   The capstone of a Flag/pet-heavy board — cash it all in for a retaliating wall.
   Human-sourced mechanic (Hallie); placeholder art via KnifeHeroCard. */
public sealed class PrideGolem() : KnifeHeroCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // tally the pride being sacrificed: every Flag stack + every other pet on your side
        var flagPowers = Owner.Creature.Powers.Where(p => p is IFlag).ToList();
        int x = (int)flagPowers.Sum(p => p.Amount);

        var otherPets = CombatState.Creatures
            .Where(c => c.IsPet && c.Side == Owner.Creature.Side)
            .ToList();
        x += otherPets.Count;

        // destroy the Flags, then the pets
        foreach (var fp in flagPowers)
            await PowerCmd.Remove(fp);
        if (otherPets.Count > 0)
            await CreatureCmd.Kill(otherPets, force: true);

        // forge the golem with 2X HP (min 1) and give it its retaliation
        int hp = Math.Max(1, 2 * x);
        var golem = await PlayerCmd.AddPet<PrideGolemPet>(Owner);
        await CreatureCmd.SetMaxHp(golem, hp);
        await CreatureCmd.Heal(golem, hp, false);
        await PowerCmd.Apply<PrideGolemThorns>(golem, 1m, golem, this, false);
    }
}
