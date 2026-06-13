using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE KEYSTONE — Knife in Front. Summon a knife that stands in front of you with its own HP;
   while it lives, enemy attacks meant for you hit the knife instead (DieForYouPower, the same
   redirect Osty uses). v1: a single 8-HP knife (Osty-style singleton-ish). Numbers are
   placeholders for Hallie to tune; "many knives you control" is a later design choice.
   Art falls back to the placeholder via KnifeHeroCard; the knife's body uses the placeholder
   creature visual until a rig is borrowed/drawn. */
public sealed class KnifeInFront() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const int KnifeHp = 8;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var knife = await PlayerCmd.AddPet<KnifePet>(Owner);
        await CreatureCmd.SetMaxHp(knife, KnifeHp);
        await CreatureCmd.Heal(knife, KnifeHp, false);
        await PowerCmd.Apply<DieForYouPower>(knife, 1m, null, this, true);
    }
}
