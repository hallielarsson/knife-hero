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

/* MENDED BODY — the Creature's starting relic. It grants nothing; it does the accounting.

     WHOLENESS = mended organs in your deck.
     GRIEF     = unmended organs, plus scars at double weight.

   Both are DERIVED from the deck at every combat start and turn start, and SET (never added to), so
   nothing else in the codebase may grant or spend either one. At turn start you lose HP equal to Grief.

   It's a relic and not a power because a power resets every fight and the body doesn't: the deck is
   the state, so there is nothing to serialize and nothing that can drift out of sync. */
[Pool(typeof(TheCreatureRelicPool))]
public sealed class MendedBody : CustomRelicModel
{
    public override string PackedIconPath => "relic.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "relic_outline.png".RelicImagePath();
    protected override string BigIconPath => "relic.png".BigRelicImagePath();

    public override RelicRarity Rarity => RelicRarity.Starter;

    // Counter shows GRIEF, not Wholeness — Wholeness is 0 until your first mend, so the relic just sat
    // on the bar reading "0" and looked broken.
    public override bool ShowCounter => true;
    public override int DisplayAmount => BrokenCount();

    /* ⚠ COUNT THE COMBAT PILES *OR* THE RUN DECK — NEVER BOTH. During a fight a card exists in the
       combat piles (draw/hand/discard/exhaust) AND is still listed in PileType.Deck. Passing all five
       to GetCards double-counts every part: one organ read as Grief 2 and bled at twice the rate. */
    private IEnumerable<CardModel> AllOfMe() =>
        CombatManager.Instance != null && CombatManager.Instance.IsInProgress
            ? CardPile.GetCards(Owner, PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust)
            : CardPile.GetCards(Owner, PileType.Deck);

    private int WholeCount() => AllOfMe().Count(c => c is IMendedPart);

    /* ⚠ BALANCE: a scar costs 2, an unmended part costs 1. Grief can only climb by scarring, so when a
       scar was worth the same as a fresh wound the Mourner topped out around Grief 3 and Wallow/Keening
       had nothing to scale on. Order matters — IScar : IPart, so IScar must be tested first. */
    private int BrokenCount() =>
        AllOfMe().Sum(c => c switch { IScar => 2, IPart => 1, _ => 0 });

    public override async Task BeforeCombatStart()
    {
        await Recount(new ThrowingPlayerChoiceContext());
    }

    // Turn start: take a part if nothing's broken, recount, then bleed for what is.
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

    /* If no part of you is broken, take one. This is the floor that keeps the character's central fork
       (mend it, or let it rot) being asked more than once per fight. Without it, measured over 300
       fights: the Heart mends on turn 3, Grief sits at 0 for the rest of the combat, and Lessons pile up
       to 32 with nothing to spend them on. */
    private async Task TheAppetiteReturns(PlayerChoiceContext choiceContext)
    {
        if (Parts.AnyBroken(Owner)) return;

        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(Parts.Random(Owner), PileType.Hand, Owner);
    }

    /* ALL of the Creature's sustain: heal 2 per Wholeness, ONCE, at combat end.
       ⚠ BALANCE RULE: sustain must never scale with turns spent, or the optimal line is to stop killing
       the enemy and farm HP (HP is run-level, so it's real money). Three separate engines used to break
       this at once — per-turn Wholeness healing, a heal on each mend, and a replayable Mended Gut.

       ⚠ And we must REMEMBER the count, not recount at the end: AllOfMe() reads the run deck once combat
       is over, and mends are combat-local (PopulateCombatState clones the deck; Transform writes back to
       PileType.Deck), so the run deck holds zero mended organs, always. Recounting here would silently
       heal 2 × 0 forever. So Recount stamps it while it still means something. */
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
