// Throwaway spike: can we load sts2.dll + GodotSharp.dll in a plain console app and touch
// the combat model layer without a running Godot engine? Read-only against the Steam install.
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Logging;

Console.WriteLine("Step 0: process started, CLR loaded our own assembly.");

try
{
    Console.WriteLine("Step 0b: Harmony-patching MegaCrit.Sts2.Core.Logging so it never calls into");
    Console.WriteLine("  native Godot.OS / GD.Print (there is no engine running here).");
    var harmony = new Harmony("tools.sim.spike.headless-shim");

    // Logger's static constructor calls Godot.OS.GetCmdlineArgs()/OS.HasFeature() UNCONDITIONALLY
    // (before it even checks TestMode.IsOn) to decide which ILogPrinter to use. Those are native
    // calls into an engine that was never booted here, and segfault (SIGSEGV) rather than throwing
    // a catchable .NET exception. Prefix it to skip the native calls and just report "not the editor".
    var getIsRunningFromGodotEditor = typeof(Logger).GetMethod("GetIsRunningFromGodotEditor", BindingFlags.NonPublic | BindingFlags.Static);
    harmony.Patch(getIsRunningFromGodotEditor, prefix: new HarmonyMethod(typeof(HeadlessLogShim).GetMethod(nameof(HeadlessLogShim.SkipGodotEditorCheck))));

    // Even with ConsoleLogPrinter chosen (not EditorLogPrinter), ConsoleLogPrinter.Print still calls
    // GD.Print/GD.PrintErr directly on every log line. Redirect those to plain Console output instead.
    var consolePrint = Type.GetType("MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter, sts2")!.GetMethod("Print");
    harmony.Patch(consolePrint, prefix: new HarmonyMethod(typeof(HeadlessLogShim).GetMethod(nameof(HeadlessLogShim.PrintToConsoleInstead))));
    Console.WriteLine("  Patches applied.");

    Console.WriteLine("Step 1: turning on TestMode...");
    TestMode.TurnOnInternal();
    Console.WriteLine("  TestMode.IsOn = " + TestMode.IsOn);

    Console.WriteLine("Step 2: touching a Godot value type (Color) via StsColors...");
    var color = MegaCrit.Sts2.Core.Helpers.StsColors.cream;
    Console.WriteLine("  StsColors.cream = " + color);

    Console.WriteLine("Step 3: registering base-game AbstractModel subtypes directly (bypassing ModManager, which we don't want/need)...");
    int injected = 0;
    foreach (var t in MegaCrit.Sts2.Core.Models.AbstractModelSubtypes.All)
    {
        ModelDb.Inject(t);
        injected++;
    }
    Console.WriteLine("  Injected " + injected + " base-game model types.");

    Console.WriteLine("Step 3b: calling ModelIdSerializationCache.Init() (must run before ModelDb.InitIds())...");
    MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache.Init();
    Console.WriteLine("  ModelIdSerializationCache.Init() completed.");

    Console.WriteLine("Step 4: calling ModelDb.InitIds()...");
    ModelDb.InitIds();
    Console.WriteLine("  ModelDb.InitIds() completed.");

    Console.WriteLine("Step 5: counting AllCards...");
    int count = 0;
    foreach (var c in ModelDb.AllCards) count++;
    Console.WriteLine("  AllCards count = " + count);

    Console.WriteLine("Step 6: constructing a Player for Ironclad via Player.CreateForNewRun...");
    var unlockState = MegaCrit.Sts2.Core.Unlocks.UnlockState.all;
    var player = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun<MegaCrit.Sts2.Core.Models.Characters.Ironclad>(unlockState, 1UL);
    Console.WriteLine("  Player created: " + player.Character.Id + " HP=" + player.Creature.CurrentHp + "/" + player.Creature.MaxHp);

    Console.WriteLine("Step 7: building a real CombatState (NullRunState) with the player vs. a TenHpMonster dummy...");
    var combatState = new MegaCrit.Sts2.Core.Combat.CombatState();
    combatState.AddPlayer(player);
    var monster = ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.TenHpMonster>().ToMutable();
    var enemyCreature = combatState.CreateCreature(monster, MegaCrit.Sts2.Core.Combat.CombatSide.Enemy, null);
    combatState.AddCreature(enemyCreature);
    Console.WriteLine($"  Enemy: {monster.Title} HP={enemyCreature.CurrentHp}/{enemyCreature.MaxHp}");

    Console.WriteLine("Step 7b: player.Deck cards have Owner == null (they're only owned once a real RunState");
    Console.WriteLine("  adds the player to a run - the doc comment on CreateForNewRun warns of exactly this:");
    Console.WriteLine("  \"will not work properly until the player is added to a RunState and/or CombatState\").");
    Console.WriteLine("  Setting Owner ourselves, same as a real RunState.AddPlayer would, since we're using NullRunState.");
    foreach (var c in player.Deck.Cards) c.Owner = player;
    foreach (var r in player.Relics) if (r.Owner == null) Console.WriteLine("  WARNING: relic " + r.Id + " has null Owner too");

    Console.WriteLine("Step 8: registering the combat with CombatManager and force-setting IsInProgress");
    Console.WriteLine("  (skipping CombatManager.StartCombatInternal on purpose: it drives Godot-side");
    Console.WriteLine("   presentation - banners, music, NCombatRoom, SaveManager.SaveRun - that a headless");
    Console.WriteLine("   sim has no business touching. We call SetUpCombat for its real bookkeeping, then");
    Console.WriteLine("   flip the same private IsInProgress flag StartCombatInternal would have flipped.)");
    var combatManager = MegaCrit.Sts2.Core.Combat.CombatManager.Instance;
    combatManager.SetUpCombat(combatState);
    typeof(MegaCrit.Sts2.Core.Combat.CombatManager).GetProperty("IsInProgress")!.SetValue(combatManager, true);
    Console.WriteLine("  IsInProgress = " + combatManager.IsInProgress);

    Console.WriteLine("Step 9: playing a real StrikeIronclad card against the dummy via the real DamageCmd/Hook pipeline...");
    var strike = combatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(player);
    int hpBefore = enemyCreature.CurrentHp;
    var ctx = new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext();
    var cardPlay = new MegaCrit.Sts2.Core.Entities.Cards.CardPlay
    {
        Card = strike,
        Target = enemyCreature,
        ResultPile = MegaCrit.Sts2.Core.Entities.Cards.PileType.Discard,
        Resources = new MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo { EnergySpent = 1, EnergyValue = 1, StarsSpent = 0, StarValue = 0 },
        IsAutoPlay = false,
        PlayIndex = 0,
        PlayCount = 1,
    };
    var onPlay = typeof(MegaCrit.Sts2.Core.Models.CardModel).GetMethod("OnPlay", BindingFlags.NonPublic | BindingFlags.Instance)!;
    await (Task)onPlay.Invoke(strike, new object[] { ctx, cardPlay })!;
    int hpAfter = enemyCreature.CurrentHp;
    Console.WriteLine($"  Dummy HP: {hpBefore} -> {hpAfter} (expected 10 -> 4 for a 6-damage Strike on an unblocked 10 HP dummy)");

    if (hpBefore - hpAfter != 6)
    {
        throw new Exception($"UNEXPECTED: real Strike card dealt {hpBefore - hpAfter} damage, not 6. Something about the pipeline is not behaving as in real gameplay.");
    }

    Console.WriteLine();
    Console.WriteLine("ALL STEPS PASSED. A real StrikeIronclad card, resolved through the real CardModel/DamageCmd/Hook");
    Console.WriteLine("pipeline from sts2.dll, dealt exactly the damage it deals in actual gameplay, with no Godot engine running.");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("FAILED: " + ex);
    Environment.Exit(1);
}

public static class HeadlessLogShim
{
    // Prefix for Logger.GetIsRunningFromGodotEditor(): skip Godot.OS.GetCmdlineArgs()/HasFeature()
    // entirely and report false (we are never the Godot editor). Must set the return value via the
    // ref/out convention Harmony uses (a `ref bool __result` parameter), and returning false from
    // this prefix skips the original method body.
    public static bool SkipGodotEditorCheck(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Prefix for ConsoleLogPrinter.Print(LogLevel, string, int): print via plain Console instead of
    // Godot's GD.Print/GD.PrintErr, which require a running engine. Mirrors the original's formatting
    // closely enough to be readable; skips the original body.
    public static bool PrintToConsoleInstead(object logLevel, string text, int skipFrames)
    {
        string level = logLevel.ToString()!.ToUpperInvariant();
        string line = $"[{level}] {text}";
        if (level is "ERROR" or "WARN")
        {
            Console.Error.WriteLine(line);
        }
        else
        {
            Console.WriteLine(line);
        }
        return false;
    }
}
