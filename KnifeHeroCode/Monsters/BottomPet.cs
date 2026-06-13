using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Monsters;

/* Bottom — a Flag Pet. A sword flying the Bottom pride flag that stands in front of you. Fancy
   Footwork held to the end of your turn (used to defend) summons or feeds it. While Bottom is in
   play it grants the Dexterity it was fed; if it dies, that Dexterity leaves with it. Borrows Osty's
   rig as placeholder — real art is a pride-flag sword (human-sourced). */
public sealed class BottomPet() : CustomPetModel(visibleHp: true)
{
    public int GrantedDexterity;

    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;

    public override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/osty");

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, idleName: "idle_loop", deadName: "die",
            hitName: "hurt", attackName: "attack", castName: "cast");

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature.Player != null && GrantedDexterity != 0)
            await PowerCmd.Apply<DexterityPower>(creature.Player.Creature, (decimal)(-GrantedDexterity), null, null);
    }
}
