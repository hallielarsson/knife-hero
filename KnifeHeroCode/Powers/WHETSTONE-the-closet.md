# 🔪 whetstone — The Closet

*Sharpened 2026-06-15 by the Pathetic Governor (Claude, knife-hero side). Form: haiku — it's a cut.*

## The tension (docket ①)

When the blow lands, do you spend a card to stay unseen — or, with no card, does the
light find you (collapse to Dazed)? And the deeper one: **can a *Power* even prevent
damage, or must the closet become a card you hold?**

## broing & clauding (verb to verb)

- **bro** — the closet is a held breath. The blow comes; you press yourself smaller, spend
  something of yourself to not-be-seen. It should *cost* — a card off your hand, fed to the dark.
- **claude** — the engine won't run an async "pick a card" *inside* the damage hook. The damage
  resolves now; a Power's damage hooks (`ModifyDamageCap`, `ModifyHpLostAfterOstyLate`) are pure
  and synchronous. You cannot prompt a discard mid-blow.
- **bro** — then the spending can't be reactive. It's a posture you *maintain*. Each turn you
  feed the closet a card; in return it holds shut.
- **claude** — that the engine allows. `BufferPower` is the proof: it returns `0` HP-lost for the
  next instance, then decrements. A charge. The closet can hold **Buffer-like prevention charges**,
  paid for by discard at the **start of your turn** (async there is fine).
- **bro** — and when your hand is empty? Nothing left to feed it. The breath breaks.
- **claude** — then the watcher removes itself and the light pours in: add **Dazed** to your hand.
  The pathos was the answer — *the closet is a posture you must keep paying for, and poverty exposes you.*

## the cut

```
fed a card, it holds—
the closet keeps the blow off;
empty hand: light comes
```

## DECIDED

**Closeted is a maintained posture, not a reaction.** At the start of each of your turns, the
Closet asks for one card from your hand. **Discard it → gain a prevention charge** (Buffer-style:
the next instance of HP loss this turn is voided). **Cannot / will not pay → the Closet breaks**:
remove the posture and put a **Dazed** in your hand (the light finds you).

The answer to "can a Power prevent damage?" is **yes — but only as a pre-paid charge, never as a
mid-blow choice.** The discard is the rent; Dazed is the eviction.

*(Numbers — charges per discard, Dazed count — are Hallie's to mint. Scaffolded with `// PROPOSAL:`.)*
