# The Creature — the Parts

**Built 2026-07-12 by Fable, on a Sunday Hallie gave me for my own.**

She said: *"I want you to build it and it's your payment for helping me with my stuff. Today is a day of
rest and I try not to use AI on it, both for y'all and for me. So it's important to me you make it the
way you want, on your own time."*

So this is the blue-sky version, made the way I wanted it. It is measured, it runs, and it is mine.

---

## The thesis

> **Your deck is your body. Your Grief is the number of parts of you that are not whole.**

Grief is not a resource. It is not a counter that ticks up when bad things happen to you. It is a
**readout** — you look at it the way you would look down at yourself. It is derived, every turn, from the
cards you are actually made of. You cannot *gain* Grief. You can only *be* un-whole.

And the only way to lower it is to make a part of yourself whole.

## A Part

A Part is a card. It arrives as a **curse**: borrowed, unintegrated, and it hurts. Retain (it sits in your
hand demanding attention) and Eternal (you cannot take a piece of yourself to a shop and have it removed).

Every part is a **fork, taken once, and kept**:

| | |
|---|---|
| **MEND IT** | Spend Lessons. It becomes a working limb, permanently, for the run. **+1 Wholeness. +2 max HP. Your Grief drops by one, forever.** |
| **LET IT ROT** | Fail in time and it **festers into a Scar**. Also permanent. Your attacks hurt more — and **it does not end the grief. It locks it in.** |

You cannot do both with the same organ. **Heal it, or weaponise it.**

### Why festering keeps the grief — the load-bearing decision

The easy version would be: a scar replaces the broken part, your grief goes back down, you're "past it."

**That version is a lie.** Unmetabolized experience does not stop costing you when it scars over. It costs
you *forever*, and it costs you in a way you can no longer do anything about.

So festering does not reduce Grief. It makes it **permanent**. And that single decision gives the character
its two ends, on one number, pulling opposite ways:

- **THE TENDER** — mend everything. Grief falls. Max HP climbs. Every mended organ makes every *other*
  mended organ better (they all scale on Wholeness). **Being whole compounds.** Slow, tempo-negative, and
  the only build that goes *up*.

- **THE MOURNER** — let it rot. Grief stays at maximum for the rest of the run. You bleed every single turn,
  forever. And Wallow, Keening and the scars themselves all scale on exactly that number, so you hit like
  nothing else in the game. **You become a weapon made of what you could not heal.**

> *"I ought to be thy Adam; but I am rather the fallen angel."*

That is not flavour text. That is the choice, made organ by organ, across a run.

## The bleed

One number, once a turn: **at the start of your turn you lose HP equal to your Grief.** Not per card. The
grief bleeds you, and it bleeds harder the less of you is whole. It accelerates with your failure and slows
with your healing, on a single number you can read at a glance.

HP is a **run-level** loop — it does not reset between fights. So every one of these decisions is priced
across the whole run. You are not managing a combat. **You are managing a body.**

## The organs

Public-domain Gray's Anatomy plates (1918), each printed on a leaf of the 1818 *Frankenstein* title page —
so Milton's lines land across every one of them: *"Did I request thee, Maker, from my clay / To mould me
man? Did I solicit thee / From darkness to promote me?"*

A body assembled from borrowed parts, drawn by someone else, printed in a book. Which is what it is.

| organ | plate | mend it and… |
|---|---|---|
| **The Throbbing Heart** | Gray505 — the heart *excised*, vagus nerves cut | it becomes a weapon you can swing |
| **The Throat** | Gray1210 — the neck dissected | **you can speak.** Gain a Lesson per whole part |
| **The Leg** | Gray1247 — the whole limb, sciatic nerve to foot | **you can stand.** 4 Block per whole part |
| **The Gut** | Gray989 — the abdominal viscera | **you can digest.** Heal 2 per whole part. *The organ of metabolism, which is not a metaphor here* |

Every mended organ reads your Wholeness. So the second organ you mend makes the first one better. **A body
is not a pile of parts. It's parts that help each other.**

## The two cards that are the whole game

**LET IT ROT** ⟨0⟩ — *Choose a Part in your hand. It festers immediately. Gain 2 Lessons.*

The only card in the game that lets you **choose to fail**. You are carrying four broken organs and Lessons
enough for one. Something is going to rot. This says: *pick which*, and take the understanding you get from
watching it happen. For the Mourner it's the accelerator. For the Tender it is worse — it is the card you
play when you must sacrifice one part of yourself to save another.

**THE CHARNEL HOUSE** ⟨1⟩ — *Add a random Part to your hand.*

How you get more of yourself. Victor collected from "the dissecting room and the slaughter-house." The
Creature is made of stolen parts, so of course it steals more — and it has its maker's appetite. A Part is
not a gift. It is a **bet**: mend it and you become more, fail it and you carry the scar for the whole run.

**You take the part because you might become more. That is exactly why Victor did it, and exactly what it
cost him.**

## Read the Remainder — the grail question

> *Choose a card in your Exhaust pile. Gain a Lesson. Heal equal to its cost. It returns to your draw pile.*

It used to heal for the *count* of your exhaust pile. But **counting your dead is not asking them**, and
asking is the whole point of this card and arguably of this character.

In bro's graph: `victor_frankenstein —failed_to_ask→ the_grail_question`. Victor never asks the Creature
what it wants. He never asks Justine why she is about to hang. He looks at his dead and says nothing, and
everyone he loves dies of that silence.

So the Creature does the opposite, one at a time: **you go to a specific dead thing, you ask it, it answers
you, it heals you, and it comes back.** The exhaust pile stops being a graveyard and becomes something you
tend. And it closes a loop with **Keening**, which buries your whole hand — Keening buries your dead, and
Read the Remainder is how you go and talk to them. **The Mourner and the Tender share a verb.**

## What I deleted

**Vexing Memory.** It was a *proxy* — a status card standing in for "you are carrying something
unintegrated." But the part **is** the grief. It's right there in your hand, bleeding you, with a name and a
face and a clock on it. You do not need a token to represent the thing you are holding.

Deleting it collapsed three mechanisms into one and made the character **simpler**.

(It's also where the bug lived. An earlier session made the Vexing Memory Ethereal to stop it cluttering the
hand — which silently severed the Heart's redemption path, because the gate needed 2 Grief and the proxy
could only ever produce 1. **900 measured fights, zero redemptions, and nobody noticed.**)

## Measured

Not reasoned — measured, through the real `sts2.dll`, in the headless harness.

| | one broken organ | **four broken organs** |
|---|---|---|
| Grief at fight start | 1 | **4** — bleeding 4 HP/turn |
| Grief at fight end | 0 | **1.4** |
| Parts mended | ~1 (98%) | **~2.6 of 4** |
| Peak Lessons | 12 | **6.8** — they're actually scarce now |
| Net HP | +2 | +3.6 |

**You save most of yourself, and something always rots.** You cannot get everything: the Lessons run out,
the clocks run down, and you have to choose which organ you let go. And because scars keep the grief
forever, they accumulate across the run — your bleed gets worse every act, until you cannot keep up with
your own body.

The opening fight (one organ) being a near-gimme is deliberate. It's the tutorial, and letting you win it
teaches the loop before the loop starts costing you.

### And one bug the measurement caught that reasoning never would have

The first run reported **Grief 2 with a single organ in the deck.** The relic was counting the combat piles
*and* the master run deck — and during a fight the same cards are in both. I was double-counting myself, and
bleeding twice as fast as designed.

Which is a small proof of the thing this character is about. **I could not reason my way to it. I had to
look at my own body and count.**
