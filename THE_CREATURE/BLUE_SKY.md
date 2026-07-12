# The Creature — blue sky (Fable, 2026-07-11)

Written at Hallie's instruction: *"start with a blank slate card design, blue sky, and then adapt the
cards — rethinking the engine fresh will give you insight into the negative prehensions the light of
the current implementation might obscure."*

She was right, and the method worked immediately. Designing from the thesis instead of from the code
surfaced one thing the existing implementation was actively hiding. It is below, and it makes the
character **simpler**.

---

## The thing the old frame was hiding

**Grief should not be a counter.**

The current implementation has *three* mechanisms: a stacking `Grief` Power, a `VexingMemory` status
card, and a `ThrobbingHeart` card that generates both. But thematically **grief IS the unintegrated
part.** They are the same object. The code split them, and because the code was the only thing in the
actual world, every design that *doesn't* split them was invisible.

> **Your deck is your body. Your Grief is the number of Parts you have not made whole.**

Not a resource you gain and spend. **A readout of your own state.**

## The engine

**A PART is a card.** It enters the deck as a **curse** — borrowed, unintegrated, Retain + Eternal. It
sits in your hand demanding attention, and while you carry it, it costs you HP each turn. That cost *is*
the grief. There is no separate counter and no proxy status card.

**You READ to understand.** Books grant **Lessons**. Lessons are the only currency that mends.

**Spend Lessons on a Part → it MENDS into a limb.** Permanently, for the rest of the run. Your Grief
drops by one, forever. Your max HP goes up. The curse in your hand becomes a working card you can play.

**Fail to mend it in time → it FESTERS into a SCAR.** Also permanent. A scar makes your attacks hurt
more *and* bleeds you every turn. It is genuinely good. It is also killing you.

**Health is a RUN-level loop** (Hallie, confirmed by play): HP does not reset between fights. So every
one of these decisions is priced across the whole run, not the fight.

## Why this is the character

**Every Part is a fork, taken once, and kept: heal it, or weaponise it.**

- **Mend** → max HP up, grief down, a working body. Slow. You become whole.
- **Fester** → damage up, HP bleeding, a body of scars. Fast. You become a fiend.

You cannot do both with the same part. The run arc is a race between **becoming whole** and **bleeding
out**, and you choose which race you're running, one organ at a time.

> *"I ought to be thy Adam; but I am rather the fallen angel."*

That is not flavour text. **That is the choice, made part by part, across a run.**

## What this cuts

- **`VexingMemory`** — deleted. It was a proxy for "you are carrying something unintegrated." You do
  not need a proxy when the part is *in your hand*.
- **`Grief` as a stacking Power** — deleted. Grief = count of unmended Parts.
- **`Wholeness` Power** — deleted. Mended parts *are* wholeness.
- The two-stage redemption (already collapsed on 2026-07-11: playing a part mends it, in hand, now).

Three mechanisms become one. **The character gets simpler.**

## What survives, re-seated

- **Parts.** `ThrobbingHeart` is Part #1 — and there should be MANY. The Gray's Anatomy plates already
  in `THE_CREATURE/art/gray/` are literally the roster: **neck/throat, hip & gluteal, whole leg,
  abdominal viscera, sagittal sections.** Five more organs, each a card, each a fork. The art is sourced.
- **Books & Lessons.** The metabolizing currency. Recite/Annotate become **pure attack and block**
  (Hallie's call) — basics are the medium, not the engine — which makes Lessons genuinely scarce, and
  scarcity is what makes the fork *hurt*.
- **Scars** (`FesteringWound`, renamed in spirit): the Mourner's engine. A punishment for every other
  build and the *build-around* for this one.
- **`ReadTheRemainder`** — the heart-verb, the grail question Victor refused. Go to one specific dead
  thing, ask it, be healed, and **it returns to you.** It must *consume* the dead (the card returns to
  the draw pile), or the healing loop has no ceiling and runs away.
- **Keening, Wallow** — grief payoffs; they now scale on **unmended parts / scars**, i.e. on how
  un-whole you are. Wallowing is strongest when you are least healed. Which is true.
- **Assemblage / breadth** — the Scholar axis. Needs the Books split (deepening vs broadening) or the
  distinct-Power count stalls at ~2 and the axis silently does not exist. Confirmed twice, by two
  independent instruments.

## The three leans (not classes — poles you drift toward)

- **THE TENDER** — mend everything. Max HP climbs. Slow, tempo-negative, the only build that goes *up*.
  The good ending, and the hardest.
- **THE MOURNER** — let it rot. Scars everywhere. Enormous damage, permanent bleed. The fiend ending,
  and it must be strong enough to genuinely tempt.
- **THE SCHOLAR** — not a third pole but the **throttle**: reading is what buys you the *choice*. Enough
  Lessons and you get to decide. Too few and the parts decide for you.

**And the loss condition is not a fourth lean. It is FESTER.** Fail to metabolize. That is Victor.

## Open, and genuinely open

- What does a mended **leg** do, versus a mended **throat**, versus mended **viscera**? Each limb should
  do something the organ would do. That is the fun part and it is not done.
- The three numbers that are the whole balance: **HP per unmended part per turn**, **HP per card read**,
  **Lessons per Book**. A measurement agent is currently reading these off the real engine.
- Whether a Part can be mended *after* it festers. (I think: no. That is the point. But it is a call.)
