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
using MegaCrit.Sts2.Core.Rooms;
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

    /* A SCAR COUNTS DOUBLE. (Hallie, playtest: *"Grief gains too slowly for grief cards."*)

       She's right, and the reason was that Grief could only climb by scarring, and a scar was worth
       exactly as much as a fresh wound — so the Mourner, who deliberately lets everything rot, ended a
       long fight on Grief 3, and Wallow and Keening were reading a number that barely moved. The build
       was theoretical. You cannot make a weapon out of a stat that doesn't grow.

       So: a broken part costs you 1. **A part that will never be whole costs you 2.**

       Which is the truest sentence in the character. A fresh wound is a thing you might still answer; a
       scar is the answer, and the answer was no. It goes on costing you at a rate the open wound never
       did, and it costs you in the one way you can no longer do anything about. *Unmetabolized experience
       does not get cheaper when it stops bleeding.*

       And mechanically it does exactly what the Mourner needed: rot on purpose and your Grief climbs
       twice as fast, your bleed doubles with it, and Keening and Wallow finally have something to eat.
       **You become a weapon made of what you could not heal, and it costs you double to hold.** */
    private int BrokenCount() =>
        AllOfMe().Sum(c => c switch { IScar => 2, IPart => 1, _ => 0 });

    // At the top of every combat, count yourself.
    public override async Task BeforeCombatStart()
    {
        await Recount(new ThrowingPlayerChoiceContext());
    }

    // And at the top of every turn, count yourself again — and pay for what's still broken.
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.Creature == null) return;

        await TheAppetiteReturns(choiceContext);
        await Recount(choiceContext);

        int grief = BrokenCount();
        if (grief <= 0) return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner.Creature, grief, ValueProp.Unpowered,
            Owner.Creature, null);
    }

    /* ── YOU ARE NEVER FINISHED ─────────────────────────────────────────────────────────────────
       If no part of you is still broken, your body takes another one.

       (Hallie, post-playtest: *"Lessons are stacking up with nowhere to go."* / *"I feel like I'm not
       making a ton of interesting decisions mid-fight by the first or second boss."* / *"Charnel House
       only way to get parts?"* — three complaints, one cause.)

       She was describing a character that RUNS OUT. Here is what the fixed harness measured on the
       shipped build, over 300 fights: the Heart mends on turn 3, Grief goes to 0 and stays there, and
       Lessons climb to a peak of **32** with literally nothing to spend them on. After turn 3 the
       Creature is a pile of cards with no question attached. The fork — *mend it, or let it rot* — is
       the entire character, and it was being asked **once**, and it was easy.

       So the body asks again. The moment nothing in you is broken, it goes and gets something broken.
       Grief never reaches zero for long, the bleed never fully stops, the Lessons always have somewhere
       to go, and every few turns you are asked the only question this character knows how to ask.

       ── WHY THIS IS THE RELIC AND NOT A RARE POWER ─────────────────────────────────────────────
       It *was* a Rare Power (The Appetite), which meant the character only became itself if you happened
       to be offered a specific card. An identity you might not be dealt is not an identity. This is who
       the Creature is, so it starts in your hands.

       And it is Victor's appetite, exactly — he could have stopped at one, and the whole novel is what
       it cost that he could not. The Creature does not get to be innocent of its maker: it wants to be
       more, and it will rob a grave to do it. **Every part is a bet you did not have to take.**

       (The Appetite card still exists, and now does the thing a Rare should: it takes a part EVERY turn,
       whether or not you are already carrying one. That's the Mourner's accelerator — grief stacks
       faster than any Lesson economy can answer, and Wallow and Keening eat well.) */
    private async Task TheAppetiteReturns(PlayerChoiceContext choiceContext)
    {
        if (Parts.AnyBroken(Owner)) return;

        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(Parts.Random(Owner), PileType.Hand, Owner);
    }

    /* ── THE BODY KNITS WHEN THE FIGHT IS OVER ──────────────────────────────────────────────────
       Heal 2 per Wholeness. Once. At the end.

       (Hallie, playtest: *"there's an incentive to just spend the whole game healing at the end, because
       then it feels like an obligation to play unfun."*)

       This is where ALL of the Creature's sustain now lives, and the reason it lives *here* is the only
       design rule that matters for healing in a game with run-level HP:

           **Sustain must not scale with turns spent, or the optimal play is to never finish the fight.**

       Every previous version broke that rule three ways at once (Wholeness healed per turn, the mend
       healed 2, the Gut healed 2×Wholeness and was replayable forever). All of it paid out per-turn
       against a bleed of ~1, so the correct line was to stop killing things and sit there — and the more
       whole you were, the better the pay. The game was bribing her to be bored.

       Paying at combat end keeps the *entire* reward — a Tender who assembled four organs still walks out
       +8 HP — and removes every reason to linger. You cannot farm a number that is only counted once.

       And it says the right thing. **You do not knit while you are being hit. You knit afterwards.** */
    /* ⚠ We must REMEMBER how whole we got, not recount it at the end.

       AllOfMe() counts the combat piles during a fight and the run deck outside one — and by the time
       AfterCombatVictory runs, the combat is over, so it would count the RUN DECK. Mends are
       combat-local (Player.PopulateCombatState clones the deck; Transform only writes back to
       PileType.Deck), so the run deck contains zero mended organs, always. Recounting here would heal
       for 2 × 0, every time, silently, forever — a dead reward that looks live in the code.

       So the count is stamped at each Recount, while it still means something. */
    private int _wholeThisCombat;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner?.Creature == null || _wholeThisCombat <= 0) return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, 2m * _wholeThisCombat, false);
        _wholeThisCombat = 0;
    }

    // Set both powers to exactly what the deck says. Not add — SET. These are readouts, not resources.
    private async Task Recount(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Creature == null) return;
        int whole = WholeCount();
        _wholeThisCombat = whole;   // stamped while it still means something — see AfterCombatVictory.
        await Sync<Wholeness>(choiceContext, whole);
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
