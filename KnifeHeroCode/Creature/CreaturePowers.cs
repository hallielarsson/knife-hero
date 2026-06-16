using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

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

/* ProcessedPartPower — hidden/transient. Applied when you process a Throbbing Heart; at the end of
   combat it grants the "new part" to your run deck. PLACEHOLDER reward for now: a fresh Throbbing
   Heart (the body regrows). What the new part actually IS — a roster, an upgrade, AMALGAM's Accept —
   is the open design Hallie flagged ("or i dunno"); deliberately not built ahead. */
public sealed class ProcessedPartPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        for (int i = 0; i < (int)Amount; i++)
            Owner.Player.RunState.AddCard(ModelDb.Card<ThrobbingHeart>(), Owner.Player);
        return Task.CompletedTask;
    }
}

/* Wholeness — the healing axis (THE_CREATURE/HEALING.md, the open keystone, now built).
   A counter you raise ONLY by mending a part (redeeming a Throbbing Heart that survives the combat).
   Vengeance scales with Grief — loud, capped, reset every fight. Wholeness is the orthogonal payoff:
   it amplifies your healing (Mended Hearts heal +1 per Wholeness) and, alongside the permanent max-HP
   raise the mend grants, it is the slow road that out-scales vengeance late.

   PROPOSAL (Claude, Pathetic Governor 2026-06-15): HEALING.md specified "+1 Wholeness per part mended,
   +2 max HP per Wholeness, healing amplified per Wholeness." The permanent max-HP raise is applied
   directly on the creature when a heart mends (SetMaxHp — it persists across combats, the run-long
   body), and this power is the visible in-combat tracker that amplifies healing. A cleaner run-level
   persistence (re-deriving the count each combat) is left for Hallie once a stable run-state hook is
   chosen — flagged rather than guessed. */
public sealed class Wholeness : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
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
