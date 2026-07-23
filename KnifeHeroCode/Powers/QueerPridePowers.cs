using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* GAY WRATH MONTH — at the end of your turn, gain `Amount` Vigor per Pride or Queer card in your hand. */
public sealed class GayWrathPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner)) return;
        int n = CardPile.GetCards(Owner.Player, PileType.Hand).Count(c => c is IPride || QueerMod.IsQueer(c));
        if (n > 0) await PowerCmd.Apply<VigorPower>(choiceContext, Owner, Amount * n, Owner, null, false);
    }
}

/* SOLIDARITY — whenever you play a Queer or Pride card, gain `Amount` Block. */
public sealed class SolidarityPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (!(cardPlay.Card is IPride || QueerMod.IsQueer(cardPlay.Card))) return;
        await CreatureCmd.GainBlock(Owner, new BlockVar(Amount, ValueProp.Move), null);
    }
}

/* SMOKE BOMB (KNIVES) — one-shot: at the end of your turn, exhaust every Shiv in hand and gain 1 Stealth
   each, then vanish. BeforeSideTurnEnd so the Shivs are still in hand (before the end-of-turn flush). */
public sealed class SmokeKnivesPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner)) return;
        var shivs = CardPile.GetCards(Owner.Player, PileType.Hand)
            .Where(c => c.Tags.Contains(CardTag.Shiv)).ToList();
        foreach (var shiv in shivs)
            await CardCmd.Exhaust(choiceContext, shiv, causedByEthereal: false);
        if (shivs.Count > 0)
            await PowerCmd.Apply<Stealth>(choiceContext, Owner, shivs.Count, Owner, null, false);
        await PowerCmd.Remove(this);
    }
}

/* KNIFE TO MEET U — when you draw a Shiv, draw a card. (A drawn non-Shiv doesn't re-trigger, so a run of
   Shivs chains and then stops.) */
public sealed class KnifeToMeetUPower : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != Owner.Player || !card.Tags.Contains(CardTag.Shiv)) return;
        await CardPileCmd.Draw(choiceContext, 1m, Owner.Player);
    }
}
