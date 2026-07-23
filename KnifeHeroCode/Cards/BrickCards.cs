using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE BRICK — a Pride-holding payoff pair (Stonewall was a riot; these are what got thrown).
   Both reward a hand full of Prides: the Hammer swings for the count, the Shield walls up by it. */

// BRICK HAMMER — Pride Blade. Swung: deal {Per} per Pride in your hand. Held: every Pride Blade in hand +1 attack.
public sealed class BrickHammer() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Per", 3m) };

    protected override void OnUpgrade() => DynamicVars["Per"].UpgradeValueBy(1m);

    // SWUNG — this leaves hand before we count, so it tallies the OTHER Prides you're still holding.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int prides = CardPile.GetCards(Owner, PileType.Hand).Count(c => c is IPride);
        if (prides <= 0) return;
        await DamageCmd.Attack((int)DynamicVars["Per"].BaseValue * prides).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // ON RETAIN (held at end of turn) — every Pride Blade you're holding gets sharper. Cumulative.
    // Skips itself: its damage scales by count, not a flat Damage var, so there's nothing to bump.
    protected override Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        foreach (var card in CardPile.GetCards(Owner, PileType.Hand).Where(c => c is IPride))
            if (card.DynamicVars.TryGetValue("Damage", out var dmg))
                dmg.UpgradeValueBy(1m);
        return Task.CompletedTask;
    }
}

// BRICK SHIELD — Gain {Block} Block, plus {Per} for each Pride in your hand. Not a Pride itself.
public sealed class BrickShield() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(5m, ValueProp.Move), new IntVar("Per", 3m) };

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Per"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int prides = CardPile.GetCards(Owner, PileType.Hand).Count(c => c is IPride);
        decimal total = DynamicVars.Block.BaseValue + DynamicVars["Per"].BaseValue * prides;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(total, ValueProp.Move), cardPlay);
    }
}
