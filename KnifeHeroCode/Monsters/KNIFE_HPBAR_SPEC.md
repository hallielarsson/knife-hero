# Knife HP-bar — research findings (parked issue)

Symptom (Hallie, playtest): the summoned Knife (KnifePet, borrowing Osty's visual) takes damage and
dies correctly, but **no HP bar shows** over it. Mechanic works; only the UI is missing.

## What the decompile says
- The HP bar is **not** part of the visuals scene (`CustomVisualPath`). It's the standard
  `NHealthBar`, owned by `NCreatureStateDisplay` and fetched as a unique node `%HealthBar`
  (`NCreatureStateDisplay.cs:111`). So borrowing Osty's *visuals* does not, by itself, delete the bar.
- Visibility gate: `MonsterModel.IsHealthBarVisible`. Base **Osty** returns `Creature.IsAlive`
  (so Osty *does* show a bar when alive). Our `CustomPetModel(visibleHp: true)` returns the ctor
  bool unconditionally → `true`. So visibility isn't the blocker.
- **The lead:** the bar positions/sizes itself from the creature's bounds —
  `_healthBar.UpdateLayoutForCreatureBounds(bounds)` (`NCreatureStateDisplay.cs:181`). Those bounds
  come from the **"Bounds" node in the visuals scene** (NCreatureVisualsFactory wires "Bounds",
  "Visuals", "IntentPos", "CenterPos"). We borrow `creature_visuals/osty` — so the bar is being laid
  out against **Osty's** bounds, which may place it off-screen / zero-sized / behind the body.
- Value refresh path exists: `_healthBar.SetCreature(_creature)` (160) + `RefreshValues()` (193).

## Ranked hypotheses (all need a build to confirm — do NOT build during playtest)
1. **Bounds mismatch (most likely).** The borrowed Osty visuals scene's "Bounds" node drives
   `UpdateLayoutForCreatureBounds`, mispositioning/hiding the bar for our pet.
   - Test: borrow a *different* creature's visuals (one with a normal upright bound) and see if the
     bar appears. If it does, it's Osty's bounds.
   - Fix: ship a minimal custom visuals scene with a sane "Bounds" (and "Visuals"/"IntentPos"/
     "CenterPos") instead of borrowing Osty's — this is the same step the real knife rig needs
     anyway, so it folds into the eventual art work.
2. **Spawn-HP init / no refresh on resize.** `KnifePet` declares `MinInitialHp=1, MaxInitialHp=1`,
   and `KnifeInFront` does `AddPet` (spawns 1/1) **then** `SetMaxHp(8)+Heal(8)` *after*. If the bar
   caches layout/values at spawn (1/1) and `SetMaxHp` doesn't re-trigger `RefreshValues`/
   `UpdateLayoutForCreatureBounds`, the bar can render degenerate (zero-width) and never recover.
   - **Low-risk ready fix:** give the pet its real HP at spawn via the model, drop the post-spawn
     mutation:
     - `KnifePet`: `MinInitialHp => 8; MaxInitialHp => 8;`
     - `KnifeInFront.OnPlay`: delete the `SetMaxHp`/`Heal` lines, keep `AddPet` + `DieForYouPower`.
   - This is worth doing regardless (cleaner), and may fix the bar outright if (2) is the cause.

## Recommended order when Hallie is out of the game
1. Apply hypothesis-2 fix (cheap, clean) and playtest — does the bar appear?
2. If not, swap the borrowed visual to test hypothesis-1 (bounds), then commit to a minimal custom
   visuals scene with proper Bounds.

Parked per Hallie (`familiar-parked`): visuals are deferred while the Flag/Momentum design is the
focus. This spec is so the fix is one step away when we pick the familiar back up.

---

## Rig re-skin question (Hallie: "can we use the rig on a different png?")
Short answer: not with a flat PNG alone. A Spine rig animates bones whose attachments map to a
texture atlas; a single flat image has no slot/bone mapping. `CustomMonsterModel` only takes a
*scene* (`CustomVisualPath`/`CreateCustomVisuals`), no static-sprite path.
- Spine **skins** ARE exposed (`MegaSkeleton.SetSkin`/`SetSkinByName`, `MegaSprite.NewSkin`), so a
  rig's textures CAN be swapped — but only via a skin whose pieces line up with the skeleton's slots.
  Swapping a sword skin onto Osty's hand-skeleton would look wrong.
- Clean path for the Top/Bottom sword-pets: a **minimal custom Spine rig** (1–2 bones: blade + idle
  sway), then the pride-flag variants are just texture/skin swaps on that one rig. Small rigging job.
