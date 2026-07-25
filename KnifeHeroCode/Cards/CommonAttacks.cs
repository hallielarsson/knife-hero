using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE COMMON ATTACKS — Hallie, 2026-07-12. The deck's baseline attacks, all cost 1.

   Three of the four talk to Visibility, which is what makes Visibility a real axis rather than just a punishment:
     • CHILL TOUCH  cools you down    (−1 Visibility)
     • DASHING STRIKE         heats you up      (+1 Visibility, and hits harder for it)
     • BACKSTAB     hides you first   (+1 Stealth, then strikes — and striking normally breaks Stealth,
                                       so it hands you cover with one hand and takes it with the other)

   And HEAD EMPTY is the one with no opinion about any of it. */


/* HEAD EMPTY, NO THOUGHTS — ⟨1⟩ Deal 8 (11). Discard a card.
   The biggest common attack in the deck, and the price is a card out of your hand. No thoughts. Just
   the swing. Pairs filthily with Silent Pride, which pays you 3 Block every time you discard. */
public sealed class HeadEmptyNoThoughts() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        if (!CardPile.GetCards(Owner, PileType.Hand).Any()) return;
        var prefs = new CardSelectorPrefs(new LocString("gameplay_ui", "CHOOSE_CARD_DISCARD_HEADER"), 1);
        foreach (var c in await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this))
            await CardCmd.Discard(choiceContext, c);
    }
}


/* CHILL TOUCH — ⟨1⟩ Deal 7 (10). Lose 1 Visibility.
   The attack that cools you down. In a deck where attacking normally *reveals* you, this is the one that
   walks it back a step. The only attack in the game that makes you harder to find. */
public sealed class ChillTouch() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(7m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        var vis = CardPile.GetCards(Owner, PileType.Hand).OfType<Visibility>().FirstOrDefault();
        if (vis != null) await CardCmd.Exhaust(choiceContext, vis, causedByEthereal: false);   // cool down: -1 Visibility
    }
}


/* DASHING STRIKE — ⟨1⟩ Deal 6 (9), plus your Visibility. Gain 1 Visibility.
   The louder you are, the harder you hit — and the louder you get. DashingStrike is the whole Honeypot build in
   one card: it's a spiral you climb on purpose. Every swing makes the next one bigger and makes you
   easier to find, and if you're running Honeypot (Thorns = Visibility + 2) that's exactly what you want.
   Dead Name turns the drawback off entirely, which is the other way to abuse it. */
public sealed class DashingStrike() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        decimal visibility = Visibility.CountInHand(Owner);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue + visibility).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        await Visibility.Add(choiceContext, Owner, 1, PileType.Hand);   // chosen → to hand (climb the spiral)
    }
}


/* BACKSTAB — ⟨1⟩ Deal 6 (9), plus 2 for every Stealth you have.
   (Hallie, 2026-07-12: "Backstab should not gain stealth, it should do extra damage based on stealth.")

   A backstab is the PAYOFF for being hidden, not a way to get hidden. Which makes your Stealth bank a
   damage bank: you sneak, you stack, and then you put the knife in.

   And it's the right way to LOSE the Stealth, too — attacking breaks it (see Stealth.cs), so the cover
   was always going to go the moment you swung. Backstab is what you swing. Every other attack throws
   your cover away for nothing; this one converts it.

   Its rivals for the same bank are Sneak Attack (3 per Stealth, all-in) and Look What I Found Down Here
   (turn it into Shivs). Three ways to cash the same currency, and you'll only ever draw one of them. */
public sealed class Backstab() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    /* {Per} is a DynamicVar, not a private field, so the card text can print it and cannot lie after an
       upgrade. It used to be `IsUpgraded ? 3m : 2m` with a hardcoded "2" in the loc string — so an
       upgraded Backstab dealt 3 per Stealth and told you 2, forever, and nothing anywhere would ever
       catch it. Four cards were doing this. If a number can change, it lives in a DynamicVar. */
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new IntVar("Per", 2m) };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Per"].UpgradeValueBy(1m);   // the deeper the shadow, the deeper the knife
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        decimal stealth = Owner.Creature.GetPower<Stealth>()?.Amount ?? 0m;
        decimal damage = DynamicVars.Damage.BaseValue + stealth * DynamicVars["Per"].BaseValue;

        await DamageCmd.Attack(damage).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}
