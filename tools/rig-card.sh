#!/usr/bin/env bash
# rig-card.sh — fit a raw card drawing into the two card-portrait slots the game wants.
# Authored by Claude (Bro) at Hallie's ask for a reusable "rig" so raw art drops straight in.
#
#   ./tools/rig-card.sh [--fit|--cover] [--pad <color>] <raw.png> <card_name>
#
#   ./tools/rig-card.sh RawExport/Card/Strike.png gay_blade_strike        # fit (default)
#   ./tools/rig-card.sh --cover Untitled_Artwork.png finger_guns          # full-bleed painting
#   ./tools/rig-card.sh --pad '#F8E6D0' Gray505.png throbbing_heart       # plate on aged paper
#
# Produces:
#   KnifeHero/images/card_portraits/<card_name>.png       (250x190)
#   KnifeHero/images/card_portraits/big/<card_name>.png   (1000x760)
#
# MODES — the card slot is landscape (250:190 ≈ 1.316). Raw art usually isn't.
#   --fit   (DEFAULT) Scale the whole image to FIT inside the slot, then pad to size. Nothing is
#           ever cut off. Correct for anything that isn't full-bleed: a sword, a limb, an organ,
#           a figure with margin around it, any anatomical plate. Pads with --pad (default white).
#   --cover Scale to FILL the slot and center-crop the overflow. ONLY correct when the art already
#           reaches every edge and the edges are expendable (a full-bleed painted scene).
#
# WHY THE DEFAULT FLIPPED (2026-07-11): this script used to always --cover, which silently sliced
# the ends off any tall or centered art — it beheaded the Gray505 heart plate (cutting the severed
# vagus nerves and the cut aorta, i.e. the entire point of the image) and mangled several of
# Hallie's exports. Cover-crop destroys art; fit never does. So fit is the default and cover is now
# opt-in. If an old card looks wrong, re-rig it with the default.
#
# SAFE: only writes PNGs; does NOT build/publish (run ./tools/dev.sh publish for that).
set -euo pipefail
cd "$(dirname "$0")/.."

mode="fit"
pad="white"
while [[ "${1:-}" == --* ]]; do
    case "$1" in
        --fit)   mode="fit";   shift ;;
        --cover) mode="cover"; shift ;;
        --pad)   pad="${2:?--pad needs a color}"; shift 2 ;;
        *) echo "unknown flag: $1" >&2; exit 1 ;;
    esac
done

raw="${1:?usage: rig-card.sh [--fit|--cover] [--pad <color>] <raw.png> <card_name>}"
name="${2:?usage: rig-card.sh [--fit|--cover] [--pad <color>] <raw.png> <card_name>}"
[ -f "$raw" ] || { echo "no such file: $raw" >&2; exit 1; }
command -v magick >/dev/null || { echo "needs imagemagick (brew install imagemagick)" >&2; exit 1; }

BIG_W=1000; BIG_H=760
SM_W=250;   SM_H=190
dest="KnifeHero/images/card_portraits"
big="$dest/big/$name.png"
small="$dest/$name.png"
mkdir -p "$dest/big"

if [ "$mode" = "cover" ]; then
    # Fill the slot, center-crop the overflow. Destructive at the edges — use only on full-bleed art.
    magick "$raw" -resize "${BIG_W}x${BIG_H}^" -gravity center -extent "${BIG_W}x${BIG_H}" "$big"
else
    # Fit the whole image inside the slot, pad out to size. Nothing is ever cut off.
    magick "$raw" -resize "${BIG_W}x${BIG_H}" -background "$pad" -gravity center \
        -extent "${BIG_W}x${BIG_H}" "$big"
fi
magick "$big" -resize "${SM_W}x${SM_H}!" "$small"

echo "rigged '$name' [$mode${pad:+, pad=$pad}]:"
echo "  big   -> $big   (${BIG_W}x${BIG_H})"
echo "  small -> $small (${SM_W}x${SM_H})"
echo "(not published — run ./tools/dev.sh publish when Hallie is at a stopping point)"
