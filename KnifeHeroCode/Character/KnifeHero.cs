using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using KnifeHero.KnifeHeroCode.Cards;
using KnifeHero.KnifeHeroCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace KnifeHero.KnifeHeroCode.Character;

public class KnifeHero : PlaceholderCharacterModel
{
    public const string CharacterId = "KnifeHero";
    
    public static readonly Color Color = new("ffffff");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;
    
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<GayBladeStrike>(),
        ModelDb.Card<GayBladeStrike>(),
        ModelDb.Card<GayBladeStrike>(),
        ModelDb.Card<GayBladeDefend>(),
        ModelDb.Card<GayBladeDefend>(),
        ModelDb.Card<GayBladeDefend>(),
        ModelDb.Card<FancyFootwork>(),
        ModelDb.Card<FancyFootwork>(),
        ModelDb.Card<FancyFootwork>(),
        ModelDb.Card<FancyFootwork>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BurningBlood>()
    ];
    
    public override CardPoolModel CardPool => ModelDb.CardPool<KnifeHeroCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<KnifeHeroRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<KnifeHeroPotionPool>();
    
    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
        override all the other methods that define those assets. 
        These are just some of the simplest assets, given some placeholders to differentiate your character with. 
        You don't have to, but you're suggested to rename these images. */
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