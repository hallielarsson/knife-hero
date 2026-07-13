using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.Patches;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   QUEER GETS A GLOSS — the side panel, like every other keyword.
   (Hallie, 2026-07-13: "Is there a way to get a gloss on the Queer in the side like other keywords?")

   There is, and it's cheap. The side panel is just `CardModel.HoverTips` (CardModel.cs:952) — a public
   getter that assembles tips from the card's keywords, enchantment, affliction, and a `protected virtual
   ExtraHoverTips`.

   ── WHY NOT JUST OVERRIDE ExtraHoverTips ───────────────────────────────────────────────────────
   Because it lives on the CARD, and Queer lives on a MODIFIER — and the whole point of the modifier is
   that it can ride cards we do not own. `EverythingIMakeIsQueer` queers *the first Attack you create each
   turn*, and that Attack can come from anywhere: a colorless card, a relic, a potion, a monster's gift.
   Overriding ExtraHoverTips on KnifeHeroCard would gloss our Strikes and miss exactly the cards where a
   player most needs telling what just happened to them.

   So we postfix the getter itself. Every card in the game, ours or not, gets the tip iff it is queer.
   **The gloss follows the modifier, because the modifier is the thing that travels.**

   ── AND IT CANNOT THROW ────────────────────────────────────────────────────────────────────────
   This getter runs for every card the player hovers, everywhere — combat, shop, compendium, card reward,
   the deck screen. An exception here would take out tooltips for the entire game, including the base
   characters, and it would look like the game's bug rather than ours. So the whole body is wrapped: if
   anything goes wrong we log it and hand back the untouched list. A cosmetic feature is never worth a
   crash in someone else's character.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */
[HarmonyPatch(typeof(CardModel))]
public static class QueerHoverTips
{
    [HarmonyPostfix]
    [HarmonyPatch("HoverTips", MethodType.Getter)]
    public static void AppendQueerTips(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        try
        {
            var tips = QueerMod.TipsFor(__instance);
            if (tips.Count == 0) return;
            __result = __result.Concat(tips);
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error($"QueerHoverTips failed on {__instance.Id}: {e}");
        }
    }
}
