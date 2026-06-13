using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Discoursed — the debuff The Discourse leaves on you. At your next energy reset (start of next
   turn) you gain that much less energy, then it clears itself. Built on the engine's own
   EnergyNextTurnPower pattern but with a NEGATIVE amount: GainEnergy(-Amount) at AfterEnergyReset.
   Crucially this only fires at a turn-START reset, never at the turn-END where it's applied, so
   there's no same-turn double-fire to guard against. Counter-stacks: holding two Discourse = -2. */
public sealed class Discoursed : KnifeHeroPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PlayerCmd.GainEnergy(-Amount, player);   // one less energy per stack, next turn
        await PowerCmd.Remove(this);
    }
}
