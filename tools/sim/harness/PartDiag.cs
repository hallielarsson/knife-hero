using System;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace KnifeHero.Sim;

/* PART DIAGNOSTIC — why won't the Heart mend?
   Fable, 2026-07-13.

   `creature 300` reported a 92% fester rate and ZERO redemptions, while Lessons piled up to 38. The
   bot gates on the real CardModel.CanPlay(), the Heart costs 0, and PartCard.IsPlayable only wants
   Lessons >= 2 — which the bot has by turn 3. So on paper it should mend, and it never does.

   Rather than reason about that any further, this asks the engine. Every turn, for each Part in hand:
   print the Lessons, the mend cost, and the exact UnplayableReason bitmask the engine hands back.
   The engine knows why. Ask it. */
internal static class PartDiag
{
    public static async Task Run()
    {
        Engine.SetFightSeed("part-diag-1");
        var player = Engine.CreateReadyPlayer<TheCreature>();
        var enemy = ModelDb.Monster<Axebot>();
        var state = Engine.SetUpCombat(player, new[] { enemy });
        var fight = new Fight(player, state);

        int safety = 0;
        while (!fight.IsOver && safety++ < 14)
        {
            await fight.StartTurn();

            int lessons = (int)(player.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
            Console.WriteLine($"\n── turn {fight.TurnNumber}  HP {player.Creature.CurrentHp}  Lessons {lessons}");

            foreach (var c in fight.Hand.ToList())
            {
                bool isPart = c is PartCard;
                bool can = c.CanPlay(out var reason, out var preventer);
                string mark = isPart ? "PART" : "    ";
                Console.WriteLine($"   {mark} {c.Id,-34} canPlay={can,-5} reason={reason} preventer={preventer?.Id.ToString() ?? "-"}");
            }

            while (!fight.IsOver)
            {
                var playable = fight.Hand.Where(c => fight.CanAfford(c) && c.CanPlay()).ToList();
                if (playable.Count == 0) break;
                var card = playable[0];
                int? target = card.TargetType is TargetType.AnyEnemy or TargetType.RandomEnemy ? 0 : null;
                Console.WriteLine($"      play → {card.Id}");
                await fight.PlayCard(card, target);
            }
            if (fight.IsOver) break;

            // The mend transform is DEFERRED to BeforeSideTurnEnd (the float-bug fix). If you don't end
            // the turn, it never lands — which is exactly the trap I fell into on the first pass of this
            // diagnostic and briefly mistook for a live exploit.
            await fight.EndTurn();

            var deck = PileType.Discard.GetPile(player).Cards
                .Concat(PileType.Draw.GetPile(player).Cards)
                .Concat(PileType.Hand.GetPile(player).Cards).ToList();
            Console.WriteLine($"      end of turn → mended in combat piles: "
                + string.Join(", ", deck.Where(c => c is IMendedPart).Select(c => c.Id.ToString()).DefaultIfEmpty("none")));
        }

        /* THE PERSISTENCE QUESTION.
           Player.PopulateCombatState does `state.CloneCard(item)` — combat piles hold CLONES of the run
           deck, and CardCmd.Transform only writes back when the pile is PileType.Deck. So on paper, a
           mend or a fester is combat-local and the RUN DECK still holds the original Throbbing Heart.
           THE_PARTS.md claims the opposite, loudly and repeatedly ("permanently, for the rest of the
           run", scars "accumulate across the run"). One of them is lying. Ask the deck. */
        Console.WriteLine("\n═══ RUN DECK after the fight (does the mend persist?) ═══");
        foreach (var g in player.Deck.Cards.GroupBy(c => c.Id.ToString()).OrderBy(g => g.Key))
            Console.WriteLine($"   {g.Count()}x {g.Key}");
        Console.WriteLine($"   MaxHp now: {player.Creature.MaxHp}");
    }
}
