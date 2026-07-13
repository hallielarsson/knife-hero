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
   THE QUEER ENGINE — chassis + rider. Queering never REPLACES a card; it keeps its identity and bolts
   something on. Divergence by what is *added*, not by substitution.

   ── REBUILT 2026-07-12, post-playtest. Hallie:
      *"Queer in the base card text isn't getting it. I almost want it to be a card modifier like Sharp
      that has the number of queered effects on it — like **Queer 3** — and you can see all the ways it's
      queered."*

   She's right, and the old shape couldn't do it. Every rider used to be its OWN CardModifier, so a card
   queered three times grew three separate sentences stapled onto the end of it:

       "Deal 6 damage. Queer: Sharp. Queer: Loud. Queer: Fade."

   That doesn't read as *thrice queer*. It reads as clutter. And the whole thesis is that queerness
   ACCUMULATES and DIVERGES.

   So now there is exactly ONE modifier — QueerMod — and it *holds* the riders:

       "Deal 6 damage.
        Queer 3: Sharp, Loud, Fade."

   One line. A number you can read at a glance. And the list tells you what THIS card became, which is
   different from what every other queered card became. **Divergence by source IS the diversity** — and
   now you can see it.
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

    /* Queer a card: find its existing QueerMod (or make one) and append a random rider.

       This is the ONLY entry point. The relic (what you MAKE) and the Queer curse (what the world CASTS
       OUT) both call it — so both come back other in exactly the same way, and never the same way twice. */
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
        description += $"\nQueer {_riders.Count}: {string.Join(", ", _riders)}.";
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
