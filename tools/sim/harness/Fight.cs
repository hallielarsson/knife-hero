// Fight.cs — a thin turn sequencer driven on top of the real engine (see Engine.cs).
//
// FIDELITY NOTE (read this before trusting a number out of here): this class does NOT call
// CombatManager.StartTurn/EndPlayerTurnPhaseOneInternal/etc. It replicates their call *order* by
// hand, using the same public Cmd/Hook entry points those methods call. That order was read
// directly out of CombatManager.cs in .decompiled/ (not recalled/guessed), specifically:
//   start of player turn:  Hook.BeforeSideTurnStart -> reset energy -> Hook.AfterEnergyReset ->
//                           Hook.BeforeHandDraw -> CardPileCmd.Draw -> Hook.AfterPlayerTurnStart
//   end of player turn:    Hook.BeforeTurnEnd -> DoTurnEnd (per-hand-card ethereal-exhaust +
//                           HasTurnEndInHandEffect wrapper, see below) -> Hook.BeforeFlush -> discard
//                           non-retained hand cards -> Hook.AfterFlush -> PlayerCombatState.EndOfTurnCleanup
//   enemy turn:            Creature.TakeTurn() (the real per-monster AI + move execution) per enemy
// What this sequencer does NOT reproduce from CombatManager: the AutoPrePlay/AutoPostPlay hook
// phases (auto-played cards from effects like Mayhem/Necronomicon), multiplayer turn-readiness
// bookkeeping, and the pause/unpause frame-wait loop (irrelevant here: TestMode.IsOn makes
// NonInteractiveMode.IsActive true, so CombatManager.WaitForUnpause is a no-op in this process).
// If a card or power under test depends on AutoPrePlay/AutoPostPlay, this harness will not
// exercise that path faithfully - see tools/sim/README.md.
//
// FIDELITY FIX (found 2026-07-11 while instrumenting The Creature, see tools/sim/README.md and
// CreatureInstrument.cs): EndTurn originally skipped CombatManager.DoTurnEnd entirely - the method
// that walks the hand and (a) exhausts still-in-hand Ethereal cards whose Hook.ShouldEtherealTrigger
// says yes, and (b) calls CardModel.OnTurnEndInHandWrapper on every card with
// HasTurnEndInHandEffect (which itself later self-exhausts if also Ethereal). That is the ENTIRE
// mechanism behind any "does something at end of turn while sitting in your hand" card - base-game
// Status/Curse cards (Wound-alikes, Doubt, Shame, Regret, Decay, Debt, Toxic, Infection, Burn,
// BadLuck, Beckon) as well as The Creature's Vexing Memory and Festering Wound. Its absence was
// silent: no exception, just the effect never firing. First 10-fight sanity run of "creature" mode
// with this missing showed Grief pinned at 0 and HP exactly flat across every single fight despite
// Vexing Memory/Festering Wound both being present and (per the fester-rate stat) definitely landing
// in hand - the sign that gave it away. Fixed below by adding the same DoTurnEnd logic, read
// directly out of CombatManager.cs in .decompiled/ (CombatManager.DoTurnEnd, called between
// Hook.BeforeTurnEnd and Hook.BeforeFlush in EndPlayerTurnPhaseOneInternal), with only the
// multiplayer HookPlayerChoiceContext/OrbQueue plumbing stripped, same pattern as every other method
// in this file.
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

namespace KnifeHero.Sim;

public sealed class Fight
{
    public Player Player { get; }
    public CombatState State { get; }
    public int TurnNumber { get; private set; } = 1;

    // The real engine removes creatures from CombatState.Enemies once they die (via
    // CombatManager.RemoveCreature/CombatState.RemoveCreature triggered off the death hook), so
    // "no enemies left in the list" - not "every enemy in the list is dead" - is what victory looks
    // like. Checking State.Enemies.All(dead) on an already-emptied list is vacuously true and was an
    // early bug in this harness: it reported every won fight as a 1-turn loss because the dead (and
    // by then removed) enemy made both IsOver and the old PlayerWon's Count>0 guard disagree.
    public bool IsOver => Player.Creature.IsDead || State.Enemies.Count == 0;
    public bool PlayerWon => State.Enemies.Count == 0 && !Player.Creature.IsDead;

    private static readonly MethodInfo OnPlayMethod = typeof(CardModel).GetMethod("OnPlay", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingMethodException("CardModel.OnPlay not found - sts2.dll API drifted, re-check .decompiled/");

    public Fight(Player player, CombatState state)
    {
        Player = player;
        State = state;
        foreach (var enemy in state.Enemies)
            enemy.Monster.RollMove(state.Players.Select(p => p.Creature));
    }

    /// <summary>Every distinct power currently on any allied creature (players + pets). Used for
    /// the "distinct Powers" resource-curve metric the design docs call out as the thing the
    /// prototype Python sim caught going wrong (Recombinant's payoff silently never scaling).</summary>
    public int DistinctAlliedPowerCount => State.Allies.SelectMany(c => c.Powers).Select(p => p.GetType()).Distinct().Count();

    public async Task StartTurn()
    {
        var ctx = new BlockingPlayerChoiceContext();
        var creatures = new List<Creature> { Player.Creature };
        await Hook.BeforeSideTurnStart(State, CombatSide.Player, creatures);

        // FIDELITY FIX (found 2026-07-11 alongside the DoTurnEnd gap, see EndTurn's header note and
        // tools/sim/README.md): StartTurn was missing Creature.AfterTurnStart (which calls the
        // private ClearBlock — skipped only on turn 1, exactly as the real method does) and
        // Hook.AfterBlockCleared, read out of CombatManager.cs's turn-start method between
        // Hook.BeforeSideTurnStart and SetupPlayerTurn's energy reset. Without this, Block NEVER
        // clears between turns in this harness - it only ever accumulates. That's not
        // Creature-specific: it would have silently inflated survivability in every past batch run
        // that played any Block-granting card (GayBlade included), by letting one turn's Block go on
        // absorbing damage indefinitely instead of expiring at the start of the blocking creature's
        // next turn as it should. It's what made The Creature's first instrumented runs come back
        // "100% win rate, net HP delta exactly 0 in every fight" - Annotate's Block, once granted,
        // was silently permanent, fully absorbing both Axebot's attacks and Vexing Memory/Festering
        // Wound's own grief self-damage forever. Fixed by calling the real
        // Creature.AfterTurnStart/Hook.AfterBlockCleared pair here, in the same position CombatManager
        // calls it.
        foreach (var creature in creatures)
            await creature.AfterTurnStart(CombatSide.Player);
        foreach (var creature in creatures)
            await Hook.AfterBlockCleared(State, creature);

        if (Hook.ShouldPlayerResetEnergy(State, Player))
            Player.PlayerCombatState!.ResetEnergy();
        else
            Player.PlayerCombatState!.AddMaxEnergyToCurrent();
        await Hook.AfterEnergyReset(State, Player);

        await Hook.BeforeHandDraw(State, Player, ctx);
        decimal handDraw = Hook.ModifyHandDraw(State, Player, 5m, out _);
        await CardPileCmd.Draw(ctx, handDraw, Player, fromHandDraw: true);
        await Hook.AfterPlayerTurnStart(State, ctx, Player);
        await Hook.AfterSideTurnStart(State, CombatSide.Player, creatures);
    }

    public IReadOnlyList<CardModel> Hand => PileType.Hand.GetPile(Player).Cards;

    public bool CanAfford(CardModel card) => Player.PlayerCombatState!.Energy >= card.EnergyCost.GetAmountToSpend();

    /// <summary>Play a card from hand. targetIndex indexes State.Enemies; ignored for untargeted cards.</summary>
    public async Task PlayCard(CardModel card, int? targetIndex)
    {
        var ctx = new BlockingPlayerChoiceContext();
        Creature? target = targetIndex.HasValue ? State.Enemies[targetIndex.Value] : null;
        int cost = card.EnergyCost.GetAmountToSpend();
        Player.PlayerCombatState!.LoseEnergy(cost);

        // FIDELITY FIX (found 2026-07-11 alongside the DoTurnEnd/block-clear gaps, see EndTurn's
        // header note): ResultPile was hardcoded to PileType.Discard. The real
        // CardModel.GetResultPileTypeForCardPlay() (.decompiled/.../CardModel.cs) sends Power-type
        // cards to PileType.None (they never sit in Discard - their effect already applied via
        // PowerCmd, the physical card is just removed) and cards with ExhaustOnNextPlay or the
        // Exhaust keyword to PileType.Exhaust, Discard only otherwise. Hardcoding Discard meant every
        // played card - including every Exhaust-keyword card (Galvanism, Solitude, Wretchedness, Fire
        // Stolen, all four of The Creature's "broadening" Books) and every Power card (Marginalia,
        // Polymath, Become Who You Are) - piled up in Discard instead. For The Creature specifically
        // this silently zeroed out the Exhaust-pile-size measurement (the healing pool "Read the
        // Remainder" reads from): confirmed by a run showing max Exhaust size pinned at exactly 1
        // (only the one Ethereal Vexing Memory) even in the assemblage-injected deck where four
        // Exhaust-keyword Books get played every fight.
        PileType resultPileType = card.Type == CardType.Power ? PileType.None
            : (card.ExhaustOnNextPlay || card.Keywords.Contains(CardKeyword.Exhaust)) ? PileType.Exhaust
            : PileType.Discard;

        var cardPlay = new CardPlay
        {
            Card = card,
            Target = target,
            ResultPile = resultPileType,
            Resources = new ResourceInfo { EnergySpent = (int)cost, EnergyValue = (int)cost, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = false,
            PlayIndex = 0,
            PlayCount = 1,
        };
        PileType.Hand.GetPile(Player).RemoveInternal(card);
        PileType.Play.GetPile(Player).AddInternal(card);
        await (Task)OnPlayMethod.Invoke(card, new object[] { ctx, cardPlay })!;

        /* FIDELITY FIX (Fable, 2026-07-11): PlayCard invoked OnPlay and then NEVER fired
           Hook.AfterCardPlayed / AfterCardPlayedLate. Those are the POST-RESOLUTION hooks, and they are
           where every "what does this card become / what does the relic do about it" effect lives:
             • relics reacting to a played card (base game: LetterOpener, MummifiedHand, ArtOfWar;
               ours: THE WASH, which turns played Strikes/Defends into Switch Blades)
             • PrideCard.Becomes() — Top Chop cashing out into a Strike, Pillow Princess into a Defend
           Without this, the entire Gay Blade transform engine was a silent no-op IN THE HARNESS ONLY —
           it would have worked in the real game, so this was the harness lying about a working feature,
           which is the exact failure mode a sim exists to avoid. Caught by playing a Defend and watching
           the deck census not change.
           Placed here, after OnPlay resolves and BEFORE the card is moved to its result pile, matching
           Hook.AfterCardPlayed's contract. This is also *why* transforms must live here and never in
           OnPlay: the card is still mid-resolution in OnPlay (the "Rapier stuck floating" bug).

           Only ONE call is needed here: Hook.AfterCardPlayed (see Hook.cs in the decompiled source,
           MegaCrit.Sts2.Core.Hooks.Hook) already iterates every hook-listening AbstractModel twice
           internally — once calling model.AfterCardPlayed(...), then again calling
           model.AfterCardPlayedLate(...). There is no separate public Hook.AfterCardPlayedLate to call;
           AbstractModel.AfterCardPlayedLate is dispatched for you. Do not add a second call here — it
           doesn't exist on Hook (CS0117) and isn't needed even if it did. */
        await Hook.AfterCardPlayed(State, ctx, cardPlay);

        if (!card.HasBeenRemovedFromState)
        {
            if (resultPileType == PileType.Exhaust)
            {
                await CardCmd.Exhaust(ctx, card);
            }
            else
            {
                PileType.Play.GetPile(Player).RemoveInternal(card);
                if (resultPileType == PileType.Discard)
                    PileType.Discard.GetPile(Player).AddInternal(card);
                // PileType.None (Power cards): removed from Play, added nowhere - matches the real
                // engine leaving a played Power card out of the pile-tracked lifecycle entirely.
            }
        }
        await CombatManager.Instance.CheckForEmptyHand(ctx, Player);
    }

    public async Task EndTurn()
    {
        var ctx = new BlockingPlayerChoiceContext();
        var creatures = new List<Creature> { Player.Creature };
        await Hook.BeforeTurnEnd(State, CombatSide.Player, creatures);
        if (IsOver) return;

        // DoTurnEnd (see FIDELITY FIX note above): ethereal-in-hand exhaust, then turn-end-in-hand
        // effects (Vexing Memory's grief pulse, Festering Wound's grief bite, etc). The real method
        // snapshots both lists up front before acting on either, same order preserved here.
        var handSnapshot = PileType.Hand.GetPile(Player).Cards.ToList();
        var turnEndCards = new List<CardModel>();
        var etherealOnly = new List<CardModel>();
        foreach (var card in handSnapshot)
        {
            if (card.HasTurnEndInHandEffect)
                turnEndCards.Add(card);
            else if (card.Keywords.Contains(CardKeyword.Ethereal) && Hook.ShouldEtherealTrigger(State, card))
                etherealOnly.Add(card);
        }
        foreach (var card in etherealOnly)
            await CardCmd.Exhaust(ctx, card, causedByEthereal: true);
        foreach (var card in turnEndCards)
            await card.OnTurnEndInHandWrapper(ctx);
        if (IsOver) return;

        await Hook.BeforeFlush(State, Player);
        if (IsOver) return;

        var hand = PileType.Hand.GetPile(Player);
        bool shouldFlush = Hook.ShouldFlush(State, Player);
        var toDiscard = hand.Cards.Where(c => !shouldFlush || !c.ShouldRetainThisTurn).ToList();
        var toRetain = hand.Cards.Where(c => shouldFlush && c.ShouldRetainThisTurn).ToList();
        if (toDiscard.Count > 0)
            await CardPileCmd.Add(toDiscard, PileType.Discard);
        await Hook.AfterFlush(State, Player, ctx, toDiscard, toRetain);
        Player.PlayerCombatState!.EndOfTurnCleanup();
        await Hook.AfterTurnEnd(State, CombatSide.Player, creatures);
    }

    public async Task EnemyTurn()
    {
        var creatures = State.Enemies.ToList().Cast<Creature>().ToList();
        foreach (var enemy in State.Enemies.ToList())
        {
            if (!State.ContainsCreature(enemy) || enemy.IsDead) continue;
            await enemy.TakeTurn();
        }
        await Hook.AfterTurnEnd(State, CombatSide.Enemy, creatures);
        foreach (var enemy in State.Enemies.Where(e => !e.IsDead))
            enemy.Monster.RollMove(State.Players.Select(p => p.Creature));
        TurnNumber++;
    }
}
