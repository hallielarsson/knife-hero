using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* ═══════════════════════════════════════════════════════════════════════════════════════════════
   STABBY — ⟨1⟩ Attack. Retain.

     Deal {Damage} damage. You may absorb a Strike or Defend in your hand: gain Vigor or Block equal
     to its value, and Stabby is Upgraded.
     Retained: gain {Block} Block.

   Damage starts at 8 and Block at 2, and BOTH go up by 1 every time it's upgraded — and it upgrades
   itself every time it eats one of your basics.

   **It is one knife that gets better by eating the deck it came in.**

   ── HALLIE'S SPEC, KEPT VERBATIM, BECAUSE I KEPT NOT HEARING IT ────────────────────────────────
   *"OK. Once more. THIS TIME. Let's get rid of it and just call it 'Stabby'. On play, it still CAN
   absorb a strike or defend. If it does so, you gain block or vigor equal to the amount gained and it
   upgrades once. On retain, it gives you U + 2 block, on attack it does 8 + U damage and the above
   absorb option. That's it. That's the whole tweet."*

   Four rewrites to get here. Every earlier one failed the same way: I kept making the CARD carry the
   ENGINE — forge a token, recruit from the discard, branch on held-vs-played, exhaust itself. Each
   version was a paragraph, and each time I was told it was too complicated I made it *cleverer* instead
   of *smaller*. This one is a knife. It hits, it eats, it grows.

   ── ABSORBING IS NOT A COST ────────────────────────────────────────────────────────────────────
   (Hallie, correcting me on precisely this: *"exhausting a card isn't a straight cost, esp. a basic."*)

   Eating a Strike **thins your deck**, which is one of the strongest things you can do in this genre —
   AND it pays you that Strike's damage back as Vigor — AND it sharpens the knife, permanently. Three
   good things, and the price is a card you did not want.

   So you are not spending your basics. **You are digesting them.** The normative doesn't get thrown
   away; it gets absorbed into the thing that replaces it, and every point of it shows up in the blade.

   No ceiling (MaxUpgradeLevel 99). A Stabby that has eaten your whole starting deck is a monster, and it
   *is* your starting deck — the same eight cards, in one hand, sharpened.
   ═══════════════════════════════════════════════════════════════════════════════════════════════ */
public sealed class FancyFootwork() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;
    public override int MaxUpgradeLevel => 99;   // it eats for as long as you feed it

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move), new BlockVar(2m, ValueProp.Move) };

    /* Both halves scale by 1 per upgrade, so "8 + U damage" and "U + 2 Block" fall straight out of the
       DynamicVars and the card text prints {Damage} / {Block} and cannot lie. Thirteen cards lied about
       themselves after upgrade last week because their numbers were hand-written into the loc string. */
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
    }

    private static bool IsBasic(CardModel c) =>
        c.Tags.Contains(CardTag.Strike) || c.Tags.Contains(CardTag.Defend);

    /* RETAINED — the flat little wall.
       ⚠ BeforeSideTurnEnd, NEVER HasTurnEndInHandEffect: that wrapper discards the card afterwards and
       does not check Retain, so a retained card with a turn-end effect throws itself away every turn,
       silently. See PrideCard.cs for the full autopsy. */
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Pile?.Type != PileType.Hand) return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        await Absorb(choiceContext);
    }

    /* THE ABSORB. Optional — Cancelable, so you can always decline and keep the basic. You often want to:
       Stabby has Retain, so it will still be there next turn, and a Defend you're about to need is worth
       more than a point of sharpness. */
    private async Task Absorb(PlayerChoiceContext choiceContext)
    {
        if (!CardPile.GetCards(Owner, PileType.Hand).Any(c => c != this && IsBasic(c))) return;

        var prefs = new CardSelectorPrefs(new LocString("gameplay_ui", "CHOOSE_CARD_HEADER"), 1)
        {
            Cancelable = true,
        };
        var chosen = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs,
            c => c != this && IsBasic(c), this)).FirstOrDefault();
        if (chosen == null) return;

        /* "Gain block or vigor equal to the amount gained" — the absorbed card's OWN value, read off its
           DynamicVars rather than hardcoded. So an upgraded Defend feeds you more than a base one, and a
           Queered Strike feeds you its real number. **Eat what it was actually worth.** */
        bool wasStrike = chosen.Tags.Contains(CardTag.Strike);
        decimal worth = Worth(chosen, wasStrike);

        await CardCmd.Exhaust(choiceContext, chosen, causedByEthereal: false);

        if (worth > 0m)
        {
            if (wasStrike)
                await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, worth, Owner.Creature,
                    this, false);
            else
                await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(worth, ValueProp.Move), null);
        }

        CardCmd.Upgrade(this);   // and the knife is sharper now. For good.
    }

    private static decimal Worth(CardModel card, bool wasStrike) =>
        card.DynamicVars.TryGetValue(wasStrike ? "Damage" : "Block", out var v) ? v.BaseValue : 0m;
}
