using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace KnifeHero.KnifeHeroCode.CreatureHero.Cards;

/* Throbbing Heart — the Part you start the run holding. Mend for 2 Lessons → Mended Heart; let it rot
   → Festering Wound. All the machinery is in PartCard. */
public sealed class ThrobbingHeart() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "throbbing_heart.png".CardImagePath();
    public override string CustomPortraitPath => "throbbing_heart.png".BigCardImagePath();

    protected override int LessonsToMend => 2;

    protected override CardModel Mended() => CombatState.CreateCard<MendedHeart>(Owner);
}
