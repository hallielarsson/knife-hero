// Engine.cs — headless bootstrap for the real Slay the Spire 2 combat engine (sts2.dll).
//
// This is the load-bearing piece of the whole harness: it gets the REAL CardModel/PowerModel/
// DamageCmd/Hook pipeline from the actual game DLL running inside a plain console process, with
// no Godot engine, no window, no scene tree. Every workaround here was found empirically (see
// tools/sim/README.md "Fidelity" section for the full account). Every shim here is either (a) a
// redirect of a presentation/logging/mod-loading call to something that doesn't need a live Godot
// engine, with zero effect on any gameplay decision, or (b) in exactly one case (the shuffle
// animation pacing, see ApplyHeadlessShuffleShim below) a line-for-line copy of the real method
// with only the animation-timing wait removed - gameplay-relevant statements (shuffle order, RNG
// draws, hook calls) are untouched and in the same order as the decompiled original.
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace KnifeHero.Sim;

public static class Engine
{
    private static bool _booted;

    /// <summary>
    /// Boots the real sts2 combat engine in this process. Idempotent — safe to call more than once
    /// (batch mode calls this once per process, not once per fight).
    /// </summary>
    public static void Boot()
    {
        if (_booted) return;
        _booted = true;

        ApplyHeadlessLogShim();
        ApplyHeadlessShuffleShim();
        ApplySeedableRngShim();

        // TestMode.IsOn is the game's own flag for "we are not a real interactive session" (its doc
        // comment: "True when we're running unit tests, true when we're running the normal game").
        // It's what makes CreatureCmd.TriggerAnim, WaitForUnpause, etc. skip animation/pause waits
        // that would otherwise stall forever with no live NCreature/scene tree to drive them.
        TestMode.TurnOnInternal();

        RegisterModels();
    }

    // Logger's static constructor calls Godot.OS.GetCmdlineArgs()/OS.HasFeature("editor") to decide
    // which ILogPrinter to use — UNCONDITIONALLY, before it even checks TestMode.IsOn. Those are
    // native calls into an engine that was never booted here; they don't throw a catchable .NET
    // exception, they segfault the process (SIGSEGV / exit 139). And even past that, the printer it
    // would pick (ConsoleLogPrinter) calls Godot's GD.Print/GD.PrintErr directly on every log line,
    // which is the same problem. Both are logging/presentation, not gameplay logic, so we redirect
    // them to plain Console output via two narrow Harmony prefixes applied before anything else in
    // sts2.dll is touched.
    private static void ApplyHeadlessLogShim()
    {
        var harmony = new Harmony("tools.sim.harness.headless-log-shim");

        var getIsRunningFromGodotEditor = typeof(Logger).GetMethod(
            "GetIsRunningFromGodotEditor", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("Logger.GetIsRunningFromGodotEditor not found - sts2.dll API drifted, re-check .decompiled/");
        harmony.Patch(getIsRunningFromGodotEditor,
            prefix: new HarmonyMethod(typeof(LogShim).GetMethod(nameof(LogShim.SkipGodotEditorCheck))));

        var consoleLogPrinterType = Type.GetType("MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter, sts2")
            ?? throw new TypeLoadException("MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter not found - sts2.dll API drifted");
        var consolePrint = consoleLogPrinterType.GetMethod("Print")
            ?? throw new MissingMethodException("ConsoleLogPrinter.Print not found - sts2.dll API drifted");
        harmony.Patch(consolePrint,
            prefix: new HarmonyMethod(typeof(LogShim).GetMethod(nameof(LogShim.PrintToConsoleInstead))));
    }

    // CardPileCmd.Shuffle (real reshuffle-discard-into-draw-pile logic, found empirically by
    // playing real fights until the deck ran out and a mid-combat reshuffle triggered - it doesn't
    // happen on the very first, pre-built shuffle at combat start, only later) staggers adding each
    // card back with a small delay for the "cards flying into the deck" animation, and decides
    // whether to actually wait via `((SceneTree)Engine.GetMainLoop()).Root.GetProcessDeltaTime()`.
    // That's a native Godot call with no engine behind it here - SIGSEGV, not a catchable exception,
    // same failure mode as the Logger cctor. Godot.Engine.GetMainLoop() can't be made safe by itself
    // (there is no fake SceneTree we can hand back - SceneTree is itself a native-backed GodotObject
    // we cannot construct outside a running engine), so instead of patching that one native call we
    // replace the whole Shuffle method with a line-for-line copy of the decompiled original (see
    // .decompiled/MegaCrit/sts2/Core/Commands/CardPileCmd.cs) with ONLY the animation-pacing wait
    // removed. Every gameplay-relevant statement is unchanged and in the same order: the same
    // list.StableShuffle(player.RunState.Rng.Shuffle), the same real Hook.ModifyShuffleOrder call,
    // the same DebugForcedTopCardOnNextShuffle handling, the same real Hook.AfterShuffle call. This
    // is the one place in the harness that replaces engine code rather than only skipping a native
    // call, and it is called out here and in tools/sim/README.md for exactly that reason.
    private static void ApplyHeadlessShuffleShim()
    {
        var harmony = new Harmony("tools.sim.harness.headless-shuffle-shim");
        var cardPileCmdType = Type.GetType("MegaCrit.Sts2.Core.Commands.CardPileCmd, sts2")
            ?? throw new TypeLoadException("MegaCrit.Sts2.Core.Commands.CardPileCmd not found - sts2.dll API drifted");
        var shuffle = cardPileCmdType.GetMethod("Shuffle", BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException("CardPileCmd.Shuffle not found - sts2.dll API drifted, re-check .decompiled/");
        harmony.Patch(shuffle,
            prefix: new HarmonyMethod(typeof(ShuffleShim).GetMethod(nameof(ShuffleShim.ShuffleWithoutAnimationPacing))));
    }

    // NullRunState.Rng always returns `new RunRngSet(string.Empty)` - a fresh, IDENTICALLY-seeded
    // RNG set on every access. That's correct for its one real caller in the base game (a genuine
    // "there is no run" fallback), but it means every fight built on NullRunState - shuffle order,
    // targeting, everything derived from RunState.Rng - is bit-for-bit identical across fights. This
    // was caught empirically: batch mode reported the exact same win/turns/HP outcome across 500
    // fights, which would have been a very quiet way for this harness to produce confident, wrong
    // "resource curve" distributions (exactly the risk this whole project exists to avoid). Since we
    // deliberately use NullRunState for everything (see CreateReadyPlayer/SetUpCombat), we patch its
    // Rng getter to read a process-wide seed we control (Engine.SetFightSeed), instead of reimplementing
    // any RNG-consuming gameplay logic itself.
    private static void ApplySeedableRngShim()
    {
        var harmony = new Harmony("tools.sim.harness.seedable-rng-shim");
        var nullRunStateType = Type.GetType("MegaCrit.Sts2.Core.Runs.NullRunState, sts2")
            ?? throw new TypeLoadException("MegaCrit.Sts2.Core.Runs.NullRunState not found - sts2.dll API drifted");
        var rngGetter = nullRunStateType.GetProperty("Rng")?.GetGetMethod()
            ?? throw new MissingMethodException("NullRunState.Rng getter not found - sts2.dll API drifted, re-check .decompiled/");
        harmony.Patch(rngGetter,
            prefix: new HarmonyMethod(typeof(RngShim).GetMethod(nameof(RngShim.UseFightSeed))));
    }

    /// <summary>Sets the seed NullRunState.Rng will use until changed again. Batch mode calls this
    /// once per fight so fights actually differ from each other; interactive mode can call it once
    /// for a reproducible session. Not calling this at all reproduces the all-identical-fights bug
    /// this shim exists to fix.</summary>
    public static void SetFightSeed(string seed) => RngShim.CurrentSeed = seed;

    // ModelDb.Init() insists on ModManager being fully initialized (mod-loading, asset pipeline,
    // Harmony patch application for every installed mod) before it will enumerate mod types. We
    // don't want any of that — we want exactly the base game's models plus KnifeHero's own, nothing
    // else, no touching of disk-based mod manifests or .pck files. ModelDb.Inject(Type) is the
    // same public API mods/tests are meant to use to add one model at a time, so we just drive it
    // ourselves over three sources: the base game's compiled-in subtype list, BaseLib's assembly,
    // and KnifeHero's assembly.
    private static void RegisterModels()
    {
        foreach (var t in AbstractModelSubtypes.All)
            ModelDb.Inject(t);

        foreach (var asm in new[] { typeof(BaseLib.Abstracts.PlaceholderCharacterModel).Assembly, typeof(global::KnifeHero.KnifeHeroCode.Character.KnifeHero).Assembly })
        {
            foreach (var t in MegaCrit.Sts2.Core.Helpers.ReflectionHelper.GetSubtypesFromAssembly(asm, typeof(AbstractModel)))
                ModelDb.Inject(t);
        }

        // NOTE: we deliberately do NOT call ModelDb.InitIds() / ModelIdSerializationCache.Init()
        // here. Both exist purely to back multiplayer/replay wire compression (ModelId <-> a compact
        // int). ModelIdSerializationCache.Init() only learns about mod-defined model types via
        // ModManager.Mods, which we intentionally never populate (see RegisterModels above) - so it
        // throws "ModelId entry X could not be mapped to any net ID!" the moment it meets any
        // KnifeHero type. AbstractModel.Id itself (the thing gameplay code actually reads - card/
        // power/relic identity) is set at construction time in ModelDb.Inject, independent of
        // InitIds(); only CategorySortingId/EntrySortingId (a display/sort-order nicety used for
        // things like deterministic reward-card ordering) are left at their default of 0. If a future
        // fight-under-test turns out to depend on that sort order, that's the place to come back to.
    }

    /// <summary>
    /// Create a Player for a new run, playing the given character, with a fully-owned starting deck
    /// ready to enter combat.
    ///
    /// FIDELITY NOTE: Player.CreateForNewRun's own doc comment warns "these models will not work
    /// properly until the player is added to a RunState and/or CombatState" — concretely, the
    /// player's starting-deck CardModels come back with Owner == null. In real gameplay this gets
    /// fixed up when the player is added to a real RunState (run start / map traversal code we are
    /// not running here). We do the equivalent minimal fix-up by hand: set Owner on every deck card.
    /// This was found by hitting the NullReferenceException it causes and reading the stack trace
    /// back to CombatState.Contains -> cardModel.Owner, not by guessing.
    /// </summary>
    public static Player CreateReadyPlayer<TCharacter>(ulong netId = 1) where TCharacter : MegaCrit.Sts2.Core.Models.CharacterModel
    {
        var unlockState = MegaCrit.Sts2.Core.Unlocks.UnlockState.all;
        var player = Player.CreateForNewRun<TCharacter>(unlockState, netId);

        /* ⚠ FIDELITY FIX (Fable, 2026-07-13) — THE HARNESS WAS DEAF TO EVERY TURN-END HOOK.
           This one line was worth ~300 fights of wrong answers, so here is the whole story.

           `Hook.BeforeTurnEnd` and `Hook.AfterTurnEnd` both open with:

               ulong? netId = LocalContext.NetId;
               if (!netId.HasValue) return;          // <-- silent. no log, no throw.

           We created players with a NetId but never told LocalContext which player is "me". So both of
           those hooks returned immediately, every turn, in every sim ever run from this harness — and
           they are the ONLY dispatchers of:

               BeforeSideTurnEndVeryEarly / BeforeSideTurnEndEarly / BeforeSideTurnEnd
               AfterSideTurnEnd / AfterSideTurnEndLate

           Which quietly deleted, from the simulation only:
             • **The Creature's entire mend.** PartCard defers its transform to BeforeSideTurnEnd (that
               deferral IS the float-bug fix). No hook, no transform. The batch reported a 92% fester
               rate and ZERO redemptions across 300 fights and I nearly "fixed" a design that was fine.
             • **Every Pride's held effect.** PrideCard.WhileFlown fires in BeforeSideTurnEnd. Every
               measurement of the Gay Blade's flag engine was taken with half the engine switched off.
             • Stealth's end-of-turn decay, and anything else that ends a turn.

           The bitter part: the harness's whole selling point is "it isn't a model of sts2.dll, it IS
           sts2.dll." That's true, and it's exactly why a gap like this is so dangerous — the fidelity
           is real everywhere else, so you trust the number. A hook that returns silently is worse than
           one that throws, because a throw would have found this in June.

           So: measurement finds THAT something is broken. It cannot tell you the measuring instrument
           is the broken thing. Only reading the engine can do that. */
        MegaCrit.Sts2.Core.Context.LocalContext.NetId = netId;

        foreach (var card in player.Deck.Cards)
            card.Owner = player;
        foreach (var relic in player.Relics)
            if (relic.Owner == null)
                throw new InvalidOperationException($"Relic {relic.Id} has null Owner after CreateForNewRun - sts2.dll behavior changed, re-check the Owner fix-up above.");
        return player;
    }

    /// <summary>
    /// Build a real CombatState with the given player against the given enemies, and register it
    /// with the real CombatManager singleton.
    ///
    /// FIDELITY NOTE: we deliberately do NOT call CombatManager.StartCombatInternal(). That method
    /// drives full turn-based presentation: NRunMusicController, NCombatRoom banners, FTUE popups,
    /// SaveManager.SaveRun, achievement checks - none of which apply to a local sim, and some of
    /// which (encounter.HasBgm on a null Encounter, since we don't build a real EncounterModel) will
    /// NRE outright. Instead we call CombatManager.SetUpCombat (the real bookkeeping: player combat
    /// state, deck shuffle, creature registration, StateTracker subscription) and then flip the same
    /// private IsInProgress flag StartCombatInternal would eventually have flipped. Everything after
    /// that point - card plays, damage, powers, hooks - runs through the exact same CombatManager /
    /// DamageCmd / PowerCmd / Hook code the real game uses; only the turn *sequencing* (draw, energy,
    /// end-of-turn cleanup, enemy intent execution) is driven by our own Fight loop instead of
    /// CombatManager's Node-orchestrated state machine. See Fight.cs for exactly which calls we make
    /// and in what order, mirroring CombatManager.StartTurn/EndPlayerTurnPhase*/ExecuteEnemyTurn.
    /// </summary>
    public static CombatState SetUpCombat(Player player, IEnumerable<MegaCrit.Sts2.Core.Models.MonsterModel> enemyMonsters)
    {
        var state = new CombatState();
        state.AddPlayer(player);
        foreach (var monster in enemyMonsters)
        {
            var mutableMonster = monster.ToMutable();
            var creature = state.CreateCreature(mutableMonster, CombatSide.Enemy, null);
            state.AddCreature(creature);
        }

        var combatManager = CombatManager.Instance;
        combatManager.SetUpCombat(state);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(combatManager, true);
        return state;
    }

    private static readonly FieldInfo CombatManagerStateField = typeof(CombatManager)
        .GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingFieldException("CombatManager._state not found - sts2.dll API drifted, re-check .decompiled/");

    /// <summary>
    /// Resets CombatManager between fights in batch mode. CombatManager is a process-wide singleton
    /// (CombatManager.Instance), so batch mode must tear each fight down before starting the next one
    /// or SetUpCombat's "make sure to reset the combat before setting up a new one" guard will throw.
    ///
    /// FIDELITY NOTE: we do NOT call the real CombatManager.Reset(). It unconditionally touches
    /// RunManager.Instance.ActionQueueSynchronizer, which is only initialized when a real run/lobby
    /// is started (multiplayer action-queue plumbing we never set up) and is null here, so it NREs.
    /// We only need the one piece of state Reset() clears that SetUpCombat's guard actually checks -
    /// the private _state field - so we clear that and IsInProgress directly by reflection instead.
    /// </summary>
    public static void ResetCombat()
    {
        var combatManager = CombatManager.Instance;
        CombatManagerStateField.SetValue(combatManager, null);
        typeof(CombatManager).GetProperty(nameof(CombatManager.IsInProgress))!.SetValue(combatManager, false);
    }
}

internal static class LogShim
{
    public static bool SkipGodotEditorCheck(ref bool __result)
    {
        __result = false;
        return false;
    }

    public static bool PrintToConsoleInstead(object logLevel, string text, int skipFrames)
    {
        string level = logLevel.ToString()!.ToUpperInvariant();
        string line = $"[{level}] {text}";
        if (level is "ERROR" or "WARN")
            Console.Error.WriteLine(line);
        else
            Console.WriteLine(line);
        return false;
    }
}

internal static class RngShim
{
    [ThreadStatic] public static string? CurrentSeed;

    public static bool UseFightSeed(ref MegaCrit.Sts2.Core.Runs.RunRngSet __result)
    {
        __result = new MegaCrit.Sts2.Core.Runs.RunRngSet(CurrentSeed ?? string.Empty);
        return false;
    }
}

internal static class ShuffleShim
{
    // Line-for-line copy of MegaCrit.Sts2.Core.Commands.CardPileCmd.Shuffle from
    // .decompiled/MegaCrit/sts2/Core/Commands/CardPileCmd.cs, with the animation-pacing wait
    // (`((SceneTree)Engine.GetMainLoop()).Root.GetProcessDeltaTime()` and the `await Cmd.Wait(num)`
    // it gates) removed. See Engine.ApplyHeadlessShuffleShim for why. If sts2.dll's real Shuffle
    // changes, this copy goes stale silently - it is intentionally small and reads close to its
    // source so a future diff against .decompiled/ is easy.
    public static bool ShuffleWithoutAnimationPacing(PlayerChoiceContext choiceContext, Player player, ref Task __result)
    {
        __result = Run(choiceContext, player);
        return false;
    }

    private static async Task Run(PlayerChoiceContext choiceContext, Player player)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        CardPile drawPile = PileType.Draw.GetPile(player);
        List<CardModel> list = PileType.Discard.GetPile(player).Cards.ToList();
        HashSet<CardModel> drawPileCards = drawPile.Cards.ToHashSet();
        list.AddRange(drawPileCards);
        list.StableShuffle(player.RunState.Rng.Shuffle);
        Hook.ModifyShuffleOrder(player.Creature.CombatState, player, list, isInitialShuffle: false);
        foreach (CardModel item in drawPileCards)
            drawPile.RemoveInternal(item, silent: true);
        if (CombatManager.Instance.DebugForcedTopCardOnNextShuffle != null)
        {
            if (!list.Remove(CombatManager.Instance.DebugForcedTopCardOnNextShuffle))
                throw new InvalidOperationException("Could not find card " + CombatManager.Instance.DebugForcedTopCardOnNextShuffle.Id.Entry + " in discard pile.");
            list.Insert(0, CombatManager.Instance.DebugForcedTopCardOnNextShuffle);
            CombatManager.Instance.DebugClearForcedTopCardOnNextShuffle();
        }
        foreach (CardModel item2 in list)
        {
            if (!drawPileCards.Contains(item2))
            {
                await CardPileCmd.Add(item2, drawPile, skipVisuals: true);
                if (CombatManager.Instance.IsOverOrEnding) return;
            }
            else
            {
                drawPile.AddInternal(item2, -1, silent: true);
            }
        }
        if (!CombatManager.Instance.IsOverOrEnding)
            await Hook.AfterShuffle(player.Creature.CombatState, choiceContext, player);
    }
}
