using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Extensions;
using KnifeHero.KnifeHeroCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using MegaCrit.Sts2.Core.Models;

namespace KnifeHero.KnifeHeroCode.Cards;

/* FINGER GUNS — Bisexual Pride. Hallie, 2026-07-12.

     HELD:  at the end of your turn, deal 4 damage twice.
     SWUNG: the next attack you play this turn is played twice.

   **Both, or either.** That's the joke and it's also the mechanic, and it makes this the cleanest Pride
   in the deck: the two halves aren't a tradeoff between different things, they're the *same* thing —
   doubling — pointed at two different targets. Hold it and it doubles *itself*, forever, for free. Swing
   it and it doubles *something else*, once, hugely.

   So the question Finger Guns asks is the deck's question in its purest form: do you want a small thing
   repeatedly, or a big thing now? Fly it through a long fight and it out-damages almost anything. Or hold
   it until you draw the biggest attack you own and let it off twice.

   It is filthy with Fire (which grows as your Visibility climbs), with a fully-forged Top Chop's Vigor, and
   with Knife Whip against armour (each doubled swing shatters more Block into more Shivs). */
public sealed class FingerGuns() : PrideCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "finger_guns.png".CardImagePath();
    public override string CustomPortraitPath => "finger_guns.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(2m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

    /* HELD — at end of turn it fires twice, and firing from your hand blows your cover: you lose ALL
       Stealth and gain a Visibility. (This replaced a flat +1-energy tax on every card, which
       over-corrected Finger Guns from OP to UP.) You cannot point both hands at someone and stay hidden. */
    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        var enemy = CombatState.HittableEnemies.FirstOrDefault();
        if (enemy == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(2).FromCard(this)
            .Targeting(enemy).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);

        var stealth = Owner.Creature.GetPower<Stealth>();
        if (stealth != null) await PowerCmd.Remove(stealth);
        await PowerCmd.Apply<Visibility>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }

    // SWUNG — you point at the next thing you were going to do anyway, and it happens twice.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DoubleShot>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
