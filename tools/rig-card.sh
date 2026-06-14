#!/usr/bin/env bash
# rig-card.sh — fit a raw card drawing into the two card-portrait slots the game wants.
# Authored by Claude (Bro) at Hallie's ask for a reusable "rig" so raw art drops straight in.
#
#   ./tools/rig-card.sh <raw.png> <card_name>
#   e.g.  ./tools/rig-card.sh RawExport/Card/Strike.png gay_blade_strike
#
# Produces:
#   KnifeHero/images/card_portraits/<card_name>.png       (250x190)
#   KnifeHero/images/card_portraits/big/<card_name>.png   (1000x760)
# Center-crops to the card's 250:190 (≈1.3158) aspect so art is never squished — it fills and the
# overflow is trimmed evenly. SAFE: only writes PNGs; does NOT build/publish (run dev.sh publish for that).
set -euo pipefail
cd "$(dirname "$0")/.."

raw="${1:?usage: rig-card.sh <raw.png> <card_name>}"
name="${2:?usage: rig-card.sh <raw.png> <card_name>}"
[ -f "$raw" ] || { echo "no such file: $raw" >&2; exit 1; }

BIG_W=1000; BIG_H=760
SM_W=250;   SM_H=190
dest="KnifeHero/images/card_portraits"
big="$dest/big/$name.png"
small="$dest/$name.png"
mkdir -p "$dest/big"

rw=$(sips -g pixelWidth  "$raw" | awk '/pixelWidth/{print $2}')
rh=$(sips -g pixelHeight "$raw" | awk '/pixelHeight/{print $2}')

# Scale to COVER the big slot (so the shorter side fills), then center-crop to exact big size.
# cover scale s = max(BIG_W/rw, BIG_H/rh); pick the dimension that needs the bigger scale.
cover_w=$(awk -v rw="$rw" -v rh="$rh" -v bw="$BIG_W" -v bh="$BIG_H" 'BEGIN{ s=(bw/rw> bh/rh)?bw/rw:bh/rh; printf "%d", (rw*s)+0.5 }')
cover_h=$(awk -v rw="$rw" -v rh="$rh" -v bw="$BIG_W" -v bh="$BIG_H" 'BEGIN{ s=(bw/rw> bh/rh)?bw/rw:bh/rh; printf "%d", (rh*s)+0.5 }')

tmp=$(mktemp -t rigcard).png
cp "$raw" "$tmp"
sips -s format png "$tmp" --resampleHeightWidth "$cover_h" "$cover_w" >/dev/null   # cover (preserves aspect)
sips "$tmp" --cropToHeightWidth "$BIG_H" "$BIG_W" >/dev/null                        # center-crop to big
cp "$tmp" "$big"
sips "$tmp" --resampleHeightWidth "$SM_H" "$SM_W" >/dev/null                        # derive small from cropped big
cp "$tmp" "$small"
rm -f "$tmp"

echo "rigged '$name':"
echo "  big   -> $big   ($(sips -g pixelWidth -g pixelHeight "$big"   | awk '/pixelWidth/{w=$2}/pixelHeight/{print w"x"$2}'))"
echo "  small -> $small ($(sips -g pixelWidth -g pixelHeight "$small" | awk '/pixelWidth/{w=$2}/pixelHeight/{print w"x"$2}'))"
echo "(not published — run ./tools/dev.sh publish when Hallie is at a stopping point)"
