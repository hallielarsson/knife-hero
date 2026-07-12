using System.Linq;
// Program.cs — CLI entry point.
//   dotnet run -- interactive              play one fight from the terminal, GayBlade vs Axebot
//   dotnet run -- batch [N]                simulate N fights (default 100) with a greedy policy,
//                                           report win rate / turns / HP remaining / distinct powers
//
// NOTE ON NAMES: cards/enemies print as their raw ModelId (e.g. CARD.GAY_BLADE_STRIKE) rather than
// their pretty localized title. CardModel.Title reads from the game's LocString/localization
// table, which is asset data we never load here (no .pck, no Godot resource pipeline) - accessing
// it throws. IDs are enough to identify a card; if this harness grows into something Hallie reads
// often, loading the mod's own localization/eng/*.json into whatever backs LocString would be a
// reasonable follow-up (tracked as a known gap, not fixed here - see tools/sim/README.md).
using KnifeHero.Sim;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

Engine.Boot();

string mode = args.Length > 0 ? args[0] : "interactive";
switch (mode)
{
    case "interactive":
        await Interactive.Run();
        break;
    case "play-creature":
        await Interactive.Run<global::KnifeHero.KnifeHeroCode.CreatureHero.TheCreature>();
        break;
    case "batch":
        int n = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 100;
        await Batch.Run(n);
        break;
    case "creature":
        int cn = args.Length > 1 && int.TryParse(args[1], out var cparsed) ? cparsed : 300;
        await KnifeHero.Sim.CreatureInstrument.Run(cn, assemblageVariant: false);
        break;
    case "creature-aggressive":
        int cagn = args.Length > 1 && int.TryParse(args[1], out var cagparsed) ? cagparsed : 300;
        await KnifeHero.Sim.CreatureInstrument.Run(cagn, assemblageVariant: false, aggressive: true);
        break;
    case "creature-assemblage":
        int can = args.Length > 1 && int.TryParse(args[1], out var caparsed) ? caparsed : 300;
        await KnifeHero.Sim.CreatureInstrument.Run(can, assemblageVariant: true);
        break;
    default:
        Console.WriteLine("Usage: dotnet run -- [interactive|batch [N]|creature [N]|creature-aggressive [N]|creature-assemblage [N]]");
        break;
}

internal static class Interactive
{
    /* Fixed 2026-07-11 (Fable, while actually trying to play the Creature):
       (1) was hardcoded to the Gay Blade — now takes a character, so `-- play-creature` works;
       (2) only checked CanAfford, NOT IsPlayable — so it would happily let you play a Throbbing Heart
           with the 2-Grief/2-Lesson gate unmet, i.e. it lied about the single most important rule in
           the character. The batch bot got this fix; interactive never did;
       (3) showed no Powers at all — so you could not SEE Grief or Lessons, which means you could not
           see the gate you were trying to reach. You cannot feel a mechanic you cannot read. */
    public static Task Run() => Run<global::KnifeHero.KnifeHeroCode.Character.KnifeHero>();

    public static async Task Run<TChar>() where TChar : MegaCrit.Sts2.Core.Models.CharacterModel, new()
    {
        Engine.SetFightSeed(DateTime.UtcNow.Ticks.ToString());
        var player = Engine.CreateReadyPlayer<TChar>();
        var enemy = ModelDb.Monster<Axebot>();
        var state = Engine.SetUpCombat(player, new[] { enemy });
        var fight = new Fight(player, state);

        Console.WriteLine($"=== {player.Character.Id} vs {state.Enemies[0].Monster.Id} ===");
        Console.WriteLine($"RELICS: {string.Join(", ", player.Relics.Select(r => r.Id.ToString()))}");
        while (!fight.IsOver)
        {
            await fight.StartTurn();
            while (!fight.IsOver)
            {
                PrintStatus(fight);
                Console.Write("Play card # (blank to end turn): ");
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) break;
                if (!int.TryParse(line, out int idx) || idx < 0 || idx >= fight.Hand.Count)
                {
                    Console.WriteLine("  invalid index");
                    continue;
                }
                var card = fight.Hand[idx];
                if (!fight.CanAfford(card))
                {
                    Console.WriteLine("  not enough energy");
                    continue;
                }
                if (!card.CanPlay())
                {
                    Console.WriteLine("  card says it isn't playable right now (gate unmet / unplayable keyword)");
                    continue;
                }
                int? target = card.TargetType is TargetType.AnyEnemy or TargetType.RandomEnemy ? 0 : null;
                await fight.PlayCard(card, target);
                if (fight.IsOver) break;
            }
            if (fight.IsOver) break;
            await fight.EndTurn();
            if (fight.IsOver) break;
            Console.WriteLine("-- enemy turn --");
            await fight.EnemyTurn();
        }
        Console.WriteLine(fight.PlayerWon ? "VICTORY" : "DEFEAT");
    }

    private static void PrintStatus(Fight fight)
    {
        var p = fight.Player.Creature;
        Console.WriteLine($"\nTurn {fight.TurnNumber} | You: {p.CurrentHp}/{p.MaxHp} HP, {p.Block} Block, {fight.Player.PlayerCombatState!.Energy} energy");
        var powers = p.Powers.Where(pw => pw.Amount != 0m).Select(pw => $"{pw.GetType().Name.Replace("Power", "")} {pw.Amount:0.#}").ToList();
        if (powers.Count > 0) Console.WriteLine($"  Powers: {string.Join(", ", powers)}");
        foreach (var e in fight.State.Enemies)
            Console.WriteLine($"  Enemy: {e.Monster.Id} {e.CurrentHp}/{e.MaxHp} HP, {e.Block} Block, {(e.Monster.IntendsToAttack ? "intends to attack" : "not attacking")}");
        // Deck census — the ONLY way to see The Wash actually converting basics into Switch Blades.
        var all = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
            .SelectMany(t => t.GetPile(fight.Player).Cards)
            .GroupBy(c => c.Id.ToString()).OrderBy(g => g.Key)
            .Select(g => $"{g.Key.Replace("CARD.KNIFEHERO-", "").Replace("CARD.", "")}x{g.Count()}");
        Console.WriteLine($"  DECK: {string.Join("  ", all)}");
        Console.WriteLine("Hand:");
        for (int i = 0; i < fight.Hand.Count; i++)
        {
            var c = fight.Hand[i];
            string flag = c.CanPlay() ? "" : "  ✗LOCKED";
            string kw = c.Keywords.Count > 0 ? "  [" + string.Join(",", c.Keywords) + "]" : "";
            string ret = c.ShouldRetainThisTurn ? " RETAINS" : "";
            Console.WriteLine($"  [{i}] {c.Id} (cost {c.EnergyCost.GetAmountToSpend()}){flag}{kw}{ret}");
        }
    }
}

internal static class Batch
{
    public static async Task Run(int n)
    {
        int wins = 0;
        var turnCounts = new List<int>();
        var hpRemaining = new List<int>();
        var maxDistinctPowers = new List<int>();

        for (int i = 0; i < n; i++)
        {
            Engine.SetFightSeed($"batch-fight-{i}");
            var player = Engine.CreateReadyPlayer<global::KnifeHero.KnifeHeroCode.Character.KnifeHero>();
            var enemy = ModelDb.Monster<Axebot>();
            var state = Engine.SetUpCombat(player, new[] { enemy });
            var fight = new Fight(player, state);
            int maxPowers = 0;

            // safety valve: a broken policy/deck loop should not hang batch mode forever.
            int safety = 0;
            while (!fight.IsOver && safety++ < 200)
            {
                await fight.StartTurn();
                maxPowers = Math.Max(maxPowers, fight.DistinctAlliedPowerCount);
                while (!fight.IsOver)
                {
                    var playable = fight.Hand.Where(fight.CanAfford).ToList();
                    if (playable.Count == 0) break;
                    var card = playable[0]; // greedy policy: first affordable card, in hand order
                    int? target = card.TargetType is TargetType.AnyEnemy or TargetType.RandomEnemy ? 0 : null;
                    await fight.PlayCard(card, target);
                }
                if (fight.IsOver) break;
                await fight.EndTurn();
                if (fight.IsOver) break;
                await fight.EnemyTurn();
            }

            wins += fight.PlayerWon ? 1 : 0;
            turnCounts.Add(fight.TurnNumber);
            hpRemaining.Add(Math.Max(0, fight.Player.Creature.CurrentHp));
            maxDistinctPowers.Add(maxPowers);
            Engine.ResetCombat();
        }

        Console.WriteLine($"Fights: {n}");
        Console.WriteLine($"Win rate: {wins}/{n} ({100.0 * wins / n:F1}%)");
        Console.WriteLine($"Turns: avg {turnCounts.Average():F1}, min {turnCounts.Min()}, max {turnCounts.Max()}");
        Console.WriteLine($"HP remaining (winners+losers): avg {hpRemaining.Average():F1}, min {hpRemaining.Min()}, max {hpRemaining.Max()}");
        Console.WriteLine($"Max distinct allied Powers seen in a fight: avg {maxDistinctPowers.Average():F2}, max {maxDistinctPowers.Max()}");
    }
}
