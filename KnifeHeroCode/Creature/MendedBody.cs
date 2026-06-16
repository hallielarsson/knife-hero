using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace KnifeHero.KnifeHeroCode.CreatureHero;

/* Mended Body — the Creature's starting relic and the VISIBLE, run-persistent Wholeness counter
   (THE_CREATURE/HEALING.md / GOVERNOR_HANDOFF.md open item, now built — DECIDED: bro, design owner
   of The Creature, 2026-06-15).

   The problem: Wholeness is a combat-scoped power, so the counter reset every fight even though the
   +2-max-HP-per-mend already persisted (it lives on the Creature). The mended parts themselves DO
   persist — every Mended Heart is a permanent card in your run deck, the durable record of a part you
   made whole. So Wholeness need not be stored separately: it can be RE-DERIVED each combat by counting
   your Mended Hearts. That's exactly what this relic does, at BeforeCombatStart.

   Why a relic, not a stored field: relics persist across combats with zero serialization guesswork,
   and a relic gives the player a visible counter (ShowCounter + DisplayAmount) — the "visible Wholeness
   counter persistence across combats" the handoff asked for. No new art is authored: it reuses the
   mod's placeholder relic.png (the same bootstrap pattern the character art uses; reading existing art
   is fine, the Art Mapper owns authoring new art).

   Numbers (1 Wholeness per Mended Heart) follow the mend in ThrobbingHeart.AfterCombatVictory; Hallie
   mints final tuning. */
[Pool(typeof(TheCreatureRelicPool))]
public sealed class MendedBody : CustomRelicModel
{
    // Reuse the mod's placeholder relic art (not authoring new art).
    public override string PackedIconPath => "relic.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "relic_outline.png".RelicImagePath();
    protected override string BigIconPath => "relic.png".BigRelicImagePath();

    // Starter: it's the Creature's signature relic, granted at run start, never offered as a reward.
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override bool ShowCounter => true;

    // The counter shows how whole you are = how many parts you've mended this run.
    public override int DisplayAmount => MendedHeartCount();

    // At the start of every combat, re-derive Wholeness from your mended parts. The power is
    // combat-scoped, so re-applying it fresh each fight is the correct shape; the count is permanent.
    public override async Task BeforeCombatStart()
    {
        int whole = MendedHeartCount();
        if (whole <= 0) return;
        Flash();
        await PowerCmd.Apply<Wholeness>(Owner.Creature, whole, Owner.Creature, null, false);
    }

    // Mended Hearts live permanently in the run deck — count them across all piles (combat or map).
    private int MendedHeartCount() =>
        CardPile.GetCards(Owner, PileType.Deck, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust)
            .OfType<Cards.MendedHeart>()
            .Count();
}
