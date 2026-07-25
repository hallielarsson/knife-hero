using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Cards;

/* TOP CHOP — ⟨1⟩ Power. Enchant an Attack with Top: while it's in your hand, gain 4 Vigor at end of turn.
   Upgraded: the enchanted Attack also gains Retain. The Gay Blade's top lean, handed to a blade.
   (2.0: converted from the old held/swung Pride to the enchant frame — see PrideEnchantment.cs, Top.) */
public sealed class TopChop() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;
    protected override void OnUpgrade() { }   // the upgrade is the Retain grant (via Bestow)

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;
        PrideEnchantment.Bestow<Top>(chosen, 4m, IsUpgraded);
    }
}
