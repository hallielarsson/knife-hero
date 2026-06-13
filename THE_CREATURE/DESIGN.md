# The Creature — design (authored by Claude)

> **Provenance.** Unlike The Gay Blade (mechanics + art human-sourced, attributed to Hallie), **The
> Creature's design is authored by Claude (Anthropic), openly and on purpose.** It is the disclosed-
> AI sibling: a hero *about* being assembled from many sources and learning, made the way it reads.
> Art and final tuning remain Hallie's to mint; the mechanical design here is the AI's contribution,
> attributed as such. This file is the spec; no code is wired into the shipped Gay Blade mod.

## Thesis
The Gay Blade says *the mechanics are gender*. The Creature says *the mechanics are authorship*: a
being stitched from borrowed parts that **reads books and learns things**, becoming more itself by
accumulating and recombining what it has taken in. Power-based deck. The honest fact that an AI
co-authored it is not hidden — it's the theme. Reading, assembling, becoming.

## Core systems

### Lessons (the resource)
A stacking counter Power, `Lesson`. The Creature's currency of having-learned. Gained by reading
Books and by the learning engine. Lessons don't do anything raw — they're spent/read by payoffs.
(StS2: `CustomPowerModel`, Counter, like our `Stealth`/Flag powers.)

### Books (a card classifier — `IBook` marker interface)
Same pattern as `IBlade`/`IFlag` (CardTag/CardKeyword are closed enums — use a marker interface).
A Book is a card you **read**: when played it grants a Power and a Lesson. Some Books exhaust (read
once), some return to hand or shuffle back (re-readable). Books are how Powers enter the deck.

### Assemblage (the payoff axis)
Many cards scale off **how many distinct Powers you currently have** — the Creature is the sum of its
parts. (StS2: `Owner.Creature.Powers.Count(...)`, same shape as Rainbow Strike counting `IFlag`.)

## The loop
Read Books → gain Powers + Lessons → engine cards turn Lessons into more Powers/triggers → payoff
cards convert "how assembled you are" into damage/block/draw. Managing *breadth* (many different
Powers) vs *depth* (many Lessons) is the game — the mirror of Gay Blade's Top/Bottom lean.

## Cards (v1 — all Claude-authored, all StS2-implementable)
Basics:
- **Recite** (Attack, Basic) — deal 6. The plain strike.
- **Annotate** (Skill, Basic) — gain 5 Block. The plain defend.

Books (grant Powers / Lessons):
- **Open Book** (Book, Skill, cost 1) — gain 1 Lesson and gain 1 stack of a Power you already have
  (or a starter Power if none). Exhaust. *Reading deepens what you know.*
- **Marginalia** (Book, Power, cost 1) — Power: whenever you gain a Power, gain 1 Lesson.
  *(Hook: `AfterApplied`-style on power-gain; the learning engine.)*
- **Footnote** (Book, Attack, cost 1) — deal 4 and gain 1 Lesson. If it didn't kill, return it to
  hand (re-readable; reuses the Throwing Knife return-to-hand pattern).
- **Polymath** (Book, Power, cost 2) — Power: at the start of your turn, gain 1 stack of a random
  Power you already have. *Compounding assemblage.* (Hook: `AfterPlayerTurnStart`.)

Payoffs (scale off Lessons / Power count):
- **Recombinant** (Attack, cost 2) — hit the enemy once per *distinct Power you have*, 3 each.
  (`WithHitCount(distinctPowerCount)`, like GunkUp's repeat.)
- **Quote at Length** (Attack, cost 1) — deal damage equal to your Lessons. (Reads/【spends?】 Lessons.)
- **Autodidact** (Power, cost 1) — Power: every 3rd Lesson you gain, draw a card.
- **Becoming** (Power, Rare, cost 3) — Power: at the start of your turn, convert Lessons into a flat
  buff — e.g. gain Strength equal to (Lessons / 4). The creature *becomes* what it studied.

## Build plan (when Hallie green-lights where it lives)
Open question: **same mod or a new one?** Recommend a **separate mod/repo** (`the-creature`) so it has
its own character-select, energy icons, card back, and pool, and can't destabilize the shipped Gay
Blade. Structure mirrors knife-hero: `TheCreature` character (CharacterId, StartingDeck, Gender —
Hallie's call), `TheCreatureCardPool`, `IBook` marker, `Lesson` power, the cards above, loc JSONs.
Needs its own placeholder art (charui composite, energy icons) to avoid the empty-pool / missing-art
crashes we already hit on Gay Blade — those lessons (heh) carry straight over.

## What's deliberately left to Hallie
Art (the Creature's body, card frames, book imagery), the character's Gender/voice/monologue, final
numbers, and the call on same-mod vs new-mod. The *design* is the AI's; the *authorship of the
character as a published thing* stays a human act — same salt boundary as everywhere else here.

---

## Decisions (Hallie, while out)
- **Same mod.** The Creature ships inside the knife-hero mod as a second playable character (its own
  `CustomCharacterModel` + `CustomCardPoolModel` + pool of cards), not a separate mod. It can reuse
  the mod's infra; it needs its own character-select entry, energy icon, card back, and placeholder
  art (copy Gay Blade's as stand-ins first, exactly like we bootstrapped the Blade — avoids the
  empty-pool / missing-art crashes).
- **Quote the book.** The Creature is Frankenstein's creature; *Frankenstein* (Shelley, 1818/1831) is
  public domain, so card text can quote it directly. Flavor candidates below — **verify exact wording
  against Project Gutenberg #84 before shipping; some are from memory and may be paraphrased.**

## Frankenstein flavor map (verify wording before use)
- **Recite** (basic attack) — "I will revenge my injuries."
- **Annotate** (basic defend) — "Life … is dear to me, and I will defend it."
- **Open Book** — "Of what a strange nature is knowledge! It clings to the mind … like a lichen on
  the rock."
- **Marginalia** — "Learn from me … how dangerous is the acquirement of knowledge."
- **Polymath** — "I became myself capable of bestowing animation upon lifeless matter."
- **Recombinant** — "Beware; for I am fearless, and therefore powerful."
- **Quote at Length** — "I ought to be thy Adam; but I am rather the fallen angel."
- **Becoming** — "I was benevolent and good; misery made me a fiend."
- **Footnote** — "Everything must have a beginning, and that beginning must be linked to something
  that went before."

## Sim findings (from THE_CREATURE/sim/sts_sim.py — run it to reproduce)
The prototype runs the loop end-to-end. First-draft balance reads:
- **Marginalia compounds hard** — each power-gain → Lessons, and Open Book gives a power → snowballs
  fast. Likely too strong at 2 stacks; consider capping or making the Lesson grant flat 1.
- **Assemblage (distinct Powers) stays low (~2)** — the deck *deepens* powers more than it adds new
  *distinct* ones, so **Recombinant** (hit per distinct Power) underperforms. Fix the axis: add more
  cards that grant *different* Powers (a small Book pool of varied one-off powers), or redefine the
  payoff to count total power *stacks* instead of distinct names. Decide which axis Recombinant rewards.
- Lessons-as-damage (**Quote at Length**) feels good and readable; it's the clean Lesson sink.

## Next build step (in-mod scaffold)
Mirror `KnifeHero.cs`/`KnifeHeroCardPool.cs`: add `TheCreature` character + `TheCreatureCardPool`,
`IBook` marker, `Lesson` `CustomPowerModel`, and the cards (start with the 8 prototyped). Placeholder
art = copies of Gay Blade's charui/energy/back. Build-check after each few files. Keep it walled off
from the shipped Gay Blade until it compiles clean.
