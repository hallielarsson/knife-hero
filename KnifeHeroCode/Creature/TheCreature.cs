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

/* The Creature — the second character. A power-based deck with two axes: Lessons (depth) and
   assemblage (breadth). Design: THE_CREATURE/DESIGN.md. Placeholder art reuses the Blade's. */
public class TheCreature : PlaceholderCharacterModel
{
    public const string CharacterId = "TheCreature";

    public static readonly Color Color = new("9be19b"); // pale green

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral; // Hallie's call
    public override int StartingHp => 72;

    /* ONE broken organ, and two Open Books — just enough Lessons to mend it if you draw them in time.
       The first fight is the tutorial for the mend-or-rot fork. Exactly one Part: two Eternal+Retain
       curses clog the hand and read as oppressive. Recite is the Strike and Annotate the Defend (4 each,
       so the deck has real basics). The Charnel House is in the pool, not the deck — taking on more of
       yourself should be a choice. */
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
        ModelDb.Card<OpenBook>()
    ];

    // Mended Body derives Grief/Wholeness from the deck and does all the bleeding and healing. The
    // character does not work without it.
    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<MendedBody>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<TheCreatureCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TheCreatureRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TheCreaturePotionPool>();

    // Placeholder character art — reuses the Blade's template assets for now.
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
