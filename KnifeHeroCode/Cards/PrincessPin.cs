using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* PRINCESS PIN — Hallie's design (Gay Blade 2.0 sheet).

   FLY IT:   while it's in your hand, ALL damage to you is reduced by 1. Continuous, not end-of-turn.
   SWING IT: gain 6 (+U) Block, and 4 (+U) Thorns this turn — every attacker gets pricked.

   This is the card that shows what a Pride really is, because its flown effect is a **standing passive**,
   not a turn-end trigger. It is a pin you are WEARING. It works while you wear it. That's Sentinels
   equipment — a thing you have out on the table, doing its job, with no upkeep and no action cost.

   And it pairs viciously with STEALTH: Stealth caps every incoming hit at 1, and the Pin reduces damage
   by 1 — so while you're hidden AND wearing the Pin, incoming attacks are reduced to ZERO, your Block
   is never spent, and your Stealth never breaks. You are simply not there.

   ⁉️ The sheet says "all attackers take X+U damage this turn" and I could not read what X was. I've
   built it as Thorns 4 (+U) for now — Hallie's number to mint. */
public sealed class PrincessPin() : PrideCard(1, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override bool GainsBlock => true;

    public override int MaxUpgradeLevel => 99;

    // THE RETURN STROKE: the Pillow Princess is the BOTTOM — she cashes out into a Defend
    // (+1 more per retain level). Top begets Strikes; Princess begets Defends. That's the joke and
    // it's also the mechanic.
    protected override CardModel? Becomes() => CombatState.CreateCard<GayBladeDefend>(Owner);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new BlockVar(6m, ValueProp.Move) };

    // Plain consts for the power amount, same as Pin does for its Weak/Vulnerable.
    private decimal ThornsAmount => IsUpgraded ? 5m : 4m;

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    /* FLOWN — a standing passive, active the whole time it's in your hand. Not a trigger.
       (Same pattern as Butch Blade's "+1 to your attacks while held": override the Modify hook and gate
       on Pile?.Type == PileType.Hand. Cards in hand are live hook listeners.) */
    public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != null && target == Owner?.Creature && Pile?.Type == PileType.Hand)
            return -1m;
        return 0m;
    }

    // SWUNG — cash it out: armour up and make yourself sharp to touch.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, ThornsAmount,
            Owner.Creature, this, false);
    }
}
