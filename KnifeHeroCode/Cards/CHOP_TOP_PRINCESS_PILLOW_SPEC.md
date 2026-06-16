# Chop Top & Princess Pillow — rename + transform spec (HELD for Hallie)

Captures the play-session beat (`hallie-beats/play-session-1--26-6-15.txt`, 2026-06-15)
for the renamed Fancy-Footwork blades. **This is design, not shipped.** Nothing here is wired
into the live cards yet — the rename, the transform mechanic, and the Discourse risk are all
Hallie's to ratify. Numbers and the framing are marked `// PROPOSAL` so she can mint or veto.

## The beat, verbatim
> - Butch-Blade → **Chop Top**, and Femme-Flechette → **Princess Pillow**.
> - (will resolve drama — conflation of butch/femme and top/bottom may result in **Discourse**)
> - Chop Top and Princess Pillow should both **transform into X Strikes / Defends respectively,
>   when played from hand, where X is the number of times they've been upgraded.**

## What's live today (the thing being renamed)
The two Retain blades Fancy Footwork forges (`ButchBlade.cs`, `FemmeFlechette.cs`):

- **Butch Blade** → forged by Fancy Footwork's ATTACK use. Retain; while in hand, +1 to your
  attacks; played, deal 8. Re-forge (`OnUpgrade`) sharpens damage +1. One copy per combat.
- **Femme Flechette** → forged by Fancy Footwork's DEFEND (held) use. Retain; while in hand,
  retaliate 3 when an enemy attack damages you; played, deal 5. Re-forge sharpens retaliate +1.
  One copy per combat.

Fancy Footwork itself is also slated to become **Switch Blade** (its art slug is `switch_blade`,
already wired 2026-06-15). That rename rides the same gated pass.

## The rename (gated — Hallie owns the call AND the Discourse risk)
- `ButchBlade` class + `KNIFEHERO-BUTCH_BLADE` loc keys  → **Chop Top**
- `FemmeFlechette` class + `KNIFEHERO-FEMME_FLECHETTE` loc keys → **Princess Pillow**
- `FancyFootwork` → **Switch Blade** (art already in place).

**Discourse risk (Hallie flagged it):** mapping butch/femme onto top/bottom is a real
conflation she wants to *resolve as drama*, possibly via The Discourse card, not silently rename.
DO NOT rename in code until she's decided how the mod holds that. Lines & veils apply
(`gender_based_jokes_or_violence` is a LINE) — the resolution is hers, in her register.

## The transform mechanic (net-new — PROPOSAL shape only)
"Transform into X Strikes/Defends when played, X = times upgraded."

- **Chop Top** (the attack blade): on play, instead of (or in addition to?) its strike, it
  **transforms into X Gay Blade Strikes** in hand/discard, where X = upgrade count.
- **Princess Pillow** (the defend blade): on play, **transforms into X Gay Blade Defends**,
  X = upgrade count.

Open design questions for Hallie (do NOT pick these unilaterally):
1. `// PROPOSAL` Does the blade still deal its own damage/retaliate on the play that transforms,
   or is the transform the *whole* effect? (The beat says "transform into", suggesting replace.)
2. `// PROPOSAL` X = upgrade count means an un-upgraded blade transforms into **0** cards — i.e.
   it does nothing but vanish. Is that intended (upgrade-gated payoff), or X = upgrades + 1?
3. `// PROPOSAL` Where do the generated Strikes/Defends go — hand, discard, draw? (StS transform
   convention is usually into hand or the same pile.)
4. `// PROPOSAL` Do the generated Strikes/Defends inherit any of the blade's bonuses, or are they
   vanilla Gay Blade Strike / Gay Blade Defend tokens? (Vanilla is simpler and likely right.)
5. `// PROPOSAL` Interaction with the existing Retain + one-copy-per-combat + ModifyDamage-in-hand
   identity: does transforming end the in-hand buff, since the blade leaves play? Almost certainly
   yes (it's gone), but it changes the card's whole feel from "held passive" to "spent engine."

## Engine notes (the *can*, for when Hallie ratifies)
- Generating cards into a pile: `CombatState.CreateCard<GayBladeStrike>(Owner)` then
  `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true)` — the pattern
  KnifeWhip and SuperfanOfKnives already use for Shivs.
- Reading upgrade count: there isn't a stored "times upgraded" counter today; `OnUpgrade` just
  bumps a stat. A transform-by-upgrade-count card needs to **track its own upgrade count** (an
  `int _upgrades;` incremented in `OnUpgrade`) — straightforward, but it's new state, so it's
  Hallie's to bless along with the mechanic.
- Transform vs generate: StS2's transform replaces the card; here "transform into X" reads as
  *generate X and remove self*, which is `OnPlay` → loop CreateCard → `CardCmd.Exhaust(this)`.

## Status
HELD. Rename gated on the Discourse resolution; transform gated on Q1–Q5 above. When Hallie
rules, this spec becomes the build order. No code touched by this scaffold.
