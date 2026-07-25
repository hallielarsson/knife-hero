using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace KnifeHero.KnifeHeroCode.Cards;

/* NECROBINDER PRIDE — ⟨1⟩ Power. Enchant an Attack in your hand with Slay: on retain (end of your turn,
   while it's in your hand), apply Doom to a random enemy equal to the number of Pride-enchanted cards in
   your hand. Upgraded: the enchanted Attack also gains Retain, so the flag keeps slaying every turn.

   The wide-hand payoff of the flag build: each Pride-enchanted card you're holding raises the Doom, and
   Doom kills anything whose HP is at or below its stacks at end of turn. Fly a fistful of flags and one
   random enemy is marked for death. See PrideEnchantment.cs (Bound). New for 2.0 (Hallie, 2026-07-24) —
   the retired "flags as weapons" spec had Necrobinder summoning an Osty pet; that is deprecated. */
public sealed class NecrobinderPride() : KnifeHeroCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;

    // The upgrade IS the Retain grant (applied in OnPlay via GrantsRetain).
    protected override void OnUpgrade() { }

    // Unplayable unless you own an Attack to slay with (hand, draw, or discard).
    protected override IEnumerable<IHoverTip> ExtraHoverTips => PrideEnchantment.TipFor<Slay>(0m);

    protected override bool IsPlayable => PrideEnchantment.HasEnchantableAttack(Owner, this);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var chosen = await PrideEnchantment.ChooseAttack(choiceContext, Owner, this);
        if (chosen == null) return;

        PrideEnchantment.Bestow<Slay>(chosen, 1m, IsUpgraded);
    }
}
