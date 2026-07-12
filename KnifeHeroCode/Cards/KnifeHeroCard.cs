using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.Character;
using KnifeHero.KnifeHeroCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace KnifeHero.KnifeHeroCode.Cards;

[Pool(typeof(KnifeHeroCardPool))]
public abstract class KnifeHeroCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    // CRASH-SAFE DEFAULT: every card falls back to the shared placeholder art (card.png),
    // so a WIP / "unaligned" card with no drawing yet won't 404 the game. When a card is
    // finished, override these three to its own "<id>.png" (big: 1000x760, small: 250x190,
    // full-art: 606x852 / 250x350).
    public override string CustomPortraitPath => "card.png".BigCardImagePath();
    public override string PortraitPath => "card.png".CardImagePath();
    public override string BetaPortraitPath => "card.png".CardImagePath();

    // API DRIFT SHIM: the shipped game build dropped CardModel.CombatState (get_CombatState) — the
    // base getter throws MissingMethodException at runtime, which faults the card's play task and
    // HANGS the turn (godot.log 2026-07-06, Fancy Footwork). Route through the still-live path
    // (Creature.CombatState) instead. Shadows the vanished base member; all card CombatState.* uses
    // resolve here. If the engine restores CardModel.CombatState later, delete this.
    // NOTE: Creature.CombatState is ICombatState?, not the concrete CombatState class — typed
    // accordingly (2026-07-11 API re-verify against .decompiled).
    public new MegaCrit.Sts2.Core.Combat.ICombatState? CombatState => Owner.Creature.CombatState;
}