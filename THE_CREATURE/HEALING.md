# The Creature — the healing payoff (what a processed part grows into)

The open keystone. Vengeance is the easy route (grief → damage, ungated, strong through Acts 1–2).
For the hard *healing* route to be worth choosing, processing a Throbbing Heart must grow you on an
axis vengeance can't touch. Here's that axis.

## The answer: permanence vs. loudness
- **Vengeance scales with GRIEF** — in-combat, resets every fight, immediate and easy. But Grief is
  capped by the combat: you can only pile so much per battle, and it's gone next fight. It plateaus.
- **Healing scales with WHOLENESS** — a run-long, permanent, compounding stat you can only raise by
  doing the whole hard loop (grieve + learn + redeem + survive). Slow — maybe one part mended every
  couple of combats — but it never resets and never caps.

So: **vengeance is loud and immediate but perishes; healing is quiet and slow but it STAYS and grows.**
By Act 3, a Creature that did the hard work all run out-scales the vengeful one — *only* if it paid
the cost the whole way. That's the orthogonal payoff, and it matches the north star (vengeance comfy
through Acts 1–2; healing the deliberate long road that wins late).

## Mechanics

### A processed part grows into a Mended Heart (a boon, not a curse)
When a redeemed Throbbing Heart survives to end of combat, it transforms into a **Mended Heart** —
the part made whole. Stable: no Vexing Memories, no festering, no Eternal-curse weight. It's a good,
reliable card (e.g. a cheap attack/skill that also nudges Wholeness, or just a solid Strike-equivalent
that's *yours*). The body, healed in one place.

### Wholeness — the healing axis (the real payoff)
Each part you mend grants **+1 Wholeness**, a persistent run-long counter (a power that's re-applied
each combat from a run-level count, or a relic-style tracker). Wholeness compounds:
- **+2 max HP per Wholeness** (you literally become more durable as you heal — and max HP is the
  resource the *body* deck cares about most).
- **Your healing is amplified** — Read the Remainder / Ironclad-style heals do +1 per Wholeness.
- (Stretch) at high Wholeness, your **Mended Hearts upgrade** — the whole parts get better the more
  whole you are. Healing begets healing.

None of this is loud. It won't win you Act 1. But it's the only thing on the table that *carries
across combats and never stops growing* — which is exactly what vengeance, resetting every fight,
can never be.

## Why this is the right shape (and what it is NOT)
- It's **internal** — becoming whole, by your own slow work. That's distinct from Hallie's own
  (separate) character idea — redemption / forgiveness-from-others / reconciliation — which is about
  the *community's* response to you, a currency others hold. Don't conflate them: the Creature heals
  *itself*; the redemption hero is *let back in*. Different axes, kept apart on purpose.
- It keeps the tragedy intact: the easy route is always right there, louder and faster. Choosing the
  slow whole road is a *choice*, and it should feel like one — costly early, vindicated late.

## Build plan (gated; sim first, per the loop)
1. Sim the two routes across a run (run_sim): a "vengeance" line (dump grief, never mend) vs a
   "healing" line (mend ~1 part / 2 combats, build Wholeness). Confirm vengeance leads early and
   healing overtakes by ~Act 3. Find the Wholeness numbers that make that crossover real.
2. Only then, in mod: ThrobbingHeart's end-of-combat upgrade → MendedHeart; add a Wholeness power
   (run-persisted) + a couple Mended cards. Until then this is design, not code.
