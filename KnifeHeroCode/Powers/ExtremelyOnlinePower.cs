using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Extremely Online (the power) — persistent +energy every turn. Same hook as the engine's
   GenesisPower (GainEnergy at AfterEnergyReset) but it does NOT remove itself, so the energy
   keeps coming each turn. Counter-stacks if replayed. The clutter cost lives on the card.

   It's an IFlag: a pride flag you fly. So the Flag cards reach it — Corporate Sponsored Pride can
   cash a stack down (−1 energy/turn for +2 energy now), Rainbow Strike counts it for damage, etc.
   That's the "way to remove it" Hallie wanted: log off by selling out. */
public sealed class ExtremelyOnlinePower : KnifeHeroPower, IFlag
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
