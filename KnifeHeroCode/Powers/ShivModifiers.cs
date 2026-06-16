using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Shiv Modifier Engine — playable first cut (SHIV_MODIFIER_ENGINE_SPEC.md).
   Hallie said: do the proposal, then reap in playtesting — felt qualia beat held instincts.
   So these ship turned-on, as turn-wide buff Powers on the player, the proven FanOfKnives
   pattern (the base Shiv already checks player powers; we ride the same road). Each buff watches
   for a shiv being played (CardTag.Shiv) and adds its rider, then strips itself at end of turn.

   These two are the safe, strong-signal pair to play first. The other four (Quiver, Long Bow,
   Ricochet, Pin) live in the spec and want the damage-pipeline hooks — built once these feel right.

   All numbers below are // PROPOSAL — set to be *playable*, not final. Tune by feel. */

/* Poison Coating — this turn, every shiv you play also lays Poison on its target.
   // PROPOSAL: 3 Poison per shiv. Hallie tunes by feel. */
public sealed class PoisonCoatingPower : KnifeHeroPower
{
    // ART NOTE for the Art Mapper: no poison_coating_power.png yet — fall back to the generic
    // power icon so playtest loads. Swap to a real icon when one lands.
    public override string CustomPackedIconPath => "power.png".PowerImagePath();
    public override string CustomBigIconPath => "power.png".BigPowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;   // stacks = Poison per shiv

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (!cardPlay.Card.Tags.Contains(CardTag.Shiv)) return;
        if (cardPlay.Target == null) return;

        await PowerCmd.Apply<PoisonPower>(cardPlay.Target, Amount, Owner, null, false);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await PowerCmd.Remove(this);
    }
}

/* Explosive Tip — this turn, the shivs you play hit ALL enemies and Exhaust.
   The AoE half is literally FanOfKnives (already in the engine); we add it, and add Exhaust to
   each shiv as it's played, then strip both at end of turn. */
public sealed class ExplosiveTipPower : KnifeHeroPower
{
    // ART NOTE for the Art Mapper: no explosive_tip_power.png yet — generic fallback for playtest.
    public override string CustomPackedIconPath => "power.png".PowerImagePath();
    public override string CustomBigIconPath => "power.png".BigPowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // Borrow FanOfKnives so shivs go AoE the moment this lands (Superfan uses the same power).
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.GetPower<FanOfKnivesPower>() == null)
            await PowerCmd.Apply<FanOfKnivesPower>(Owner, 1m, Owner, null, false);
    }

    // The "explosive" half: a shiv you play this turn Exhausts after it goes off.
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (!cardPlay.Card.Tags.Contains(CardTag.Shiv)) return;
        if (cardPlay.Card.Keywords.Contains(CardKeyword.Exhaust)) return;   // base Shiv already exhausts

        await CardCmd.Exhaust(context, cardPlay.Card, causedByEthereal: false);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var fan = Owner.GetPower<FanOfKnivesPower>();
        if (fan != null) await PowerCmd.Remove(fan);
        await PowerCmd.Remove(this);
    }
}
