# The Gay Blade — 2.0 card list (design, consolidated 2026-07-11)

**Hallie's character, made as a gift for Lori.** Design and art hers.

> **STATUS: this is the DESIGN. Almost none of it is in the code yet.** What's currently *implemented*
> is the old character (Prides as Powers, no orbs, no retain rhythm) wearing the new art — which is
> exactly why playtesting feels "in between." For what's actually compiled right now, see
> `CARDS_IMPLEMENTED.md` (generated from source; it can't drift).
>
> **This file is editable and is meant to be edited.** ⚠ = not built. ✅ = built and matches design.

---

## THE CORE RHYTHM — one verb for the whole deck

> **Held:** does a small thing every turn, and costs you a hand slot.
> **Played:** cashes out, and frees the slot.

Learn it once and every card in the deck is legible on sight. It makes **hand space the deck's real
currency** — one contested resource, everything competing for it. And it's the same decision fractally
at every scale: one card (bank or cash), one blade (forge or swing), one deck (stay lean or wash).

**Retain must NEVER be a gift.** Retain is the *cost* you pay for the passive. Any card that hands out
Retain for free is donating the scarcest thing in the deck. (This is why old Watcher Pride is incoherent
— see below.)

**Hand size is therefore the most important number in the game.** Anything that raises it is a major
effect and should be priced like one.

### The variety lives in the RELEASE PRESSURE, not in a second verb

Same verb, six different reasons to let go. This is the rhythm section:

| card | why you let go | timing |
|---|---|---|
| **Butch / Femme** | passive *grows* as you re-forge | **greed**-timed |
| **Dark orb** | passive banks silently, automatically | **patience**-timed |
| **Lightning / Frost orb** | flat passive, no growth — cash when you need the burst | **need**-timed |
| **Femme's retaliate** | only pays if you're being hit | **the enemy** times it |
| **the bad orb** | passive is *negative* — a hand-hostage | **coerced** |
| **the Prides** | the passive *is* the point; you never want to let go | **never** — they're the clog |

**The Prides are the clog; the blades are the flow.** The tension falls out for free: Prides accumulate,
your slots die, and you're forced to cash out blades you'd rather have kept banking.

---

## THE THREE SUB-ENGINES

Three different relationships to persistence. There is no fourth.

| | lives in | costs you | you… |
|---|---|---|---|
| **Blades** | your **hand** | a slot, every turn | **carry** it |
| **Shivs** | **nowhere** | tempo, not space | **throw** it |
| **Queering** | your **deck** | dilution | **become** it |

**Queering is predominantly FIGHT-scale** (with some campaign-scale). So the bloat resets each fight,
you can be reckless, and the *sculpting is the gameplay* rather than a run-level tax.

**The wash makes basics; Queering makes basics good.** Cash out a 3×-forged Butch → four Strikes in the
discard → that's mush *unless* you can queer it. They're one engine with two strokes.

**The governor:**
- **Entropy pump** ⚠ — cards that do one strong thing ONCE and dissolve into a Strike/Defend. Pay now,
  charge yourself in deck quality. *Manufactures the raw material.*
- **Entropy sink** ⚠ — Powers that exhaust X cards and fold their queerness into a new card. **The only
  thing in the deck that makes it smaller.** Make sure there's more than one and that they're findable.
- **Throttle** ⚠ — shiv-likes that "queer all attacks played this turn." Rewards going wide, which is
  what shivs already do.

**The skill is staying between too-thin (no throughput) and too-fat (mush).**

---

## THE GAY BLADE ENGINE — forge → bank → cash → recycle ⚠ NOT BUILT

- **Fancy Footwork** ⟨1⟩ — **Exhausts on BOTH paths.** Play it (attack → forge Butch) or hold it to end
  of turn (block → forge Femme). One forge per Footwork. That's the loop's governor. ✅ *(partially built)*
- **Butch Blade** ⟨1⟩ [Token] — Retain. Held: your attacks deal +1. Played: deal 8. ✅
- **Femme Flechette** ⟨1⟩ [Token] — Retain. Held: deal 3 back to any enemy that attacks you. Played: deal 5. ✅
- **Re-forging a held blade = +1 retain level** ⚠ — raises its passive (+1 atk / +1 retaliate).
- **Playing Butch/Femme = transform + cash out** ⚠ — becomes a basic Strike/Defend **AND spawns one more
  per retain level.** A 3×-forged Butch = a Strike + 3 more Strikes, into discard.
- **Relic: Strike/Defend → Fancy Footwork** ⚠ — the only replenishment. **This relic's conversion rate is
  the dial for the whole engine's tempo.**

**"Do I put it through the wash?"** Keep forging (bank levels, ride the growing passive) vs cash out (dump
the pile into your deck). Hold too long → never spend. Cash early → a trickle. **THE decision.**

---

## THE PRIDES — Powers → RETAINED BLADES ⚠ NOT BUILT (this is the big reflow)

**Each Pride becomes a retain sword, not a power** — even if it's a sword that *produces* a power. Each
gets an **on-retain passive** (flying the flag) and an **on-hit rider** (swinging it).

**And swinging a flag is HAND RELIEF.** You put the banner down because you need your hands. That's the
closet in miniature, and it means the mechanic and the meaning are the same object.

| Pride | HELD (on retain) | SWUNG (on hit) | status |
|---|---|---|---|
| **Silent** | gain 3 Block when you discard a card | inflicts **Weak** | ⚠ |
| **Ironclad** | deal 3 damage when you exhaust a card | inflicts **Vulnerable** (level) | ⚠ |
| **Watcher** | draw 2, discard 1 at start of turn | ⚠ *(no rider specified)* | ⚠ |
| **Regent** | costs another **Pride Blade** (not a pet); each turn deal 6 + gain 6 Block | ⚠ | ⚠ |
| **Defect** | **on draw → a random retained ORB in hand** (Dark / Lightning / Frost) | ⚠ | ⚠ |
| **Necrobinder** | summons an Osty (a real pet — a "maker") | — | ✅ *(as Power)* |
| **Dyke** | makes a **Labrys** parry-weapon (block next hit, bank as attack, discard) | — | ⚠ NOT BUILT AT ALL |

### ⚠ Watcher Pride is currently INCOHERENT
It grants **Retain to a random card in your hand.** Under the new grammar that isn't a buff — it's
**making a random card sticky**, which is the bad thing. It donates the scarcest resource in the deck,
for free, without asking. Your own note already replaces it (*"draw two, discard one"*).

### ⚠ Pride Golem: CUT
Its only structural reason to exist was the pet-sacrifice that Regent needed. Once Regent eats **Pride
Blades** instead of pets, the Golem has no dependents. `//We should remove pride golem, not there yet`
and `//Regent works as is if it applies to Blades` are the **same decision written twice.**

### Defect Pride's orbs — the chaos knob ⚠
A Power that puts a random orb-card in your hand. Orbs **stack up in hand** (the hand-lock is the point).
Same grammar as everything else: **hold for the passive, play to evoke and free the slot.**

- **Dark** — held: banks power silently each turn. Evoked: dump the whole pile. *(This is the wash, in one card.)*
- **Lightning** — held: chip damage each turn. Evoked: the big hit.
- **Frost** — held: block each turn. Evoked: the big wall.
- **the bad one** — held: **−1 energy each turn.** Evoked: it goes away. **A hand-hostage** — and it's the
  one that makes the other two mean anything. *The Defect's pride is that you don't get to choose what
  you carry.*

---

## THE FLAG PAYOFFS — count-in-HAND vs count-in-DECK ⚠ NOT BUILT

**Both, on different cards.** They're coupled: **every time you cash out a blade, you dilute your Pride
draws.** Going fat doesn't add Prides — it makes the ones you have *harder to find*.

- **Stonewall** ⟨2⟩ — counts Prides **IN HAND**. *The riot: only as strong as who actually showed up.*
  **This is what the wash threatens.** ⚠ *(currently counts Flags generally)*
- **Rainbow Strike** ⟨1⟩ — counts Prides **IN DECK**. The payoff that survives the bloat. ⚠
- **Corporate Sponsored Pride** ⟨0⟩ — counts Prides **IN DECK**, *because a corporation counts all of your
  pride, including the parts you aren't showing anyone.* ⚠
- **Pride was a Riot** ⟨1⟩ — strip target's Block, then deal 5. ✅
- **Portal to the Knife Dimension** ⟨3⟩ — each turn, copy a Blade from deck to hand w/ Exhaust+Ethereal. ✅

**Terminology:** "Flag" → **"Pride"** everywhere in card text. ⚠ *(still says Flag in Rainbow Strike,
Stonewall, Corporate Pride)*

---

## STEALTH / THE CLOSET — the anti-flag axis

**Stealth is no longer an IFlag.** ✅ *(done 2026-07-11)* Being hidden is not a pride flag, so it no
longer counts for Stonewall / Rainbow Strike. That sets up the real axis:

> **Visible** — flags in hand, everything scales, hand full of banners you can't fight with, taking hits.
> **Closeted** — Intangible, safe, nothing scales.

- **Vanish** ⟨1⟩ — Gain 2 Stealth. ✅
- **The Closet** ⟨1⟩ — Gain 3 Stealth. Next Attack played → lose all Stealth. ✅
- **The Discourse** [Status] — in hand at end of turn → 1 less energy next turn. Exhaust. ✅
- **Extremely Online** ⟨0⟩ [Power] — +2 energy, +2 each turn, shuffle a Discourse into draw. ✅

---

## KNIVES / SHIVS — disposable, no hand cost

**The shiv's real cost is that it isn't a blade.** Shivs *spend*; blades *bank*. A pure shiv deck is fast
and has no late game. **Queering is what gives a shiv deck a late game** — by making the basics it never
bothered to improve actually worth drawing.

- **Throwing Shiv** ⟨0⟩ — deal 1; held to end of turn → 3 to ALL and Exhaust. ✅ *(class is still `Kunai`;
  CARDS.md still calls it Kunai)*
- **Knife Whip** ⟨1⟩ — deal 8, put a Shiv in discard, this card's damage −1. ✅
- **Throwing Knife** ⟨1⟩ — deal 6; if it deals HP damage, Exhaust; else return to hand. ✅
- **Superfan of Knives** ⟨2⟩ — deal 4 to ALL, add a Shiv per enemy, your Shivs hit all this turn. ✅
- **Pin** ⟨1⟩ — deal 4; on damage, 2 Weak + 2 Vulnerable. ✅
- **Shiv modifiers** — Poison Coating, Explosive Tip. ✅

---

## OPEN

- **Watcher's swing rider** — unspecified.
- **Regent's swing rider** — unspecified.
- **Dyke Pride / Labrys parry** — mechanism solved (BufferPower hooks); the A-vs-B design question is open.
- **Two Labryses** (the pet vs the in-hand parry) — keep both, or fold?
- **The relic's conversion rate** — the single most important number in the character.
