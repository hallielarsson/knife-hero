using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Labrys — the parry-weapon forged by Dyke Pride (FLAGS_AS_WEAPONS_SPEC.md). A Retain IFlagBlade
   that drinks a blow and grows heavier.

   PROPOSAL (Claude, Pathetic Governor 2026-06-15): the spec's RESOLVED parry mechanism, built
   net-new (additive — touches no existing Power, dissolves nothing; the big PridePowers→blades
   conversion stays HELD for Hallie's batched pass). Implements reading **A — primed parry (held)**,
   the spec's stated default: while Labrys sits in your hand, the NEXT instance of HP loss is voided;
   the axe banks that amount as permanent attack and then goes to your discard. Draw it again and it's
   sharper. Numbers (base 6) are Hallie's to mint.

   Engine basis: BufferPower (StS "Buffer") is the template — both hooks are virtual on AbstractModel,
   so the card itself does the work, no separate power:
     - ModifyHpLostAfterOstyLate: if WE are the target and we're in hand, stash the amount, return 0m
       (the last gate before HP loss — Osty/Block already applied).
     - AfterModifyingHpLostAfterOsty: if a hit was stashed, grow attack by it, clear the stash, and
       move ourselves hand -> discard.
   (This is the damage-PREVENTION path; Femme's retaliate uses the sibling BeforeDamageReceived —
   different hook, don't conflate. See spec.) */
public sealed class Labrys() : KnifeHeroCard(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy), IBlade, IFlagBlade
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new List<CardKeyword> { CardKeyword.Retain };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move) };

    // Set when we absorb a hit this resolution, so AfterModifyingHpLostAfterOsty knows to bank+discard.
    private decimal _absorbed;

    // The parry: while Labrys is in hand and WE'd take HP loss, void the next instance and stash it.
    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (amount > 0m && target == Owner?.Creature && Pile?.Type == PileType.Hand && _absorbed == 0m)
        {
            _absorbed = amount;   // the axe catches the blow
            return 0m;            // ...and you take none of it
        }
        return amount;
    }

    // Bank what we caught as permanent attack, then the axe leaves your hand (to discard, heavier).
    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (_absorbed <= 0m) return;

        DynamicVars.Damage.UpgradeValueBy(_absorbed);   // drinks the hit; persists, rides one-copy upgrade
        _absorbed = 0m;
        await CardPileCmd.Add(this, PileType.Discard);  // spent the parry — back to discard, sharper
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // Re-forged (Dyke Pride again): the axe is heavier. +2 base damage.
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
