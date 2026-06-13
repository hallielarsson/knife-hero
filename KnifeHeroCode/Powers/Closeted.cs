using System.Threading.Tasks;
using KnifeHero.KnifeHeroCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace KnifeHero.KnifeHeroCode.Powers;

/* Closeted — the rider that makes The Closet's Stealth fragile. While you hold it, the moment you
   play an Attack you blow your cover: all Stealth is stripped and this watcher removes itself.
   Lives on The Closet only (NOT on Stealth generally), so Vanish-granted Stealth stays robust —
   it's specifically *being closeted* that breaks when you act out. Not an IFlag: it's a hidden
   condition, not a pride flag, so Rainbow Strike / Corporate Sponsored Pride don't count it. */
public sealed class Closeted : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        var stealth = Owner.GetPower<Stealth>();
        if (stealth != null) await PowerCmd.Remove(stealth);   // cover blown — lose all Stealth
        await PowerCmd.Remove(this);
    }
}
