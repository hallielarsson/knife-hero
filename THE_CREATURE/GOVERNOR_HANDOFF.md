# Pathetic Governor — handoff (2026-06-15, session 2)

Session: `pathetic_governor_creature_20260615` (bro-engine). The Creature is **GREEN at HEAD**
(verified HEAD-in-isolation; the working tree carries another session's Knife-Hero WIP — build with
the foreign files stashed, or use `git pull --rebase --autostash`).

## What this session did — ALL THREE open items from the prior handoff are now CLOSED

### 1. The Rare card — **Become Who You Are** (commit `19cc396`)
The pool had no Rare; it does now. **DECIDED (bro, design owner):** a Rare Power (cost 3, also an
`IBook`) that pays off the **breadth/assemblage** axis the sim found underperforming. At each turn
start: **gain Strength equal to your distinct Powers, and gain 1 Lesson.** The Creature becomes the
sum of its assembled parts; it compounds across a long fight and ties the two axes (more Books → more
distinct Powers → more Strength; the Lesson trickle feeds Quote at Length / the process threshold).
Supersedes DESIGN.md's flatter "Becoming" (Lessons/4 → Strength), which overlapped Galvanism.
Frankenstein: *"I was benevolent and good; misery made me a fiend."*
Code: `BecomeWhoYouArePower` in `CreaturePowers.cs`, `BecomeWhoYouAre` in `CreatureCards.cs`, loc in
`cards.json` + `powers.json`. NOTE: it counts THIS power among the distinct (consistent with
Recombinant's "all of it is you" decision). Numbers Hallie's to mint.

### 2. Wholeness persistence — **Mended Body** relic (commit `d5baa0a`)
The handoff's "visible Wholeness counter persistence across combats." Wholeness was combat-scoped, so
the counter reset each fight (only the +2 max-HP persisted, on the Creature). **DECIDED:** re-derive
Wholeness from the **Mended Hearts in your run deck** at the start of each combat — the mended parts
ARE the permanent record, so no run-state serialization is needed. A **starting relic** (`MendedBody`,
`RelicRarity.Starter`) carries it: `BeforeCombatStart()` counts Mended Hearts and applies that much
Wholeness; `ShowCounter`/`DisplayAmount` give the visible counter. **No new art** — it reuses the
mod's placeholder `relic.png` (reading existing art, allowed; the Art Mapper owns authoring). It
replaced the placeholder `BurningBlood` starting relic. Loc in `relics.json`.

### 3. Act-3 crossover **sim pass** (commit `a63ffa7`)
New `THE_CREATURE/sim/crossover_asbuilt.py` models the SHIPPED mechanics (+2 max HP + per-turn heal
per Wholeness, re-derived by MendedBody), not the old abstract "power" proxy in `creature_routes.py`.
**Finding:** on the durability axis the design is built around, healing overtakes vengeance by **Act 1**
(~combat 2-4), NOT Act 3 — the abstract sim only said "Act 3" because it scored healing as *offense*,
which it isn't. The "slow road that wins late" framing holds on **speed** (vengeance bursts/kills
faster early; healing can't), not on durability. **DECIDED:** keep +2 max HP — the Creature SHOULD
feel its body knitting, and surviving-to-win IS the healing fantasy. The **sensitive knob is mend-RATE,
not the per-Wholeness number**: at 1-part-per-3-combats the body actually DIES in late Act 3
(eHP < incoming). Flag for **playtest**: how often can a real player actually complete the gated
grieve+learn+redeem+survive loop? If mending is too hard in practice, the healing route collapses late.

## Build / play status (for Hallie's "when can I play?" question)
- HEAD builds GREEN. The build auto-copies the mod DLL+JSON into the StS2 mods folder
  (`CopyToModsFolderOnBuild` target). For art/`.pck` changes you need a Godot **publish** (`GodotPublish`
  target) — code/loc-only changes (everything this session) just need `dotnet build`.
- **Both characters register** via reflection: The Gay Blade (`KnifeHero`) AND The Creature
  (`TheCreature`). The Gay Blade is the shipped, fully-arted PC; The Creature is playable but on
  **placeholder art** (reuses Blade/template assets) until Hallie draws it.
- To play: build the mod, launch StS2 with BaseLib loaded, pick the character at character-select.

## Still open (Hallie's calls — design is decided, these are tuning/playtest)
- **Numbers**: +2 max HP, heal-per-Wholeness, Become Who You Are's Strength/Lesson rates, Mended Heart
  damage — all Hallie's to mint.
- **Playtest the mend-rate** (the sim's sensitive knob — see item 3). Confirm a real player can keep
  the healing loop going often enough to survive Act 3.
- **Recombinant axis** (total vs distinct vs only-parts) — DECIDED total in code, but still Hallie's
  to override if it plays wrong.
- **Become Who You Are**: should it count itself among distinct Powers? Currently yes; cheap to filter
  if Hallie wants "distinct OTHER powers."

## ⚠️ Foreign working-tree WIP (NOT the Creature's; do not sweep up)
The working tree holds another session's Knife-Hero edits (`KnifeHeroCode/Powers/`, `Character/`,
`Cards/`, untracked `hallie-beats/`, card_portrait PNGs). They are NOT this governor's. Verify Creature
green HEAD-in-isolation: `git stash push --include-untracked -- KnifeHeroCode/Powers/ KnifeHeroCode/Character/ KnifeHeroCode/Cards/; dotnet build; git stash pop`.
Push with `git pull --rebase --autostash` so the foreign WIP doesn't block you.

## Governor practice note
All Creature work stayed inside `THE_CREATURE/` + `KnifeHeroCode/Creature/` + the shared loc JSONs.
Each beat: build green (isolated), commit only Creature files, rebase, push, record a bro edge.
