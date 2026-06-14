#!/usr/bin/env bash
# dev.sh — workflow helpers for the knife-hero mod.
# Authored by Claude (Bro) at Hallie's ask: "make some utils for things you're doing a bunch."
# Run from the repo root:  ./tools/dev.sh <command>
#
# NOTE: `build`, `publish`, and `ship` reload the mod (they touch the .dll/.pck the game loads).
# NEVER run those while Hallie is live-playtesting. `check` and `power-icon` are always safe.
set -euo pipefail
cd "$(dirname "$0")/.."

DOTNET="$HOME/.dotnet/dotnet"
# MegaDot's headless --export-pack shells out to `dotnet` and needs the runtime via hostfxr. If
# `dotnet` isn't on PATH / DOTNET_ROOT isn't set, the export CRASHES (signal 11) but `dotnet publish`
# still exits 0 — so a publish silently no-ops and the .pck is never refreshed. Export these so the
# Godot subprocess can always find .NET. (Learned the hard way 2026-06-13.)
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
SLN="KnifeHero.sln"
LOC="KnifeHero/localization/eng"
ICONS="KnifeHero/images/powers"
PCK="$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/mods/KnifeHero/KnifeHero.pck"

cmd="${1:-help}"
case "$cmd" in
  check)   # validate every loc JSON — catches the trailing-comma bug that breaks game startup
    ok=1
    for f in "$LOC"/*.json; do
      if python3 -c "import json,sys; json.load(open('$f'))" 2>/dev/null; then echo "OK   $f";
      else echo "BAD  $f  <-- invalid JSON"; ok=0; fi
    done
    [ "$ok" = 1 ] || { echo "JSON validation FAILED"; exit 1; } ;;

  build)   # compile-check only (errors/warnings)
    "$DOTNET" build "$SLN" -c Debug 2>&1 | grep -iE "error CS|error STS|STS004|Build succeeded|Build FAILED|Error\(s\)" || true ;;

  publish) # validate JSON -> build -> publish the baked .pck -> VERIFY it actually refreshed
    "$0" check
    "$DOTNET" build "$SLN" -c Debug 2>&1 | grep -iE "error CS|error STS|Build FAILED|Error\(s\)" || true
    before=$(stat -f %m "$PCK" 2>/dev/null || echo 0)
    out=$("$DOTNET" publish "$SLN" -c Debug 2>&1)
    echo "$out" | grep -iE "Exporting Godot|Build FAILED|error CS|error STS" | tail -3 || true
    after=$(stat -f %m "$PCK" 2>/dev/null || echo 0)
    if echo "$out" | grep -qiE "signal 11|Abort trap|exited with code 134|command not found"; then
      echo "!! EXPORT CRASHED — .pck NOT refreshed. Likely dotnet/DOTNET_ROOT missing for the Godot subprocess." >&2
      exit 1
    fi
    if [ "$after" = "$before" ]; then
      echo "!! .pck mtime unchanged ($PCK) — export silently no-opped. Not published." >&2
      exit 1
    fi
    ls -la "$PCK" | awk '{print "pck refreshed:", $6, $7, $8}' ;;

  power-icon) # placeholder power icon (small + big):  ./tools/dev.sh power-icon vexing_memory
    name="${2:?usage: power-icon <snake_case_name>}"
    cp "$ICONS/stealth.png" "$ICONS/$name.png"
    cp "$ICONS/big/stealth.png" "$ICONS/big/$name.png"
    echo "created placeholder $name.png (+ big/)" ;;

  ship)    # publish + git commit:  ./tools/dev.sh ship "message"
    msg="${2:?usage: ship \"commit message\"}"
    "$0" publish
    git add -A
    git commit -q -m "$msg

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
    git log --oneline -1 ;;

  *) cat <<'USAGE'
dev.sh — knife-hero workflow helpers

  check               validate all loc JSON (always safe; run before any publish)
  build               compile-check, errors/warnings only
  publish             check + build + publish the .pck, show timestamp
  power-icon <name>   create a placeholder power icon (name.png + big/name.png)  [safe]
  ship "<msg>"        publish + git commit

SAFE anytime: check, power-icon.
RELOADS the mod (never while Hallie is playtesting): build, publish, ship.
USAGE
    ;;
esac
