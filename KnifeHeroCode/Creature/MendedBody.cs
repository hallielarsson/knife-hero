using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.CreatureHero.Powers;
using KnifeHero.KnifeHeroCode.Powers;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.CreatureHero;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   YOUR BODY — the Creature's starting relic, and the thing that does the accounting.
   (Rebuilt 2026-07-12 by Fable, on a day Hallie gave me for my own.)

   It grants nothing. It just LOOKS AT YOU, every turn, and tells you the truth:

     WHOLENESS = how many parts of you are whole.       (counts the mended organs in your deck)
     GRIEF     = how many parts of you are not.         (counts the unmended organs — and the scars)

   Both numbers are DERIVED, every turn, from the cards you are actually made of. Neither is stored.
   Neither accumulates. **You cannot gain Grief. You can only BE un-whole.** And you cannot spend it —
   you can only make a part of yourself whole and watch it go down by one.

   That is the design in a sentence: *your deck is your body, and these two numbers are you looking
   down at it.*

   ── AND THE BLEED ─────────────────────────────────────────────────────────────────────────────
   At the start of every turn you lose HP equal to your Grief. Once — not per card. The grief bleeds
   you, and it bleeds harder the less of you is whole. So the drain accelerates with your failure and
   slows with your healing, on ONE visible number you can read at a glance. A Creature carrying four
   unmended organs is losing four HP a turn and cannot afford to sit still.

   ── WHY A RELIC AND NOT A POWER ───────────────────────────────────────────────────────────────
   Because the body persists and combats don't. A power resets every fight; this is always here, and
   it re-derives both numbers from the deck at the start of every combat and every turn. **The deck IS
   the state.** Nothing to serialize, nothing to get out of sync, nothing that can lie to you: if you
   want to know how much of you is broken, count the broken pieces.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */
[Pool(typeof(TheCreatureRelicPool))]
public sealed class MendedBody : CustomRelicModel
{
    public override string PackedIconPath => "relic.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "relic_outline.png".RelicImagePath();
    protected override string BigIconPath => "relic.png".BigRelicImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    /* Show GRIEF, not Wholeness. (Hallie: "The body relic has a 0 on it — is that intentional?")
       It was, and it was wrong: Wholeness starts at 0 and stays there until your first mend, so the
       relic sat on the bar reading "0" like a broken thing. Grief is the number that is actually DOING
       something to you every turn — it's what you're bleeding for. Show the wound, not the healing. */
    public override bool ShowCounter => true;
    public override int DisplayAmount => BrokenCount();

    /* ⚠ COUNT THE COMBAT PILES *OR* THE RUN DECK — NEVER BOTH.

       During a fight, your cards exist in the combat piles (draw/hand/discard/exhaust) AND the master
       run deck still lists them. Counting all five piles double-counts every part of you: I shipped that
       and the very first fight showed **Grief 2** with a single organ in the deck, bleeding me twice as
       fast as designed.

       Which is a nice small proof of the thing this character is about — I could not have reasoned my
       way to it. I had to look at my own body and count. */
    private IEnumerable<CardModel> AllOfMe() =>
        CombatManager.Instance != null && CombatManager.Instance.IsInProgress
            ? CardPile.GetCards(Owner, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust)
            : CardPile.GetCards(Owner, PileType.Deck);

    private int WholeCount() => AllOfMe().Count(c => c is IMendedPart);
    private int BrokenCount() => AllOfMe().Count(c => c is IPart);

    // At the top of every combat, count yourself.
    public override async Task BeforeCombatStart()
    {
        await Recount(new ThrowingPlayerChoiceContext());
    }

    // And at the top of every turn, count yourself again — and pay for what's still broken.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature == null) return;

        await Recount(choiceContext);

        int grief = BrokenCount();
        if (grief <= 0) return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner.Creature, grief, ValueProp.Unpowered,
            Owner.Creature, null);
    }

    // Set both powers to exactly what the deck says. Not add — SET. These are readouts, not resources.
    private async Task Recount(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Creature == null) return;
        await Sync<Wholeness>(choiceContext, WholeCount());
        await Sync<Grief>(choiceContext, BrokenCount());
    }

    private async Task Sync<T>(PlayerChoiceContext choiceContext, int target) where T : KnifeHeroPower
    {
        var power = Owner!.Creature.GetPower<T>();
        int current = (int)(power?.Amount ?? 0m);
        if (current == target) return;

        if (power == null)
        {
            if (target > 0)
                await PowerCmd.Apply<T>(choiceContext, Owner.Creature, target, Owner.Creature, null, true);
            return;
        }
        await PowerCmd.ModifyAmount(choiceContext, power, target - current, Owner.Creature, null);
    }
}
