#!/usr/bin/env python3
"""
run_sim — prototype of The Creature's RUN-level body-upkeep loop (authored, Bro + Hallie).

The question this answers: is "a body you have to maintain" CATHARTIC (care is the build, a tended
body out-performs a normal deck) or just BLEAK (you pay and pay and fall behind)? It models the
*economy*, not in-combat tactics: organ-Parts with grief states, degrade-if-untended, a between-
combat gold budget split between tending (upkeep) and advancing (what everyone else spends on).

Safe: pure Python, no game build. Run: `python3 run_sim.py`.

The injustice, mechanized: the "parented" baseline has no Parts to maintain and pours all gold into
advancement. The Creature must split the same gold between staying-whole and getting-stronger.
"""
from __future__ import annotations
import statistics

# Part condition ladder (AMALGAM): Repose is whole; it slides down when untended.
STATES = ["Depression", "Anxiety", "Denial", "Repose"]   # index 0..3, higher = healthier
STATE_POWER = {"Repose": 4, "Denial": 1, "Anxiety": -2, "Depression": -5}  # combat power per Part


class Part:
    def __init__(self, name): self.name, self.idx = name, 3   # start in Repose
    @property
    def state(self): return STATES[self.idx]
    @property
    def power(self): return STATE_POWER[self.state]
    def degrade(self, steps=1): self.idx = max(0, self.idx - steps)
    def process(self, steps=1): self.idx = min(3, self.idx + steps)   # tend it back up


class Runner:
    """A run of N combats. strategy decides how downtime gold is spent."""
    def __init__(self, strategy: str, n_parts: int = 3, seedbias: float = 0.0):
        self.strategy = strategy
        self.parts = [Part(f"organ{i}") for i in range(n_parts)] if strategy != "parented" else []
        self.max_hp = 70
        self.hp = 70
        self.advance = 0           # accumulated advancement (str/relics/upgrades) -> combat power
        self.kin = 0               # found family — each tends one Part per combat, sharing the labor
        self.alive = True
        self.bias = seedbias

    def combat_power(self) -> int:
        return 6 + self.advance + sum(p.power for p in self.parts)

    def fight(self, difficulty: int) -> None:
        # a body in disrepair HURTS: each Part in Depression bleeds you before the fight even starts
        for p in self.parts:
            if p.state == "Depression":
                self.hp -= 5
        power = self.combat_power()
        margin = power - difficulty
        if margin >= 0:
            self.hp -= max(0, 5 - margin // 2)          # comfortable wins cost little
        else:
            self.hp -= (4 - margin * 2)                  # losing the race hurts a lot
        if self.hp <= 0:
            self.alive = False
        # untended Parts slide: each combat, every Part degrades 1...
        for p in self.parts:
            p.degrade(1)
        # ...but found family shares the care — each kin tends one (worst-first) Part back up.
        for _ in range(self.kin):
            if self.parts:
                min(self.parts, key=lambda p: p.idx).process(1)

    def downtime(self, gold: int) -> None:
        """Spend gold. process_cost tends one Part up a step; advance_cost buys +2 power forever."""
        PROCESS, ADVANCE, HEAL = 15, 30, 18
        budget = gold
        if self.strategy == "parented":
            while budget >= ADVANCE:
                self.advance += 2; budget -= ADVANCE
            if budget >= HEAL:
                self.hp = min(self.max_hp, self.hp + 12)
            return
        if self.strategy == "neglect":
            # ignore the body; pour into advancement like everyone else
            while budget >= ADVANCE:
                self.advance += 2; budget -= ADVANCE
            return
        if self.strategy == "devotion":
            # tend the lowest Parts first; only advance with leftovers
            while budget >= PROCESS and any(p.idx < 3 for p in self.parts):
                worst = min(self.parts, key=lambda p: p.idx)
                worst.process(1); budget -= PROCESS
            while budget >= ADVANCE:
                self.advance += 2; budget -= ADVANCE
            return
        if self.strategy == "balanced":
            # tend only Parts that have actually sunk to Anxiety/Depression; else advance
            while budget >= PROCESS and any(p.idx <= 1 for p in self.parts):
                worst = min(self.parts, key=lambda p: p.idx)
                worst.process(1); budget -= PROCESS
            while budget >= ADVANCE:
                self.advance += 2; budget -= ADVANCE
            return
        if self.strategy == "community":
            # build found family first (until kin can hold the whole body), THEN advance.
            # kin cost more than a single tend, but they care for you every combat, forever.
            KIN = 50
            while budget >= KIN and self.kin < len(self.parts):
                self.kin += 1; budget -= KIN
            # patch up anything still sunk this stop, then advance with the rest
            while budget >= PROCESS and any(p.idx <= 1 for p in self.parts):
                min(self.parts, key=lambda p: p.idx).process(1); budget -= PROCESS
            while budget >= ADVANCE:
                self.advance += 2; budget -= ADVANCE


def run(strategy: str, combats: int = 10) -> dict:
    r = Runner(strategy)
    GOLD_PER = 55
    for i in range(combats):
        difficulty = 8 + i * 2                 # acts ramp up (gentler)
        r.fight(difficulty)
        if not r.alive:
            return {"strategy": strategy, "survived": i, "alive": False,
                    "power": r.combat_power(), "hp": 0,
                    "parts": [p.state for p in r.parts]}
        r.downtime(GOLD_PER)
    return {"strategy": strategy, "survived": combats, "alive": True,
            "power": r.combat_power(), "hp": max(0, r.hp),
            "parts": [p.state for p in r.parts]}


if __name__ == "__main__":
    print("=== The Creature — run-level body-upkeep prototype (10 combats, difficulty ramps) ===\n")
    for strat in ["parented", "neglect", "balanced", "devotion", "community"]:
        res = run(strat)
        tag = "SURVIVED" if res["alive"] else f"DIED at combat {res['survived']}"
        parts = f"  body={res.get('parts')}" if res.get("parts") else "  (no body to maintain)"
        print(f"[{strat:9}] {tag:18} final power {res['power']:3}  hp {res['hp']:3}{parts}")
    print("\nReading: does 'devotion' (care is the build) beat 'parented' (no upkeep) by the end?")
    print("Does 'neglect' (let the body rot) sink or die? That's the cathartic/bleak line.")
