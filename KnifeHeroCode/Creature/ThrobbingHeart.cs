using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Throbbing Heart — the heart of The Creature: a PART that starts as a curse. Eternal + Retain, so it
   sits in your hand demanding attention. When drawn it spits up an intrusive Vexing Memory. You can
   only PROCESS it once you've both grieved and learned enough (2 Grief + 2 Lessons) — that resets ALL
   your Grief and Lessons and marks the part redeemed; it doesn't exhaust. If it's still in your deck
   at the end of combat (redeemed, not festered), it UPGRADES into a new part. If you DON'T redeem it
   within 3 turns, it festers into a Festering Wound curse. Redeem your parts or carry the rot.
   (Hallie's design; "what it upgrades INTO" is still the open keystone — placeholder for now.) */
public sealed class ThrobbingHeart() : CreatureCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    private const int TurnsToFester = 3;
    private int _turnsLeft = TurnsToFester;
    private bool _redeemed;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Eternal, CardKeyword.Retain };

    // Process at 2 Grief AND 2 Lessons.
    protected override bool IsPlayable => GriefAmount() >= 2 && LessonAmount() >= 2;

    // When drawn (and not yet redeemed), the wound throbs: an intrusive Vexing Memory lands in hand.
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this || _redeemed) return;
        await Cmd.Wait(0.25f);
        var vex = CombatState.CreateCard<VexingMemory>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(vex, PileType.Hand, addedByPlayer: false);
    }

    // Unredeemed, it rots: each turn in hand counts down, and at zero it festers into a curse.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (_redeemed || player != Owner || Pile?.Type != PileType.Hand) return;
        _turnsLeft--;
        if (_turnsLeft <= 0)
            await CardCmd.Transform(this, CombatState.CreateCard<FesteringWound>(Owner));
    }

    // Process: clear Vexing Memories, RESET all Grief and Lessons, mark redeemed. No exhaust.
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var vexes = CardPile.GetCards(Owner, PileType.Hand, PileType.Draw, PileType.Discard)
            .Where(c => c is VexingMemory).ToList();
        if (vexes.Count > 0)
            await CardPileCmd.RemoveFromCombat(vexes);

        foreach (var p in Owner.Creature.Powers.Where(p => p is Grief || p is Lesson).ToList())
            await PowerCmd.Remove(p);

        _redeemed = true;
    }

    // The mend happens if it's still in your deck at end of combat (redeemed, not festered):
    // the part grows WHOLE. It transforms into a stable Mended Heart, you gain +1 Wholeness, and your
    // body becomes permanently more durable — +2 max HP that persists across the whole run. This is
    // HEALING.md's keystone: the slow, permanent healing axis that vengeance (capped, reset each fight)
    // can never be. (PROPOSAL — Claude, Pathetic Governor 2026-06-15; numbers Hallie's to mint.)
    private const int MaxHpPerWholeness = 2;

    public override async Task AfterCombatVictory(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        if (!_redeemed) return;

        // +1 Wholeness (the healing axis; amplifies Mended Heart healing while in combat).
        await PowerCmd.Apply<Wholeness>(Owner.Creature, 1m, Owner.Creature, this, false);

        // +2 max HP, permanent and run-long — the body made durable by the work of mending.
        await CreatureCmd.SetMaxHp(Owner.Creature, Owner.Creature.MaxHp + MaxHpPerWholeness);
        await CreatureCmd.Heal(Owner.Creature, MaxHpPerWholeness, false);

        // The part is whole now: a stable Mended Heart, not another throbbing curse.
        await CardCmd.Transform(this, CombatState.CreateCard<MendedHeart>(Owner));
    }

    private int GriefAmount() => (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
    private int LessonAmount() => (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
}
