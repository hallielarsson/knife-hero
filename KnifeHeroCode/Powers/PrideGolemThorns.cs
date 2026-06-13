using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Pride Golem's retaliation — whenever the golem takes HP damage, it deals that much straight back
   to the attacker. Built on the engine's ReflectPower pattern (AfterDamageReceived) but reflecting
   the FULL UnblockedDamage (the damage that actually landed on the golem's HP), not just blocked
   damage. Persistent (no decrement) — it's the golem's nature for as long as it lives. */
public sealed class PrideGolemThorns : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer == Owner) return;   // only hits on the golem, no self-loop
        if (result.UnblockedDamage <= 0) return;
        await CreatureCmd.Damage(choiceContext, dealer, result.UnblockedDamage, ValueProp.Unpowered, Owner, null);
    }
}
