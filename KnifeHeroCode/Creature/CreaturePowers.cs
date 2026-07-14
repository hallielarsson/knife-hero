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

/* Lesson — the depth resource. Inert; spent by mending Parts, read by Quote at Length. */
public sealed class Lesson : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/* Grief — a READOUT, not a resource: MendedBody SETs it each turn to the number of parts of you that
   are not whole (scars count double). ⚠ Nothing may apply or spend it directly — the next Recount
   would overwrite the change and the number would silently disagree with the deck. */
public sealed class Grief : KnifeHeroPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

/* Marginalia — play a Book or a Power, gain a Lesson.
   ⚠ The engine has no on-power-GAINED hook, so this rides AfterCardPlayed instead. */
public sealed class MarginaliaPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card is IBook || cardPlay.Card.Type == CardType.Power)
            await PowerCmd.Apply<Lesson>(context, Owner, 1m, Owner, null, false);
    }
}

/* Wholeness — a READOUT, like Grief: MendedBody SETs it each turn to the number of mended parts in
   your deck. Every mended organ scales on it. ⚠ Do not apply or spend it directly.

   ⚠ IT MUST NOT HEAL. The turn-start heal that used to live here is deliberately a no-op override, kept
   so the reason survives: it scaled with TURNS SPENT against a bleed of ~1, so the strictly correct line
   was to stop killing the enemy and farm run-level HP. Sustain now pays once, at combat end, in
   MendedBody.AfterCombatVictory. */
public sealed class Wholeness : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) =>
        Task.CompletedTask;
}

/* BecomeWhoYouArePower — each turn, gain Strength equal to (Wholeness × stacks).
   ⚠ BALANCE: it reads Wholeness, NOT distinct-Power count. Counting Powers opened at +3 Strength/turn
   for free — Grief, Wholeness and Lesson are almost always on you — and compounded from there. */
public sealed class BecomeWhoYouArePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0m) return;

        int whole = (int)(Owner.GetPower<Wholeness>()?.Amount ?? 0m);
        if (whole <= 0) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, whole * Amount, Owner, null, false);
    }
}

/* Polymath — gain a Lesson per stack at the start of each turn. */
public sealed class PolymathPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        Flash();
        await PowerCmd.Apply<Lesson>(choiceContext, Owner, Amount, Owner, null, false);
    }
}


/* THE APPETITE — take a Part at the start of every turn, unconditionally, per stack.
   The unconditional version; MendedBody has the "only if nothing is broken" floor. */
public sealed class AppetitePower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        Flash();
        for (int i = 0; i < (int)Amount; i++)
            await CardPileCmd.AddGeneratedCardToCombat(
                Cards.Parts.Random(Owner.Player), PileType.Hand, Owner.Player);
    }
}
