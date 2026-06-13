using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Fan of Knives, but just for THIS turn — Superfan of Knives uses it. We apply the base game's
   FanOfKnivesPower (which the base Shiv already checks to hit all opponents) and strip it at end of
   turn, so your Shivs go AoE this turn only. */
public sealed class FanOfKnivesThisTurnPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var fan = Owner.GetPower<FanOfKnivesPower>();
        if (fan != null) await PowerCmd.Remove(fan);
        await PowerCmd.Remove(this);
    }
}
