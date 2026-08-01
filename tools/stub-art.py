#!/usr/bin/env python3
"""stub-art.py — give every art-less card a placeholder portrait made from an emoji.

    ./tools/stub-art.py                 # stub every card with no portrait
    ./tools/stub-art.py --dry-run       # print what it WOULD do, touch nothing
    ./tools/stub-art.py --only Stabby   # one card, by class name
    ./tools/stub-art.py --force         # re-render stubs that already exist
    ./tools/stub-art.py --clean         # delete every stub + its overrides, back to card.png

A card with real art declares its own portrait:

    public override string PortraitPath => "labrys.png".CardImagePath();
    public override string CustomPortraitPath => "labrys.png".BigCardImagePath();

A card without one falls back to the shared `card.png` placeholder — so 65 of 70-odd cards are
currently the SAME grey rectangle, and you can't tell them apart while playtesting. This renders a
per-card emoji into the two portrait slots and writes those two override lines into the class, so
every card reads differently at a glance. It's scaffolding, not art: the manifest below tracks
exactly which portraits are stubs so real art can displace them and `--clean` can undo the lot.

── THE ZWJ TRAP ────────────────────────────────────────────────────────────────────────────────
Pillow here is built WITHOUT raqm, so it cannot shape ZWJ emoji sequences. 🏳️‍🌈 does not render as
a pride flag — it renders as a white flag AND a separate rainbow, side by side, spilling out of the
frame. Which is a genuinely bad thing to silently ship into a game about pride flags. So EMOJI is
validated at startup and any ZWJ sequence is a hard error, not a warning. Use 🌈 and friends: single
codepoints (optionally + VS16) shape correctly.

SAFE: writes PNGs under KnifeHero/images/card_portraits/ and inserts two lines per class. Does not
build or publish. New PNGs need a Godot import before the game sees them — run ./tools/dev.sh
publish (its export step imports), and re-run if a portrait shows as the old grey card.png.
"""
import argparse
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
PORTRAITS = ROOT / "KnifeHero/images/card_portraits"
LOC = ROOT / "KnifeHero/localization/eng/cards.json"
MANIFEST = PORTRAITS / "_stubs.json"
EMOJI_FONT = "/System/Library/Fonts/Apple Color Emoji.ttc"
FONT_SIZE = 160          # Apple Color Emoji is sbix: only 20/26/32/40/48/52/64/96/160 load at all.

BIG = (1000, 760)
SMALL = (250, 190)

CARD_RE = re.compile(r"class\s+(\w+)\(\)\s*:\s*(KnifeHeroCard|CreatureCard|PrideCard)\(")
TYPE_RE = re.compile(r"CardType\.(\w+)")
USING = "using KnifeHero.KnifeHeroCode.Extensions;"

# Backdrop tint per card type, so a stub also tells you what KIND of card it is at a glance.
TINTS = {
    "Attack": (58, 26, 32),
    "Skill": (24, 38, 54),
    "Power": (44, 28, 56),
    "Status": (40, 40, 40),
    "Curse": (30, 22, 30),
}
DEFAULT_TINT = (32, 26, 38)

# Hand-picked, one per card. Single codepoints only — see THE ZWJ TRAP above.
EMOJI = {
    "AllYouCanEat": "🍽️",       "BottomBlade": "🔽",         "BrickHammer": "🔨",
    "BrickShield": "🧱",         "HeadEmptyNoThoughts": "😶", "ChillTouch": "❄️",
    "DashingStrike": "💨",       "Backstab": "🗡️",           "ExtremelyOnline": "📱",
    "Faith": "🙏",               "FancyFootwork": "🔪",       "Feint": "🎭",
    "GayPride": "🌈",            "NecrobinderPride": "💀",    "Pin": "📌",
    "RegentPride": "👑",         "DykePride": "🪓",           "GayParris": "🤺",
    "KnifeBlock": "🛡️",         "BisexualLightning": "⚡",   "GayWrathMonth": "😤",
    "Solidarity": "🤝",          "KnifeToMeetU": "👋",        "GlowUp": "✨",
    "PrideWasARiot": "🚩",       "RainbowMatador": "🐂",      "ShivMagnet": "🧲",
    "PoisonCoating": "🧪",       "ExplosiveTip": "💥",        "SilentPride": "🤫",
    "Stonewall": "🏛️",          "TopChop": "🔝",             "Vanish": "🫥",
    "Visibility": "👁️",         "Honeypot": "🍯",            "SmokeBomb": "💣",
    "ShadowDodge": "🌑",         "GoToGround": "🕳️",         "LookWhatIFoundDownHere": "🔦",
    "DayOfInvisibility": "👻",   "Pickpocket": "🤏",          "DeadName": "⚰️",
    "IntoTheStreets": "🚶",      "TheCloset": "🚪",           "Flank": "↔️",
    "SneakAttack": "🥷",         "Assassin": "🎯",            "SmokeBombKnives": "🌫️",
    "OpenBook": "📖",            "Marginalia": "✏️",          "Polymath": "🧠",
    "Galvanism": "🔌",           "Solitude": "🕯️",           "Wretchedness": "🥀",
    "FireStolen": "🔥",          "Recombinant": "🧬",         "QuoteAtLength": "💬",
    "BecomeWhoYouAre": "🦋",     "DontLookAway": "👀",        "ReadTheRemainder": "📜",
    "Wallow": "🌊",              "Keening": "💔",             "LetItRot": "🍄",
    "TheCharnelHouse": "🏚️",    "TheAppetite": "🦷",
}

# Fallbacks for cards added after this table was written, matched against title + description.
# First hit wins, so put the specific words before the generic ones.
KEYWORDS = [
    ("shiv", "🔪"), ("knife", "🔪"), ("blade", "🗡️"), ("stab", "🗡️"),
    ("brick", "🧱"), ("block", "🛡️"), ("pride", "🌈"), ("rainbow", "🌈"),
    ("stealth", "🫥"), ("visib", "👁️"), ("shadow", "🌑"), ("poison", "🧪"),
    ("lesson", "📖"), ("book", "📖"), ("grief", "💔"), ("heart", "🫀"),
    ("energy", "⚡"), ("fire", "🔥"), ("heal", "💚"), ("draw", "🃏"),
]
FALLBACK = "🔪"


def snake(name: str) -> str:
    """BrickHammer -> BRICK_HAMMER (loc key), brick_hammer (slug)."""
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).upper()


def check_emoji() -> None:
    """A ZWJ sequence renders as two overlapping glyphs without raqm. Refuse to ship that."""
    bad = {c: e for c, e in EMOJI.items() if "‍" in e}
    if bad:
        print("ZWJ emoji cannot render without raqm — pick single-codepoint emoji for:", file=sys.stderr)
        for cls, e in bad.items():
            print(f"  {cls}: {e!r}", file=sys.stderr)
        sys.exit(1)


def pick(cls: str, title: str, text: str) -> str:
    if cls in EMOJI:
        return EMOJI[cls]
    hay = f"{title} {text}".lower()
    for word, emoji in KEYWORDS:
        if word in hay:
            return emoji
    return FALLBACK


def render(emoji: str, tint: tuple, dest_big: pathlib.Path, dest_small: pathlib.Path) -> None:
    from PIL import Image, ImageDraw, ImageFont

    font = ImageFont.truetype(EMOJI_FONT, FONT_SIZE)

    # Draw the glyph large on a transparent square, then crop to its ink so every emoji ends up
    # optically the same size regardless of how much padding its own artwork carries.
    scratch = Image.new("RGBA", (FONT_SIZE * 2, FONT_SIZE * 2), (0, 0, 0, 0))
    ImageDraw.Draw(scratch).text(
        (FONT_SIZE, FONT_SIZE), emoji, font=font, embedded_color=True, anchor="mm")
    box = scratch.getbbox()
    if box is None:
        raise SystemExit(f"emoji {emoji!r} rendered as nothing — is it in Apple Color Emoji?")
    glyph = scratch.crop(box)

    big = Image.new("RGBA", BIG, (*tint, 255))

    # Faint diagonal hatching, so a stub never gets mistaken for finished art in a screenshot.
    hatch = ImageDraw.Draw(big)
    lighter = tuple(min(255, c + 10) for c in tint)
    for x in range(-BIG[1], BIG[0], 48):
        hatch.line([(x, BIG[1]), (x + BIG[1], 0)], fill=(*lighter, 255), width=14)

    target_h = int(BIG[1] * 0.62)
    scale = target_h / glyph.height
    glyph = glyph.resize((max(1, int(glyph.width * scale)), target_h), Image.LANCZOS)
    big.alpha_composite(glyph, ((BIG[0] - glyph.width) // 2, (BIG[1] - glyph.height) // 2))

    dest_big.parent.mkdir(parents=True, exist_ok=True)
    dest_small.parent.mkdir(parents=True, exist_ok=True)
    big.convert("RGB").save(dest_big)
    big.convert("RGB").resize(SMALL, Image.LANCZOS).save(dest_small)


def patch(path: pathlib.Path, cls: str, slug: str) -> bool:
    """Insert the two portrait overrides at the top of the class body. Returns False if already there."""
    text = path.read_text()
    m = next((m for m in CARD_RE.finditer(text) if m.group(1) == cls), None)
    if m is None:
        return False

    # The class body opens with a `{` on its own line after the declaration (possibly after an
    # interface list). Find it, and insert directly below so the overrides lead the class.
    brace = text.index("{", m.end())
    line_end = text.index("\n", brace) + 1

    lines = (
        f'    public override string PortraitPath => "{slug}.png".CardImagePath();\n'
        f'    public override string CustomPortraitPath => "{slug}.png".BigCardImagePath();\n'
        f'\n'   # keep a blank line so the overrides don't fuse onto the next member's comment
    )
    text = text[:line_end] + lines + text[line_end:]

    # CardImagePath/BigCardImagePath are extension methods; the file needs their namespace.
    if USING not in text:
        usings = list(re.finditer(r"^using .+;$", text, re.M))
        if usings:
            at = usings[-1].end() + 1
            text = text[:at] + USING + "\n" + text[at:]

    path.write_text(text)
    return True


def unpatch(path: pathlib.Path, slug: str) -> None:
    text = path.read_text()
    text = re.sub(
        rf'^\s*public override string (?:Custom)?PortraitPath => "{re.escape(slug)}\.png"'
        rf'\.(?:Big)?CardImagePath\(\);\n', "", text, flags=re.M)
    path.write_text(text)


def cards() -> list:
    """Every card class, with the chunk of source it owns (used to spot an existing portrait)."""
    loc = json.loads(LOC.read_text())
    found = []
    for cs in sorted(ROOT.glob("KnifeHeroCode/**/*.cs")):
        text = cs.read_text(encoding="utf-8", errors="replace")
        matches = list(CARD_RE.finditer(text))
        for i, m in enumerate(matches):
            end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            chunk = text[m.start():end]
            key = f"KNIFEHERO-{snake(m.group(1))}"
            ctype = TYPE_RE.search(chunk)
            found.append({
                "cls": m.group(1),
                "file": cs,
                "has_art": "PortraitPath" in chunk,
                "type": ctype.group(1) if ctype else None,
                "title": loc.get(f"{key}.title", m.group(1)),
                "text": loc.get(f"{key}.description", ""),
            })
    return found


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", metavar="CLASS")
    ap.add_argument("--force", action="store_true", help="re-render stubs that already exist")
    ap.add_argument("--clean", action="store_true", help="remove every stub and its overrides")
    args = ap.parse_args()

    check_emoji()
    stubs = json.loads(MANIFEST.read_text()) if MANIFEST.exists() else {}

    if args.clean:
        for cls, slug in list(stubs.items()):
            card = next((c for c in cards() if c["cls"] == cls), None)
            if args.dry_run:
                print(f"would unstub {cls} ({slug})")
                continue
            if card:
                unpatch(card["file"], slug)
            (PORTRAITS / f"{slug}.png").unlink(missing_ok=True)
            (PORTRAITS / "big" / f"{slug}.png").unlink(missing_ok=True)
            del stubs[cls]
            print(f"unstubbed {cls}")
        if not args.dry_run:
            MANIFEST.write_text(json.dumps(stubs, indent=2, ensure_ascii=False) + "\n")
        return 0

    todo = []
    for c in cards():
        if args.only and c["cls"] != args.only:
            continue
        # A card with real art is never touched. A card we stubbed before is only redone on --force.
        if c["has_art"] and not (args.force and c["cls"] in stubs):
            continue
        todo.append(c)

    if not todo:
        print("nothing to stub — every card has a portrait ✅")
        return 0

    for c in todo:
        slug = snake(c["cls"]).lower()
        emoji = pick(c["cls"], c["title"], c["text"])
        already = c["cls"] in stubs
        print(f"{'would stub' if args.dry_run else 'stub'}  {emoji}  {c['title']:32} "
              f"-> {slug}.png{'  (re-render)' if already else ''}")
        if args.dry_run:
            continue

        render(emoji, TINTS.get(c["type"], DEFAULT_TINT),
               PORTRAITS / "big" / f"{slug}.png", PORTRAITS / f"{slug}.png")
        if not already:
            patch(c["file"], c["cls"], slug)
        stubs[c["cls"]] = slug

    if args.dry_run:
        print(f"\n{len(todo)} cards would be stubbed (nothing written)")
        return 0

    MANIFEST.write_text(json.dumps(stubs, indent=2, ensure_ascii=False) + "\n")
    print(f"\nstubbed {len(todo)} cards -> {PORTRAITS.relative_to(ROOT)}")
    print("manifest:", MANIFEST.relative_to(ROOT))
    print("NOTE: new PNGs need a Godot import before the game shows them — ./tools/dev.sh publish")
    return 0


if __name__ == "__main__":
    sys.exit(main())
