using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Powers;

/* Lesson — the Creature's depth resource. A stacking counter that does nothing raw; payoff cards
   (Quote at Length) read it. Reuses the mod's KnifeHeroPower base (icon from images/powers/). */
public sealed class Lesson : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/* Grief — a stacking debuff you accumulate (e.g. a Vexing Memory festering in hand). Inert on its
   own, but cards that make you "take damage equal to your Grief" cash it in — and the more you've
   stacked, the worse it bites. Note: that damage is grief damage, so Lessons can cancel it
   (see CreatureCard.TakeGriefDamage). */
public sealed class Grief : KnifeHeroPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/* Marginalia — the learning engine. Whenever you play a Book or a Power, gain a Lesson. (The engine
   has no on-power-GAINED hook, so we hook card-play instead — same shape as Panache/Closeted.)
   Single-stack: presence matters, not count. */
public sealed class MarginaliaPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card is IBook || cardPlay.Card.Type == CardType.Power)
            await PowerCmd.Apply<Lesson>(Owner, 1m, Owner, null, false);
    }
}

/* (ProcessedPartPower removed 2026-06-15, Pathetic Governor — it was an orphan: never applied
   anywhere, and superseded by the Wholeness mend in ThrobbingHeart.AfterCombatVictory, which now
   handles what a redeemed part grows into directly. Grieved in bro-engine; see THE_CREATURE/HEALING.md.) */

/* Wholeness — the healing axis (THE_CREATURE/HEALING.md, the open keystone, now built).
   A counter you raise ONLY by mending a part (redeeming a Throbbing Heart that survives the combat).
   Vengeance scales with Grief — loud, capped, reset every fight. Wholeness is the orthogonal payoff:
   alongside the permanent max-HP raise the mend grants, it knits your body a little each turn — the
   slow road that out-scales vengeance late.

   The healing is a PASSIVE turn-start trickle on the power itself: at the start of each of your turns,
   heal equal to your Wholeness. Once per turn-start, it is UNPUMPABLE — there is no playable card that
   re-triggers it, so it cannot feed back into a runaway healing loop.

   PROPOSAL (Claude, Pathetic Governor 2026-06-15): HEALING.md specified "+1 Wholeness per part mended,
   +2 max HP per Wholeness, healing amplified per Wholeness." The permanent max-HP raise is applied
   directly on the creature when a heart mends (SetMaxHp — it persists across combats, the run-long
   body). The in-combat healing lives here as the passive trickle (NOT on Mended Heart — the earlier
   per-play heal there could be re-played via DontLookAway → runaway loop, fixed/grieved 2026-06-15).
   A cleaner run-level persistence (re-deriving the count each combat) is left for Hallie once a stable
   run-state hook is chosen — flagged rather than guessed. Final numbers are Hallie's to mint. */
public sealed class Wholeness : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // The healed body knits a little each turn. Heal equal to Wholeness at turn start — fires exactly
    // once per turn no matter how many cards you play, so it can't be pumped into a feedback loop.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0m) return;
        Flash();
        await CreatureCmd.Heal(Owner, Amount, false);
    }
}

/* BecomeWhoYouArePower — the Rare capstone's engine (THE_CREATURE/DESIGN.md, the breadth payoff).
   At the start of each of your turns: gain Strength equal to your number of DISTINCT Powers, then gain
   1 Lesson. The Creature becomes the sum of its assembled parts — and the more distinct parts you've
   read into yourself, the faster it compounds. Stack count adds a flat bonus per stack (so a second
   copy / upgrade just adds to the per-turn Strength), but the scaling driver is breadth, not stacks.
   Counts THIS power among the distinct powers (you are also a part you're made of) — consistent with
   Recombinant's "all of it is you" decision. */
public sealed class BecomeWhoYouArePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0m) return;
        Flash();
        int distinct = Owner.Powers.Select(p => p.GetType()).Distinct().Count();
        // Per-turn Strength = distinct Powers + (flat bonus per stack of this power). Amount is the
        // stack count; the flat add per stack is (Amount - 1), so 1 stack = pure breadth, upgrades add.
        decimal str = distinct + (Amount - 1m);
        if (str > 0m)
            await PowerCmd.Apply<StrengthPower>(Owner, str, Owner, null);
        await PowerCmd.Apply<Lesson>(Owner, 1m, Owner, null, false);
    }
}

/* Polymath — at the start of each turn, gain a Lesson (compounding study). Counter-stacks. */
public sealed class PolymathPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PowerCmd.Apply<Lesson>(Owner, Amount, Owner, null, false);
    }
}
