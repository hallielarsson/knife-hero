# Footwork — consolidated spec

Consolidates the scattered `footwork-*` Penelope remainders (`footwork-spec-v1`,
`footwork-by-usage`, `footwork-grant-5def`, `footwork-transform-artifact`) and the
`rapier-float-transform-bug` into one place. Read this before touching Footwork.

## Current live decision (what's shipped)
**Footwork stays SIMPLE.** This is the `genders-as-decaying-statuses` pivot, and it's what the
whole session's Flag work assumes. `VersatileFootwork.cs` already implements it:

- Play it as an attack → deal 6 damage (offense).
- Hold it → at end of turn, gain 5 Block and Exhaust it (defense).
- **No transform. No relic. No float bug.** Use it however the turn needs.

The deck's identity now rides on **Flags** (the "genders" — Stealth, the Closet, Extremely Online,
etc.): decaying/cashable status powers that *some* cards grant and *others* pay off. That replaced
the Top/Bottom momentum axis, which is **DROPPED** (see "Superseded" below). Managing the Flag
economy — not a transform loop — is the game right now.

## Deferred alternative: transform-by-usage Footwork (NOT current)
The earlier, richer vision — kept here so it isn't lost, but **not** being built unless Hallie
revives it. "Managing the loop is the game": Footwork is a flexible seed that skews the deck
toward how you spend it.

- A signature **base ARTIFACT (relic)** drives all transforms (not the card's `OnPlay`).
- Use a Footwork to **ATTACK** → deal damage, then it becomes a **Strike** (strikes beget strikes).
- Use a Footwork to **DEFEND** (held to end of turn) → gain **5 Block** AND it becomes a **Defend**
  (defends beget defends).
- The deck self-reinforces toward your play pattern; not drowning in one type is the tension.

### The float-bug fix (load-bearing, applies if transform is ever revived)
The Rapier "stuck floating after play" bug was caused by `CardCmd.TransformTo<…>(this)` called
**mid-`OnPlay`** — transforming a card while it's still resolving, so the engine can't dispose it.
**Fix:** never transform in `OnPlay`. Move the transform onto the relic, which reacts **after the
card resolves** (in the discard pile / post-resolution hook). No mid-play conflict. This is also
*why* the transform belongs on the artifact, not the card.

## Superseded / dropped (don't rebuild)
- **Top/Bottom momentum axis** and the Butch/Femme variants (`momentum-spec-v2`,
  `momentum-top-bottom-gender-thesis`, `momentum-butch-femme`, `momentum-v1-build-ready`,
  `momentum-build-prep`): **DROPPED** by `genders-as-decaying-statuses`. The gender-mechanics
  thesis ("the mechanics ARE gender") survives — it just lives in the **Flag** system now, not in a
  signed momentum power.
- Making Footwork itself a Status card: rejected (status reads as bad/unplayable; wrong for a flex
  card you want to play).

## If Hallie revives the transform loop
1. Build the relic as a `CustomRelicModel` that hooks a post-resolution / discard event.
2. On that hook, inspect the just-resolved card's tags (`CardTag.Strike` / `CardTag.Defend` /
   Footwork) and `CardCmd.Transform` it to the begotten type — never from within a card's `OnPlay`.
3. Keep `VersatileFootwork.cs` as-is for the immediate effect; the relic only changes what the
   card *becomes* afterward.
