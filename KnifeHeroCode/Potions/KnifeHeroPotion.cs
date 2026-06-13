using BaseLib.Abstracts;
using BaseLib.Utils;
using KnifeHero.KnifeHeroCode.Character;

namespace KnifeHero.KnifeHeroCode.Potions;

[Pool(typeof(KnifeHeroPotionPool))]
public abstract class KnifeHeroPotion : CustomPotionModel;