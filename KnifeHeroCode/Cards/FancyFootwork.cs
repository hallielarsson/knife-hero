using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace KnifeHero.KnifeHeroCode.Cards;

/* Fancy Footwork — the flex card and the engine of The Gay Blade. The loop ("managing it is the
   game"): how you USE it decides which Flag Pet you feed.
     - PLAY it as an attack  -> deal damage, then summon/feed TOP   (+1 Strength while Top lives).
     - HOLD it to end of turn -> gain Block, then summon/feed BOTTOM (+1 Dexterity while Bottom lives).
   Top and Bottom are sword-pets that stand in front of you and eat hits (DieForYou). Each use grows
   the matching pet's HP and your matching stat; lean one pole for offense, the other for defense.
   It does NOT exhaust — it recycles through your discard so the loop keeps going.
   Human-sourced mechanic + art (Hallie); placeholder art via KnifeHeroCard. */
public sealed class FancyFootwork() : KnifeHeroCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const int FeedHp = 4;

    public override bool GainsBlock => true;
    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        new List<DynamicVar> { new DamageVar(6m, ValueProp.Move), new BlockVar(5m, ValueProp.Move) };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        await FeedTop();
    }

    public override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        await FeedBottom();
        // no Exhaust: it discards normally at end of turn and recycles, so the loop continues
    }

    private async Task FeedTop()
    {
        var top = CombatState.Creatures.FirstOrDefault(c => c.Monster is TopPet);
        if (top == null)
            top = await SummonPole<TopPet>();
        else
            await GrowPet(top);

        await PowerCmd.Apply<StrengthPower>(Owner.Creature, 1m, Owner.Creature, this);
        ((TopPet)top.Monster!).GrantedStrength += 1;
    }

    private async Task FeedBottom()
    {
        var bottom = CombatState.Creatures.FirstOrDefault(c => c.Monster is BottomPet);
        if (bottom == null)
            bottom = await SummonPole<BottomPet>();
        else
            await GrowPet(bottom);

        await PowerCmd.Apply<DexterityPower>(Owner.Creature, 1m, Owner.Creature, this);
        ((BottomPet)bottom.Monster!).GrantedDexterity += 1;
    }

    private async Task<Creature> SummonPole<T>() where T : MonsterModel
    {
        var pet = await PlayerCmd.AddPet<T>(Owner);
        await CreatureCmd.SetMaxHp(pet, FeedHp);
        await CreatureCmd.Heal(pet, FeedHp, false);
        await PowerCmd.Apply<DieForYouPower>(pet, 1m, null, this, true);
        return pet;
    }

    private static async Task GrowPet(Creature pet)
    {
        await CreatureCmd.SetMaxHp(pet, pet.MaxHp + FeedHp);
        await CreatureCmd.Heal(pet, FeedHp, false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
