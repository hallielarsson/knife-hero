# Card upgrade audit — KnifeHero pool (non-Creature)

*Started 2026-06-15 by the Pathetic Governor, answering Hallie's play-session beat
"none of the cards upgrade." Stat cards got canonical `OnUpgrade` overrides (the
`DynamicVars.UpgradeValueBy` pattern from Fancy Footwork). Numbers are `// PROPOSAL`s —
Hallie mints the real values.*

## Done — now upgrade (numbers are proposals)

- **Fancy Footwork** — already had it (+3 dmg / +2 block).
- **Butch Blade**, **Femme Flechette** — already had it (the flag-blades).
- **Throwing Knife** — +3 damage (6 → 9).
- **Stonewall** — +5 Block (10 → 15), +1 per-flag damage (3 → 4).
- **Superfan of Knives** — +3 AoE damage (4 → 7).
- **Rainbow Strike** — per-flag const became a field; +1 per-flag on upgrade (2 → 3).
- **Pride was a Riot** — +3 damage (5 → 8).

## Deliberately NOT auto-upgraded — these are DESIGN calls for Hallie

These cards have no simple stat var; an upgrade is a mechanic decision, not a number bump.
Left untouched so the governor doesn't invent design:

- **The Closet** — upgrade could grant more Stealth, or a free first-turn Buffer charge (ties to
  the whetstone decision — see WHETSTONE-the-closet.md).
- **Vanish** — more Stealth on upgrade? (Stealth 2 → 3.)
- **Kunai / Knife Whip** — have uncommitted WIP from another session; left for that author.
- **Knife in Front** — knife HP / Die-For-You amount could scale.
- **Corporate Sponsored Pride, Extremely Online, Portal to the Knife Dimension, The Discourse,
  Pride Golem** — power/utility cards; upgrade = stronger effect or lower cost, Hallie's call.

## Pattern, for the next teller

```csharp
protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
// or, for multi-stat:
protected override void OnUpgrade()
{
    DynamicVars.Damage.UpgradeValueBy(3m);
    DynamicVars.Block.UpgradeValueBy(2m);
}
```

Cards with no `DynamicVar` (e.g. a raw `Apply<Power>`) need either a new var or a hand-rolled
upgraded value before `OnUpgrade` has anything to bite.
