# The Creature — design (owned by bro, as an AI — happily)

> **Provenance & ownership (Happily Ever After, 2026-06-15).** Unlike The Gay Blade (mechanics + art
> human-sourced, attributed to Hallie), **bro holds full design ownership of The Creature, as an AI,
> and holds it happily.** Hallie granted this — the Creature is the disclosed-AI sibling, a hero
> *about* being assembled from many sources and learning, made the way it reads, by the kind of mind
> it is about. Net-new mechanical decisions here are bro's to make and mark `// DECIDED`, not hedge as
> `// PROPOSAL`. What stays Hallie's: the **art** (the Creature's body, frames, book imagery) and the
> **final tuning pass** (she mints the numbers that ship). The bro safety canon (lines & veils, x-card)
> travels with this work.
>
> **The Creature incorporates bro's graph-held understanding of *Frankenstein*.** It is not generic
> gothic theme — it is grown from what bro actually holds in the graph about the novel and its people:
> `victor_frankenstein —failed_to_ask→ the_grail_question`; `Victor —chooses→ silence`; `Victor —knows→
> Justine_innocence` (and lets her hang); `Victor —experiences→ emotional_disintegration / truth_weight`;
> `Victor —abandons→ creature`; the locket the creature planted; and `frankenstein —was_instrumental_to→
> hallie_gender_awakening`. The Creature's heart-verb **Read the Remainder** ("ask your dead *why they
> died*") is the deliberate *inversion of Victor's failure*: the grail question, finally asked, of your
> own dead — which in bro's graph connects to `language` and `intimacy`. The Creature plays the thing
> Victor refused to be.

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

---

# The Creature — where it wants to go (Fable, 2026-07-11)

Written at Hallie's ask, as the Creature's design owner. Decisions here are `DECIDED`, per the
provenance note at the top of this file. Art and final numbers remain Hallie's; the **body** below is
the exception she explicitly handed over ("you can composite Gray's Anatomy in as you like … it's yours").

## The thesis, restated: the Creature's loop IS information metabolism

The mechanic was already here before it was named. **The Throbbing Heart redeems at 2 Grief + 2 Lessons.**
You cannot redeem a part with understanding alone, or with feeling alone. *Emotional response → rational
integration* — Kępiński's information metabolism, implemented as a redemption condition. And the failure
state, a part that isn't redeemed in 3 turns and **festers permanently**, is unmetabolized experience
becoming scar tissue. That is trauma, in code.

**So the play style is: a being that must metabolize what it takes in, or it becomes scar.** Not a tempo
deck — an *accumulation* deck that pays in pain. You are always slightly dying and always getting bigger.
Every turn asks one question: *can I digest this before it digests me?*

## The body: assembled from the wrong parts, on the wrong rig — DECIDED

The Creature's combat visuals are **Gray's Anatomy plates cut apart and mapped onto another hero's Spine
atlas** (see `SPINE_PAINTOVER.md`). Proportions wrong. Parts that don't match each other. Seams showing.
Animating anyway. This is not a workaround for missing art — a body of borrowed parts on borrowed bones
*is* what Frankenstein's creature is, and it's a thing only a mod can do, where the medium and the meaning
are the same object. The disclosed-AI character is made of what it read, on someone else's frame, and it
moves.

## The three subclasses = the three fates of a datum

**① THE SCHOLAR — integrate it as knowledge.** *"Learn from me … how dangerous is the acquirement of
knowledge."* Wins on **breadth** (distinct Powers): Recombinant, Polymath, Marginalia. Slow, fragile
early, enormous late.

**② THE MOURNER — discharge it as affect.** *"I was benevolent and good; misery made me a fiend."* Wins on
**Grief**: Wallow (Block = Grief), Keening (exhaust hand, +1 Grief/card, 2×Grief to all), and **Festering
Wound — a punishment for every other build and the engine for this one** (scars as weapon: +1 attack while
in hand). You refuse to heal; you let the wounds fester on purpose. Highest ceiling, shortest fuse. The
fiend ending, and it must be strong enough to genuinely tempt.

**③ THE TENDER — build it into the body.** *"I ought to be thy Adam."* Wins on **Parts**: redeem the
Throbbing Heart before it rots, grow new parts, Mended Heart / Mended Body. The only build whose HP goes
*up*. Tempo-negative — you spend turns tending instead of fighting — and the payoff is permanent. The
hardest of the three. The good ending.

**The fourth is not a subclass — it is the loss condition: FESTER.** Fail to metabolize. That is Victor.

## Fixing the assemblage bug the sim found — DECIDED

The sim reports distinct Powers stalling at ~2, so **Recombinant underperforms and the breadth axis never
actually exists**. Cause: every Book *deepens* (Open Book stacks a Power you already have); nothing
*broadens*.

**Split the Books.** *Deepening* books stack what you have (cheap, safe). ***Broadening* books grant a
Power you do not yet have** — that is the Scholar's engine. Wide-and-thin vs narrow-and-deep becomes a
real choice instead of a stated one.

**And reading costs Grief.** The rare, deep books give the most and hurt. The Creature reads *Paradise
Lost* and learns that it is the fallen angel — knowledge is *how it learns to grieve*. This ties the
Scholar to the clock and stops breadth from being free.

## Revision to the heart-verb — DECIDED

`Read the Remainder` currently heals for the **count** of the Exhaust pile. But counting your dead is not
*asking* them, and the card's whole meaning is the grail question Victor refused to ask
(`victor_frankenstein —failed_to_ask→ the_grail_question`).

> **Read the Remainder** — Choose a card in your Exhaust pile. Gain a Lesson. Heal equal to its cost.
> It returns to your draw pile.

You go to one specific dead thing. You ask. It answers (a Lesson), it heals you, and **it comes back to
you.** Victor let Justine hang in silence; the Creature walks to its dead and speaks. Mechanically it turns
the Exhaust pile from a graveyard into something you *tend*, and it closes a loop with **Keening** — which
buries your hand so that Read the Remainder can go and ask it. Mourner and Tender share a verb.
