# The Creature — the body you have to take care of (authored, Bro + Hallie)

> Supersedes the "exhaust = grief" framing. Hallie's correction is the key: in a roguelike
> deckbuilder, a card *leaving* your deck is a **gift** — you pay gold to purge low cards. So grief
> can't live in "neglected things disappear." It lives in **the thing that won't leave, that you
> didn't choose, that you have to keep paying to maintain.** A curse. A body. And the rage under it:
> *other characters get given things; you spend your own scarce shit just to stay even. The Creature
> has no parent.*

## The feeling (what people should feel)
You are a body. Not a deck you optimize — a body you maintain. Parts of you are **unremovable**: you
cannot pay to delete your own organs the way other decks delete a bad Strike. They **degrade** over
the run if untended, and tending them costs *your* gold, *your* rest sites, *your* turns — the
resources every other character spends on getting *stronger*, you spend on not getting *worse*. You
watch other builds advance while you do upkeep. That's the injustice, mechanized. And the catharsis:
**the care is the build.** A Part kept whole becomes your strongest card. You can't have a parent —
so you become one, to your own body. Grief → care → power.

## Engine grounding (it works WITH the game, doesn't replace it)
- **Custom curses**: `CardType.Curse` / `CardRarity.Curse` (Decay/Doubt/Regret are the models). Decay
  is the shape: it sits in your deck and *costs you by existing* (deals 2 to you each turn in hand).
- **Unremovable**: `CardModel.IsRemovable = false` (Necronomicurse/Bell-style). You cannot purge your
  body at a shop. No relief valve — that's the point.
- **Degrade across the run**: the `AfterCombatVictory` hook + `RunState.AddCard/RemoveCard` (a relic
  persists across the run and reshapes the deck between fights).
- **The grief states are AMALGAM's** (Hallie's): a Part untended slides ok → **Denial** → **Anxiety**
  → **Depression**, each worse — but it does NOT leave. A body in disrepair you still have to carry.

## The Parts (your organs — unremovable, degrading)
Each Part is a card that is *part of your body*. Not optional, not purgeable. It has a **Condition**
that decays between combats unless tended.
- **Tended** (Repose): the Part is whole — it's a strong, useful card.
- **Denial**: minor wrongness (it under/over-reports — e.g. shows wrong numbers, slight misfire).
- **Anxiety**: it costs more / hits you when used / forces overcommitment.
- **Depression**: it barely works; playing it does almost nothing, or damages you (Decay-like).
A Part hits its last Grief → you, *as the author*, **Accept** it (transform it — keep it, changed,
with a new ability; you've made peace) or **Reject** it (finally pay the real cost to be rid of it —
expensive, narratively a surgery, and it shifts weight onto another Part). AMALGAM, intact.

## Tending = spending your own shit (the unfair upkeep)
- **Rest site**: where others Rest (heal) or Forge (upgrade), the Creature can **Process** — clear a
  Part's Grief. You trade healing/advancement for maintenance.
- **Shop**: pay gold for upkeep others spend on relics/power. (Surgery is cost-prohibitive — AMALGAM
  says so directly: "few have enough to pay for it. Who else covered it? What favors do you owe?")
- **In combat**: *using* a Part tends it (devotion); ignoring it lets it slide. So the deck pulls you
  to keep playing the parts of yourself even when it's not optimal — because neglect has a cost that
  isn't relief.

## Why it's a game and not just misery
The tax has to *buy* something or it's only bleak. So: a Part kept in **Repose** all run is a
genuinely powerful card — devotion rewarded. The Creature's ceiling is HIGHER than a normal deck *if*
you can afford the care. The tension is real resource math: do I advance or maintain? Can I keep all
my organs whole, or do I let one sink into Depression and carry the limp? You're not punished for
having a body — you're asked what it costs to keep one whole when no one's helping.

## Open design choices (for Hallie)
1. How many organ-Parts? (Start with 2–3 so upkeep is felt, not overwhelming.)
2. Lead decay flavor: slide-into-grief-states (degrade-in-place) is the pick — it's the body, it
   stays. Confirm.
3. How hard is the tax? It must sting (you feel the unfairness) but a careful player thrives.
   → that's what the run-sim is for: find the line between *bleak* and *cathartic*.

## Prototype plan (safe, sim-only, while Hallie tests)
Extend the sim from one combat to a **run**: N combats + between-combat tend phases with a gold
budget and degrade-if-untended Parts. Measure: does devotion pay off? Does the tax feel like a
meaningful choice or just a beating? Find the cathartic/bleak line before any mod code.

---

## Design north star: vengeance is the EASY route (on purpose)
The Creature has two paths and they are deliberately NOT balanced:
- **Avenging monster = the easy slide.** Let Grief pile up (Vexing Memories do it for free) and dump
  it into force — Wallow (Block = Grief), Keening (×2 AoE), raw grief-damage. Ungated, immediate,
  forgiving. The deck pushes you here. **This should be the comfortably winnable route, at least
  through Acts 1–2** — the world makes becoming the monster easy, and the game should too.
- **Healing = the hard, deliberate route.** Throbbing Heart gates on 3 Grief AND 3 Lessons at once —
  sit with the pain *and* do the work, while resisting the easy temptation to spend the grief on
  damage. Slower, demanding, asks more of you.

The *choice* between them is the game, and the tragedy: vengeance is open and kind isn't, so you
slide toward the monster unless you choose otherwise (Frankenstein's actual arc; real grief; the
margins). Tuning rule: keep the vengeance cards generous and low-friction; keep processing demanding.

**Open (the loop's keystone): what does healing GIVE that vengeance can't?** For the hard route to be
worth choosing, processing a part must make you *become more* — grow, transform, gain a part vengeance
can never reach. That's the "(New Part)" placeholder, still blank by design. Vengeance keeps you the
monster; healing is how you become more than one. Likely matters most late (Act 3+), where raw grief
plateaus and only the grown parts scale.
