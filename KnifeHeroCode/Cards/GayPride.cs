using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* GAY PRIDE — ⟨1⟩ Skill. Enchant an Attack in your hand with Proud: while it's in your hand, gain
   {Visibility} Visibility at the end of your turn. Upgraded: the enchanted Attack also gains Retain, so
   the flag stays in your hand and flies every turn.

   ── THE 2.0 SHAPE (Hallie, 2026-07-24) ──────────────────────────────────────────────────────────────
   A Pride is no longer a two-state held/swung card (see the retired PrideCard framework). It hands its
   held effect to another card as an ENCHANTMENT and leaves. Proud is Gay Pride's old HELD clause,
   verbatim. Playing it on an Attack is the whole card.

   Why this is better: the effect rides ON the target card with native UI — an icon, a hover tip, the
   text on the face — so the learning curve is atomic (you learn "Proud = bolt this onto a card" once).
   And it's HAND-ONLY and unplayable with no Attack in hand, which is the fun: sometimes you fly the flag
   on a sub-optimal blade, because that's what you're holding. See PrideEnchantment.cs.

   ⚠ Proud grants Visibility via PowerCmd directly, NOT through Stealth's found-you path, so DeadNamePower
   does NOT intercept it — visibility you chose, not visibility of being found. (Preserved from 1.0.) */
public sealed class GayPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "gay_pride.png".CardImagePath();
    public override string CustomPortraitPath => "gay_pride.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new IntVar("Visibility", 1m) };

    public override int MaxUpgradeLevel => 1;

    // The upgrade IS the Retain grant (applied in OnPlay via GrantsRetain); Visibility-per-turn stays 1.
    protected override void OnUpgrade() { }

    // Gloss what Proud does, on THIS card (so "Enchant an Attack with Proud" is legible in hand).
    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Proud>(1m);

    // Unplayable unless you own an Attack to fly the flag on (hand, draw, or discard).
    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;

        PrideEnchantment.Bestow<Proud>(chosen, DynamicVars["Visibility"].BaseValue, IsUpgraded);
    }
}
