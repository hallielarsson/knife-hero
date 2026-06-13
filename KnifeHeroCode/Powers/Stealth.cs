using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Gender #1 — Stealth. A gender (a stacking buff). At the end of your turn, consume a
   gender (v1: this Stealth, the only gender so far) and gain Intangible. So each stack of
   Stealth is one stored turn of Intangible (take only 1 damage from each source). */
public sealed class Stealth : KnifeHeroPower, IFlag
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await PowerCmd.Apply<IntangiblePower>(Owner, 1m, Owner, null, false);
        await PowerCmd.Decrement(this);
    }
}
