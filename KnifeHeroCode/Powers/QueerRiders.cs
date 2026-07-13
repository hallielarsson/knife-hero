using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   THE QUEER ENGINE — chassis + rider. Queering never replaces the CARD; it keeps its identity (a Strike
   is still a Strike) and bolts something on. Divergence by what is added, not by substitution.

   ── A QUEERING REPLACES THE LAST QUEERING ──────────────────────────────────────────────────────
   (Hallie, 2026-07-13: "a Queer on a card replaces the other queers on a card unless otherwise
   specified.")

   I had them accumulate, and that was wrong. Stacked riders meant a card that went through the wash
   enough times became an unreadable pile of every effect in the game at once —
   "Queer 5: Sharp, Loud, Guarded, Poisoned, Generous" — which is not divergence. It's just *more*, and
   everything converges on the same maximal card. Accumulation makes everything the same in the end.

   Replacement keeps the thing that actually matters: **each queering makes the card something ELSE.**
   The Strike you exhaust today comes back Sly. Exhaust it again and it comes back Guarded instead. It is
   never the same card twice, and it is never all of the cards at once.

   **You don't collect queerness. You keep becoming.**

   ── AND IT READS IN ONE LINE ───────────────────────────────────────────────────────────────────
       "Deal 6 damage.
        Queer: Sharp."

   One modifier, holding the current rider. Not N modifiers each stapling on their own sentence.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */
public enum QueerKind
{
    Poisoned,   // 2 Poison on what it hits
    Sharp,      // +3 damage
    Loud,       // 1 Weak
    Exposed,    // 1 Vulnerable
    Generous,   // draw a card
    Guarded,    // 4 Block
    Fade,       // 1 Stealth, granted AFTER the card resolves — so an Attack breaks your cover and Fade
                // hands it straight back. See AfterCardPlayedLate. Attacking from Fade still costs Heat:
                // they heard the knife, they just can't find you.
    Sly,        // keyword: it plays itself when discarded
    Clingy,     // keyword: Retain
    Early,      // keyword: Innate
}

public sealed class QueerMod : CardModifier
{
    private readonly List<QueerKind> _riders = new();

    public int Count => _riders.Count;

    // Take back any keyword the previous rider granted, so replacing really replaces.
    private void ClearKeywords(CardModel card)
    {
        foreach (var old in _riders)
        {
            switch (old)
            {
                case QueerKind.Sly:    card.RemoveKeyword(CardKeyword.Sly); break;
                case QueerKind.Clingy: card.RemoveKeyword(CardKeyword.Retain); break;
                case QueerKind.Early:  card.RemoveKeyword(CardKeyword.Innate); break;
            }
        }
    }

    /* Queer a card: find its QueerMod (or make one), and REPLACE whatever it was with something new.

       The single entry point. The relic calls it for both halves of the thesis — what you MAKE (the first
       Attack you create each turn) and what is CAST OUT (the Strike or Defend you exhaust, which comes
       back to your draw pile other). Same door, so they're the same kind of becoming. */
    public static void Queer(CardModel card, Player player)
    {
        var mod = DirectModifiers(card).OfType<QueerMod>().FirstOrDefault();
        if (mod == null)
        {
            mod = (QueerMod)Get<QueerMod>().MutableClone();
            AddModifier(card, mod);
        }

        var rng = player.RunState.Rng.CombatCardGeneration;
        var kind = rng.NextItem(Enum.GetValues<QueerKind>().ToList());

        /* A QUEERING REPLACES THE LAST ONE (Hallie, 2026-07-13: "a Queer on a card replaces the other
           queers on a card unless otherwise specified").

           Accumulation was the wrong shape and I should have seen it. Stacked riders meant a card that
           went through the wash enough times turned into an unreadable pile of every effect in the game
           at once — Queer 5: Sharp, Loud, Guarded, Poisoned, Generous — which is not divergence, it's
           just *more*. Everything converges on the same maximal card.

           Replacing keeps the thing that actually matters: **each queering makes the card something
           ELSE.** The Strike you exhaust today comes back Sly; exhaust it again and it comes back
           Guarded instead. It is never the same card twice, and it is never all of the cards at once.
           You don't collect queerness. You keep becoming. */
        mod.ClearKeywords(card);
        mod._riders.Clear();
        mod._riders.Add(kind);

        // Keyword riders change what the card IS, so they land the moment they're applied.
        switch (kind)
        {
            case QueerKind.Sly:    card.AddKeyword(CardKeyword.Sly); break;
            case QueerKind.Clingy: card.AddKeyword(CardKeyword.Retain); break;
            case QueerKind.Early:  card.AddKeyword(CardKeyword.Innate); break;
        }
    }

    public override void ModifyDescription(Creature? target, ref string description)
    {
        if (_riders.Count == 0) return;
        description += $"\nQueer: {string.Join(", ", _riders)}.";
    }

    /* ── THE GLOSS ──────────────────────────────────────────────────────────────────────────────
       (Hallie, 2026-07-13: "Is there a way to get a gloss on the Queer in the side like other keywords?")

       Yes. `CardModel.HoverTips` is what fills the side panel, and it is a plain public getter — so
       QueerHoverTipPatch (KnifeHeroCode/Patches/QueerHoverTips.cs) postfixes it and appends whatever we
       return here. Two tips: the umbrella (**what queering IS**, including that a new one replaces the
       old) and the specific rider currently riding.

       The three keyword riders — Sly, Clingy, Early — get a *second* tip for free, because they really
       do add the engine's own keyword, and the engine already glosses every keyword on a card. So
       "Clingy" tells you it grants Retain, and Retain tells you what Retain does. The rider names stay
       ours; the rules stay the game's. */
    private static readonly LocString Umbrella = new("static_hover_tips", "queer.description");
    private static readonly LocString UmbrellaTitle = new("static_hover_tips", "queer.title");

    public static IReadOnlyList<IHoverTip> TipsFor(CardModel card)
    {
        var mod = DirectModifiers(card).OfType<QueerMod>().FirstOrDefault();
        if (mod == null || mod._riders.Count == 0) return Array.Empty<IHoverTip>();

        var tips = new List<IHoverTip> { new HoverTip(UmbrellaTitle, Umbrella) };
        foreach (var kind in mod._riders)
        {
            string slug = kind.ToString().ToLowerInvariant();
            tips.Add(new HoverTip(
                new LocString("static_hover_tips", $"queer.{slug}.title"),
                new LocString("static_hover_tips", $"queer.{slug}.description")));
        }
        return tips;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var me = Owner?.Owner?.Creature;
        var player = Owner?.Owner;
        var enemy = cardPlay.Target;
        if (me == null || player == null) return;

        foreach (var kind in _riders)
        {
            bool hitting = enemy != null && enemy != me;
            switch (kind)
            {
                case QueerKind.Poisoned when hitting:
                    await PowerCmd.Apply<PoisonPower>(choiceContext, enemy!, 2m, me, null, false);
                    break;

                case QueerKind.Sharp when hitting:
                    await DamageCmd.Attack(3m).FromCard(Owner!).Targeting(enemy!)
                        .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
                    break;

                case QueerKind.Loud when hitting:
                    await PowerCmd.Apply<WeakPower>(choiceContext, enemy!, 1m, me, null, false);
                    break;

                case QueerKind.Exposed when hitting:
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy!, 1m, me, null, false);
                    break;

                case QueerKind.Generous:
                    await CardPileCmd.Draw(choiceContext, 1m, player);
                    break;

                case QueerKind.Guarded:
                    await CreatureCmd.GainBlock(me, new BlockVar(4m, ValueProp.Move), null);
                    break;

                // FADE is not here. It has to land after the attack breaks your cover — see below.

                // Sly / Clingy / Early are keywords. They already did their work when they landed.
            }
        }
    }

    /* ── FADE LANDS LAST ────────────────────────────────────────────────────────────────────────
       (Hallie, 2026-07-13: "The stealth queer should proc AFTER damage.")

       She's right, and it was worse than mistimed — **Fade did literally nothing on an Attack**, which
       is the only place it was interesting. Here is the sequence the engine actually runs (CardModel
       ~line 1931):

           OnPlay            → the card deals its damage; modifiers fire; Fade grants 1 Stealth
           AfterCardPlayed   → Stealth.AfterCardPlayed sees an Attack was played and removes ALL Stealth

       So Fade handed you a point of cover and then the very same swing threw it away. My comment in the
       enum called it "filthy on an attack" and it had never once worked. I reasoned about the interaction
       instead of following the call order, which is the same mistake as the float bug wearing a new hat.

       The fix is a hook, not a workaround. `Hook.AfterCardPlayed` dispatches TWO full passes over every
       listener — `AfterCardPlayed`, then `AfterCardPlayedLate` (Hook.cs:278). Stealth's break is in the
       first pass. Granting Fade's Stealth in the second pass therefore lands strictly after it, for
       every listener, with **no dependence on listener order**. Not a race we win; a race we're not in.

       And now the rider means what its name means: you strike, they see the knife, and by the time they
       look up **you are already gone.** */
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Owner) return;
        if (!_riders.Contains(QueerKind.Fade)) return;

        var me = Owner?.Owner?.Creature;
        if (me == null) return;
        await PowerCmd.Apply<Stealth>(choiceContext, me, 1m, me, null, false);
    }
}
