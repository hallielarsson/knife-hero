using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Faith — (1). Deal 1 damage. The next time you draw Faith, it deals 10.
   Hallie 2026-06-17: "just the flat climb. You have to have faith." So it's a single step
   1 -> 10 (no compounding), and the payoff is for KEEPING it — you have to draw it back. The
   climb is per-combat (a fresh combat copy starts at 1 again). // PROPOSAL numbers (1 -> 10).

   Mechanic: the card persists as one instance across piles within a combat. First draw arms it;
   the next draw rewards your faith — its damage var climbs to 10, once. */
public sealed class Faith() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private bool _drawnOnce;
    private bool _believed;

    /* {Payoff} is a DynamicVar so the card can NAME the reward it's promising. The text used to say "it
       deals 10 damage" as a literal — and an upgraded Faith pays 15, so the card was making a promise it
       then overkept, silently, forever. A card that lies in your favour is still a card that lies. */
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(1m, ValueProp.Move), new IntVar("Payoff", 10m) };

    // UPGRADE: faith is rewarded more. The payoff climbs from 10 to 15.
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars["Payoff"].UpgradeValueBy(5m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // The flat climb: first draw arms; the next draw rewards faith → 10 (once, then it holds).
    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this && !_believed)
        {
            if (_drawnOnce)
            {
                // Climb to exactly the payoff we PROMISED on the card — derived, never hardcoded, so an
                // upgraded Faith lands on 15 and an un-upgraded one on 10 without a second magic number.
                var dmg = DynamicVars.Damage;
                dmg.UpgradeValueBy(DynamicVars["Payoff"].BaseValue - dmg.BaseValue);
                _believed = true;
            }
            else
            {
                _drawnOnce = true;
            }
        }
        return Task.CompletedTask;
    }
}
