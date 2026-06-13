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

/* Top — a Flag Pet. A sword flying the Top pride flag that stands in front of you (DieForYou redirect,
   so it eats enemy attacks). Fancy Footwork played as an ATTACK summons or feeds it. While Top is in
   play it grants the Strength it was fed; if it dies absorbing hits, that Strength leaves with it
   ("Top increases your damage by one while it's in play"). Borrows Osty's rig as placeholder —
   real art is a pride-flag sword (human-sourced). */
public sealed class TopPet() : CustomPetModel(visibleHp: true)
{
    public int GrantedStrength;

    public override int MinInitialHp => 1;
    public override int MaxInitialHp => 1;

    public override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/osty");

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) =>
        SetupAnimationState(controller, idleName: "idle_loop", deadName: "die",
            hitName: "hurt", attackName: "attack", castName: "cast");

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature.Player != null && GrantedStrength != 0)
            await PowerCmd.Apply<StrengthPower>(creature.Player.Creature, (decimal)(-GrantedStrength), null, null);
    }
}
