using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.Extensions;

public static class CardTransformExtensions
{
    /* THE STUCK-IN-PLAY LANDMINE (found 2026-07-11, verified against the real engine through
       tools/sim/harness — this is not a harness artifact, every API used below is the same
       CardPileCmd/CardCmd surface the real game uses).

       `CardCmd.Transform(original, replacement)` (.decompiled/.../Commands/CardCmd.cs) inserts the
       replacement into whatever pile the ORIGINAL was sitting in at the moment of the call. The
       Gay Blade's whole transform engine (The Wash, PrideCard.Becomes()) fires from
       AfterCardPlayed, which — per FOOTWORK_SPEC.md and the Rapier "stuck floating" bug — is the
       ONLY safe place to transform a just-played card. But `CardModel.OnPlayWrapper`
       (.decompiled/.../Models/CardModel.cs) calls `Hook.AfterCardPlayed` *before* it moves the
       played card out of the Play pile into its result pile. So at the moment we transform, the
       original — and therefore the replacement — is still in Play.

       Nothing downstream ever sweeps the Play pile again: `PlayCardAction.ExecuteAction`
       (.decompiled/.../GameActions/PlayCardAction.cs) calls `OnPlayWrapper` exactly once and never
       revisits the card, and `CombatManager.DoTurnEnd` (.decompiled/.../Combat/CombatManager.cs)
       only ever iterates `PileType.Hand` at end of turn, never Play. So a card transformed here,
       left alone, is permanently orphaned: not in Hand, Draw, Discard, or Exhaust. It can never be
       drawn again — it just silently vanishes from the deck. This is a genuine sts2.dll landmine
       that Hallie's design (any relic/card that transforms the just-played card) triggers; it isn't
       specific to us, it's just untested territory, since no base-game relic does this from
       AfterCardPlayed.

       Caught for real: the harness's DECK census (which only scans Hand/Draw/Discard/Exhaust, same
       as a player would ever see) came up exactly one card short after The Wash fired on a played
       Strike — the new Switch Blade was sitting in the Play pile the whole time.

       FIX: call this instead of `CardCmd.Transform` directly wherever a *just-played* card is being
       transformed. After the transform, if the replacement landed in Play, move it to its own
       natural resting pile ourselves — Discard, or Exhaust if the replacement itself carries the
       Exhaust keyword, matching the same resultPileType logic `OnPlayWrapper` would have applied to
       it. Power-type replacements are removed from Play and added nowhere, same as a played Power
       card. This is exactly what FOOTWORK_SPEC.md assumed already happened ("reacts after the card
       resolves, in the discard pile") — it doesn't, on its own, so this makes it true.

       The counterintuitive half: it's specifically the REPLACEMENT that's orphaned, not the
       original. `CardCmd.Transform`'s last step calls `original.RemoveFromState()`
       (.decompiled/.../Commands/CardCmd.cs, ~line 506), which sets `HasBeenRemovedFromState = true`
       on the original — so `OnPlayWrapper`'s post-hook cleanup sees a removed card and correctly
       skips it, no crash. It's the *new* card, inserted into the original's old Play-pile slot,
       that nothing ever looks at again. */
    public static async Task<CardModel?> TransformAndSettle(
        this PlayerChoiceContext choiceContext, CardModel original, CardModel replacement)
    {
        CardPileAddResult? result = await CardCmd.Transform(original, replacement);
        CardModel? settled = result?.cardAdded;
        if (settled == null || settled.Pile?.Type != PileType.Play)
            return settled;

        if (settled.Type == CardType.Power)
        {
            // Matches a played Power card: removed from Play, added nowhere.
            settled.RemoveFromCurrentPile();
        }
        else if (settled.ExhaustOnNextPlay || settled.Keywords.Contains(CardKeyword.Exhaust))
        {
            await CardCmd.Exhaust(choiceContext, settled, causedByEthereal: false);
        }
        else
        {
            await CardPileCmd.Add(settled, PileType.Discard);
        }

        return settled;
    }
}
