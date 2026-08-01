using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* SILENT PRIDE — ⟨1⟩ Power. Enchant an Attack you own with Shady: while it's in your hand, put a Shiv on
   top of your draw pile at the end of your turn; and when you play it, draw a card. Upgraded: the
   enchanted Attack also gains Retain, so it keeps making knives every turn.

   ── THE 2.0 SHAPE (Hallie, 2026-07-24) ──────────────────────────────────────────────────────────────
   Converted from the old two-state Pride blade to the enchant frame (see PrideEnchantment.cs, Shady).
   The Silent doesn't make noise — it makes knives, and now it makes any Attack you own into the engine
   that keeps them coming. */
public sealed class SilentPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "silent_pride.png".CardImagePath();
    public override string CustomPortraitPath => "silent_pride.png".BigCardImagePath();

    public override int MaxUpgradeLevel => 1;

    // The upgrade IS the Retain grant (applied in OnPlay via GrantsRetain).
    protected override void OnUpgrade() { }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Shady>(0m);

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;

        PrideEnchantment.Bestow<Shady>(chosen, 1m, IsUpgraded);
    }
}
