using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* The Closet — Hallie's design: "Gain Stealth 3. If you play an attack, lose Stealth."
   A big burst of Stealth (3 stored turns of Intangible) that holds only as long as you stay
   hidden — swing once and the whole stance collapses (the Closeted watcher strips it). High
   defense, zero offense: the tension is the card. Human-sourced mechanic (Hallie); placeholder
   art via KnifeHeroCard. */
public sealed class TheCloset() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<Stealth>(Owner.Creature, 3m, Owner.Creature, this, false);
        await PowerCmd.Apply<Closeted>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
