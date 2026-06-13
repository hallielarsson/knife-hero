using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Pride flag cards — play one to raise a character-themed Pride (a Flag that counts for Stonewall /
   Rainbow Strike). Hallie's design; a growing set, one per StS character. */

/* Silent Pride — at the start of each turn, put a Shiv in your discard. */
public sealed class SilentPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SilentPridePower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* Ironclad Pride — heal 5 at the end of combat while it's out. */
public sealed class IroncladPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IroncladPridePower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* Necrobinder Pride — summon the real Osty. Osty IS the Necrobinder pride flag (a sword); since all
   pets are Flags for the Gay Blade, the summoned Osty counts as one. */
public sealed class NecrobinderPride() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await OstyCmd.Summon(choiceContext, Owner, 1m, this);
    }
}

/* Regent Pride — sacrifice another Pride sword (a pet), then each turn deal 6 damage and gain 6 Block. */
public sealed class RegentPride() : KnifeHeroCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pet = CombatState.Creatures.FirstOrDefault(c => c.IsPet && c.Side == Owner.Creature.Side);
        if (pet != null)
            await CreatureCmd.Kill(pet, force: true);
        await PowerCmd.Apply<RegentPridePower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* Watcher Pride — at the start of each turn, add Retain to a random card in your hand. */
public sealed class WatcherPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WatcherPridePower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* Defect Pride — your Powers cost 1 less, and you draw a card when you play a Power. */
public sealed class DefectPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DefectPridePower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
