namespace KnifeHero.KnifeHeroCode.Cards;

/* Blade — a marker classifier for the knife cards. Does NOTHING on its own; it exists purely so
   other cards can key off "is this a Blade?" (Portal to the Knife Dimension copies a Blade; future
   blade-synergy cards can read it too).

   Why an interface and not a keyword/tag: Hallie asked for "a keyword à la Ethereal that we can key
   off of." The engine won't allow it — CardTag (None/Strike/Defend/Minion/OstyAttack/Shiv) and
   CardKeyword are BOTH closed enums baked into the game; a mod can't add a value. So we mark Blades
   exactly the way we mark Flags (IFlag): a marker interface. Cost: it's invisible to the player (no
   "Blade." printed on the card). If we later want it shown, that's separate loc/UI work; the keying
   logic works today. To make any card a Blade, add ", IBlade" to its class declaration. */
public interface IBlade { }

/* IFlagBlade — a retained "flag-blade" card (Top, Bottom, and the pride swords). Held in hand, it's
   one of your Flags (counts in FlagCount). The flags are swords you carry, not just pets. */
public interface IFlagBlade { }
