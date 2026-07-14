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

/* THE QUEER ENGINE — chassis + rider. A queering keeps the card's identity (a Strike stays a Strike)
   and bolts one rider on. A new queering REPLACES the previous rider; riders never accumulate — stacked
   riders converge every washed card on the same maximal effect pile and stop being readable. */
public enum QueerKind
{
    Poisoned,   // 2 Poison on what it hits
    Sharp,      // +3 damage
    Loud,       // 1 Weak
    Exposed,    // 1 Vulnerable
    Generous,   // draw a card
    Guarded,    // 4 Block
    Fade,       // 1 Stealth, granted AFTER the card resolves — see AfterCardPlayedLate. Attacking from
                // Fade still costs Heat.
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

    /* The single entry point: find the card's QueerMod (or make one) and REPLACE its rider with a new
       random one. Called by EverythingIMakeIsQueer for both halves — the Attack you create each turn,
       and a draw-pile card when you exhaust a basic. */
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

        // Replace, never accumulate. ClearKeywords first, or a keyword rider outlives its replacement.
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

    /* THE SIDE-PANEL GLOSS. `CardModel.HoverTips` fills the side panel and is a public getter, so
       Patches/QueerHoverTips.cs postfixes it and appends these. Two tips: the umbrella, plus the current
       rider. The keyword riders (Sly/Clingy/Early) get a second tip for free — the engine auto-glosses
       every keyword on a card, so don't restate Retain/Innate/Sly in loc text. */
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

                // FADE is not here — it must land after the attack breaks your cover. See below.
                // Sly / Clingy / Early are keywords; they did their work when they landed.
            }
        }
    }

    /* ⚠ FADE MUST GRANT ITS STEALTH IN AfterCardPlayedLate, NOT OnPlay.

       Engine call order: OnPlay (the card damages; modifiers fire) → AfterCardPlayed, where
       Stealth.AfterCardPlayed sees an Attack was played and removes ALL Stealth. So a Fade granted in
       OnPlay is thrown away by the very swing that triggered it — the rider did literally nothing on an
       Attack, which is the only place it's interesting.

       `Hook.AfterCardPlayed` dispatches TWO full passes over every listener: AfterCardPlayed, then
       AfterCardPlayedLate. Stealth's break is in the first pass, so granting in the second lands strictly
       after it with no dependence on listener order. */
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Owner) return;
        if (!_riders.Contains(QueerKind.Fade)) return;

        var me = Owner?.Owner?.Creature;
        if (me == null) return;
        await PowerCmd.Apply<Stealth>(choiceContext, me, 1m, me, null, false);
    }
}
