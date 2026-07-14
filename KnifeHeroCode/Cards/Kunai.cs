using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;


/* Kunai — the THROWING SHIV. Hallie's unification (beat 2026-06-15): kunai and shiv are now one
   "throwing shiv". BASE_DAMAGE = 3.
     - Thrown now (played): a quick, light throw — deal BASE_DAMAGE - 2 (= 1).
     - Held to end of turn: you wind up and bury it — deal BASE_DAMAGE (= 3), then it Exhausts.
   So the patient throw hits harder; the snap throw is cheap chip. Numbers are Hallie's (explicit).
   Human-sourced mechanic (Hallie); art override is the Art Mapper's to land. */
public sealed class Kunai() : KnifeHeroCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IBlade
{
    /* ⚠ IT IS CALLED "THROWING SHIV" AND IT WAS NOT A SHIV.

       It carried IBlade but not CardTag.Shiv — so Poison Coating, Explosive Tip and Fan of Knives, all of
       which gate on `Tags.Contains(CardTag.Shiv)`, could not see it. The card whose NAME is Shiv was
       invisible to every card that cares about shivs, and nothing failed; it just quietly did less than
       the player had every reason to expect.

       Found by the agent reconciling card text against code. A display name is not a tag, and the game
       will never tell you the difference. */
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Shiv };

    public override string CustomPortraitPath => "card.png".BigCardImagePath();
    public override string PortraitPath => "card.png".CardImagePath();

    // The throwing shiv's one number, per Hallie's spec.
    private const decimal BaseDamage = 3m;

    // Card's listed damage is the SNAP-THROW value (played): BASE - 2.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(BaseDamage - 2m, ValueProp.Move) };

    public override bool HasTurnEndInHandEffect => true;

    // Thrown now: the light, snap throw — deal BASE - 2.
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // Held to end of turn: the buried throw — deal full BASE damage to the field, then Exhaust.
    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await DamageCmd.Attack(BaseDamage).FromCard(this).TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await CardCmd.Exhaust(choiceContext, this, causedByEthereal: false);
    }
}
