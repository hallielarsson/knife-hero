using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* STEALTH + HEAT.

   STEALTH — while you have it, every incoming hit is capped at 1 + Visibility damage (the cap is on DAMAGE,
   via ModifyDamageCap, not on HP loss — so Block still matters and a 20-damage swing costs the same
   Block as a 3-damage one). Losing HP wipes the ENTIRE Stealth bank. Attacking wipes it too, and adds
   Visibility. The stacks are fuel: Backstab, Sneak Attack and Look What I Found Down Here cash them.

   HEAT — the clock, and Stealth's only balancing pressure (without it: hide, chip your Block, hide
   forever). It does two things at once: hits get bigger (cap = 1 + Visibility), and every hit strips Visibility
   stacks of Stealth EVEN IF Block ate it completely. Visibility never decays within a fight.

   NOT an IFlag: being hidden is not a pride flag, so it doesn't count for Stonewall / Rainbow Strike.

   ⚠ ModifyDamageCap and AfterDamageReceived are separate engine passes — the first sizes the hit, the
   second reads what happened. Visibility can enlarge a hit AND strip Stealth in the same swing, no special
   handling needed. */
public sealed class Stealth : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Visibility is a status CARD now (Cards/Visibility.cs). "How visible you are" = the ones in your hand.
    private int VisibilityAmount => Owner != null ? Visibility.CountInHand(Owner.Player) : 0;

    // The cap displays as a live number rather than "1 plus your Visibility".
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Cap", 1m) };

    /* ⚠ PowerModel has NO preview/refresh hook, so the displayed number must be re-synced by hand at
       every moment Visibility can change: turn start, being hit, and playing a card (Fire gains Visibility on play).
       Miss one and the card shows a stale cap. */
    private void SyncCap()
    {
        var cap = DynamicVars["Cap"];
        decimal want = 1m + VisibilityAmount;
        if (cap.BaseValue != want) cap.UpgradeValueBy(want - cap.BaseValue);
    }

    /* ⚠ Cap the DAMAGE, not the HP loss. IntangiblePower clamps both; we clamp only damage, so Block
       still absorbs. Fires before Block is subtracted. */
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress) return decimal.MaxValue;
        if (target != Owner) return decimal.MaxValue;
        SyncCap();
        return 1m + VisibilityAmount;
    }

    /* Playing an Attack costs you the whole Stealth bank AND a point of Visibility. This is the hidden build's
       central tension — safe, or doing something, not both — and it's why the cash-out cards exist.
       (Unseen, from Day of Invisibility, suspends it for a turn.) */
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SyncCap();
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (Owner.GetPower<Unseen>() != null) return;   // Day of Invisibility

        var deadName = Owner.GetPower<DeadNamePower>();
        if (deadName != null) await deadName.RefuseTheName();
        else await Visibility.Add(choiceContext, Owner.Player, 1, PileType.Draw);   // found → looms in your draw

        await PowerCmd.Remove(this);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        SyncCap();

        // You bled: the whole Stealth bank goes, and you gain a Visibility (permanent for the fight).
        // ⚠ Dead Name intercepts here and in AfterCardPlayed — between them, these are the ONLY two
        // places Visibility is ever granted by being found. Any new Visibility source must check DeadNamePower too.
        if (result.UnblockedDamage > 0m)
        {
            var deadName = Owner.GetPower<DeadNamePower>();
            if (deadName != null) await deadName.RefuseTheName();
            else await Visibility.Add(choiceContext, Owner.Player, 1, PileType.Draw);   // found → looms in your draw

            await PowerCmd.Remove(this);
            return;
        }

        // Blocked → nothing happens. (The old "getting warmer" rule — a blocked hit strips Visibility-many
        // Stealth — was cut with the Visibility-as-card rework, 2026-07-25: the new pressure is the
        // draw-a-Visibility-lose-a-Stealth loop. If the clock needs more teeth in play, bring it back.)
    }

    /* Lose 1 Stealth at end of the ENEMY turn. ⚠ BALANCE: without this the bank has no leak — every other
       way to lose Stealth needs the fight to act on you, so a turn of Defends was pure deposit and the optimal
       line was always "hide four turns, cash one enormous Backstab". The cash-out stays possible; it just
       isn't free.

       ⚠ TIMING: the leak fires at the end of the ENEMY turn, not yours — so Stealth you build on your turn
       fully protects you through the immediately-following enemy turn, and only ticks down once that turn
       resolves. (Leaking at your own turn end instead would strip a stack before the enemy ever swings.)

       AfterSideTurnEnd + participants.Contains(Owner) is the engine's idiom (cf. FlankingPower). Hitting
       0 self-removes — ModifyAmount calls ShouldRemoveDueToAmount — so there's no dead icon to clean up. */
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        // ⚠ Only the ENEMY turn ending leaks a Stealth — Owner is a participant on both sides' turn end,
        // so without the side gate the bank drained twice a round.
        if (side != CombatSide.Enemy) return;
        if (!participants.Contains(Owner)) return;
        await PowerCmd.ModifyAmount(choiceContext, this, -1m, Owner, null);
    }
}
