using MegaCrit.Sts2.Core.Localization.DynamicVars;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Enchantments;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Rainbow Strike — deal {Per} damage for every Pride card in your hand. */
public sealed class RainbowStrike() : KnifeHeroCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override string PortraitPath => "rainbow_strike.png".CardImagePath();
    public override string CustomPortraitPath => "rainbow_strike.png".BigCardImagePath();


    // Damage per Flag — a DynamicVar, so the card text prints {Per} and stays true after upgrade.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Per", 2m) };

    protected override void OnUpgrade() => DynamicVars["Per"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int prides = CardPile.GetCards(Owner, PileType.Hand).Count(c => Queer.Is(c));
        await DamageCmd.Attack(DynamicVars["Per"].BaseValue * prides).FromCard(this)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}

// RAINBOW MATADOR — Gain {Block} Block, then return a Pride from your discard to your hand.
public sealed class RainbowMatador() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "rainbow_matador.png".CardImagePath();
    public override string CustomPortraitPath => "rainbow_matador.png".BigCardImagePath();

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(7m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var pride = CardPile.GetCards(Owner, PileType.Discard).FirstOrDefault(c => Queer.Is(c));
        if (pride != null) await CardPileCmd.Add(pride, PileType.Hand);
    }
}
