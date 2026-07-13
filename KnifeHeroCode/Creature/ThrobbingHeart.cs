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

/* Throbbing Heart — the heart of The Creature: a PART that starts as a curse. Eternal + Retain, so it
   sits in your hand demanding attention. When drawn it spits up an intrusive Vexing Memory. You can
   only PROCESS it once you've both grieved and learned enough (2 Grief + 2 Lessons) — emotional
   response AND rational integration; neither alone will do. If you DON'T redeem it within 3 turns, it
   festers into a Festering Wound. Redeem your parts or carry the rot.

   DECIDED (Fable, 2026-07-11, Creature design owner — Hallie's playtest: "redeeming the heart feels
   viscerally GOOD but muddy"). The mend was TWO-STAGE: playing it only set a hidden `_redeemed` flag,
   and the actual reward (Mended Heart + max HP) fired silently at AfterCombatVictory. So the player
   did the hard thing, felt the vexes clear — and then watched a curse stay in their hand with no
   feedback. The payoff happened off-screen. Collapsed to ONE stage: playing it mends it, in your hand,
   immediately. The curse becomes a weapon while you're holding it. Side-effect, and a good one: mending
   EARLY now gets you a Mended Heart you can actually swing this fight, so the 3-turn clock has a second
   gradient — fast metabolization is rewarded, not merely un-punished. */
public sealed class ThrobbingHeart() : PartCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "throbbing_heart.png".CardImagePath();
    public override string CustomPortraitPath => "throbbing_heart.png".BigCardImagePath();

    /* The first organ, and the cheapest to understand — it's the one you were born holding.
       (Gray505: the heart EXCISED. Vagus nerves severed, thoracic aorta cut clean. Printed on the 1818
       title page, with Milton's "Did I request thee, Maker, from my clay" landing across it.) */
    protected override int LessonsToMend => 2;

    protected override CardModel Mended() => CombatState.CreateCard<MendedHeart>(Owner);
}
