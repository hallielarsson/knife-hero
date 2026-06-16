using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Shiv Modifier cards — the playable face of the shiv modifier engine
   (SHIV_MODIFIER_ENGINE_SPEC.md). Hallie: do the proposal, reap in playtest.
   Each is a cheap skill that lights up your shivs for the turn. The buff Powers do the work
   (ShivModifiers.cs). Numbers are // PROPOSAL — set to be felt, then tuned. */

/* Poison Coating — this turn, your shivs lay Poison.
   // PROPOSAL: 1 cost, 3 Poison per shiv. Hallie tunes by feel. */
public sealed class PoisonCoating() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PoisonCoatingPower>(Owner.Creature, 3m, Owner.Creature, this, false);
    }
}

/* Explosive Tip — this turn, your shivs hit all enemies and Exhaust.
   // PROPOSAL: 1 cost. Hallie tunes by feel (maybe 0 cost, maybe Exhaust the card itself). */
public sealed class ExplosiveTip() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ExplosiveTipPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
