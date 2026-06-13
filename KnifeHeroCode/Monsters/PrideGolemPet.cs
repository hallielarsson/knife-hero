using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace KnifeHero.KnifeHeroCode.Monsters;

/* The Pride Golem — a big retaliating body forged from your sacrificed Flags and pets. Its HP is set
   dynamically on summon (2X), so it must use post-spawn SetMaxHp (see KNIFE_HPBAR_SPEC.md — the same
   borrowed-Osty-bounds caveat applies; the HP bar fix is parked). Borrows Osty's rig like KnifePet;
   swap to a custom golem rig later. The retaliation lives in PrideGolemThorns, applied on creation. */
public sealed class PrideGolemPet() : CustomPetModel(visibleHp: true)
{
    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;

    public override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/osty");

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, idleName: "idle_loop", deadName: "die",
            hitName: "hurt", attackName: "attack", castName: "cast");
}
