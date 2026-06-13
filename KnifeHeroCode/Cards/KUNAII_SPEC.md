# Kunaii — spec (ready to build, not yet built)

Hallie's design (verbatim):
> Kunaii — Summon a Kunaii with 5 hp. Kunaii does 3 damage. At the start of your turn,
> Kunaii dies and you get a Kunaii in your hand. Exhaust when played.

A disposable, self-returning attacker. Throw it down, it stabs for 3, then at your next turn
it sacrifices itself back into your hand so you can throw it again. The card itself Exhausts on
play, so the *only* way to keep the loop alive is the body dying and handing the card back.

## Why it's not built with the others
`KnifePet` is a `CustomPetModel`, and `CustomPetModel` hard-codes a `NOTHING_MOVE` (pets take
no turn — they just stand there, like the Knife-in-Front guardian). Kunaii has to *attack*, so it
can't be a pet; it must be a `CustomMonsterModel` (an ally creature) with a real attack move.
That's a genuinely different creature type from the guardian — worth building deliberately, not
guessing inline.

## Build plan (grounded in decompile)
1. **`KnifeHeroCode/Monsters/KunaiiPet.cs`** — `CustomMonsterModel`, 5 HP.
   - Borrow a visual rig like `KnifePet` does (`creature_visuals/osty` + matching anim names)
     until a real Kunaii rig exists.
   - Give it ONE attack move dealing 3, built with BaseLib's `MoveBuilder`
     (`.decompiled-baselib/Baselib/Monsters/MoveBuilder.cs`, `AbstractIntent` attack intent).
     Reference the base game's `Osty` (`.decompiled/.../Models/Monsters/Osty.cs`) — it's the
     canonical "summoned thing that hits the enemy each turn."
2. **`KnifeHeroCode/Powers/KunaiiCyclePower.cs`** — a hidden power applied to the Kunaii on summon.
   - Hook `BeforeHandDraw` (the same hook `Bolas` uses to re-add itself to hand) OR an
     at-start-of-turn hook: kill the Kunaii creature, then
     `CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<Kunaii>(Owner), PileType.Hand, addedByPlayer: true)`.
   - Open question for Hallie: does it die at the start of *your* turn (cycles every turn) or
     only persist one round? Spec assumes start-of-your-turn = one attack then back to hand.
3. **`KnifeHeroCode/Cards/Kunaii.cs`** — Skill (or Attack), Self-target, `CardKeyword.Exhaust`.
   - `OnPlay`: `var k = await PlayerCmd.AddPet<KunaiiPet>(Owner);` (AddPet works for any
     CustomMonsterModel, not just pets), `SetMaxHp(k, 5)`, `Heal(k, 5)`, apply `KunaiiCyclePower`.
   - Card exhausts via `CanonicalKeywords => { CardKeyword.Exhaust }`.
4. **Localization**: `KNIFEHERO-KUNAII.title` / `.description` in cards.json; monster name in
   monsters.json (`KNIFEHERO-KUNAII_PET.name`).

## Naming watch
Hallie has TWO things spelled close: `Kunai` (existing 0-cost throw-and-exhaust card, used as the
"shiv" Knife Whip drops) and `Kunaii` (this summon). Keep them distinct — don't let one overwrite
the other's loc keys.

## Art (human-sourced)
There's at least one drawing of a Kunaii on one of Hallie's hand-drawn Sentinels-of-the-Multiverse
cards — cut/clean that as the Kunaii card art (and possibly the creature body) when building.
Drawing is Hallie's; this is for attribution, per the human-sourced art principle.

## Tuning knobs (Hallie's call)
Cost, whether 5 HP / 3 damage are right, attack vs skill type, every-turn vs one-shot cycle.
