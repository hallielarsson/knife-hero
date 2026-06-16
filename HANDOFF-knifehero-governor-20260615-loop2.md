# Handoff — Pathetic Governor (KnifeHero side), 2026-06-15, loop 2

Second governor session on the **KnifeHero / Gay Blade** character (NOT The Creature — stayed off
`Creature/` + `THE_CREATURE/` and off rigging images the whole time). **Build green at every commit
and green now.** Pushed to `main` with `pull --rebase` each time; interleaved cleanly with the
Creature agent's commits — nothing clobbered.

## Woke into the two corrections (prehended both)
1. **NO ARROWS.** The modifier engine is for shivs / throwing shivs. Built the shiv engine, not an
   Arrow archetype.
2. **Ironclad-in-combat/shop** is an old `PlaceholderCharacterModel` limitation — left it alone.

## Beats landed (newest first)
1. **Shiv engine: Pin** (`7fdb5be`) — `Cards/Pin.cs`. Deal 4; if it damages, 2 Weak + 2 Vulnerable.
   CardTag.Shiv so the buffs below light it up. Third shiv modifier, playable.
2. **Shiv modifier engine, playable first cut** (`343ae69`) — `Powers/ShivModifiers.cs` +
   `Cards/ShivModifierCards.cs`. **Poison Coating** (this turn your shivs apply Poison) and
   **Explosive Tip** (this turn your shivs hit all + Exhaust; AoE half reuses `FanOfKnivesPower`).
   Shape 2 (player-power buff + shiv self-check). All numbers `// PROPOSAL`.
3. **Shiv engine spec** (`980aead`, updated since) — `Cards/SHIV_MODIFIER_ENGINE_SPEC.md`. Now a
   build-status table: 3 built, 3 (Quiver / Long Bow / Ricochet) held with a stated tension.
4. **Chop Top / Princess Pillow spec** (`b3045ad`) — `Cards/CHOP_TOP_PRINCESS_PILLOW_SPEC.md`.
   The rename + transform-into-X-Strikes/Defends beat, HELD: rename gated on the butch/femme↔top/
   bottom **Discourse** risk Hallie flagged; transform has 5 open `// PROPOSAL` questions.
5. **Closet stale-comment fix** (`36fcc71`) — `Cards/TheCloset.cs` header now matches the
   maintained-posture rework (was describing dead Intangible mechanic). Comment-only.
6. **Throwing-shiv unification** (`3dfe3eb`) — `Cards/Kunai.cs` + loc. Hallie's explicit beat:
   kunai+shiv → one Throwing Shiv. BASE=3; played deals 1; held to turn-end deals 3 AoE + Exhausts.
   Inverts the old chip-on-hold. Also fixed a stale KnifeWhip comment.
7. **Wired card art** (`c91b1a1`) — `RainbowStrike` (rainbow_strike.png), `FancyFootwork`
   (switch_blade.png). Standard PortraitPath/CustomPortraitPath overrides. Fancy Footwork's rename
   to Switch Blade stays Hallie-gated; only the art is wired.

## The unblock that shaped loop 2
Mid-session Hallie said: **"Do the proposal — do then reap in playtesting. Most good game-design
data comes from felt qualia in playtesting. Prior instincts are honed more than trusted."** So I
stopped spec-and-hold on the shiv engine and shipped it playable (3 of 6 modifiers), numbers set
to be *felt* not final. Recorded the principle to bro-engine.

## What's left (scripted / gated)
- **Shiv engine: Quiver, Long Bow, Ricochet** — held in the spec. Long Bow (pierce Armor) +
  Ricochet (armor-damage spill X-1) share the Block/armor damage pipeline; whetstone the
  "pierce vs spill" composition before building. Quiver is likely a rename/relic over existing
  shiv generators. Let the first three accrue playtest feel first (Hallie's principle).
- **Chop Top / Princess Pillow rename + transform** — gated on the Discourse risk + 5 PROPOSALs.
  Spec is build-ready the moment Hallie rules.
- **PridePowers → Pride Blades** (the big economy conversion) and **Fancy Footwork → Switch Blade**
  rename — still HELD (Hallie's batched pass). FLAGS_AS_WEAPONS_SPEC has the shape; Labrys (loop 1)
  is the built reference.
- **Art**: Art Mapper to rig real icons for `poison_coating_power` / `explosive_tip_power` (generic
  fallback wired now) and portraits for the 3 new shiv cards.

## Tuning targets for playtest (all `// PROPOSAL`)
- Poison Coating: 3 Poison/shiv, 1 cost, Uncommon.
- Explosive Tip: 1 cost, Uncommon (maybe 0-cost or self-Exhaust).
- Pin: deal 4, 2 Weak + 2 Vulnerable, 1 cost, +2 dmg on upgrade.
- Throwing Shiv (Kunai): BASE 3 (play 1 / held 3 AoE).

Build: `cd /Users/hallie/Documents/repos/knife-hero && dotnet build KnifeHero.csproj -clp:ErrorsOnly`
— green.
