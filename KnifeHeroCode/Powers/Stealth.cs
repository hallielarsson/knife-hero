using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

   STEALTH — while you have it, every incoming hit is capped at 1 + Heat damage (the cap is on DAMAGE,
   via ModifyDamageCap, not on HP loss — so Block still matters and a 20-damage swing costs the same
   Block as a 3-damage one). Losing HP wipes the ENTIRE Stealth bank. Attacking wipes it too, and adds
   Heat. The stacks are fuel: Backstab, Sneak Attack and Look What I Found Down Here cash them.

   HEAT — the clock, and Stealth's only balancing pressure (without it: hide, chip your Block, hide
   forever). It does two things at once: hits get bigger (cap = 1 + Heat), and every hit strips Heat
   stacks of Stealth EVEN IF Block ate it completely. Heat never decays within a fight.

   NOT an IFlag: being hidden is not a pride flag, so it doesn't count for Stonewall / Rainbow Strike.

   ⚠ ModifyDamageCap and AfterDamageReceived are separate engine passes — the first sizes the hit, the
   second reads what happened. Heat can enlarge a hit AND strip Stealth in the same swing, no special
   handling needed. */
public sealed class Heat : KnifeHeroPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // No behaviour of its own — Stealth reads it. Its own power so the player can see the clock.
}

public sealed class Stealth : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int HeatAmount => (int)(Owner?.GetPower<Heat>()?.Amount ?? 0m);

    // The cap displays as a live number rather than "1 plus your Heat".
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Cap", 1m) };

    /* ⚠ PowerModel has NO preview/refresh hook, so the displayed number must be re-synced by hand at
       every moment Heat can change: turn start, being hit, and playing a card (Fire gains Heat on play).
       Miss one and the card shows a stale cap. */
    private void SyncCap()
    {
        var cap = DynamicVars["Cap"];
        decimal want = 1m + HeatAmount;
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
        return 1m + HeatAmount;
    }

    /* Playing an Attack costs you the whole Stealth bank AND a point of Heat. This is the hidden build's
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
        else await PowerCmd.Apply<Heat>(choiceContext, Owner, 1m, Owner, null, false);

        await PowerCmd.Remove(this);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        SyncCap();

        // You bled: the whole Stealth bank goes, and you gain a Heat (permanent for the fight).
        // ⚠ Dead Name intercepts here and in AfterCardPlayed — between them, these are the ONLY two
        // places Heat is ever granted by being found. Any new Heat source must check DeadNamePower too.
        if (result.UnblockedDamage > 0m)
        {
            var deadName = Owner.GetPower<DeadNamePower>();
            if (deadName != null) await deadName.RefuseTheName();
            else await PowerCmd.Apply<Heat>(choiceContext, Owner, 1m, Owner, null, false);

            await PowerCmd.Remove(this);
            return;
        }

        // Blocked, but they're getting warmer: a hit costs you Heat stacks of cover even when Block ate
        // it completely. At Heat 0 being swung at is free.
        int heat = HeatAmount;
        if (heat > 0)
            await PowerCmd.ModifyAmount(choiceContext, this, -heat, Owner, null);
    }

    /* Lose 1 Stealth at end of turn. ⚠ BALANCE: without this the bank has no leak — every other way to
       lose Stealth needs the fight to act on you, so a turn of Defends was pure deposit and the optimal
       line was always "hide four turns, cash one enormous Backstab". The cash-out stays possible; it just
       isn't free.

       AfterSideTurnEnd + participants.Contains(Owner) is the engine's idiom (cf. FlankingPower). Hitting
       0 self-removes — ModifyAmount calls ShouldRemoveDueToAmount — so there's no dead icon to clean up. */
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        await PowerCmd.ModifyAmount(choiceContext, this, -1m, Owner, null);
    }
}
