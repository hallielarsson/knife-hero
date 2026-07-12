#!/usr/bin/env python3
"""sync.py — reconcile the DESIGN doc (CARDS.md) against what is actually IMPLEMENTED.

    ./tools/sync.py            # human report
    ./tools/sync.py --warn     # MSBuild-format warnings (wired into the build)
    ./tools/sync.py --strict   # exit 1 on any drift (for CI / pre-commit)

WHY THIS EXISTS
`CARDS.md` is Hallie's design doc — prose, intent, cards not yet built. The C# is the truth of what
runs. These two drift apart silently, and they did: CARDS.md was a 2026-06-13 snapshot describing 50
cards while 53 were actually compiled, including an entire Creature deck the doc barely mentioned.
Nobody noticed for a month.

A doc that silently disagrees with the code is worse than no doc, because people trust it. So: this
reconciles them and *warns*. It deliberately does NOT error by default — design SHOULD be allowed to
run ahead of implementation; that's what design is for. It just shouldn't do it invisibly.

WHAT IT CAN AND CANNOT CHECK
Reliably: existence (designed-not-built / built-not-designed), cost drift, rarity drift, and design
entries still carrying an unknown ⟨?⟩ cost.
NOT reliably: whether the *rules text* still matches. Design prose and shipped card text are different
registers and diffing them produces noise. So text is printed side-by-side for a human to eyeball, and
never auto-failed. Pretending to check it would be worse than admitting we can't.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DESIGN = ROOT / "CARDS.md"
LOC = ROOT / "KnifeHero/localization/eng/cards.json"

# - **Fancy Footwork** ⟨1⟩ [Common] — Deal 6, forge a Butch Blade…   *(⚠ HELD: …)*
DESIGN_RE = re.compile(
    r"^- \*\*(?P<name>[^*]+)\*\*\s*(?:⟨(?P<cost>[^⟩]*)⟩)?\s*(?:\[(?P<rarity>[^\]]*)\])?\s*(?:—\s*(?P<text>.*))?$"
)
CARD_RE = re.compile(
    r"class\s+(\w+)\(\)\s*:\s*(KnifeHeroCard|CreatureCard|PrideCard)\("
    r"\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)"
)


def snake(n):
    return re.sub(r"(?<!^)(?=[A-Z])", "_", n).upper()


def norm(n):
    """'Fancy Footwork' / 'FancyFootwork' -> 'fancyfootwork'"""
    return re.sub(r"[^a-z0-9]", "", n.lower())


# CARDS.md writes rarity and type in one bracket ("[Unc, Power]") and abbreviates. Normalise, or the
# report fills with format noise — and a warning that cries wolf gets muted, which is worse than none.
RARITY_ALIASES = {"unc": "uncommon", "com": "common", "basic": "basic", "token": "token",
                  "rare": "rare", "curse": "curse", "status": "status"}
NOT_A_RARITY = {"power", "skill", "attack", "retain", "eternal", "innate", "exhaust"}


def parse_rarity(field):
    """'[Unc, Power]' -> 'uncommon'. '[Retain, Eternal]' -> None (those are keywords, not a rarity)."""
    if not field:
        return None
    for part in (p.strip().lower() for p in field.split(",")):
        if part in NOT_A_RARITY:
            continue
        return RARITY_ALIASES.get(part, part)
    return None


def main():
    warn_mode = "--warn" in sys.argv
    strict = "--strict" in sys.argv

    loc = json.loads(LOC.read_text())

    # --- implemented (source is truth) ---
    impl = {}
    for cs in sorted(ROOT.glob("KnifeHeroCode/**/*.cs")):
        for m in CARD_RE.finditer(cs.read_text(encoding="utf-8", errors="replace")):
            cls, base, cost, ctype, rarity = m.groups()
            title = loc.get(f"KNIFEHERO-{snake(cls)}.title") or cls
            impl[norm(title)] = {
                "class": cls, "title": title, "cost": int(cost), "rarity": rarity,
                "base": base, "file": str(cs.relative_to(ROOT)),
                "text": loc.get(f"KNIFEHERO-{snake(cls)}.description", ""),
            }

    # --- designed (CARDS.md) ---
    designed = {}
    for i, line in enumerate(DESIGN.read_text().splitlines(), 1):
        m = DESIGN_RE.match(line.strip())
        if not m:
            continue
        d = m.groupdict()
        name = d["name"].strip()
        entry = {
            "name": name, "line": i,
            "cost": d["cost"].strip() if d["cost"] else None,
            "rarity": parse_rarity(d["rarity"]),
            "text": (d["text"] or "").strip(),
        }
        # A design line may carry alternate names: `Knife in Front / "Labrys Axe"`. Register each,
        # so a card the doc knows under one name and the game ships under another still matches.
        for alias in re.split(r"\s*/\s*", name):
            alias = alias.strip().strip('"').strip()
            if alias:
                designed.setdefault(norm(alias), entry)

    warnings = []

    def W(kind, msg, line=None):
        warnings.append((kind, msg, line))

    # Also index implemented cards by CLASS name, not just shipped title — a card renamed in loc
    # (class `Kunai`, ships as "Throwing Shiv") must still match the design entry that predates it.
    by_class = {norm(c["class"]): c for c in impl.values()}

    seen_impl = set()
    for k, d in sorted(designed.items(), key=lambda kv: kv[1]["line"]):
        c = impl.get(k) or by_class.get(k)
        if not c:
            W("designed-not-built", f"'{d['name']}' is in CARDS.md but has no card class", d["line"])
            continue
        if norm(c["title"]) in seen_impl:
            continue
        seen_impl.add(norm(c["title"]))
        if norm(d["name"]) != norm(c["title"]) and norm(d["name"]) == norm(c["class"]):
            W("renamed", f"'{d['name']}' ships in-game as '{c['title']}' — CARDS.md still uses the old name", d["line"])
        if d["cost"] in ("?", "", None):
            W("cost-undecided", f"'{d['name']}' has no design cost; code says ⟨{c['cost']}⟩ — copy it back?", d["line"])
        elif d["cost"].lstrip("-").isdigit() and int(d["cost"]) != c["cost"]:
            W("cost-drift", f"'{d['name']}': CARDS.md says ⟨{d['cost']}⟩, code says ⟨{c['cost']}⟩", d["line"])
        if d["rarity"] and d["rarity"] != c["rarity"].lower():
            W("rarity-drift", f"'{d['name']}': CARDS.md says [{d['rarity']}], code says [{c['rarity']}]", d["line"])

    for k, c in sorted(impl.items()):
        if norm(c["title"]) not in seen_impl:
            W("built-not-designed", f"'{c['title']}' ({c['class']}) is implemented but absent from CARDS.md", None)

    if warn_mode:
        for kind, msg, line in warnings:
            loc_s = f"CARDS.md({line})" if line else "CARDS.md"
            print(f"{loc_s}: warning CARDSYNC: [{kind}] {msg}")
        return 0

    # human report
    order = ["built-not-designed", "designed-not-built", "renamed", "cost-drift", "rarity-drift", "cost-undecided"]
    print(f"design entries: {len(designed)}   implemented: {len(impl)}\n")
    for kind in order:
        group = [w for w in warnings if w[0] == kind]
        if not group:
            continue
        print(f"── {kind}  ({len(group)})")
        for _, msg, line in group:
            print(f"   {'CARDS.md:%-4d' % line if line else ' ' * 13} {msg}")
        print()
    if not warnings:
        print("✅ design and implementation agree.")
    else:
        print(f"{len(warnings)} drift(s). Text/rules are NOT auto-checked — that needs eyes.")
    return 1 if (strict and warnings) else 0


if __name__ == "__main__":
    sys.exit(main())
