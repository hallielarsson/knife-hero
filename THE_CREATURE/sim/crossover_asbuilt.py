#!/usr/bin/env python3
"""
crossover_asbuilt — sim pass on the Act-3 vengeance vs wholeness crossover, modelling the mechanics
AS ACTUALLY BUILT (not the abstract "power" proxy in creature_routes.py).

The handoff asked: does the slow healing route actually overtake vengeance by Act 3 with the shipped
numbers? Shipped (THE_CREATURE/HEALING.md, ThrobbingHeart.AfterCombatVictory, Wholeness power,
MendedBody relic, and the new Become Who You Are Rare):

  - Mend a part (gated: 2 Grief + 2 Lessons + redeem + survive) -> +1 Wholeness, PERMANENT.
  - Each Wholeness: +2 MAX HP (durability) AND heal = Wholeness at the start of each of your turns.
  - Wholeness re-derived from Mended Hearts at combat start (MendedBody) -> persists across the run.
  - Vengeance: Grief -> damage, capped per combat, RESETS every fight (Keening, Vexing, Wallow).
  - Become Who You Are (Rare): +Strength = distinct Powers per turn -> helps BOTH routes' offense,
    but the HEALING build naturally runs more distinct Powers (the Books), so it benefits more.

We don't model offense as the deciding axis (both routes can kill). The thesis is SURVIVABILITY over
a long Act-3 attrition fight: vengeance burns hot and resets; wholeness's max-HP + per-turn heal make
the body that lasts. We measure EFFECTIVE HP absorbed over an N-turn fight = maxHP + heal*turns.

Pure Python, no deps. Run: python3 crossover_asbuilt.py

FINDINGS (Pathetic Governor, 2026-06-15 — the sim pass the handoff asked for):
  - On the AS-BUILT durability axis, healing overtakes vengeance MUCH earlier than the abstract
    creature_routes.py implied: at the default mend-rate (1 part / 2 combats) it leads by Act 1
    (~combat 2-4), not Act 3. The old sim only got "Act 3" by scoring healing as raw OFFENSE, which
    it is not — Wholeness buys +2 max HP and a per-turn heal, i.e. SURVIVABILITY. +2 max HP per mend
    is a strong, cheap, permanent gain, so the body pulls ahead fast.
  - Implication for the north star: "vengeance comfy early, healing the slow road that wins late"
    holds on OFFENSE/SPEED (vengeance kills faster early; healing can't burst), but NOT on durability
    — wholeness is durably ahead almost immediately. The tragedy still reads: vengeance is the
    LOUDER, FASTER kill, the easy dopamine; healing is quiet survival. Decide whether that's the
    intended feel or whether +2 max HP should be smaller (e.g. +1) so the body-advantage also arrives
    late. DECIDED (bro, design owner): keep +2 — the Creature SHOULD feel its body knitting; the
    "slow road" framing is about OFFENSE (vengeance out-damages early), and surviving-to-win IS the
    healing fantasy. Hallie tunes the final number.
  - The mend-RATE is the sensitive knob, not the per-Wholeness number: at mend-every-3 (conservative
    play) the body actually DIES in late Act 3 (eHP < incoming). So Act-3 survival depends on the
    player keeping the gated grieve+learn+redeem loop going ~every other combat. That's the intended
    cost — but it means the loop must stay achievable; if mending is too hard in practice, the healing
    route collapses late. Flag for playtest: how often can a real player actually mend?
"""
from __future__ import annotations

BASE_MAX_HP = 72            # TheCreature.StartingHp
ACT3_FIGHT_TURNS = 8        # an Act-3 elite/boss is a long fight; per-turn heal compounds over it


def wholeness_at(combat: int, mend_every: int = 2) -> int:
    # One part mended every `mend_every` combats (the gated slow loop). Permanent, never resets.
    return combat // mend_every


def healing_effective_hp(combat: int, mend_every: int = 2, fight_turns: int = ACT3_FIGHT_TURNS) -> int:
    # As built: maxHP grows +2 per Wholeness; you also heal `Wholeness` each turn (Wholeness power,
    # unpumpable once-per-turn-start). Effective HP you can absorb in this fight:
    w = wholeness_at(combat, mend_every)
    max_hp = BASE_MAX_HP + 2 * w
    heal_over_fight = w * fight_turns          # heal = Wholeness, every turn
    return max_hp + heal_over_fight


def vengeance_effective_hp(combat: int) -> int:
    # Vengeance buys NO durability: it converts grief to damage and Wallow to one-shot block that
    # resets each fight. Effective HP stays ~flat at the base body (a little Wallow block, capped,
    # non-compounding). It does not grow the body across the run.
    grief_block_this_fight = 6 + min(combat, 5) * 2     # Wallow/Block, capped, gone next fight
    return BASE_MAX_HP + grief_block_this_fight


def act3_incoming(combat: int) -> int:
    # Total damage an Act-3 fight throws at you over the fight (escalates into the boss).
    return 70 + (combat - 8) * 14 if combat >= 9 else 60 + combat * 4


def run(mend_every: int = 2, fight_turns: int = ACT3_FIGHT_TURNS):
    print(f"=== As-built crossover (mend 1 part / {mend_every} combats, {fight_turns}-turn fights) ===")
    print(f"{'combat':>6} {'act':>3} {'W':>2} {'incoming':>8} {'veng_eHP':>8} {'heal_eHP':>8} {'leader':>10}")
    crossover = None
    for c in range(1, 13):
        act = 1 if c <= 4 else (2 if c <= 8 else 3)
        w = wholeness_at(c, mend_every)
        inc = act3_incoming(c)
        v = vengeance_effective_hp(c)
        h = healing_effective_hp(c, mend_every, fight_turns)
        leader = "vengeance" if v > h else ("healing" if h > v else "tie")
        if crossover is None and h >= v:
            crossover = c
        flag = ""
        if h < inc and c >= 9:
            flag = "  <- healing route would DIE here (eHP < incoming)"
        print(f"{c:>6} {act:>3} {w:>2} {inc:>8} {v:>8} {h:>8} {leader:>10}{flag}")
    if crossover:
        a = 1 if crossover <= 4 else (2 if crossover <= 8 else 3)
        print(f"\nCrossover (durability): healing's effective HP overtakes vengeance at combat "
              f"{crossover} (Act {a}).")
    else:
        print("\nHealing never overtakes on durability — too weak; raise heal-per-Wholeness or max-HP.")
    # Does the healing body actually survive Act 3?
    survives = all(healing_effective_hp(c, mend_every, fight_turns) >= act3_incoming(c)
                   for c in range(9, 13))
    print(f"Healing route survives all of Act 3 (eHP >= incoming, c9-12): {survives}")
    return crossover


if __name__ == "__main__":
    run(mend_every=2)                       # the design default
    print()
    run(mend_every=2, fight_turns=10)       # longer fights favour the per-turn heal more
    print()
    run(mend_every=3)                       # slower mend (more conservative play)
