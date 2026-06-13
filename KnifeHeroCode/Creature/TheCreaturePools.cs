using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Extensions;
using Godot;

namespace KnifeHero.KnifeHeroCode.CreatureHero;

/* The Creature's pools. Second character in the same mod (Hallie's call), so it reuses the mod's
   placeholder energy icons / card base. Design authored by Claude; see THE_CREATURE/DESIGN.md. */
public class TheCreatureCardPool : CustomCardPoolModel
{
    public override string Title => TheCreature.CharacterId; // not a display name
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    // A green-ish card frame to read distinct from the Gay Blade's teal (tunable HSV over the base).
    public override float H => 0.32f;
    public override float S => 0.5f;
    public override float V => 0.85f;

    public override Color DeckEntryCardColor => new("ffffff");
    public override bool IsColorless => false;
}

public class TheCreatureRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => TheCreature.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}

public class TheCreaturePotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => TheCreature.Color;
    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}
