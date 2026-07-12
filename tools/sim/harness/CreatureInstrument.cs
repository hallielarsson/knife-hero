// CreatureInstrument.cs — instrumentation for The Creature's HP-as-resource control loop
// (THE_CREATURE/DESIGN.md, "where it wants to go" section). Built at Hallie's request to MEASURE
// the loop (drain / gate / restore / assemblage), not to rebalance it. See tools/sim/README.md for
// the harness's general fidelity account; the notes below are ADDITIONAL caveats specific to this
// instrument, on top of everything already disclosed there.
//
// TWO FIDELITY FIXES MADE HERE (both are instrumentation corrections, not balance changes — see the
// comments at each site for the full reasoning):
//
//   1. Fight.CanAfford only checked energy cost, not CardModel.CanPlay() (which also gates on the
//      Unplayable keyword, IsPlayable, and Hook.ShouldPlay). That's invisible for GayBlade's deck
//      (nothing there has a conditional IsPlayable), but The Creature's Throbbing Heart has
//      `IsPlayable => Grief >= 2 && Lessons >= 2` and Vexing Memory/Festering Wound carry the
//      Unplayable keyword. The old greedy policy would have "played" Throbbing Heart the instant it
//      was affordable (cost 0, always true) regardless of the gate, redeeming it for free on turn 1
//      every single fight — which would have silently fabricated exactly the number this whole job
//      exists to check (how often the 2-Lesson gate is actually met by turn 3). Fixed by gating the
//      greedy policy on the real `card.CanPlay()`, not just energy.
//
//   2. MOOT AS OF THIS SESSION, left here as a paper trail: this instrument originally worked around
//      the harness never calling Hook.AfterCombatVictory, because an earlier version of ThrobbingHeart
//      mended in two stages (OnPlay set a hidden _redeemed flag; the actual Mended-Heart/+2-max-HP
//      payoff fired later, at AfterCombatVictory, which the harness never calls). WHILE THIS
//      INSTRUMENT WAS BEING BUILT, Hallie/Fable collapsed that to one stage (ThrobbingHeart.cs,
//      2026-07-11 same-day edit, live uncommitted work): playing the Heart now mends it immediately,
//      in hand — no _redeemed field, no AfterCombatVictory override left on the card at all. So the
//      workaround (calling Hook.AfterCombatVictory by hand) is no longer needed for the mend to be
//      observable, and isn't done here. Flagging this because it's a real example of the brief's
//      warning about the live working tree: the card this instrument targets changed shape mid-session.
//
// NOT FIXED, flagged instead: this instrument still runs a SINGLE isolated combat, not a run — no
// map, no rewards, no shop, no rest sites. The Creature's starting deck contains only Recite x4,
// Annotate x4, Open Book, Marginalia, and one Throbbing Heart (see TheCreature.cs). The "broadening"
// Books that DESIGN.md's "Fixing the assemblage bug" section calls the actual fix (Galvanism,
// Solitude, Wretchedness, Fire Stolen — each grants a Power you don't already have) live only in
// TheCreatureCardPool, i.e. they only enter a deck via a run's card rewards. A single starting-deck
// fight structurally cannot exercise them. Two configurations are run below to separate these two
// questions cleanly:
//   - "creature"            starting deck only, as actually shipped today — the real HP-loop numbers.
//   - "creature-assemblage" starting deck + one copy each of the four broadening Books, Recombinant,
//                            and Become Who You Are injected directly into the deck before combat, as
//                            a stand-in for "a Creature who has drafted the fix cards" — NOT the
//                            shipped starting experience, reported separately and labeled as such.
using KnifeHero.KnifeHeroCode.CreatureHero;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace KnifeHero.Sim;

internal sealed class FightRecord
{
    public bool Won;
    public int Turns;
    public int HpStart;
    public int HpEnd; // clamped to >= 0
    public int NetHpDelta;
    public int MaxGrief;
    public int GriefAtEnd;
    public int MaxLessons;
    public int LessonsAtEnd;
    public bool? LessonsGateMetByTurn3; // null if fight ended before turn 3 started
    public int MaxDistinctPowers;
    public int MaxExhaustSize;
    public int ExhaustSizeAtEnd;
    public bool Fested;
    public int? FesterTurn;
    public bool HeartMended; // a MendedHeart is present anywhere in the deck/piles (the Heart was redeemed)
    public int? MendedTurn;
    public List<(int Turn, int Hp, int Grief, int Lessons, int DistinctPowers, int Exhaust)> Timeline = new();
}

internal static class CreatureInstrument
{

    // Two bot policies, both respecting the real CanPlay() gate (FIX #1, see file header):
    //   "greedy"     — first playable card in hand order (the harness's existing default policy).
    //   "aggressive" — deprioritizes Annotate (Block) behind attacks/Books/the Heart, to test whether
    //                  the HP-loop numbers below are a real property of the design or an artifact of
    //                  the greedy bot reflexively blocking every turn. See the "policy sensitivity"
    //                  note in the report for why this matters: a bot that always blocks trivially
    //                  neutralizes Vexing Memory/Festering Wound's grief damage, since that damage is
    //                  ordinary blockable damage (no Unblockable prop) and Block from the SAME turn is
    //                  still up when the end-of-turn grief tick lands.
    private static CardModel ChooseCard(List<CardModel> playable, bool aggressive)
    {
        if (!aggressive) return playable[0];
        int Priority(CardModel c) => c switch
        {
            ThrobbingHeart => 0,
            Annotate or OpenBook => 3, // block-granting cards last
            _ => 1,
        };
        return playable.OrderBy(Priority).First();
    }

    public static async Task Run(int n, bool assemblageVariant, bool aggressive = false)
    {
        var records = new List<FightRecord>();

        for (int i = 0; i < n; i++)
        {
            Engine.SetFightSeed($"creature-{(assemblageVariant ? "assemblage-" : "")}{(aggressive ? "aggressive-" : "")}fight-{i}");
            var player = Engine.CreateReadyPlayer<TheCreature>();

            if (assemblageVariant)
                InjectAssemblageTestCards(player);

            var enemy = ModelDb.Monster<Axebot>();
            var state = Engine.SetUpCombat(player, new[] { enemy });
            var fight = new Fight(player, state);
            var rec = new FightRecord { HpStart = player.Creature.CurrentHp };

            int safety = 0;
            while (!fight.IsOver && safety++ < 200)
            {
                await fight.StartTurn();

                while (!fight.IsOver)
                {
                    // FIX #1 (see file header): gate on the real CanPlay(), not just energy.
                    var playable = fight.Hand.Where(c => fight.CanAfford(c) && c.CanPlay()).ToList();
                    if (playable.Count == 0) break;
                    var card = ChooseCard(playable, aggressive);
                    int? target = card.TargetType is TargetType.AnyEnemy or TargetType.RandomEnemy ? 0 : null;
                    await fight.PlayCard(card, target);
                }
                if (fight.IsOver) break;

                int grief = PowerAmount<Grief>(player);
                int lessons = PowerAmount<Lesson>(player);
                int distinct = fight.DistinctAlliedPowerCount;
                int exhaust = PileType.Exhaust.GetPile(player).Cards.Count;
                int hp = player.Creature.CurrentHp;

                rec.Timeline.Add((fight.TurnNumber, hp, grief, lessons, distinct, exhaust));
                rec.MaxGrief = Math.Max(rec.MaxGrief, grief);
                rec.MaxLessons = Math.Max(rec.MaxLessons, lessons);
                rec.MaxDistinctPowers = Math.Max(rec.MaxDistinctPowers, distinct);
                rec.MaxExhaustSize = Math.Max(rec.MaxExhaustSize, exhaust);
                if (fight.TurnNumber == 3 && rec.LessonsGateMetByTurn3 is null)
                    rec.LessonsGateMetByTurn3 = lessons >= 2;

                if (!rec.Fested && AllCreatureCards(player).OfType<FesteringWound>().Any())
                {
                    rec.Fested = true;
                    rec.FesterTurn = fight.TurnNumber;
                }
                if (!rec.HeartMended && AllCreatureCards(player).OfType<MendedHeart>().Any())
                {
                    rec.HeartMended = true;
                    rec.MendedTurn = fight.TurnNumber;
                }

                await fight.EndTurn();
                if (fight.IsOver) break;
                await fight.EnemyTurn();
            }

            if (!rec.HeartMended && AllCreatureCards(player).OfType<MendedHeart>().Any())
                rec.HeartMended = true; // catch a mend that happened on the turn the fight ended

            rec.Won = fight.PlayerWon;
            rec.Turns = fight.TurnNumber;
            rec.HpEnd = Math.Max(0, player.Creature.CurrentHp);
            rec.NetHpDelta = rec.HpEnd - rec.HpStart;
            rec.GriefAtEnd = PowerAmount<Grief>(player);
            rec.LessonsAtEnd = PowerAmount<Lesson>(player);
            rec.ExhaustSizeAtEnd = PileType.Exhaust.GetPile(player).Cards.Count;

            records.Add(rec);
            Engine.ResetCombat();
        }

        Report(records, assemblageVariant, aggressive);
    }

    // Adds one copy each of the four "broadening" Books plus Recombinant and Become Who You Are to
    // the deck before combat starts — see file header for why (they're reward-pool-only, never in
    // the shipped starting deck, so an isolated single-combat harness can't reach them otherwise).
    private static void InjectAssemblageTestCards(MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        foreach (var canonical in new CardModel[]
        {
            ModelDb.Card<Galvanism>(), ModelDb.Card<Solitude>(), ModelDb.Card<Wretchedness>(),
            ModelDb.Card<FireStolen>(), ModelDb.Card<Recombinant>(), ModelDb.Card<BecomeWhoYouAre>(),
        })
        {
            // ModelDb.Card<T>() returns the canonical (immutable) singleton — mirrors how
            // StartingDeck's cards are turned into real per-run instances by Player.PopulateDeck
            // (ToMutable, then Owner set), which CreateReadyPlayer's own Owner fix-up already
            // depends on for the starting deck.
            var card = canonical.ToMutable();
            card.Owner = player;
            player.Deck.AddInternal(card, -1, silent: true);
        }
    }

    private static int PowerAmount<TPower>(MegaCrit.Sts2.Core.Entities.Players.Player player)
        => (int)(player.Creature.Powers.FirstOrDefault(p => p is TPower)?.Amount ?? 0m);

    private static IEnumerable<CardModel> AllCreatureCards(MegaCrit.Sts2.Core.Entities.Players.Player player) =>
        new[] { PileType.Deck, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust, PileType.Play }
            .SelectMany(pt => pt.GetPile(player).Cards);

    private static double Percentile(List<int> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        double idx = p * (sorted.Count - 1);
        int lo = (int)Math.Floor(idx), hi = (int)Math.Ceiling(idx);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
    }

    private static void PrintDist(string label, List<int> values)
    {
        var s = values.OrderBy(x => x).ToList();
        Console.WriteLine($"{label}: avg {s.Average():F2}, min {s.Min()}, p10 {Percentile(s, 0.10):F1}, " +
            $"p25 {Percentile(s, 0.25):F1}, median {Percentile(s, 0.5):F1}, p75 {Percentile(s, 0.75):F1}, " +
            $"p90 {Percentile(s, 0.90):F1}, max {s.Max()}");
    }

    private static void Report(List<FightRecord> records, bool assemblageVariant, bool aggressive)
    {
        int n = records.Count;
        Console.WriteLine();
        Console.WriteLine($"=== The Creature vs Axebot — {(assemblageVariant ? "ASSEMBLAGE-INJECTED deck (NOT the shipped starting deck — see header)" : "shipped starting deck")}, {(aggressive ? "AGGRESSIVE policy (deprioritizes Block)" : "greedy policy (first playable card in hand order)")}, {n} fights ===");
        Console.WriteLine($"Win rate: {records.Count(r => r.Won)}/{n} ({100.0 * records.Count(r => r.Won) / n:F1}%)");
        PrintDist("Turns", records.Select(r => r.Turns).ToList());

        Console.WriteLine();
        Console.WriteLine("-- 1. HP over time (net HP delta per fight: HpEnd - HpStart, HpStart=72) --");
        PrintDist("Net HP delta", records.Select(r => r.NetHpDelta).ToList());
        Console.WriteLine($"  Fights ending in net-positive HP (healed more than lost): {records.Count(r => r.NetHpDelta > 0)}/{n}");
        Console.WriteLine($"  Fights ending in net-negative HP (a bleed): {records.Count(r => r.NetHpDelta < 0)}/{n}");
        Console.WriteLine($"  Fights ending exactly break-even: {records.Count(r => r.NetHpDelta == 0)}/{n}");

        Console.WriteLine();
        Console.WriteLine("-- 2. Grief: peak per fight, and Grief remaining at fight end (0 = fully cleared) --");
        PrintDist("Max Grief", records.Select(r => r.MaxGrief).ToList());
        PrintDist("Grief at end", records.Select(r => r.GriefAtEnd).ToList());

        Console.WriteLine();
        Console.WriteLine("-- 3. Lessons: peak per fight, and the redemption gate (>=2 Lessons by turn 3) --");
        PrintDist("Max Lessons", records.Select(r => r.MaxLessons).ToList());
        var reachedTurn3 = records.Where(r => r.LessonsGateMetByTurn3.HasValue).ToList();
        int metGate = reachedTurn3.Count(r => r.LessonsGateMetByTurn3 == true);
        Console.WriteLine($"  Fights that reached turn 3 at all: {reachedTurn3.Count}/{n}");
        Console.WriteLine($"  Of those, had >=2 Lessons by turn 3: {metGate}/{reachedTurn3.Count}" +
            (reachedTurn3.Count > 0 ? $" ({100.0 * metGate / reachedTurn3.Count:F1}%)" : ""));

        Console.WriteLine();
        Console.WriteLine("-- 4. Fester rate: fraction of Throbbing Hearts that rotted into Festering Wound --");
        Console.WriteLine($"  Fested: {records.Count(r => r.Fested)}/{n} ({100.0 * records.Count(r => r.Fested) / n:F1}%)");
        Console.WriteLine($"  Redeemed (Heart mended into Mended Heart, one-stage as of ThrobbingHeart.cs 2026-07-11): {records.Count(r => r.HeartMended)}/{n}");
        var mendedTurns = records.Where(r => r.MendedTurn.HasValue).Select(r => r.MendedTurn!.Value).ToList();
        if (mendedTurns.Count > 0) PrintDist("  Turn mend occurred on", mendedTurns);
        var festerTurns = records.Where(r => r.FesterTurn.HasValue).Select(r => r.FesterTurn!.Value).ToList();
        if (festerTurns.Count > 0) PrintDist("  Turn fester occurred on", festerTurns);

        Console.WriteLine();
        Console.WriteLine("-- 5. Exhaust pile size: peak and end-of-fight (the healing pool for Read the Remainder) --");
        PrintDist("Max Exhaust size", records.Select(r => r.MaxExhaustSize).ToList());
        PrintDist("Exhaust size at end", records.Select(r => r.ExhaustSizeAtEnd).ToList());

        Console.WriteLine();
        Console.WriteLine("-- 6. Distinct Powers (assemblage axis) --");
        PrintDist("Max distinct Powers", records.Select(r => r.MaxDistinctPowers).ToList());

        // A compact turn-indexed view of the median fight's trajectory, for eyeballing shape (not
        // just endpoint stats) — grouped by turn number across all fights, median at each turn.
        Console.WriteLine();
        Console.WriteLine("-- Turn-by-turn medians across all fights (HP / Grief / Lessons / DistinctPowers / Exhaust) --");
        int maxTurn = records.SelectMany(r => r.Timeline).Select(t => t.Turn).DefaultIfEmpty(0).Max();
        for (int t = 1; t <= Math.Min(maxTurn, 30); t++)
        {
            var atTurn = records.SelectMany(r => r.Timeline).Where(x => x.Turn == t).ToList();
            if (atTurn.Count == 0) continue;
            double medHp = Percentile(atTurn.Select(x => x.Hp).OrderBy(x => x).ToList(), 0.5);
            double medGrief = Percentile(atTurn.Select(x => x.Grief).OrderBy(x => x).ToList(), 0.5);
            double medLessons = Percentile(atTurn.Select(x => x.Lessons).OrderBy(x => x).ToList(), 0.5);
            double medPowers = Percentile(atTurn.Select(x => x.DistinctPowers).OrderBy(x => x).ToList(), 0.5);
            double medExhaust = Percentile(atTurn.Select(x => x.Exhaust).OrderBy(x => x).ToList(), 0.5);
            Console.WriteLine($"  turn {t,2} (n={atTurn.Count,3}): HP {medHp,5:F1}  Grief {medGrief,4:F1}  Lessons {medLessons,4:F1}  Powers {medPowers,3:F1}  Exhaust {medExhaust,4:F1}");
        }
    }
}
