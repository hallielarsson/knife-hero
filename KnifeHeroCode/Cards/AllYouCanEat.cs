using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using KnifeHero.KnifeHeroCode.Extensions;

namespace KnifeHero.KnifeHeroCode.Cards;

/* All You Can Eat — Exhaust all knives & shivs in your hand; gain HP for each. Healing past your
   max HP becomes Block instead.

   Hallie 2026-06-17: "the heal one should have a cap. Everything over the cap becomes block." The
   cap is your max HP (no overheal), and overflow -> Block — so it is never a dead heal: at low HP
   it mends, at full HP the meal becomes armor. Hand-only (decision-rich; whole-deck was the bigger
   swing we set aside). The card Exhausts itself. // PROPOSAL: 3 HP per blade eaten — tune by feel.

   Note (emergent): "knives" = IBlade. The Gay Blade Strike is IBlade *and* CardTag.Strike, so if
   one is in hand, the Queer curse will intercept its exhaust and queer it back to the deck instead
   of it being eaten — you still gain its HP (counted up front), but the Strike comes back queer.
   That interaction is left in on purpose; flag in playtest if it confuses. */
public sealed class AllYouCanEat() : KnifeHeroCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath => "all_you_can_eat.png".CardImagePath();
    public override string CustomPortraitPath => "all_you_can_eat.png".BigCardImagePath();

    // Upgrade: costs 1 less.
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    // PROPOSAL: HP gained per knife/shiv eaten.
    private const int PerBlade = 3;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new HashSet<CardKeyword> { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var me = Owner.Creature;

        // The meal: every knife (IBlade) and shiv (CardTag.Shiv) in hand, except this card.
        var food = CardPile.GetCards(Owner, PileType.Hand)
            .Where(c => c != this && (c is IBlade || c.Tags.Contains(CardTag.Shiv)))
            .ToList();
        if (food.Count == 0) return;

        foreach (var c in food)
            await CardCmd.Exhaust(choiceContext, c);

        decimal total = food.Count * PerBlade;
        decimal missing = me.MaxHp - me.CurrentHp;                 // can't overheal past max HP
        decimal healPart = total < missing ? total : missing;
        if (healPart < 0m) healPart = 0m;                          // already at/over max -> all Block
        decimal blockPart = total - healPart;

        if (healPart > 0m) await CreatureCmd.Heal(me, healPart);
        if (blockPart > 0m) await CreatureCmd.GainBlock(me, blockPart, ValueProp.Move, cardPlay);
    }
}
