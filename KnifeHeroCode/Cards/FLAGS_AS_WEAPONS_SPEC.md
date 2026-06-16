# Flags as Weapons — the Pride refactor (Hallie, 2026-06-13)

> "We should be making all these cards, even Labrys, be retain weapons. A lot of these are going to
> be in-hand-cards as flags rather than powers per se." — Hallie

## The vision
A Flag is a **sword you hold**, not a Power you apply. Each Pride flag becomes an **in-hand Retain
blade** (`IFlagBlade`, like Butch Blade / Femme Flechette): you draw it, it Retains, its flag-effect
runs *while it sits in your hand*, and playing it does a one-shot thing (and, being Retain, it isn't
gone). This makes the whole flag economy tactile — your hand IS your loadout — and unifies everything
under the one-copy-per-flag rule (`AddOrUpgradeFlagBlade<T>`): a second copy upgrades, never stacks.

**"Do I put it through the wash?" (Hallie 2026-06-13):** every Pride has a *retained* effect (passive
while held) AND a *played* effect (when you cast it). That split IS the decision loop — hold the sword
for its standing effect, or spend it for the burst. Butch/Femme already do this; the reflow makes it
the rule for all Prides. Each upgrade increments/upgrades *some part* of the sword (its retained
effect, its played effect, or its stat). Hallie is doing overnight hand-notes on the per-Pride splits.

Why this works mechanically: **every card in every pile is a hook listener.** A Retain blade in hand
can already run `AfterPlayerTurnStart`, `AfterCombatVictory`, `ModifyDamageAdditive`,
`AfterBlockGained`, `TryModifyEnergyCostInCombat`, `AfterCardPlayed`, etc. — exactly what the Pride
*Powers* do now, but located on the held card instead of a power icon. Butch/Femme/Festering Wound
already prove the pattern. So the Powers (`PridePowers.cs`) largely **dissolve into the cards**.

## Per-flag conversion (Power → in-hand Retain blade)
Each becomes `: KnifeHeroCard(cost, type, rarity, target), IBlade, IFlagBlade` with `Retain`, the
while-in-hand hook, and an `OnPlay`. Numbers are starting points; tune in playtest.

| Flag | While in hand (the flag-effect) | OnPlay (you swing it) | Hook |
|------|--------------------------------|----------------------|------|
| **Silent Pride** | At start of turn, put a Shiv in your discard | Deal 6 | `AfterPlayerTurnStart` |
| **Ironclad Pride** | At end of combat, heal 5 | Gain 8 Block | `AfterCombatVictory` |
| **Watcher Pride** | At start of turn, add Retain to a random other hand card | Gain 5 Block | `AfterPlayerTurnStart` |
| **Defect Pride** | Your Powers cost 1 less; draw a card when you play a Power | Deal 5 | `TryModifyEnergyCostInCombat` + `AfterCardPlayed` |
| **Regent Pride** | At start of turn, deal 6 to an enemy and gain 6 Block | Sacrifice another flag-blade in hand | `AfterPlayerTurnStart` |
| **Dyke Pride** | (maker) forges a **Labrys** into your hand | — | OnPlay generate |
| **Necrobinder Pride** | (maker) summons a real Osty pet (stays a summon) | — | OnPlay `OstyCmd.Summon` |

**Makers vs holders:** most flags ARE the in-hand weapon. A few (Dyke, Necrobinder) are *makers* —
playing them forges a different in-hand weapon (Labrys) or a pet (Osty). Hallie's distinction.

## Dyke Pride → the Labrys (the parry-weapon)
**Dyke Pride** (play it): forges a **Labrys** into your hand (`AddOrUpgradeFlagBlade<Labrys>`).
**Labrys** — Retain `IFlagBlade`. While it's in your hand, **when you take damage**: it **blocks the
next source of damage**, **gains attack equal to what it blocked**, then **goes to your discard**.
A parry that banks the hit as power: you eat one blow, the axe drinks it, and next time you draw it
it's heavier.

### RESOLVED — the parry mechanism (researched 2026-06-13, no build)
The engine's `BufferPower` (StS "Buffer" = prevent the next damage instance) shows exactly how to
zero a hit, and both hooks are `virtual` on `AbstractModel` — so the **Labrys card itself** does it,
no separate power:
- **Block a hit:** override `decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount,
  ValueProp props, Creature? dealer, CardModel? cardSource)` — if `target == Owner?.Creature` and
  `Pile?.Type == PileType.Hand`, **stash `amount` in a field and `return 0m`** (the blow is absorbed
  after Osty/Block, the last gate before HP loss). `BufferPower:15` is the template.
- **Bank it + discard:** override `async Task AfterModifyingHpLostAfterOsty()` — if a hit was just
  stashed: `DynamicVars.Damage.UpgradeValueBy(stashed)` (attack grows by what it blocked, persists and
  rides the one-copy upgrade), clear the stash, then move the Labrys to discard
  (`CardPileCmd` hand→discard). `BufferPower:24` decrements there; we discard instead.
- Note: this is the **damage-prevention** path. Femme's retaliate uses the sibling
  `BeforeDamageReceived` (deal-back, doesn't modify the amount). Two different hooks, don't conflate.

### ⚑ One design question for Hallie (A vs B)
"if it's in your hand when you take damage, blocks the **next** source of damage" — two readings:
- **A — primed parry (held):** while Labrys is in hand, the *next incoming attack* is blocked, the
  axe banks that amount as attack, then discards. (Cleanest: hold it = a loaded parry.)
- **B — reactive (banks the current hit):** a hit lands, the axe catches *that* blow, banks it, discards.
Default to **A** (matches "blocks the next source"). Both use the same two hooks above; A just doesn't
need a separate "I already took a hit" trigger — the held card IS the trigger. Confirm before building.

## Labrys naming note (for Hallie)
`Knife in Front` already summons a **Labrys pet** (8 HP tank). Dyke Pride's Labrys is a different
beast (in-hand parry-weapon, same name). Default for now: **keep both**. Fold-in is reversible if
Hallie wants one Labrys.

## Acquisition
Pride flags stay reward/pool cards you draft. Drawing one and holding it = the flag is "out." Since
they Retain, once drawn they stick. One-copy rule means a duplicate draft upgrades the held blade.

## Status
SPEC ONLY for the BIG conversion — Build/publish is HELD while Hallie playtests. When she's at a
stopping point: convert PrideCards.cs (Powers → blades), gut the now-dead PridePowers, add Dyke
Pride, loc keys, then ONE batched build+publish.

### Partial: Labrys scaffolded (Claude, Pathetic Governor 2026-06-15)
The **Labrys parry-weapon** is built net-new as `Cards/Labrys.cs` (+ loc `KNIFEHERO-LABRYS`),
implementing the RESOLVED parry mechanism above with **reading A (primed parry)**, the stated
default. It's additive — touches no Power, dissolves nothing, so it doesn't pre-empt the held
conversion. Build is green.
- **What works now:** as a Retain IFlagBlade, it voids the next instance of HP loss while held,
  banks that amount as permanent attack via `DynamicVars.Damage.UpgradeValueBy`, then discards
  itself; re-forge/upgrade adds +2.
- **What's NOT wired yet (Hallie / next teller):** nothing *forges* a Labrys into hand — Dyke Pride
  (its maker) doesn't exist as a card yet (it's part of the held PridePowers→blades pass). When
  Dyke Pride lands, call `CombatState.AddOrUpgradeFlagBlade<Labrys>(Owner)` from its OnPlay.
- **Confirm A vs B** (see the ⚑ question above) — if Hallie wants B (banks the *current* hit), the
  same two hooks serve; A just needs no separate "already took a hit" trigger.
- **Numbers** (base 6, +2 upgrade) are `// PROPOSAL` — Hallie to mint.
