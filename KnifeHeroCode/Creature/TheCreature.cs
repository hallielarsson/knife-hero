using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using KnifeHero.KnifeHeroCode.CreatureHero.Cards;
using KnifeHero.KnifeHeroCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace KnifeHero.KnifeHeroCode.CreatureHero;

/* The Creature — the disclosed-AI sibling to The Gay Blade. DESIGN AUTHORED BY CLAUDE (see
   THE_CREATURE/DESIGN.md): a being assembled from many parts that reads books and learns things.
   A power-based deck with two axes — Lessons (depth) and assemblage (breadth). Art, Gender/voice,
   and final tuning remain Hallie's to mint; placeholder art reuses the Blade's for now. */
public class TheCreature : PlaceholderCharacterModel
{
    public const string CharacterId = "TheCreature";

    public static readonly Color Color = new("9be19b"); // pale green

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral; // Hallie's call
    public override int StartingHp => 72;

    // You start with ONE part (a Throbbing Heart — the body that begins as a curse) plus a normal
    // backbone: Recite IS your Strike (deal 6, tagged Strike), Annotate IS your Defend (gain 5, tagged
    // Defend). DECIDED (bro, design owner of The Creature, 2026-06-15): one Heart, not two — two
    // Eternal+Retain curses, each spawning a Vexing Memory, clog the hand and read as oppressive
    // ("throbbing heart used three times"). One is enough to teach the grieve-and-learn loop; the run
    // grows you more parts. The backbone is wider (4 Strikes / 4 Defends) so the deck has plenty of
    // real Strikes and Defends to read and play.
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<ThrobbingHeart>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<OpenBook>(),
        ModelDb.Card<Marginalia>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BurningBlood>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<TheCreatureCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TheCreatureRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TheCreaturePotionPool>();

    // Placeholder character art — reuses the same template assets as the Blade for now (Hallie draws
    // the Creature's real look later). PlaceholderCharacterModel supplies a placeholder body.
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
