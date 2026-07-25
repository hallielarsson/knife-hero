using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace KnifeHero.KnifeHeroCode.Cards;

/* FINGER GUNS — ⟨1⟩ Power. Bisexual Pride. Enchant an Attack with Bi: while it's in your hand, at end of
   turn deal 3 damage twice and gain 1 Visibility. Upgraded: the enchanted Attack also gains Retain.
   (2.0: converted from the old held/swung Pride to the enchant frame — see PrideEnchantment.cs, Bi.) */
public sealed class FingerGuns() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "finger_guns.png".CardImagePath();
    public override string CustomPortraitPath => "finger_guns.png".BigCardImagePath();

    public override int MaxUpgradeLevel => 1;
    protected override void OnUpgrade() { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Bi>(3m);

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;
        PrideEnchantment.Bestow<Bi>(chosen, 3m, IsUpgraded);
    }
}
