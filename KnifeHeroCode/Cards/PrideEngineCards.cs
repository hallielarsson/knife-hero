using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.HoverTips;
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
        Queer.Apply(Owner.RunState.Rng.CombatCardGeneration.NextItem(attacks), Owner);
    }
}

/* KNIFE BLOCK — ⟨1⟩ Power. Enchant an Attack with Walling: while it's in your hand, at end of turn gain
   2 Block per enchanted card in your hand. Upgraded: the enchanted Attack also gains Retain.
   (2.0: converted from the held/swung Pride to the enchant frame — see PrideEnchantment.cs, Walling.) */
public sealed class KnifeBlock() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;
    protected override void OnUpgrade() { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Walling>(2m);

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;
        PrideEnchantment.Bestow<Walling>(chosen, 2m, IsUpgraded);
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

/* GLOW UP — ⟨1⟩ Power. Whenever you play an enchanted card, upgrade it. The flag build's scaling engine:
   fly a flag on a Strike and every replay levels it. See GlowUpPower. */
public sealed class GlowUp() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<GlowUpPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
