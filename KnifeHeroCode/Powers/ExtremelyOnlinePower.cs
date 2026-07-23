using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Extremely Online (the power) — persistent +energy every turn. Like the engine's EnergyNextTurnPower
   (GainEnergy at AfterEnergyReset) but it does NOT remove itself, so the energy keeps coming each turn.
   Counter-stacks if replayed. The clutter cost (a Dazed) lives on the card. */
public sealed class ExtremelyOnlinePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PlayerCmd.GainEnergy(Amount, player);   // +Amount energy at the start of every turn
    }
}
