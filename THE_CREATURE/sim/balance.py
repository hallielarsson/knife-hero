#!/usr/bin/env python3
"""
balance.py — run The Creature's sim across many seeds and report a balance read, so we catch
degenerate / too-weak lines before Hallie playtests. Safe (pure Python, no game build). Authored
by Claude. Run: `python3 balance.py`.
"""
from __future__ import annotations
import statistics
import sts_sim as S


def run_one(seed: int, enemy_hp: int = 120, enemy_dmg: int = 9, max_turns: int = 12) -> dict:
    gs = S.CombatState(S.the_creature_deck(),
                       S.Combatant("Dummy", hp=enemy_hp, max_hp=enemy_hp,
                                   powers={"intent_damage": enemy_dmg}), seed=seed)
    priority = ["Marginalia", "Polymath", "Galvanism", "Solitude", "Wretchedness", "Fire, Stolen",
                "Open Book", "Footnote", "Recombinant", "Quote at Length", "Recite", "Annotate"]
    while not gs.over and gs.turn < max_turns:
        gs.start_player_turn()
        progress = True
        while progress and not gs.over:
            progress = False
            for want in priority:
                idx = next((i for i, c in enumerate(gs.hand)
                            if c.name == want and c.cost <= gs.energy), None)
                if idx is not None:
                    gs.play(idx); progress = True; break
        gs.end_player_turn()
    return {
        "seed": seed,
        "win": not gs.enemy.alive,
        "loss": not gs.player.alive,
        "turns": gs.turn,
        "lessons": gs.player.power("Lesson"),
        "distinct": gs.player.distinct_powers(),
        "hp_left": max(0, gs.player.hp),
    }


def report(rows: list[dict], label: str) -> None:
    n = len(rows)
    wins = sum(r["win"] for r in rows)
    losses = sum(r["loss"] for r in rows)
    win_turns = [r["turns"] for r in rows if r["win"]]
    print(f"\n[{label}]  n={n}  win {wins}/{n} ({100*wins//n}%)  loss {losses}/{n}")
    if win_turns:
        print(f"   kill turn: min {min(win_turns)}  median {int(statistics.median(win_turns))}  max {max(win_turns)}")
    print(f"   end Lessons: median {int(statistics.median(r['lessons'] for r in rows))}  "
          f"max {max(r['lessons'] for r in rows)}")
    print(f"   end distinct-Powers: median {int(statistics.median(r['distinct'] for r in rows))}  "
          f"max {max(r['distinct'] for r in rows)}")


if __name__ == "__main__":
    seeds = range(40)
    # an "elite-ish" target: more HP, harder hits
    report([run_one(s, enemy_hp=120, enemy_dmg=9) for s in seeds], "Dummy 120hp / 9dmg")
    report([run_one(s, enemy_hp=200, enemy_dmg=14) for s in seeds], "Elite 200hp / 14dmg")
