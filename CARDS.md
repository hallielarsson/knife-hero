# Knife Hero — current card list (mechanics ground truth)

Snapshot 2026-06-13. Source: live loc (`cards.json`) + card source. **⚠ HELD** = staged in source but
NOT in the running game yet (last bake was the 19:17 `.pck`); these go in on the next `bake it`.

Legend: cost in ⟨⟩, rarity in [ ]. "Pride" = the retain-sword flags (the reflow target).

---

## THE GAY BLADE

### Basics
- **Strike** ⟨1⟩ [Basic] — Deal 6.  *(⚠ HELD: new art rigged)*
- **Defend** ⟨1⟩ [Basic] — Gain 5 Block.

### The Footwork engine (forge loop)
- **Fancy Footwork** ⟨1⟩ [Common] — Deal 6, forge a **Butch Blade** into hand. If held to end of turn: gain 3 Block, forge a **Femme Flechette**. *(loc still says Top/Bottom — stale text)*
- **Butch Blade** ⟨1⟩ [Token] — Retain. While in hand, your attacks deal +1. Deal 8. Re-forge = **+1 attack**. *(⚠ HELD: forge +1, was +4)*
- **Femme Flechette** ⟨1⟩ [Token] — Retain. While in hand, deal **3 back** to any enemy that attacks you. Deal 5. Re-forge = **+1 retaliate**. *(⚠ HELD: full retaliate rework)*
- *Rule:* one copy per blade — re-forging the one you hold **upgrades** it, never duplicates.

### Knives
- **Kunai** ⟨0⟩ — Deal 3. Exhaust. *(the basic shiv-token)*
- **Knife Whip** ⟨1⟩ [Common] — Deal 8. Put a **Shiv** in discard, this card's damage −1 (decays as you swing). *(⚠ HELD: Kunai→Shiv)*
- **Throwing Knife** ⟨1⟩ — Deal 6. If it deals HP damage, Exhaust; else return to hand.
- **Superfan of Knives** ⟨?⟩ — Deal 4 to ALL. Add a Shiv per enemy. Your Shivs hit all this turn. Exhaust.

### Stealth / online
- **Vanish** ⟨?⟩ — Gain 2 Stealth.
- **The Closet** ⟨?⟩ — Gain 3 Stealth. Next Attack played → lose all Stealth.
- **The Discourse** [Status] — In hand at end of turn → 1 less energy next turn. Exhaust.
- **Extremely Online** ⟨0⟩ [Power] — Gain 2 energy. +2 energy each turn. Shuffle a Discourse into draw.

### Pride / flag payoffs
- **Rainbow Strike** ⟨?⟩ — Deal 2 per **Flag/Pride** you have.
- **Stonewall** ⟨?⟩ — Gain 10 Block. Each **Flag/Pride** attacks for 3.
- **Pride was a Riot** ⟨?⟩ — Strip target's Block, then deal 5.
- **Corporate Sponsored Pride** ⟨?⟩ — Remove a Flag/Pride → gain 2 Energy.
- **Pride Golem** ⟨?⟩ — Destroy all your Flags+Pets → summon a Pet with 2× that HP; it retaliates.
- **Portal to the Knife Dimension** ⟨3⟩ [Power] — Each turn, copy a Blade from deck to hand w/ Exhaust+Ethereal.
- **Knife in Front / "Labrys Axe"** ⟨?⟩ — Summon a Labrys **pet** (8 HP) that tanks hits for you. *(NB: Dyke Pride's Labrys is a different, in-hand parry-weapon — see spec)*

### ⚑ THE PRIDES — reflow target (currently Powers; → retain swords)
*Each currently a Power card you play once. The reflow: each becomes a **retain sword that rides your
deck and upgrades incrementally.** "Pride" replaces "Flag" as the name.*
- **Silent Pride** ⟨1⟩ [Unc, Power] — Start of turn: Shiv to discard.
- **Ironclad Pride** ⟨1⟩ [Unc, Power] — End of combat: heal 5.
- **Necrobinder Pride** ⟨1⟩ [Unc, Skill] — Summon an Osty (real pet). *(a "maker")*
- **Regent Pride** ⟨2⟩ [Rare, Power] — Sacrifice another Pride; each turn deal 6 + gain 6 Block.
- **Watcher Pride** ⟨1⟩ [Unc, Power] — Start of turn: Retain a random hand card.
- **Defect Pride** ⟨1⟩ [Unc, Power] — Powers cost 1 less; draw on Power play.
- **Dyke Pride** — *(NOT BUILT)* makes a **Labrys** parry-weapon (block next hit, bank it as attack, discard). *(a "maker")*

---

## THE CREATURE  *(grief / Lessons / parts — Claude-authored, disclosed)*

### Basics & Books
- **Recite** ⟨1⟩ [Basic] — Deal 6 (upgrade +3).
- **Annotate** ⟨1⟩ [Basic] — Gain 5 Block (upgrade +3).
- **Open Book** ⟨1⟩ [Common] — Gain 5 Block **and** 2 Lessons. Recurs. *(⚠ HELD: now block+lessons, no exhaust)*
- **Marginalia** ⟨1⟩ [Common, Power] — Play a Book or Power → gain 1 Lesson.
- **Polymath** ⟨2⟩ [Unc, Power] — Start of turn: gain 1 Lesson.

### Lesson-makers (each Exhaust)
- **Galvanism** — 1 Lesson + 1 Strength. · **Solitude** — 1 Lesson + 1 Dex. · **Wretchedness** — 1 Lesson + 2 Thorns. · **Fire, Stolen** — 1 Lesson + 2 Regen.

### Power / Lesson payoffs
- **Recombinant** — Deal 3 once per Power you have.
- **Quote at Length** — Deal damage = your Lessons.

### Grief
- **Vexing Memory** [Status] — Unplayable. End of turn in hand → gain 1 Grief, take grief dmg = Grief.
- **Wallow** — Gain Block = your Grief.
- **Keening** — Exhaust your hand, gain 1 Grief per card, deal 2×Grief to ALL.
- **Don't Look Away** — Return a random Exhausted card to hand. Take 2 grief dmg.
- **Read the Remainder** — Heal = cards in Exhaust pile.

### The parts (the body you tend)
- **Throbbing Heart** [Retain, Eternal] — A part; can't be removed. On draw → a Vexing Memory. Playable at 2 Grief + 2 Lessons → clear Vexing Memories, reset Grief+Lessons. Survives to end of combat → grows into a new part. Not redeemed in 3 turns → festers. *(⚠ HELD: 2 in starting deck, was 3)*
- **Festering Wound** [Curse] — Unplayable. While in hand, your attacks deal +1. End of turn → take 2 grief dmg. *(⚠ HELD: the scars-as-weapon rework)*

---

## Open threads
- **Prides → retain swords** (Hallie 2026-06-13): Pride==Flag rename; each Pride a retain sword through
  the deck, upgrading incrementally. See `KnifeHeroCode/Cards/FLAGS_AS_WEAPONS_SPEC.md`.
- **Dyke Pride / Labrys** parry — mechanism solved (BufferPower hooks); A-vs-B design Q open.
- **"Flag" terminology** still in card text (Rainbow Strike, Stonewall, Corporate Pride) → rename to **Pride**.
- Two Labryses (pet vs in-hand parry) — keep both or fold?
### ⟳ The Gay Blade engine (Hallie, 2026-06-13 — overnight design, NOT built)
Forge → bank → cash → recycle. The Gay Blade's whole loop:
- **Fancy Footwork → Exhaust, BOTH paths.** Play it (attack → forge Butch) or hold to end of turn
  (block → forge Femme); either way it exhausts. One forge per Footwork — the loop's governor.
- **Re-forging a held blade = +1 retain level.** Each level raises the blade's passive (+1 atk for
  Butch / +1 retaliate for Femme). "Retain level" ≡ re-forge/upgrade count.
- **Play Butch/Femme → transform + cash out.** It becomes a basic Strike (Butch) / Defend (Femme)
  "no matter what" — AND spawns **one more Strike/Defend per retain level**. A 3×-forged Butch played
  = a Strike + 3 more Strikes. (Reads as pure-transform; spawned cards land in discard → recycle.)
- **Relic: Strike/Defend → Fancy Footwork.** The only replenishment, so throughput is gated by how many
  blades you've cashed out. Closes the loop.
- **"Do I put it through the wash?"** every turn: keep forging (bank levels, ride the growing passive,
  value locked in one card) vs cash out (dump the banked pile into your deck). Hold too long → never
  spend; cash early → a trickle. THE decision.

**The archetype (Hallie):** this makes a *deeply viable Strike/Defend deck* — your basics are the medium
you SCULPT, not dead weight to remove. Soak up strikes or defends, rebalance the ratio, convert
offense↔defense over the run — all through this one mechanic. Adaptive: forge Femme→Defends vs a big
boss, Butch→Strikes vs a swarm; re-tune your deck's shape per fight. The "good AND bad": over-convert →
bloat into a low-ceiling mush; mistime the wash → a hand of weak basics. The skill is restraint (stay
lean, cash at the right turn) — a deck that punishes greed with dilution. Levers: (a) the **relic's
conversion rate** is the dial for the whole engine's tempo; (b) keep basics relevant late via the
retain-level spawns + growing passives (maybe a Pride that scales your spawned basics = the
goes-the-distance payoff). Same soul as the Creature — a deck you TEND as a living thing — but
combat-shape, not grief.
