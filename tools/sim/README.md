# knife-hero playtest harness

## The verdict, up front

**Path (A) — drive the real engine headlessly — is viable, with a small number of disclosed
shims.** `CombatState`, `CardModel`, `PowerModel`, `DamageCmd`, `PowerCmd`, `Hook`, the whole
model/command layer, is plain C# with no Godot base classes. Godot only shows up in that layer as
lazy rendering properties (`Texture2D Portrait => ResourceLoader.Load<Texture2D>(...)`) that a
combat sim never touches, and a handful of specific native calls that were found empirically (not
guessed) and are individually documented below. `tools/sim/harness/` runs a real `StrikeIronclad`
card through the real `CardModel.OnPlay` → `DamageCmd.Attack` → `Hook` pipeline, in a plain console
process, with no Godot engine, no window, no `.pck`, and it deals exactly the damage it deals in
actual gameplay. Path (B) — reimplementing the rules in Python — was never built. It wasn't needed.

This means the harness's card/power/damage resolution has **zero fidelity drift by construction**:
it isn't a model of `sts2.dll`, it *is* `sts2.dll`, running in-process. What's *not* guaranteed to
be faithful is described exhaustively in "Fidelity" below — read it before trusting a number out
of batch mode.

## What's here

- **`tools/sim/harness/`** — the working harness (interactive + batch), described below. Its own
  code is small (`Engine.cs`, `Fight.cs`, `Program.cs`) and heavily commented at every point where
  it deviates from "just call the real game code" — those comments are the fidelity notes in
  detail; this README is the summary.
- **`tools/sim/spike/`** — the throwaway spike that answered the (A) vs (B) question before the
  harness was built. Kept as the evidence trail (see its `Program.cs`, run top to bottom, for the
  step-by-step proof: TestMode on → 1624 base-game models registered → a real Player → a real
  CombatState → a real StrikeIronclad card dealing exactly 6 damage). Superseded by the harness for
  actual playtesting; not deleted because it's the paper trail for the verdict.
- **`THE_CREATURE/sim/`** — the prior Python prototype (path B). Left alone, not extended. It found
  one real bug (distinct-Power count stalling at ~2, silently breaking `Recombinant`'s payoff) by
  reasoning about the rules in Python rather than running them - a good catch, but exactly the kind
  of result this project's brief was worried about trusting long-term, since nothing checked that
  the Python rules matched `.decompiled/`.

## How to run

```
export DOTNET_ROOT=~/.dotnet; export PATH=~/.dotnet:~/.dotnet/tools:$PATH
# harness targets net9.0 (see "Why net9.0" below) - if only .NET 10 is installed:
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --runtime dotnet --install-dir ~/.dotnet

cd /Users/hallie/Documents/repos/knife-hero
dotnet build                                    # rebuild KnifeHero.dll if you changed mod code
cd tools/sim/harness
dotnet run -- interactive                       # play one fight from the terminal
dotnet run -- batch 500                         # simulate 500 fights, print aggregate stats
```

Interactive mode: shows hand/energy/HP/block/enemy intent each turn, type a hand index to play a
card (untargeted cards auto-resolve, targeted cards go at the single enemy - multi-enemy fights and
target selection aren't wired into the CLI yet, see "Known gaps"), blank line to end turn.

Batch mode: runs N fights with a **greedy policy** (play the first affordable card in hand order,
repeat until nothing affordable, end turn) and reports win rate, turn-count distribution, HP
remaining, and the max number of *distinct* Powers seen on the player's side in a fight (the metric
that would have caught the `Recombinant` bug the Python prototype found - see "What this is for").
Cards/enemies print as raw `ModelId`s (`CARD.GAY_BLADE_STRIKE`) rather than localized titles - see
`Program.cs` for why.

The default matchup is **GayBlade vs. Axebot** (a real early-game monster with ~70-86 HP and a real
attack move, chosen deliberately over a 0-HP-damage training dummy so both multi-turn play *and*
deck reshuffling actually get exercised - see "What broke" below for why that distinction mattered
while building this).

## What this is for (and what it isn't)

This is the instrument, not a balance verdict. It exists to answer "does a change survive contact
with the real engine," not "is the number good." If a batch run surfaces something that looks like
a balance problem (an underperforming card, a power that never scales, a matchup that's a bye), the
harness's job is to *surface* it clearly enough to report back to Hallie/Johnicholas - not to tune
anything. Nothing here rebalances cards, changes design docs, or edits `KnifeHeroCode/`.

## Fidelity — what it models, what it doesn't, where it could lie to you

**Modeled with zero drift** (this is the actual real game code, unmodified, running in-process):
card `OnPlay` bodies, `DamageCmd`/`PowerCmd`/`CreatureCmd` resolution, the `Hook` system and its
ordering (before/after-attack, block-clear, energy-reset, hand-draw, turn-end, etc.), deck
shuffling and reshuffling (RNG order, `Hook.ModifyShuffleOrder`, real `Rng` - see the one exception
below), monster AI/move selection (`MonsterModel.RollMove`/`PerformMove`, real `Creature.TakeTurn`),
energy costs including cost modifiers (`CardEnergyCost.GetAmountToSpend`), retain/discard rules.

**Reimplemented, not modeled** (the one real exception): `CardPileCmd.Shuffle`'s per-card
animation-pacing wait calls `((SceneTree)Godot.Engine.GetMainLoop()).Root.GetProcessDeltaTime()` to
decide whether to pause between cards for the "cards flying into place" effect. With no Godot
engine running that's a native call into nothing, which segfaults the process outright (not a
catchable exception) - the first real "Godot leaking into the constructor path" landmine the
research brief predicted, just one level removed from where it was expected. `Engine.cs`'s
`ApplyHeadlessShuffleShim` replaces the whole method with a line-for-line copy of the decompiled
original *minus that wait*: same `StableShuffle` call, same real `Hook.ModifyShuffleOrder` call,
same `DebugForcedTopCardOnNextShuffle` handling, same real `Hook.AfterShuffle` call, in the same
order. Shuffle *order* and RNG consumption are unaffected; only the fictional pause between
individual card-adds during a reshuffle is skipped. If `sts2.dll`'s real `Shuffle` changes, this
copy goes stale silently - `Engine.cs` says so at the call site.

**Sequenced by hand, not by `CombatManager`**: `Fight.cs` does not call
`CombatManager.StartTurn`/`EndPlayerTurnPhaseOneInternal`/`ExecuteEnemyTurn`. Those methods are
entangled with presentation (`NCombatRoom` banners, `NRunMusicController`, FTUE popups) and, in
`StartCombatInternal`'s case, dereference `_state.Encounter.HasBgm` with no null-guard - we never
build a real `EncounterModel`, so that path NREs outright. Instead `Fight.cs` calls the same
`Hook.*`/`CardPileCmd.*`/`PlayerCombatState` entry points those methods call, in the same order
(read directly out of `CombatManager.cs` in `.decompiled/`, cited in `Fight.cs`'s header comment).
**Not reproduced**: the `AutoPrePlay`/`AutoPostPlay` hook phases (auto-played cards from effects
like Necronomicon/Mayhem-style relics or powers), and multiplayer turn-readiness bookkeeping. A
card or power that depends on those phases will not be exercised faithfully by this harness yet.

**RNG determinism (caught, then fixed, worth knowing about)**: the harness uses `NullRunState` for
everything (there is no real run/map/act - just a fight), and `NullRunState.Rng` always constructs
`new RunRngSet(string.Empty)` - the *same* seed, every access. First batch run of 500 fights came
back with **identical outcomes in all 500** - same win, same turn count, same HP remaining - because
every fight's shuffle order, from the first card to the last reshuffle, was bit-for-bit identical.
That is exactly the "confident wrong sim" failure mode this whole project exists to avoid, just
surfacing as an infrastructure bug instead of a rules bug. Fixed via `Engine.SetFightSeed`, a
narrow Harmony patch on `NullRunState.Rng`'s getter that reads a seed we set per fight, rather than
by touching any RNG-*consuming* gameplay code. After the fix, 500 fights against Axebot show real
turn-count variance (4-7 turns) instead of a flat line. **If you add a new entry point to the
harness and outcomes look suspiciously identical across runs, this is the first thing to check.**

**Not modeled at all**: localization (`CardModel.Title` throws - no `.pck`/localization JSON is
loaded, so the CLI prints raw `ModelId`s), any visual/audio/animation timing beyond the shuffle
shim above, save files beyond what `SaveManager.Instance`'s default construction does on first
touch (works, but nothing is meaningfully persisted or read back), and multiplayer/replay
(`ModelDb.InitIds()`/`ModelIdSerializationCache.Init()` are skipped entirely - see `Engine.cs` for
why - so `AbstractModel.CategorySortingId`/`EntrySortingId` are left at 0; nothing observed so far
depends on them, but if a reward-card-ordering or similar sort-dependent feature gets tested here,
that's the place to come back to).

**Two other real engine bugs found and worked around, both disclosed in `Engine.cs`**:
`Player.CreateForNewRun`'s starting-deck cards come back with `Owner == null` (its own doc comment
warns "will not work properly until the player is added to a RunState" - we hit the
`NullReferenceException` this causes during the very first deck shuffle, traced it back to
`CardModel.Owner`, and now set it by hand, same as a real `RunState.AddPlayer` would); and the real
`CombatManager.Reset()` unconditionally touches `RunManager.Instance.ActionQueueSynchronizer`
(multiplayer plumbing never initialized here) and NREs, so batch mode resets only the one field
(`CombatManager._state`) that `SetUpCombat`'s reentry guard actually checks, via reflection, instead
of calling the real method.

## Known gaps / good next steps (not done here — flagging, not fixing)

- **Multi-enemy targeting in the interactive CLI.** `Fight.PlayCard` supports targeting any enemy
  by index; `Program.cs`'s interactive loop always targets enemy 0. Fine for the current 1v1 demo
  matchup, not fine for a multi-enemy encounter.
- **The Creature works and reproduces the Python prototype's finding, through the real engine.**
  `TheCreature` (`KnifeHero.KnifeHeroCode.CreatureHero.TheCreature`) is a real registered
  `CharacterModel`, same as GayBlade - `Engine.CreateReadyPlayer<T>()` is generic and needed no
  changes to run it. Swapping the default matchup to The Creature vs. Axebot and running 100 fights:
  **`Max distinct allied Powers seen in a fight: avg 1.74, max 2`** - the distinct-Power count caps
  at exactly 2, never reaching wherever `Recombinant` would actually pay off. That's the same shape
  of result `THE_CREATURE/sim/sts_sim.py` found, now independently reproduced by the real
  `PowerModel`/`Hook` pipeline instead of a Python model of it. This isn't a rebalance and none was
  made - it's the harness doing the one thing it was built to do. Worth a real look before trusting
  `Recombinant`'s current design as tuned. The matchup was reverted back to GayBlade vs. Axebot as
  the shipped default (`Program.cs`); swap the `KnifeHero` type parameter in both
  `Engine.CreateReadyPlayer<...>()` calls to `global::KnifeHero.KnifeHeroCode.CreatureHero.TheCreature`
  to reproduce this.
- **Localization.** Loading `KnifeHero/localization/eng/*.json` (and the base game's own tables)
  into whatever backs `LocString` would make output read as card names instead of `ModelId`s.
  Didn't chase this down; not required for correctness, just readability.
- **AutoPrePlay/AutoPostPlay hook phases**, if a card ever needs them (see Fidelity above).

## Why net9.0, not net10.0

The spike was first built against `net10.0` (the only SDK installed) and it worked - right up until
Harmony (`0Harmony.dll`, the exact copy shipped inside the game, used for the log/shuffle/RNG shims
above) refused to patch anything: `PlatformNotSupportedException: CoreCLR version 10.0.9 is not
supported`. That Harmony build's version-compat check doesn't know about .NET 10. Installed the
.NET 9 runtime side-by-side (`dotnet-install.sh --channel 9.0 --runtime dotnet`, doesn't touch
anything else) and retargeted both projects to `net9.0`, matching what the mod itself and BaseLib's
NuGet package already build against.
