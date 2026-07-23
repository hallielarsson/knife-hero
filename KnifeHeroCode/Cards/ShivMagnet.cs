using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

// SHIV MAGNET — Gain {Block} Block. Pull every Shiv out of your draw and discard piles into your hand.
public sealed class ShivMagnet() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // Materialize first — moving a card mutates the piles we're reading.
        var shivs = CardPile.GetCards(Owner, PileType.Draw, PileType.Discard)
            .Where(c => c.Tags.Contains(CardTag.Shiv)).ToList();
        foreach (var shiv in shivs)
            await CardPileCmd.Add(shiv, PileType.Hand);
    }
}
