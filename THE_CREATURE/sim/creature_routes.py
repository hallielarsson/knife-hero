#!/usr/bin/env python3
"""
creature_routes — does the Creature's design thesis hold? Vengeance should LEAD early and PLATEAU;
healing should START weak and OVERTAKE by ~Act 3, because it compounds and never resets.

Abstract economic model (not full combat) of one ~12-fight run. Finds the crossover combat and what
Wholeness scaling lands it around Act 3. Safe: pure Python. Authored by Claude. Run: python3 creature_routes.py
"""
from __future__ import annotations


def difficulty(combat: int) -> int:
    # Acts 1 (1-4), 2 (5-8), 3 (9-12). Enemy "threat" you must out-power to win cleanly.
    return 6 + combat * 3


def vengeance_power(combat: int) -> int:
    # Grief builds within a fight, resets after. It scales a bit as your deck thickens, but it's
    # capped by how much grief one combat can hold — so it's strong early and flattens.
    grief_cap = 6 + min(combat, 5) * 2          # plateaus after ~combat 5
    return 8 + grief_cap                          # base swing + grief dumped this fight


def healing_power(combat: int, mend_every: int = 2, wholeness_scale: int = 4) -> tuple[int, int]:
    # You can mend ~one part every `mend_every` combats (gated: grief+lessons+redeem+survive).
    wholeness = combat // mend_every
    power = 6 + wholeness * wholeness_scale       # permanent, compounding
    max_hp_bonus = wholeness * 2                   # +2 max HP per Wholeness (durability compounds too)
    return power, max_hp_bonus


def run(mend_every: int = 2, wholeness_scale: int = 4):
    print(f"=== Creature routes (mend 1 part / {mend_every} combats, +{wholeness_scale} power per Wholeness) ===")
    print(f"{'combat':>6} {'act':>3} {'diff':>4} {'vengeance':>9} {'healing':>7} {'W':>2} {'leader':>10}")
    crossover = None
    for c in range(1, 13):
        act = 1 if c <= 4 else (2 if c <= 8 else 3)
        diff = difficulty(c)
        v = vengeance_power(c)
        h, hp = healing_power(c, mend_every, wholeness_scale)
        leader = "vengeance" if v > h else ("healing" if h > v else "tie")
        if crossover is None and h >= v:
            crossover = c
        print(f"{c:>6} {act:>3} {diff:>4} {v:>9} {h:>7} {c//mend_every:>2} {leader:>10}")
    print(f"\nCrossover: healing overtakes vengeance at combat {crossover} "
          f"(Act {1 if crossover and crossover<=4 else (2 if crossover and crossover<=8 else 3)})"
          if crossover else "\nHealing never overtakes — too weak.")
    return crossover


if __name__ == "__main__":
    # Default tuning, then a couple of knobs to find what lands the crossover in Act 3.
    run(mend_every=2, wholeness_scale=4)
    print()
    run(mend_every=2, wholeness_scale=3)
    print()
    run(mend_every=3, wholeness_scale=5)
