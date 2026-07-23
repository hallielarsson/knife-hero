using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* GAY PARR-IS — ⟨1⟩ Skill. Gain {Block} Block, then Queer a random Attack in your hand.
   (The pun is load-bearing: Gay Parry / Gay Paris.) */
public sealed class GayParris() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var attacks = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c != this && c.Type == CardType.Attack).ToList();
        if (attacks.Count == 0) return;
        QueerMod.Queer(Owner.RunState.Rng.CombatCardGeneration.NextItem(attacks), Owner);
    }
}

/* KNIFE BLOCK — ⟨2⟩ Pride. Gain {Per} Block per Pride or Queer card in your hand.
   The in-hand payoff (opposite Stonewall's played-count): swing it for the block now, or hold it and
   wall up every turn. Counts itself while held. */
public sealed class KnifeBlock() : PrideCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Per", 2m) };

    protected override void OnUpgrade() => DynamicVars["Per"].UpgradeValueBy(1m);

    private int Count() => CardPile.GetCards(Owner, PileType.Hand)
        .Count(c => c is IPride || QueerMod.IsQueer(c));

    protected override Task WhileFlown(PlayerChoiceContext choiceContext) => GainByCount();
    protected override Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay) => GainByCount();

    private async Task GainByCount()
    {
        int n = Count();
        if (n <= 0) return;
        await CreatureCmd.GainBlock(Owner.Creature,
            new BlockVar((int)DynamicVars["Per"].BaseValue * n, ValueProp.Move), null);
    }
}

/* BISEXUAL LIGHTNING — ⟨2⟩ Power. At the start of each turn, deal {Zap} to 2 random enemies (always 2). */
public sealed class BisexualLightning() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Zap", 3m) };

    protected override void OnUpgrade() => DynamicVars["Zap"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BisexualLightningPower>(choiceContext, Owner.Creature,
            DynamicVars["Zap"].BaseValue, Owner.Creature, this, false);
    }
}

/* GAY WRATH MONTH — ⟨3⟩ Power. At the end of your turn, gain 1 Vigor per Pride or Queer card in hand. */
public sealed class GayWrathMonth() : KnifeHeroCard(3, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<GayWrathPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

/* SOLIDARITY — ⟨2⟩ (1 upgraded) Power. Whenever you play a Queer or Pride card, gain 3 Block. */
public sealed class Solidarity() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);   // 2 -> 1

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SolidarityPower>(choiceContext, Owner.Creature, 3m, Owner.Creature, this, false);
    }
}

/* KNIFE TO MEET U — ⟨2⟩ (1 upgraded) Power. When you draw a Shiv, draw a card. */
public sealed class KnifeToMeetU() : KnifeHeroCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);   // 2 -> 1

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<KnifeToMeetUPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
