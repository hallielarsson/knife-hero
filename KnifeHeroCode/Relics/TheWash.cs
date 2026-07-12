using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Cards;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace KnifeHero.KnifeHeroCode.Relics;

/* THE WASH — the Gay Blade's signature relic. ⚠ NAME IS A PLACEHOLDER; Hallie's to mint.
   (Taken from her own phrase for the engine: "do I put it through the wash?")

   WHAT IT DOES
     When you play a Strike or a Defend, it becomes a SWITCH BLADE.

   THAT CLOSES THE LOOP — and the loop is the character (Hallie, 2026-07-11):

       Strike / Defend  ──(this relic)──▶  SWITCH BLADE
              ▲                                  │
              │                            (play it: forge)
        (swing it)                               ▼
              │                        TOP CHOP  /  PILLOW PRINCESS
              └──────────────────────────────────┘

   Play a Switch Blade → forge a Top Chop. Hold it to end of turn → forge a Pillow Princess.
   Fly them (they pay rent every turn) or swing them — and swinging cashes them back into a Strike (Top)
   or a Defend (Princess), plus one extra per retain level. Those basics go back through the wash.

   Your basics are not dead weight. They are the MEDIUM YOU SCULPT.

   WHY ONLY THIS ONE TRANSFORM LIVES HERE
   Hallie: "otherwise if we have other cards like this we have to put it ALL in the relic." Right — a
   relic holding every transform becomes a god-object that every new card must be registered in. So each
   card declares its own becoming (PrideCard.Becomes()), and the relic keeps only the job that is
   genuinely its own: the basics must not know that the Gay Blade's relic exists, so *something else* has
   to transform them, and that something is this.

   ⚠ TIMING: AfterCardPlayed, never OnPlay. Transforming a card mid-resolution is the "Rapier stuck
   floating after play" bug. FOOTWORK_SPEC.md called this exact shot and left the recipe:
   "build the relic as a CustomRelicModel that hooks a post-resolution event… never from within a card's
   OnPlay." This is that relic. */
public sealed class TheWash : KnifeHeroRelic
{
    /* Ancient = the character's signature starting relic (not draftable). The whole engine is built on
       it: without the wash, Switch Blades never enter the deck and the loop never turns. */
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Owner == null) return;

        // Only the basics go through the wash. A Switch Blade is not itself a Strike/Defend, so this
        // does not loop on itself.
        bool isBasic = card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend);
        if (!isBasic) return;

        var switchBlade = card.CombatState.CreateCard<FancyFootwork>(card.Owner);
        // See CardTransformExtensions.TransformAndSettle: transforming a just-played card leaves
        // the replacement stranded in the Play pile unless we move it ourselves.
        await choiceContext.TransformAndSettle(card, switchBlade);
    }
}
