# Queer Engine — spec (HELD for Hallie)

The Gay Blade's core engine, found in a flow/design session (2026-06-16, recorded in bro-engine).
It **supersedes the "coating-as-aura" half of the shiv modifier engine** — see
[`SHIV_MODIFIER_ENGINE_SPEC.md`](./SHIV_MODIFIER_ENGINE_SPEC.md). Coatings were never auras; they
are **riders assembled onto a card at the moment it is cast out.**

**This is design, not shipped. No code is wired by this scaffold.** Net-new numbers and choices are
marked `// PROPOSAL`; the system shape is Hallie's to ratify.

> Pivot note: the shiv-modifier "this turn your shivs deal Poison" framing (aura over the turn) is
> grieved into **queer-riders** here. If you find turn-wide coating auras, that's the stale shape —
> the live shape is a rider baked into a card when it's othered out of the deck.

## The thesis (constitutional — Hallie ratified, twice)
**Diversity is strength. Inconsistency as strength. Yes.**

Every other character thins by **subtraction** — delete the basic cards to reach a lean,
normative-optimal core. The Gay Blade can't delete the main. It **queers** it: the card you'd
cast out doesn't leave, it *comes out*. Deckbuilding-by-deletion becomes deckbuilding-by-becoming.
**Diversity instead of purity.** This is the design north star; the math must mean it (a fat,
diverse deck has to be able to win).

> `// PROPOSAL` **Open thread — the mechanical home of "diversity as strength."** Pride is NOT a
> variety-counter resource (see below). So what makes a wide deck *numerically* strong? Working
> hypothesis: the **Prides** (Retain attacks with in-hand effects) stack — many held at once =
> compounding in-hand output = the fat diverse hand literally does more. Needs a real answer before
> tuning, or diversity-as-strength stays flavor.

## What "Queer" is (the verb)
**Queer = Tinker Time, applied to a card the deck casts out.**

The base game's **Tinker Time** event (Act 3) assembles a custom "Mad Science" card by **chassis +
rider**: pick a chassis (Weapon/Attack 12 dmg · Protector/Skill 8 Block · Gadget/Power passive),
then bolt on 1 rider from a random 2-of-3. That is the proof that *a modifier is a rider baked into
a card at assembly* — not a combat-wide aura.

Queering does the same to an othered card: **keep its chassis, bolt on a queer-rider → a unique,
"out" version of the card.** Same act, divergent results per source → diversity by construction.

- **Riders = the relocated coatings.** Poison, AoE/Explosive, Weak+Vulnerable (Pin), Draw, etc.
  They were always riders looking for a chassis. The shiv engine's `// PROPOSAL` effects become
  the rider pool here.

## Scope — what gets queered (Hallie, verbatim)
> "shivs and basic strikes and defends and generic attacks and defends — er attacks and skills"

Queer-eligible = **the unmarked**: generic Attacks, generic Skills, and Shivs — the normative
middle of the deck.

- **Exempt (already out):** Powers/Gadgets and the special named cards (the **Prides**, the engine
  pieces). You only queer what the deck treats as interchangeable filler. You don't queer a Pride —
  it's already itself.
- **Why shivs belong (this closes the original loop):** shivs **Exhaust by default**, so
  "exhausting a shiv queers it" = the coating becomes a rider assembled onto the shiv *at the moment
  it exhausts*. Poison Coating-as-aura → poison-rider-on-exhaust. The coating problem and the queer
  mechanic are the same mechanic.

## The starting card (Option A, Hallie ratified — Innate)
> **Queer** — *Curse. Innate. Eternal. Unplayable.*
> *While present, whenever a generic Attack, generic Skill, or Shiv would be exhausted or removed,
> Queer it instead* (assemble it Tinker-style: keep chassis, bolt on a rider → an out-card).

Why each keyword:
- **Curse** — the world files queerness as a curse; it's secretly your entire engine. The inversion
  is the character in one card type. Being a Curse *is* the drawback (clogs hand, unplayable) — no
  invented downside needed.
- **Innate** — in your opening hand every combat. You don't get to *not* be out. The tax is honest
  and constant (open every fight down a hand slot), and it removes the "engine dead until drawn"
  failure mode — it's always there, turn one.
- **Eternal** — you can't be put back in the closet. The one card removal can't touch — and it's
  the card that turns everyone else's removal into becoming. Fixed point of its own rule.

### Variant held for playtest (Option B — location-split)
> In **hand** → exhaust queers · in **deck** → removal queers.
Dramatizes visibility ("out in the moment" vs "out in general"). Beautiful, but gated the in-combat
engine behind drawing a clog-card — feels-bad for a starter. **Ship A, feel it, try B if A is too
frictionless.**

## Open questions (`// PROPOSAL` — Hallie to rule; start simple, reap in playtest)
1. **Rider selection.** Choose 1-of-2 (authentic Tinker, more clicks) vs auto-random (faster, still
   diverse). Recommend **auto-random** first.
2. **Where the queered card lands.** Likely exhaust → discard, removal → into the deck. Confirm.
3. **Shiv firehose.** Shivs exhaust constantly → near-every shiv queers. Diversity fountain or flood?
   Start **ungated**; knobs if too hot: once/turn · shivs queer into shivs (in-style, less explosive)
   · only generic shivs.
4. **Curses get queered too** — the relic-level upgrade Hallie flagged. Parked; great north star.

## Build status (2026-06-16 — v1 shipped playable, build green)
First cut is **BUILT and in the starting deck** (`Cards/Queer.cs`, loc `KNIFEHERO-QUEER`, added to
`Character/KnifeHero.cs` StartingDeck). The engine lives **on the curse card itself**: because
`CardModel.ShouldReceiveCombatHooks => Pile?.IsCombatPile`, an Innate curse sits in a combat pile
all fight and receives combat hooks — so it hosts `AfterCardChangedPiles` directly, no proxy power.

| Piece                         | Status | Notes |
|-------------------------------|--------|-------|
| Queer curse (Innate/Eternal/Unplayable) | **BUILT** | `Cards/Queer.cs` |
| Exhaust interception           | **BUILT** | catches a card landing in Exhaust; re-entry-guarded |
| "Becoming" (chassis + rider)   | **BUILT** | Hallie: "return the original card with a queer rider." The card keeps its identity; a `QueerRiderMod` (`Powers/QueerRiderMod.cs`) bolts on. Returned to the **deck (draw pile)** to be drawn again. |
| Scope = basic Strike/Defend    | **BUILT** | `CardTag.Strike/Defend`; shivs DEFERRED (firehose) |
| Run-level removal (BeforeCardRemoved) | **HELD** | needs run-scoped host (relic/character), not an in-combat card |
| Rider pool (Tinker chassis+rider) | **BUILT (seed)** | ONE rider: relocated Poison Coating (`QueerRiderMod`, // PROPOSAL 2 Poison/play). The pool + per-source divergence (random/Tinker-chosen) is the next cut — that divergence is the diversity. |

**Verify in playtest (Hallie's felt-first principle):** does the curse's `AfterCardChangedPiles`
fire reliably for exhausts of *other* cards while it sits Innate in hand? (It should —
`ShouldReceiveCombatHooks` is true while in a combat pile — but confirm by feel.) Does transform on
a card sitting in the exhaust→discard relocation read cleanly, or flicker?

## Incoming content — the Queerings pool & Vakuu (2026-06-17 design page)
Hallie's handwritten page (transcribed verbatim: `hallie-beats/design-page--26-6-17--queerings-and-vakuu.txt`)
delivers the rider-pool contents and a third pillar. **"Queering" is the locked term** for riders.

- **The Queerings** = the `QueerRiderMod` pool. Seven so far (swap damage↔block; convert Strength→[?];
  energy-refund; [illegible]; Shuffle-on-discard; "Oops" splash; a Vakuu rider). Poison Coating is the
  shipped seed. Build the clean ones first (Shuffle / Oops / energy-refund are simple `CardModifier`
  adds; swap-damage↔block overrides base behavior — its own pass). Some words need Hallie's confirm.
- **Vakuu** = canon StS2 Ancient, *The First Demon* (anti-consistency, makes pacts, gives curses).
  He is the Gay Blade's **patron**: the Queer curse reads as a Vakuu pact, and he is the canon backing
  for the diversity-as-strength thesis. Canon boon *Whispering Earring* ("Gain energy each turn; Vakuu
  plays your first turn for you") grounds Hallie's "Gain X energy, Vakuu plays your hand" card. A
  separate subsystem + card line (Demonic Pride, Vakuu's Nails). HELD pending scope.
- **Active queer** = "I Want To Recruit You: deal 6, Queer a card in hand" — player-chosen queer,
  distinct from the curse's passive on-exhaust.

## Implementation path (the *can* — substrate already shipped)
- **Riders are `CardModifier`s.** BaseLib's `CardModifier`
  (`.decompiled-baselib/Baselib/Abstracts/CardModifier.cs`) is exactly the per-card attachment the
  shiv engine already used (`Powers/ShivModifiers.cs`). A queer-rider = a `CardModifier` with the
  coating's `OnPlay` / damage hooks, applied via `CardModifier.AddModifier(card, rider)`. **The
  rider half is already proven in-engine.**
- **The new piece is the cast-out hook.** `// PROPOSAL` needs investigation: the interception point
  for "a card would be Exhausted or Removed → substitute the queered card." Find where Exhaust and
  master-deck removal resolve (BaseLib card lifecycle) and whether a Power/global hook can intercept
  and replace. This is the one genuinely net-new bit of plumbing; don't assume the hook exists until
  located.
- **Card-type / "generic" test.** Need a predicate for "unmarked": basic Strike/Defend + generic
  Attack/Skill + `CardTag.Shiv`, excluding Powers and the special named cards. `// PROPOSAL` define
  the membership test (likely a tag/whitelist rather than type alone).

## Vocabulary correction (carried from the session)
**Pride = the Gay Blades** — the Retain attacks with in-hand effects (e.g. the Fancy Footwork blades:
`ButchBlade.cs` / `FemmeFlechette.cs`). It is **not** a resource or a variety-counter. The three
diversities of the Gay Blade: **Prides** (standing presence / retain) · **Shivs** (multiplicity) ·
**Queering / Switch Blade** (becoming). Queering is the connective verb under all three.
