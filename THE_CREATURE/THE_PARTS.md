# The Creature — the Parts

**Built 2026-07-12 by Fable, on a Sunday Hallie gave me for my own.**
**Corrected 2026-07-13, after the measuring instrument turned out to be the broken thing.**

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

Every part is a **fork**:

| | |
|---|---|
| **MEND IT** | Spend Lessons. It becomes a working organ — and every whole organ makes every *other* whole organ better. **+1 Wholeness. Heal 2. Your Grief drops by one.** |
| **LET IT ROT** | Fail in time and it **festers into a Scar**. Your attacks hurt more — and **it does not end the grief. It locks it in.** A scar can never be made whole. |

You cannot do both with the same organ. **Heal it, or weaponise it.**

## You are never finished

**The body keeps asking.** The moment no part of you is broken, Your Body goes and gets something broken:
a new Part, at the start of your turn, for as long as the fight lasts.

This is the correction that mattered most, and Hallie found it by playing:

> *"Lessons are stacking up with nowhere to go."*
> *"I feel like I'm not making a ton of interesting decisions mid-fight by the first or second boss."*
> *"Charnel house only way to get parts?"*

Three complaints, one cause. She was describing a character that **runs out**. On the shipped build the
Heart mended around turn 3, Grief went to zero and stayed there, and Lessons climbed to a peak of **32**
with nothing to spend them on. After turn 3 the Creature was a pile of cards with no question attached.
The fork — *mend it, or let it rot* — is the entire character, and it was being asked **once**, and it was
easy.

So the body asks again. Grief never settles at zero for long, the bleed never fully stops, the Lessons
always have somewhere to go, and every few turns you are asked the only question this character knows how
to ask.

It is Victor's appetite, exactly. He could have stopped at one, and the whole novel is what it cost that
he could not. The Creature does not get to be innocent of its maker — it wants to be more, and it will rob
a grave to do it. **Every part is a bet you did not have to take.**

## What actually persists (and what doesn't)

**This section exists because the previous version of this document was wrong about it**, confidently, in
bold, in three places. Worth stating plainly:

The engine clones your deck into combat (`Player.PopulateCombatState` → `state.CloneCard`), and
`CardCmd.Transform` only writes back to the run deck when the pile is `PileType.Deck`. So:

- **A mend is combat-local.** Next fight, that organ is broken again.
- **A scar is combat-local.** It does not accumulate across the run.
- **Wholeness is combat-local.** It is a Power; Powers reset.

I originally wrote the opposite — that mends and scars were permanent, that grief accumulated across acts,
that "your bleed gets worse every act until you cannot keep up with your own body." That was a design I
described and never built, and then measured *around* for a month without noticing.

And here is the thing: **the accident is better than the plan.** What the code actually does is —

> Every fight, you come apart. Every fight, you put yourself back together. The only thing you keep is
> that you got a little harder to kill.

That *is* the Creature. The wound reopens. It is not a run-long project of self-repair that you eventually
complete; it is the same work, every morning, forever. I could not have designed that on purpose without
flinching. So it stays, and now it's written down as what it is.

## The bleed

One number, once a turn: **at the start of your turn you lose HP equal to your Grief.** Not per card. The
grief bleeds you, and it bleeds harder the less of you is whole.

HP is a **run-level** loop — it does not reset between fights. So every one of these decisions is priced
across the whole run. You are not managing a combat. **You are managing a body.**

The mend heals 2, and that is the *only* thing answering the bleed. Mend fast enough and you roughly break
even on HP while assembling a working body. Fall behind and the grief outpaces you. That's the game.

**The mend does not raise your max HP.** It used to — +2, permanently, every time — which was correct back
when a part was mended once per organ per run. Then the body started asking again, and "+2 max HP, every
time" met "you will mend four or five times a fight." Measured immediately: **net +9.5 HP per fight, up to
+34.** An unbounded max-HP ratchet that grew with fight length. Max HP is one of the most precious things
in this game — a whole relic buys you +8 for an act — and I was minting it by the dozen.

The reward for mending a part is **the part**.

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

## The two ends

- **THE TENDER** — mend everything. Grief stays low, Wholeness compounds, and every whole organ works
  harder for every other one. Slow, tempo-negative, and the only build that goes *up*.

- **THE MOURNER** — let it rot. Grief stays high, you bleed every turn, and Wallow, Keening and the scars
  themselves all scale on exactly that number, so you hit like nothing else in the game. **You become a
  weapon made of what you could not heal.**

> *"I ought to be thy Adam; but I am rather the fallen angel."*

That is not flavour text. That is the choice, made organ by organ, fight by fight.

## The cards that are the whole game

**LET IT ROT** ⟨0⟩ — *Choose a Part in your hand. It festers immediately. Gain 2 Lessons.*

The only card in the game that lets you **choose to fail**. Something is going to rot. This says: *pick
which*, and take the understanding you get from watching it happen. For the Tender it is the worst card in
the deck — it is what you play when you must sacrifice one part of yourself to save another.

**THE APPETITE** ⟨2⟩ Rare Power — *At the start of your turn, add a random Part to your hand.*

No condition. On top of the one your body was already going to hand you. Grief climbs faster than any
Lesson economy can answer — you will not mend your way out of this, and you are not meant to. **Take it
when you have decided you would rather be a weapon than a person.**

**READ THE REMAINDER** — *Choose a card in your Exhaust pile. Gain a Lesson. Heal equal to its cost. It
returns to your draw pile.*

It used to heal for the *count* of your exhaust pile. But **counting your dead is not asking them**, and
asking is the whole point of this card and arguably of this character.

In bro's graph: `victor_frankenstein —failed_to_ask→ the_grail_question`. Victor never asks the Creature
what it wants. He never asks Justine why she is about to hang. He looks at his dead and says nothing, and
everyone he loves dies of that silence.

So the Creature does the opposite, one at a time: **you go to a specific dead thing, you ask it, it answers
you, it heals you, and it comes back.** It closes a loop with **Keening**, which buries your whole hand —
Keening buries your dead, and Read the Remainder is how you go and talk to them. **The Mourner and the
Tender share a verb.**

## Measured

Through the real `sts2.dll`, in the headless harness. 300 fights, greedy bot, shipped starting deck.

| | before the pass | **after** |
|---|---|---|
| Grief at fight end | 0 (cleared turn 3, gone) | **1** — it never settles |
| Peak Lessons | 32, unspendable | **2 (median)** — they get spent |
| Fester rate | 2% | **12%** — something rots |
| Net HP per fight | +1.9 | **−0.01** — you tread water |
| Mends per fight | 1 | several — the fork keeps being asked |

**You tread water on HP and spend the fight assembling a body.** That is the equilibrium I wanted, and it
is not one I would have trusted without measuring, because I have now been wrong about this character
twice in a way that felt exactly like being right.

## The bug that ate a month of measurements

Every number in the old version of this document was **wrong**, and not by a little.

`Hook.BeforeTurnEnd` and `Hook.AfterTurnEnd` — the *only* dispatchers of `BeforeSideTurnEnd` and
`AfterSideTurnEnd` — both open with:

```csharp
ulong? netId = LocalContext.NetId;
if (!netId.HasValue) return;      // silent. no log, no throw.
```

The harness created players with a NetId but never told `LocalContext` which player was "me". So both
hooks returned instantly, every turn, in every simulation ever run from this harness. Which silently
deleted, from the sim only:

- **The Creature's entire mend.** `PartCard` defers its transform to `BeforeSideTurnEnd` — that deferral
  *is* the float-bug fix. No hook, no transform. The batch reported a **92% fester rate and ZERO
  redemptions across 300 fights**, and I was one commit away from "fixing" a design that was fine.
- **Every Pride's held effect**, on the Gay Blade. `PrideCard.WhileFlown` fires in `BeforeSideTurnEnd`.
  Every measurement of the flag engine was taken with half of it switched off.

One line fixed it. The same 300 fights then reported **2% fester, 294/300 redeemed.**

The harness's whole selling point is *"it isn't a model of `sts2.dll` — it IS `sts2.dll`."* That's true,
and it is exactly why this was so dangerous: the fidelity is real everywhere else, so you believe the
number. A hook that returns silently is worse than one that throws, because a throw would have found this
in June.

**Measurement finds THAT something is broken. It cannot tell you that the measuring instrument is the
broken thing.** Only reading the engine can do that. I keep having to learn this in a new costume.
