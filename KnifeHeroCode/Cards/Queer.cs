using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Queer — the Gay Blade's core engine, shipped as a Curse (QUEER_ENGINE_SPEC.md).
   Found in a flow/design session 2026-06-16; this is the playable first cut.

   Curse · Innate · Eternal · Unplayable.
   - Curse:     the world files queerness as a curse — it is secretly your whole engine. Being a
                Curse (unplayable, clogs hand) IS the drawback; no invented downside needed.
   - Innate:    in your opening hand every combat. You don't get to *not* be out. The tax is honest
                and constant (open every fight down a hand slot), and — crucially — Innate keeps it
                in a combat pile, so it receives combat hooks and can run the engine itself.
   - Eternal:   can't be removed. You can't be put back in the closet. It's the one card removal
                can't touch — and the one that turns erasure into becoming.

   THE THESIS (constitutional): diversity is strength; thin by becoming, not subtraction. Every other
   character deletes its basic Strikes/Defends to reach a lean core. The Gay Blade can't — it queers
   them. The cast-out normative doesn't leave; it comes back OTHER.

   v1 SCOPE (do the proposal, reap in playtest — Hallie's principle):
   - Trigger: combat-side EXHAUST only. Run-level deck removal (Hook.BeforeCardRemoved) is the
     documented next cut — it needs a run-scoped host (relic/character), not an in-combat card.
   - Targets: basic Strikes & Defends (CardTag.Strike/Defend) — the truly normative. Shivs are in
     scope per the spec but DEFERRED here: shivs Exhaust by default, so including them is the
     "firehose" the spec flags as // PROPOSAL (gate it before turning on).
   - Becoming: for now the cast-out card transforms into a Throwing Shiv (Kunai). The real Tinker
     chassis+rider assembly (riders = the relocated coatings) is the next iteration; it wants the
     BaseLib CardModifier ModelDb plumbing. TransformToRandom over a curated out-pool is the
     // PROPOSAL upgrade path to true per-source divergence. */
public sealed class Queer() : KnifeHeroCard(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new HashSet<CardKeyword>
    {
        CardKeyword.Innate,
        CardKeyword.Eternal,
        CardKeyword.Unplayable,
    };

    // The engine. While this sits Innate in hand (a combat pile → ShouldReceiveCombatHooks is true),
    // it watches every card that lands in the Exhaust pile. When the normative is cast out, queer it.
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Pile?.Type != PileType.Exhaust) return;   // only when a card just LANDED in exhaust
        if (oldPileType == PileType.Exhaust) return;        // ignore our own relocation — no re-entry loop
        if (card.Owner != Owner) return;                    // only your own cast-out cards
        if (!card.Tags.Contains(CardTag.Strike) && !card.Tags.Contains(CardTag.Defend)) return;

        // Refuse erasure: return the ORIGINAL card to the deck (draw pile) with a queer rider bolted
        // on. Chassis + rider (the Tinker model) — it stays a Strike/Defend but comes back OUT. You
        // can't cast the normative away; it returns to your deck, queer, to be drawn again.
        bool alreadyQueer = CardModifier.DirectModifiers(card).Any(m => m is QueerRiderMod);
        await CardPileCmd.Add(card, PileType.Draw);
        if (!alreadyQueer)
            CardModifier.AddModifier(card, (QueerRiderMod)CardModifier.Get<QueerRiderMod>().MutableClone());
    }
}
