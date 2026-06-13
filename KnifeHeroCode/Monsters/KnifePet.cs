using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace KnifeHero.KnifeHeroCode.Monsters;

/* The knife that stands in front of you — the guardian familiar.
   CustomPetModel = auto-registered, takes no turn of its own.

   IMPORTANT: a custom creature MUST resolve to a real visuals scene. If CustomVisualPath
   is null, the base tries creature_visuals/<id> (which doesn't exist for a mod creature)
   and NREs into the *error scene* (the floating "ERROR" in combat) — there is no silent
   fallback. So we BORROW Osty's rig by pointing at its visuals scene, and match Osty's
   animation names so the borrowed skeleton drives correctly. Swap to a custom familiar
   rig (Spine 4.1) later — that's purely a visual change here. */
public sealed class KnifePet() : CustomPetModel(visibleHp: true)
{
    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;

    // borrow Osty's existing creature visual (the skeletal hand) — no Spine authoring needed
    public override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/osty");

    // map the standard states onto Osty's skeleton's animation names
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, idleName: "idle_loop", deadName: "die",
            hitName: "hurt", attackName: "attack", castName: "cast");
}
