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
    Fade,       // 1 Stealth — filthy on an attack, since attacking normally BREAKS your Stealth
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

                case QueerKind.Fade:
                    await PowerCmd.Apply<Stealth>(choiceContext, me, 1m, me, null, false);
                    break;

                // Sly / Clingy / Early are keywords. They already did their work when they landed.
            }
        }
    }
}
