using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace KnifeHero.KnifeHeroCode.Cards;

/* THE PRIDE BLADES — Prides named for the Slay the Spire characters, now enchant-Powers in the 2.0 ethos:
   each is a Power you play to enchant one of your Attacks with that character's effect, then leaves. See
   PrideEnchantment.cs. (Ironclad and Watcher Pride were cut; Both Is Good retired with the old held/swung
   framework it depended on.) */

/* REGENT PRIDE — ⟨1⟩ Power, Rare. Enchant an Attack with Regent: while it's in your hand, at the start of
   your turn deal 6 damage to a random enemy and gain 6 Block. Upgraded: the Attack also gains Retain. */
public sealed class RegentPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Regent>(6m);
    protected override void OnUpgrade() { }

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;
        PrideEnchantment.Bestow<Regent>(chosen, 6m, IsUpgraded);
    }
}

/* DYKE PRIDE — ⟨1⟩ Power. Enchant an Attack with Parry: while it's in your hand, HP loss is voided and
   banked as bonus damage on that Attack. Upgraded: the Attack also gains Retain. The labrys — it wants
   you to be hit. */
public sealed class DykePride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Parry>(0m);
    protected override void OnUpgrade() { }

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;
        PrideEnchantment.Bestow<Parry>(chosen, 1m, IsUpgraded);
    }
}
