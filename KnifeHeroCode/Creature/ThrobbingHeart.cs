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
public sealed class ThrobbingHeart() : CreatureCard(0, CardType.Curse, CardRarity.Curse, TargetType.Self)
{
    public override string PortraitPath => "throbbing_heart.png".CardImagePath();
    public override string CustomPortraitPath => "throbbing_heart.png".BigCardImagePath();

    public override int MaxUpgradeLevel => 0;

    /* 4, not 3 — and the extra turn is not slack, it is the WINDOW.
       Both the throb and the fester check live in AfterPlayerTurnStart (they must: a turn-END effect
       would discard the card, see the note below). So at 3, the turn your Grief finally reaches the
       gate is the same turn-start the timer hits zero and rots it — the window closes before you are
       allowed to act. That is an ambush, not a race. Found by PLAYING it, not by measuring it; the
       batch only said "0 redemptions" and never said why.
       At 4 the sequence is:
         turn it lands  → Grief 1, take 1, timer 3
         +1             → Grief 2, take 2, timer 2   ← gate reachable, AND you get to act
         +2             → Grief 3, take 3, timer 1   ← still your window
         +3             → Grief 4, take 4, timer 0   → festers
       Two full turns to find 2 Lessons. Card text still honestly reads "within 3 turns." */
    private const int TurnsToFester = 4;
    private const int MaxHpPerWholeness = 2;
    private int _turnsLeft = TurnsToFester;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Eternal, CardKeyword.Retain };

    // Process at 2 Grief AND 2 Lessons. Feeling alone will not do it; understanding alone will not
    // do it. The gate is the character.
    protected override bool IsPlayable => GriefAmount() >= 2 && LessonAmount() >= 2;

    /* THE WOUND THROBS, AND IT THROBS BY ITSELF — no proxy card.

       ── Why the old design used a proxy, and why I nearly broke it ──────────────────────────────
       ⚠ DO NOT give this card `HasTurnEndInHandEffect`. I tried, and it silently destroys Retain.
       `CardModel.OnTurnEndInHandWrapper` (see .decompiled/) does this, unconditionally, after your
       effect runs:
           if (Keywords.Contains(Ethereal))  Exhaust(this);
           else                              CardPileCmd.Add(this, PileType.Discard);
       It never checks Retain. The engine treats turn-end-in-hand as a mechanism for cards that LEAVE
       (Vexing Memory, The Discourse, Festering Wound). So a Retain card that throbs at turn end throws
       itself away every turn. That is why the original design routed the throb through a spawned
       VexingMemory: the Heart *cannot* throb directly at end of turn and also stay in your hand. The
       proxy was a workaround, not an accident — and I deleted it before finding out why it was there.
       `AfterPlayerTurnStart` does NOT move the card. That's where the throb belongs.

       ── The bug the proxy design actually had ──────────────────────────────────────────────────
       MEASURED 2026-07-11: 900 fights, 3 play policies, **0 redemptions**. The Heart festered ~100% of
       the time and the character's entire thesis had never once run. Mechanism: the Heart is
       Eternal+Retain, so it never leaves hand, never gets redrawn, and therefore spawned exactly ONE
       Vexing Memory, ever. That Memory had been made Ethereal in an earlier session (to fix "vexing
       memories stack up too quick") — so it self-exhausted after its FIRST grief pulse. Grief hit 1 and
       stopped forever. The gate needs 2. The redemption path was structurally unreachable. The clutter
       fix was never re-checked against the threshold it silently severed.

       ── The fix ────────────────────────────────────────────────────────────────────────────────
       The blue-sky answer, not a patch: **the part you are carrying IS the grief.** It needs no proxy
       to represent it — it is right there in your hand. So the Heart throbs on its own, at the start of
       each turn it is held, and grief COMPOUNDS (grief N costs N HP that turn — an unmetabolized datum
       gets more expensive the longer you carry it).

       And now the clock finally coheres, which it never did:
         turn it lands  → Grief 1, take 1, timer 2
         next turn      → Grief 2, take 2, timer 1   ← the gate is now REACHABLE
         next turn      → Grief 3, take 3, timer 0   → festers if you never found 2 Lessons
       You get a two-turn window to redeem, and **Lessons are the binding constraint** — which is what
       the design always claimed and never once delivered. */
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Pile?.Type != PileType.Hand) return;

        await PowerCmd.Apply<Grief>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        int grief = GriefAmount();
        if (grief > 0)
            await TakeGriefDamage(choiceContext, grief);

        _turnsLeft--;
        if (_turnsLeft <= 0)
            await CardCmd.Transform(this, CombatState.CreateCard<FesteringWound>(Owner));
    }

    /* THE MEND — one stage, in your hand, now. Clear the Vexing Memories, spend the Grief and the
       Lessons, and the part becomes whole: +1 Wholeness, +2 max HP for the rest of the run (health is
       a RUN-level loop, so this is the only thing in the character that raises your ceiling), and the
       curse transforms into a Mended Heart you are still holding — a weapon you can swing this fight.
       Redeem early and you get to use it. That's the second gradient on the 3-turn clock. */
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var vexes = CardPile.GetCards(Owner, PileType.Hand, PileType.Draw, PileType.Discard)
            .Where(c => c is VexingMemory).ToList();
        if (vexes.Count > 0)
            await CardPileCmd.RemoveFromCombat(vexes);

        foreach (var p in Owner.Creature.Powers.Where(p => p is Grief || p is Lesson).ToList())
            await PowerCmd.Remove(p);

        await PowerCmd.Apply<Wholeness>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
        await CreatureCmd.SetMaxHp(Owner.Creature, Owner.Creature.MaxHp + MaxHpPerWholeness);
        await CreatureCmd.Heal(Owner.Creature, MaxHpPerWholeness, false);

        await CardCmd.Transform(this, CombatState.CreateCard<MendedHeart>(Owner));
    }

    private int GriefAmount() => (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Grief)?.Amount ?? 0m);
    private int LessonAmount() => (int)(Owner.Creature.Powers.FirstOrDefault(p => p is Lesson)?.Amount ?? 0m);
}
