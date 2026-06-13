#!/usr/bin/env python3
"""
sts_sim — a tiny text-based Slay-the-Spire-style combat sim, authored by Claude, for prototyping
THE CREATURE's mechanics (Lessons / Books / assemblage) without the full game's build+reload loop.

Design goals:
  - One file, pure stdlib. Run it: `python3 sts_sim.py` plays a scripted demo and prints the log.
  - A clean programmatic API (CombatState + discrete actions) so it can be wrapped as an MCP server
    later: play_card / end_turn / get_state map 1:1 to methods here.
  - Faithful-enough to StS: energy, block, draw/hand/discard/exhaust, powers, enemy intents,
    Strength/Dexterity, start-of-turn hooks, on-gain-power hooks.

This sims THE CREATURE (the disclosed-AI hero), NOT The Gay Blade. Numbers are first-draft.
"""

from __future__ import annotations
import random
from dataclasses import dataclass, field
from typing import Callable, Optional


# ----------------------------------------------------------------------------- combatants & state

@dataclass
class Combatant:
    name: str
    hp: int
    max_hp: int
    block: int = 0
    powers: dict[str, int] = field(default_factory=dict)  # name -> amount (Strength, Dexterity, Lesson, Marginalia, ...)

    @property
    def alive(self) -> bool:
        return self.hp > 0

    def power(self, name: str) -> int:
        return self.powers.get(name, 0)

    # distinct Powers held (the Creature's "assemblage" — how many different things it has become)
    def distinct_powers(self, exclude: tuple[str, ...] = ("Lesson",)) -> int:
        return sum(1 for k, v in self.powers.items() if v > 0 and k not in exclude)


@dataclass
class Card:
    name: str
    cost: int
    ctype: str                       # "Attack" | "Skill" | "Power"
    effect: Callable[["CombatState"], None]
    is_book: bool = False            # the IBook marker
    exhausts: bool = False
    keywords: tuple[str, ...] = ()


class CombatState:
    def __init__(self, deck: list[Card], enemy: Combatant, seed: int = 0):
        self.rng = random.Random(seed)
        self.player = Combatant("The Creature", hp=70, max_hp=70)
        self.enemy = enemy
        self.energy = 0
        self.max_energy = 3
        self.draw_pile: list[Card] = list(deck)
        self.rng.shuffle(self.draw_pile)
        self.hand: list[Card] = []
        self.discard: list[Card] = []
        self.exhaust_pile: list[Card] = []
        self.turn = 0
        self.log_lines: list[str] = []

    # -------------------------------------------------------------------------- logging / helpers
    def log(self, msg: str) -> None:
        self.log_lines.append(msg)

    def gain_power(self, who: Combatant, name: str, amt: int) -> None:
        """All power-gains route through here so the learning hooks fire (Marginalia, etc.)."""
        if amt == 0:
            return
        who.powers[name] = who.powers.get(name, 0) + amt
        if who.powers[name] <= 0 and name not in ("Strength", "Dexterity"):
            who.powers.pop(name, None)
        if amt > 0:
            self.log(f"   {who.name} gains {amt} {name} (now {who.powers.get(name, 0)})")
            # MARGINALIA: whenever you gain a Power, gain ONE Lesson (flat — not scaled by its own
            # stacks; the runaway came from stack-scaling, so Marginalia should be Single-stack).
            if who is self.player and name not in ("Lesson",) and self.player.power("Marginalia") > 0:
                self.player.powers["Lesson"] = self.player.powers.get("Lesson", 0) + 1
                self.log(f"     Marginalia → +1 Lesson (now {self.player.powers['Lesson']})")

    def deal_damage(self, src: Combatant, tgt: Combatant, base: int, hits: int = 1) -> None:
        dmg = base + (src.power("Strength") if src is self.player else 0)
        dmg = max(0, dmg)
        for _ in range(hits):
            blocked = min(tgt.block, dmg)
            tgt.block -= blocked
            tgt.hp -= (dmg - blocked)
            self.log(f"   {src.name} hits {tgt.name} for {dmg} ({dmg-blocked} after {blocked} block) "
                     f"→ {tgt.name} {max(0,tgt.hp)} hp")

    def gain_block(self, who: Combatant, base: int) -> None:
        amt = max(0, base + who.power("Dexterity"))
        who.block += amt
        self.log(f"   {who.name} gains {amt} Block (now {who.block})")

    # -------------------------------------------------------------------------- piles
    def draw(self, n: int) -> None:
        for _ in range(n):
            if not self.draw_pile:
                if not self.discard:
                    return
                self.draw_pile = self.discard
                self.discard = []
                self.rng.shuffle(self.draw_pile)
            self.hand.append(self.draw_pile.pop())

    # -------------------------------------------------------------------------- turn structure
    def start_player_turn(self) -> None:
        self.turn += 1
        self.player.block = 0
        self.energy = self.max_energy
        self.log(f"\n=== Turn {self.turn} — energy {self.energy}, enemy intent: {self.enemy.name} ===")
        # REGEN: heal at start of turn
        if self.player.power("Regen") > 0:
            heal = min(self.player.power("Regen"), self.player.max_hp - self.player.hp)
            self.player.hp += heal
            if heal:
                self.log(f"   Regen → heal {heal} (now {self.player.hp} hp)")
        # POLYMATH: start of turn, gain a stack of a random Power you already have
        if self.player.power("Polymath") > 0:
            owned = [k for k, v in self.player.powers.items() if v > 0 and k not in ("Polymath",)]
            if owned:
                pick = self.rng.choice(owned)
                self.log("   Polymath fires:")
                self.gain_power(self.player, pick, 1)
        # BECOMING: start of turn, convert Lessons into Strength (Lessons // 4)
        if self.player.power("Becoming") > 0:
            gained = self.player.power("Lesson") // 4
            if gained:
                self.log("   Becoming fires:")
                self.gain_power(self.player, "Strength", gained)
        self.draw(5)
        self.log(f"   Hand: {[c.name for c in self.hand]}")

    def play(self, index: int) -> bool:
        """Play the card at hand position `index`. Returns False if illegal."""
        if not (0 <= index < len(self.hand)):
            self.log(f"   ! no card at index {index}")
            return False
        card = self.hand[index]
        if card.cost > self.energy:
            self.log(f"   ! not enough energy for {card.name} (cost {card.cost}, have {self.energy})")
            return False
        self.energy -= card.cost
        self.log(f" > play {card.name} (cost {card.cost})")
        self.hand.pop(index)
        card.effect(self)
        if card.exhausts or card.ctype == "Power":
            self.exhaust_pile.append(card) if card.exhausts else None
            if card.ctype == "Power" and not card.exhausts:
                pass  # powers leave play silently
        else:
            self.discard.append(card)
        return True

    def end_player_turn(self) -> None:
        # held cards discard (no retain modeled)
        self.discard.extend(self.hand)
        self.hand = []
        if not self.enemy.alive:
            return
        # enemy acts: simple — attack for its 'intent' damage
        self.log(f" < enemy {self.enemy.name} acts")
        self.enemy_act()

    def enemy_act(self) -> None:
        dmg = self.enemy.powers.get("intent_damage", 12)
        self.deal_damage(self.enemy, self.player, dmg)
        # THORNS: reflect when the player is struck
        if self.player.power("Thorns") > 0 and self.enemy.alive:
            self.log("   Thorns reflects:")
            self.deal_damage(self.player, self.enemy, self.player.power("Thorns"))

    @property
    def over(self) -> bool:
        return not self.player.alive or not self.enemy.alive

    def state_str(self) -> str:
        p, e = self.player, self.enemy
        return (f"[{p.name} {p.hp}/{p.max_hp}hp blk{p.block} | energy {self.energy} | "
                f"Lessons {p.power('Lesson')} | distinctPowers {p.distinct_powers()} | "
                f"powers { {k:v for k,v in p.powers.items() if v}} ]  "
                f"[{e.name} {max(0,e.hp)}hp blk{e.block}]")


# ----------------------------------------------------------------------------- THE CREATURE — cards
# Each card's effect mutates the CombatState. Authored by Claude; first-draft numbers.

def recite(gs: CombatState):      gs.deal_damage(gs.player, gs.enemy, 6)
def annotate(gs: CombatState):    gs.gain_block(gs.player, 5)

def open_book(gs: CombatState):
    # read: gain 1 Lesson, and deepen a Power you already have (or seed Marginalia if brand new)
    gs.gain_power(gs.player, "Lesson", 1)
    owned = [k for k, v in gs.player.powers.items() if v > 0 and k not in ("Lesson",)]
    pick = gs.rng.choice(owned) if owned else "Marginalia"
    gs.gain_power(gs.player, pick, 1)

def marginalia(gs: CombatState):  gs.gain_power(gs.player, "Marginalia", 1)
def polymath(gs: CombatState):    gs.gain_power(gs.player, "Polymath", 1)
def autodidact(gs: CombatState):  gs.gain_power(gs.player, "Autodidact", 1)
def becoming(gs: CombatState):    gs.gain_power(gs.player, "Becoming", 1)

def footnote(gs: CombatState):
    # deal 4 and gain a Lesson; (return-to-hand-if-no-kill is omitted in this minimal sim)
    gs.deal_damage(gs.player, gs.enemy, 4)
    gs.gain_power(gs.player, "Lesson", 1)

def recombinant(gs: CombatState):
    # hit once per distinct Power you have — the assemblage payoff
    hits = max(1, gs.player.distinct_powers())
    gs.log(f"   Recombinant: {hits} hits (one per distinct Power)")
    gs.deal_damage(gs.player, gs.enemy, 3, hits=hits)

def quote_at_length(gs: CombatState):
    gs.deal_damage(gs.player, gs.enemy, gs.player.power("Lesson"))

# Distinct-power Books — each reads into a DIFFERENT one-off Power, so the assemblage axis (count of
# distinct Powers) actually climbs and Recombinant has something to scale on. Each also grants a
# Lesson (reading), so they feed both axes. (Frankenstein flavor noted in DESIGN.md.)
def galvanism(gs: CombatState):    gs.gain_power(gs.player, "Lesson", 1); gs.gain_power(gs.player, "Strength", 1)
def solitude(gs: CombatState):     gs.gain_power(gs.player, "Lesson", 1); gs.gain_power(gs.player, "Dexterity", 1)
def wretchedness(gs: CombatState): gs.gain_power(gs.player, "Lesson", 1); gs.gain_power(gs.player, "Thorns", 2)
def fire_stolen(gs: CombatState):  gs.gain_power(gs.player, "Lesson", 1); gs.gain_power(gs.player, "Regen", 2)


def the_creature_deck() -> list[Card]:
    C = Card
    deck = []
    deck += [C("Recite", 1, "Attack", recite) for _ in range(4)]
    deck += [C("Annotate", 1, "Skill", annotate) for _ in range(4)]
    deck += [C("Open Book", 1, "Skill", open_book, is_book=True, exhausts=True)]
    deck += [C("Marginalia", 1, "Power", marginalia, is_book=True)]
    deck += [C("Footnote", 1, "Attack", footnote, is_book=True)]
    deck += [C("Polymath", 2, "Power", polymath, is_book=True)]
    deck += [C("Recombinant", 2, "Attack", recombinant)]
    deck += [C("Quote at Length", 1, "Attack", quote_at_length)]
    # distinct-power Books — give the assemblage axis room to climb
    deck += [C("Galvanism", 1, "Skill", galvanism, is_book=True, exhausts=True)]
    deck += [C("Solitude", 1, "Skill", solitude, is_book=True, exhausts=True)]
    deck += [C("Wretchedness", 1, "Skill", wretchedness, is_book=True, exhausts=True)]
    deck += [C("Fire, Stolen", 1, "Skill", fire_stolen, is_book=True, exhausts=True)]
    return deck


# ----------------------------------------------------------------------------- scripted demo

def demo():
    gs = CombatState(the_creature_deck(), Combatant("Dummy", hp=120, max_hp=120,
                     powers={"intent_damage": 9}), seed=7)
    # a tiny scripted policy: read books early (build the engine), then swing assemblage payoffs.
    script_priority = ["Marginalia", "Polymath",
                       "Galvanism", "Solitude", "Wretchedness", "Fire, Stolen",  # build breadth
                       "Open Book", "Footnote",
                       "Recombinant", "Quote at Length", "Recite", "Annotate"]

    while not gs.over and gs.turn < 8:
        gs.start_player_turn()
        # greedily play affordable cards by the priority above
        progress = True
        while progress and not gs.over:
            progress = False
            for want in script_priority:
                idx = next((i for i, c in enumerate(gs.hand) if c.name == want and c.cost <= gs.energy), None)
                if idx is not None:
                    gs.play(idx)
                    progress = True
                    break
        gs.log("   end of turn: " + gs.state_str())
        gs.end_player_turn()

    gs.log("\n=== RESULT ===")
    gs.log("WIN — Creature beat the dummy" if not gs.enemy.alive else
           ("LOSS" if not gs.player.alive else "time — dummy survived 8 turns"))
    gs.log(gs.state_str())
    print("\n".join(gs.log_lines))


if __name__ == "__main__":
    demo()
