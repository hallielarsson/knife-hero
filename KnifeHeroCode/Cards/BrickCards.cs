using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE BRICK — the enchanted-card payoff pair (Stonewall was a riot; these are what got thrown). Both
   reward a hand full of flags: the Hammer swings for the count of enchanted cards, the Shield walls up
   by it. (2.0: they count enchanted cards now, not Prides — one axis.) */

/* BRICK HAMMER — deal {Damage} damage, plus {Per} for each enchanted card in your hand.
   Built like the Shield now: a flat base with a Per rider on top. The base is the point — the old
   Hammer was pure scaling and returned early at zero flags, so a 1-cost Uncommon could be a total
   blank on a hand that hadn't come out yet. The brick always connects; the flags decide how hard. */
public sealed class BrickHammer() : KnifeHeroCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "brick_hammer.png".CardImagePath();
    public override string CustomPortraitPath => "brick_hammer.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new IntVar("Per", 3m) };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["Per"].UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int flags = CardPile.GetCards(Owner, PileType.Hand).Count(Queer.Is);
        decimal total = DynamicVars.Damage.BaseValue + DynamicVars["Per"].BaseValue * flags;
        await DamageCmd.Attack(total).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}

// BRICK SHIELD — gain {Block} Block, plus {Per} for each enchanted card in your hand.
public sealed class BrickShield() : KnifeHeroCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => "brick_shield.png".CardImagePath();
    public override string CustomPortraitPath => "brick_shield.png".BigCardImagePath();

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
        int flags = CardPile.GetCards(Owner, PileType.Hand).Count(Queer.Is);
        decimal total = DynamicVars.Block.BaseValue + DynamicVars["Per"].BaseValue * flags;
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(total, ValueProp.Move), cardPlay);
    }
}
