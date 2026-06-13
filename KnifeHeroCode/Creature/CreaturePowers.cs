using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

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
