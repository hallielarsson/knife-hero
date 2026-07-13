using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* SILENT PRIDE — a Pride blade. Hallie, 2026-07-12: "Silent Pride can take the old knife whip ability."

     HELD:  gain 3 Block whenever you discard a card.
     SWUNG: deal 8, apply Weak, and put a Shiv in your discard — and this blade loses 1 damage, for good.

   It's the blade that spends itself. Every swing trades a point of the sword for a knife in the pile, so
   it gets weaker as your hand gets sharper. You are, slowly, turning your pride into ammunition.

   Held, it's the Silent's discard engine (the old Silent Pride power's job, now a thing you carry). The
   two halves want opposite things — holding it wants you discarding, swinging it wants you attacking —
   which is the Pride mechanic doing what it's for.

   The decay is permanent for the fight (UpgradeValueBy(-1)), so managing it IS the play: swing it early
   and often for a fistful of shivs and a blunt sword, or hold it and keep the edge.

   ⁉ FLAGGED — Hallie's original margin note also said Silent should "inflict Weak on what it hits", so
   both riders are on the swing. Numbers (8 damage, 3 Block, 1 Weak) are hers to mint. */
public sealed class SilentPride() : PrideCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(8m, ValueProp.Move), new BlockVar(3m, ValueProp.Move) };

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(1m);
    }

    // HELD — the Silent's discard engine. You're carrying it, so every card you throw away pays you.
    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Models.CardModel card)
    {
        if (Pile?.Type != PileType.Hand || card.Owner != Owner) return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }

    // SWUNG — deal, Weaken, and shed a knife. The blade gets duller; your discard gets sharper.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, 1m, Owner.Creature, this, false);

        var shiv = CombatState.CreateCard<Shiv>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(shiv, PileType.Discard, Owner);
        DynamicVars.Damage.UpgradeValueBy(-1m);
    }
}
