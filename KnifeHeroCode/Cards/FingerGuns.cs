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

   It is filthy with Fire (which grows as your Heat climbs), with a fully-forged Top Chop's Vigor, and
   with Knife Whip against armour (each doubled swing shatters more Block into more Shivs). */
public sealed class FingerGuns() : PrideCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "finger_guns.png".CardImagePath();
    public override string CustomPortraitPath => "finger_guns.png".BigCardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(2m, ValueProp.Move) };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

    /* THE REAL COST OF LEAVING IT OUT (Hallie, post-playtest 2026-07-12).
       Every card you play costs 1 more while Finger Guns is in your hand. Not a hand slot — an ENERGY
       TAX, on everything, every turn. You are standing there with both hands up, and both hands are busy.

       That's the honest price for a free engine, and it's better than the Heat I gave it: Heat only
       punished you eventually. This punishes you NOW, on every single card, for as long as you're doing
       the bit. */
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (Pile?.Type != PileType.Hand) return false;
        if (card == this || card.Owner != Owner) return false;
        modifiedCost = originalCost + 1m;
        return true;
    }

    /* HELD — it goes off twice, every turn. And it makes NOISE: +1 Heat each time it fires.
       (Hallie, 2026-07-12: "Finger Guns maybe should just increase Heat when it fires. It's not subtle.")

       That one line fixes the balance and the theme at once. It was a free engine — 8 damage a turn for
       nothing but a hand slot, which made it quietly the best card in the deck. Now it's a **timer you
       are winding**: every turn you leave it out, they get better at finding you, your Stealth gets
       thinner, and their hits get bigger.

       Which also makes it the fastest way to build Heat ON PURPOSE — so Finger Guns held is a Honeypot
       enabler (Thorns = Heat + 2), and Fire's damage climbs with it. The loud build wants this card out.
       The hidden build wants it swung and gone. */
    protected override async Task WhileFlown(PlayerChoiceContext choiceContext)
    {
        var enemy = CombatState.HittableEnemies.FirstOrDefault();
        if (enemy == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(2).FromCard(this)
            .Targeting(enemy).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    // SWUNG — you point at the next thing you were going to do anyway, and it happens twice.
    protected override async Task OnSwung(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DoubleShot>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
