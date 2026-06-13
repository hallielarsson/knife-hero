using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Pride flags themed after the StS characters — each a passive Flag (IFlag, so it counts for
   Stonewall / Rainbow Strike). Silent = shivs, Ironclad = heal. */

/* Silent Pride — the Silent's shiv engine: at the start of each turn, put a Shiv in your discard. */
public sealed class SilentPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        for (int i = 0; i < (int)Amount; i++)
        {
            var shiv = Owner.CombatState.CreateCard<Shiv>(player);
            await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Discard, addedByPlayer: false);
        }
    }
}

/* Ironclad Pride — Burning Blood as a pride flag: heal 5 at the end of combat if it's out. */
public sealed class IroncladPridePower : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        await CreatureCmd.Heal(Owner, 5m * Amount, false);
    }
}
