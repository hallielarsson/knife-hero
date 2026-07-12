using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* STEALTH — Hallie's design, 2026-07-11. Her words: "like Intangible, but more stealth feeling."

   THE RULE
     While you have Stealth, EVERY incoming hit is capped at 1 damage.
     That 1 hits your Block like anything else.
     When Block runs out, the 1 lands on your HP — and you lose ALL your Stealth.

   Hallie's read: **"it's chip damage on block until they get through, and then it's all over."**

   WHY THAT IS NOT INTANGIBLE
   Intangible is a TIMER — N turns of invulnerability, ticking down regardless of what you do. Stealth is
   a CONDITION: you stay hidden until they *find* you, and they find you by drawing blood.

   The cap is on DAMAGE, not on HP loss (see ModifyDamageCap below) — which means a 20-damage swing and a
   3-damage swing both cost you exactly ONE Block. **They are swinging at shadows.** Block is not armour
   here; it is a stealth BATTERY, and every hit drains one charge. That is the difference between a shield
   and a disguise, and it is why this reads as sneaking.

   THE BANK
   Stealth is a Counter, and the stacks are FUEL — Sneak Attack and Flank spend them ("deal 3 per Stealth
   lost", "gain 2X Vigor where X = Stealth lost"). But a single unblocked point of damage annihilates the
   ENTIRE stack, not one of it. So the decision is the deck's decision, again: cash it out now, or hold it
   and risk losing all of it. Bank or cash — the same verb as the blades, the orbs, and the flags.

   BEING FOUND is cheap in HP (the clamp caps it at 1) and expensive in COVER (you lose the whole bank).

   Replaces the old design (each stack = one stored turn of Intangible, decremented at end of turn), which
   was a timer wearing a stealth costume. NOT an IFlag — being hidden is not a pride flag, so it doesn't
   count for Stonewall / Rainbow Strike. Stealth is the ANTI-flag axis: visible and scaling, or hidden and
   not. */
public sealed class Stealth : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /* Cap the DAMAGE at 1 — not the HP loss. This is the whole mechanic, and the distinction is the
       difference between a shield and a disguise.

       ModifyDamageCap ("set the maximum amount of damage that will be dealt in a single hit") operates
       on DAMAGE, before Block is subtracted. So a 20-damage swing becomes 1 damage, and your Block eats
       it. Every incoming hit costs you exactly ONE Block, no matter how big the swing was. They are
       swinging at shadows.

       Block is therefore not armour — it is a STEALTH BATTERY, and each hit drains one charge. Only when
       Block runs out does that 1 reach your HP, and that's when they've found you.

       (IntangiblePower uses this hook AND ModifyHpLostAfterOsty. Stealth deliberately uses only this one:
       clamping HP loss too would make Block irrelevant, and Block being relevant is the entire point.) */
    public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress) return decimal.MaxValue;
        if (target != Owner) return decimal.MaxValue;
        return 1m;
    }

    // You bled, so you were seen. The whole bank goes — not a decrement, the lot.
    // UnblockedDamage is what actually landed on HP; if Block ate it, this is 0 and you stay hidden.
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage <= 0m) return;
        await PowerCmd.Remove(this);
    }
}
