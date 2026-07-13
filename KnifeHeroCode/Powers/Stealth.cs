using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* STEALTH + HEAT — Hallie's design. "Like Intangible, but more stealth feeling."

   ── STEALTH ────────────────────────────────────────────────────────────────────────────────────
     While you have Stealth, every incoming hit is capped at **1 + your Heat** damage.
     That damage hits your Block like anything else.
     If you actually lose HP, you lose ALL your Stealth.

   Hallie's read: *"it's chip damage on block until they get through, and then it's all over."*

   WHY THAT IS NOT INTANGIBLE
   Intangible is a TIMER — N turns of invulnerability, ticking down no matter what you do. Stealth is a
   CONDITION: you stay hidden until they *find* you, and they find you by drawing blood. The cap is on
   DAMAGE, not on HP loss (ModifyDamageCap), so a 20-damage swing and a 3-damage swing cost you exactly
   the same Block. **They are swinging at shadows.** Block here is not armour — it's a stealth BATTERY,
   and every hit drains a charge. That's the difference between a shield and a disguise.

   ── HEAT (Hallie, 2026-07-12: "far too powerful as is") ────────────────────────────────────────
   Stealth alone had no clock: hide, spend a little Block, hide forever. Heat is the clock, and it does
   TWO things at once, which is what makes it bite:

     1. **You are easier to HIT.**   The damage cap becomes 1 + Heat. Your Block drains faster.
     2. **You are easier to FIND.**  Every hit strips *Heat* stacks of Stealth — **even if Block ate it
        completely.** At Heat 0 only bleeding exposes you. At Heat 3, being swung at at all costs you
        three Stealth.

   And you gain a Heat every time you're caught (i.e. every time you actually lose HP).

   So: hide, get found, gain Heat, hide again — and the second time is worse, and the third is worse
   than that. **The room is learning where you are.** You can keep going back into the shadows; the
   shadows keep getting thinner. Heat never goes down, so it is a hard ceiling on how many times in a
   fight you can disappear.

   ── THE BANK ───────────────────────────────────────────────────────────────────────────────────
   Stealth is a Counter and the stacks are FUEL — Sneak Attack and Flank spend them ("deal 3 per Stealth
   lost", "gain 2X Vigor where X = Stealth lost"). But losing HP annihilates the ENTIRE bank at once. So
   it's the deck's decision again: cash out now, or hold and risk losing all of it. Bank or cash.

   NOT an IFlag — being hidden is not a pride flag, so it doesn't count for Stonewall / Rainbow Strike.
   Stealth is the ANTI-flag axis: visible and scaling, or hidden and not.

   NOTE ON THE TWO HOOKS, since they look like they should conflict and don't:
   `ModifyDamageCap` decides how big the hit is. `AfterDamageReceived` runs afterwards and reads what
   actually happened. They are separate passes, so Heat can enlarge the hit AND strip Stealth in the same
   swing with no special handling. */
public sealed class Heat : KnifeHeroPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Heat has no behaviour of its own — Stealth reads it. It's a marker of how well they know your
    // position, and it never decays. Kept as its own power so the player can SEE the clock ticking.
}

public sealed class Stealth : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int HeatAmount => (int)(Owner.GetPower<Heat>()?.Amount ?? 0m);

    /* Cap the DAMAGE at 1 + Heat — not the HP loss. (IntangiblePower clamps both; we deliberately only
       clamp damage, because Block mattering is the entire point.) Fires before Block is subtracted. */
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress) return decimal.MaxValue;
        if (target != Owner) return decimal.MaxValue;
        return 1m + HeatAmount;
    }

    /* STRIKING SOMEONE SHOWS THEM WHERE YOU ARE.
       This is the central tension of the hidden build: you can be safe, or you can be doing something,
       not both. Every attack you play drops your whole Stealth bank — which is why Sneak Attack and
       Flank exist (cash it out on the way down), and why Day of Invisibility (→ Unseen) is worth a card:
       it buys you one turn of being a ghost with a knife. */
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (Owner.GetPower<Unseen>() != null) return;   // Day of Invisibility
        await PowerCmd.Remove(this);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;

        // FOUND. You bled, so they've seen you: the whole bank goes, and they know where to look next
        // time. (Heat is permanent for the fight.)
        if (result.UnblockedDamage > 0m)
        {
            // DEAD NAME intercepts: refuse the Heat, take a Dazed instead. It's the only place Heat is
            // ever granted, so this one check is the whole counter.
            var deadName = Owner.GetPower<DeadNamePower>();
            if (deadName != null) await deadName.RefuseTheName();
            else await PowerCmd.Apply<Heat>(choiceContext, Owner, 1m, Owner, null, false);

            await PowerCmd.Remove(this);
            return;
        }

        // BLOCKED — but they're getting warmer. Every hit costs you Heat stacks of cover even when your
        // Block ate it completely. At Heat 0 this does nothing and being swung at is free.
        int heat = HeatAmount;
        if (heat > 0)
            await PowerCmd.ModifyAmount(choiceContext, this, -heat, Owner, null);
    }
}
