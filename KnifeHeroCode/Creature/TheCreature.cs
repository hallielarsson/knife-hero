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

    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<Recite>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Recite>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<Annotate>(),
        ModelDb.Card<DontLookAway>(),   // the heart, present from turn one — reach back for your dead
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
