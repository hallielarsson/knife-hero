using BaseLib.Abstracts;
using KnifeHero.KnifeHeroCode.Extensions;
using Godot;

namespace KnifeHero.KnifeHeroCode.Character;

public class KnifeHeroRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => KnifeHero.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}