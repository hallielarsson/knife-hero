using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* BOTTOM BLADE — ⟨1⟩ Power. Enchant an Attack with Bottom: while it's in your hand, gain 4 Block at end
   of turn. Upgraded: the enchanted Attack also gains Retain. The bottom lean, the wall, handed to a blade.
   (2.0: converted from the old held/swung Pride to the enchant frame — see PrideEnchantment.cs, Bottom.) */
public sealed class BottomBlade() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;
    protected override void OnUpgrade() { }

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;
        PrideEnchantment.Bestow<Bottom>(chosen, 4m, IsUpgraded);
    }
}
