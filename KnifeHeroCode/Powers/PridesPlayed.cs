using MegaCrit.Sts2.Core.Entities.Powers;

namespace KnifeHero.KnifeHeroCode.Powers;

/* PRIDES PLAYED — a visible counter of how many Prides you have SWUNG this combat.

   This is the "played this combat" axis, and Hallie confirmed it as deliberate. The Gay Blade's payoffs
   count Prides on TWO different axes and they are in tension:

     • PLAYED this combat  (this power)  → Stonewall, Pride Parade.
       Cumulative and stable. It only ever goes UP. The wash cannot threaten it, and swinging a flag
       ADDS to it — so cashing out a Pride is not purely a loss.

     • IN YOUR HAND        (counted live) → Knife Block.
       Tactical and fragile. Swinging a flag REMOVES it from this count. The wash threatens it.

   So every Pride you swing simultaneously *feeds* one payoff and *starves* the other. That's the whole
   flag economy in one gesture, and it means "do I fly it or swing it" has a real answer that changes
   depending on which payoff is in your hand.

   And it is thematically exact: your pride, accumulating, as you put your flags into the world. It does
   not go back down. */
public sealed class PridesPlayed : KnifeHeroPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /* No hook here on purpose. A power can only observe AfterCardPlayed if it ALREADY EXISTS on the
       creature — so a self-counting power would never count the FIRST Pride, because it wouldn't exist
       yet. Instead, PrideCard's sealed OnPlay applies +1 of this every time any Pride is swung, which
       creates the power on the first swing and can never be forgotten by a subclass. */
}
