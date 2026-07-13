using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Powers;

/* THE QUEER RIDER POOL — Hallie's design. Chassis + rider (QUEER_ENGINE_SPEC.md).

   Queering a card doesn't REPLACE it. It keeps its identity — a Strike is still a Strike — and bolts
   something on. Divergence by what's *added*, not by substitution. That's how the normative comes back
   other without becoming a different card.

   And the rider is **chosen at random from this pool**, which is the whole point and the thing that was
   missing: before, every queering attached the same poison rider, so every shiv came out identical.
   Identical is the opposite of the thesis. **Divergence by source IS the diversity.** Two shivs made the
   same way, one turn apart, should come out different.

   The engine only gives a CardModifier one real hook (OnPlay), so every rider is the same grammar —
   *when this card is played, also…* — which keeps them readable side by side while making each one feel
   like a different card.

   Numbers are // PROPOSAL. Tune by feel. */
public abstract class QueerRider : CardModifier
{
    protected abstract string Tag { get; }

    public override void ModifyDescription(Creature? target, ref string description)
        => description += $" Queer: {Tag}.";

    protected Creature? Me => Owner?.Owner?.Creature;
    protected Player? MyPlayer => Owner?.Owner;

    /* THE POOL. Add to it freely — that's the point of it. A queering picks one at random.
       (Poisoned stays first because it was the original rider and the specs reference it.) */
    public static QueerRider Random(Player player)
    {
        var pool = new Func<QueerRider>[]
        {
            () => (QueerRider)CardModifier.Get<Poisoned>().MutableClone(),
            () => (QueerRider)CardModifier.Get<Sharp>().MutableClone(),
            () => (QueerRider)CardModifier.Get<Loud>().MutableClone(),
            () => (QueerRider)CardModifier.Get<Exposed>().MutableClone(),
            () => (QueerRider)CardModifier.Get<Generous>().MutableClone(),
            () => (QueerRider)CardModifier.Get<Guarded>().MutableClone(),
            () => (QueerRider)CardModifier.Get<Sly>().MutableClone(),
        };
        var rng = player.RunState.Rng.CombatCardGeneration;
        return rng.NextItem(pool.ToList())();
    }
}


/* POISONED — the original rider. It lays poison on what it hits. */
public sealed class Poisoned : QueerRider
{
    protected override string Tag => "Poisoned";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemy = cardPlay.Target;
        if (enemy == null || enemy == Me) return;
        await PowerCmd.Apply<PoisonPower>(choiceContext, enemy, 2m, Me, null, false);
    }
}

/* SHARP — it just hits harder. The plainest rider in the pool, and sometimes that's what you want. */
public sealed class Sharp : QueerRider
{
    protected override string Tag => "Sharp";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemy = cardPlay.Target;
        if (enemy == null || enemy == Me) return;
        await DamageCmd.Attack(3m).FromCard(Owner!).Targeting(enemy)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }
}

/* LOUD — it takes the wind out of them. */
public sealed class Loud : QueerRider
{
    protected override string Tag => "Loud";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemy = cardPlay.Target;
        if (enemy == null || enemy == Me) return;
        await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 1m, Me, null, false);
    }
}

/* EXPOSED — it opens them up for whatever comes next. */
public sealed class Exposed : QueerRider
{
    protected override string Tag => "Exposed";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemy = cardPlay.Target;
        if (enemy == null || enemy == Me) return;
        await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, Me, null, false);
    }
}

/* GENEROUS — it gives you another card. The best rider to land on a shiv you were going to play anyway,
   because it makes the fistful of knives draw itself. */
public sealed class Generous : QueerRider
{
    protected override string Tag => "Generous";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (MyPlayer == null) return;
        await CardPileCmd.Draw(choiceContext, 1m, MyPlayer);
    }
}

/* GUARDED — it looks after you. A queered attack that also blocks is a small miracle in a deck with no
   spare hands. */
public sealed class Guarded : QueerRider
{
    protected override string Tag => "Guarded";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Me == null) return;
        await CreatureCmd.GainBlock(Me, new BlockVar(4m, ValueProp.Move), null);
    }
}

/* SLY — it hides you. Which is filthy on an attack, because attacking is the thing that normally BREAKS
   your Stealth (see Stealth.cs) — so a Sly queered attack hands the cover straight back to you.
   The only card in the game that lets you swing and stay hidden without Day of Invisibility. */
public sealed class Sly : QueerRider
{
    protected override string Tag => "Sly";

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Me == null) return;
        await PowerCmd.Apply<Stealth>(choiceContext, Me, 1m, Me, null, false);
    }
}
