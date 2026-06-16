using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* QueerRider — the seed of the queer-rider pool (QUEER_ENGINE_SPEC.md).

   When the normative is cast out (a Strike/Defend exhausted), the Queer curse returns it to the
   deck as ITSELF + this rider. Chassis + rider — the Tinker model. The card keeps its identity
   (a Strike is still a Strike); the rider is the "out" part bolted on. Divergence by what's added,
   not replacement: that's how the normative comes back queer without becoming a different card.

   v1 rider = the relocated **Poison Coating**: when the queered card is played at an enemy, it
   also lays Poison. This is the spec's whole point made concrete — coatings were never auras, they
   are riders assembled onto a card. The shiv engine's coatings move here.

   // PROPOSAL: this is ONE rider. The real engine picks from a POOL (random, or chosen Tinker-style)
   so each queering diverges by source — that divergence IS the diversity. Add riders here as the
   pool grows (AoE/Explosive, Weak+Vulnerable, Draw, ...), and give Skills their own rider set so a
   queered Defend gets more than the tag. All numbers // PROPOSAL — tune by felt playtest. */
public sealed class QueerRiderMod : CardModifier
{
    // PROPOSAL: 2 Poison per play. Felt-first; tune.
    private const int PoisonAmount = 2;

    public override void ModifyDescription(Creature? target, ref string description)
    {
        description += " Queer.";
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature? enemy = cardPlay.Target;
        if (enemy == null) return;

        Creature? me = Owner?.Owner?.Creature;       // Owner = the card; Owner.Owner = the Player
        if (me != null && enemy == me) return;        // a queered Defend targets self — no poison (v1)

        await PowerCmd.Apply<PoisonPower>(enemy, PoisonAmount, me, null, false);
    }
}
