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

/* Throbbing Heart — Hallie's design, the heart (ha) of The Creature: a PART that starts as a curse.
   A part of your body that won't leave (unremovable) and that, every time you draw it, spits up an
   intrusive Vexing Memory. You can't power through it — you can only PROCESS it, once you've both
   GRIEVED enough and LEARNED enough (3 Grief + 3 Lessons). Then: the grief clears (remove all Vexing
   Memories), the wound resolves (Exhaust), and at the end of combat you grow a new part (a Mended
   Heart). Metabolizing a wound into growth, as the win condition. AMALGAM's Accept, mechanized. */
public sealed class ThrobbingHeart() : CreatureCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    private const int TurnsToFester = 3;
    private int _turnsLeft = TurnsToFester;

    // Eternal = unremovable; Retain = it sits in your hand, festering, until you process it.
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Eternal, CardKeyword.Retain };

    // Only playable once you've sat with the grief AND learned enough to process it.
    protected override bool IsPlayable => GriefAmount() >= 3 && LessonAmount() >= 3;

    // If you don't redeem it in time, the part rots: each turn it's in your hand counts down, and
    // when the clock runs out it festers into a Festering Wound curse (Hallie's design).
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Pile?.Type != PileType.Hand) return;
        _turnsLeft--;
        if (_turnsLeft <= 0)
            await CardCmd.Transform(this, CombatState.CreateCard<FesteringWound>(Owner));
    }

    // When you draw it, the wound throbs: an intrusive Vexing Memory lands in your hand.
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await Cmd.Wait(0.25f);
        var vex = CombatState.CreateCard<VexingMemory>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(vex, PileType.Hand, addedByPlayer: false);
    }

    // Process it: clear every Vexing Memory, exhaust the wound, and queue a new part for combat's end.
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var vexes = CardPile.GetCards(Owner, PileType.Hand, PileType.Draw, PileType.Discard)
            .Where(c => c is VexingMemory).ToList();
        if (vexes.Count > 0)
            await CardPileCmd.RemoveFromCombat(vexes);

        await CardCmd.Exhaust(choiceContext, this, causedByEthereal: false);
        await PowerCmd.Apply<ProcessedPartPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }

    private int GriefAmount() => (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
    private int LessonAmount() => (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
}
