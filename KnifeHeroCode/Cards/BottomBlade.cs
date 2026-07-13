using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* BOTTOM BLADE — Hallie's design, 2026-07-12. Forged by HOLDING a Switch Blade to end of turn.
   (Was "Princess Pin" / "Pillow Princess". Renamed 2026-07-12.)

     Retain. Gain (2 × forge level) Block.

   The mirror of Top Chop. The Top gives you ATTACK; the Bottom gives you BLOCK. Play the Switch Blade
   and you get a Top; hold it and you get a Bottom. **The card is a switch.**

   Retain, same as the Top: carry it until the turn you need the wall. Re-forging raises its level, and
   the level is the payout.

   ⁉ FLAGGED — same ambiguity as Top Chop: built as **2 × (forgeLevel + 1)**, so a fresh Bottom gives
   2 Block, a once-re-forged one 4. One line to change. */
public sealed class BottomBlade() : PrideCard(1, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override bool GainsBlock => true;

    public override int MaxUpgradeLevel => 99;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain, CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(2m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);

    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
}
