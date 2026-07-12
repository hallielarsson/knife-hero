# Ironclad paintover templates — for the Gay Blade

Drawable templates for repainting the base game's **Ironclad** combat rig with the Gay Blade's art,
keeping his existing skeleton and animations (idle/attack/death/etc.). Background and the "why this is
possible at all" is in `SPINE_PAINTOVER.md` at the repo root — read that first if you haven't.

Source: `res://animations/characters/ironclad/ironclad.atlas` + `ironclad.png` + `ironclad_2/3/4.png` +
`ironclad.skel`, extracted read-only from the base game's `.pck` via GDRE Tools. The game install was not
modified. Nothing here touches the `.skel` (the skeleton/animation data) — only the four atlas pages, which
are pure pixel art.

## What's in this folder

| File | What it is |
|---|---|
| `ironclad_page1_main_1000x269.psd` | **Idle / base combat stance.** The one to start with — this is what's on screen most of the time and is the "he's already standing holding a sword" pose. |
| `ironclad_page2_attack_632x82.psd` | Attack-pose parts (limbs redrawn at attack foreshortening — these are separate art, not the idle art reused). |
| `ironclad_page3_death_260x153.psd` | Death-pose parts (a smaller, simplified part set). |
| `ironclad_page4_slash_placeholder_17x42.psd` | One tiny placeholder region. Probably not real art — see notes below. |
| `LEGEND.md` | Full region table for all four pages: ID, region name, pixel rect, rotation flag, and a body-part description I verified by eye against crops (not just guessed from the name). |
| `ironclad_page*_IDMAP_*x.png` | Flattened, upscaled (3–4×), **not for painting** — a big readable map so you can match the tiny numbered boxes on the native-size PSD guide layer to the legend table. The PSDs are native atlas resolution (as small as 17×42px), too small for on-canvas text labels to stay legible, so I split "outline + ID number" (on-canvas, in the PSD) from "what ID means" (in these maps + the legend). |

## Each PSD has exactly 3 layers, top to bottom

1. **`Guides - HIDE before export`** — thin outline rectangles per region (magenta = normal, orange =
   rotated) with a tiny yellow ID number in the corner. Cross-reference the ID against `LEGEND.md` or the
   ID-map PNG.
2. **`Paint Here`** — empty, transparent. This is the only layer that should have content when you export.
3. **`Reference (original Ironclad art) - HIDE before export`** — the real Ironclad pixels, for tracing /
   proportion reference. Hide or delete before export.

**Canvas size matches the atlas page exactly** (e.g. 1000×269 for the idle page). Hide both guide layers,
export the flat PNG (or just the "Paint Here" layer if you keep it fully opaque/self-contained), and it
drops back into the same pixel rectangle the game already expects — no rescaling, no repositioning.

I generated these with a script (pytoshop, not Photoshop) and verified structurally: correct layer names,
correct visibility flags, correct canvas size, and I alpha-composited all three layers back together to
confirm the guide boxes land exactly on the matching art (they do — I checked all four pages this way).
I was not able to open these in real Procreate/Photoshop to confirm import (no such app here) — if the
first one you open behaves oddly, tell me and I'll rebuild it a different way (e.g. flat PNG guide overlays
you import as reference photos instead of PSD layers is a fallback that will definitely work).

## Things that will bite you if you don't know about them

**1. Rotated regions are the big one.** Every region marked "Rotated: Y" in the legend (orange outline on
the guide layer) is stored in the atlas page turned 90° from how it displays in the game. **Paint it in the
orientation the guide box shows on the page, not the orientation you'd picture the body part in.** If you
paint a rotated region "upright" as if unrotated, it will display sideways in-game. On the idle page, 21 of
48 regions are rotated — including `sword blade`, `sword_handle`, most of the leg segments, and `bod` (the
main torso piece). This is genuinely the thing most likely to silently ruin a repaint, because a rotated
region can still *look* plausible as a piece of art when you're not thinking about orientation — you won't
get an error, you'll just get a sideways elbow in combat.

**2. This is a 4-page atlas, not 1 page.** Osty (the earlier test case) was a single page. Ironclad is four:
idle (1000×269), attack (632×82, all regions prefixed `attack/`), death (260×153, prefixed `death/`), and
one placeholder page. **If you only repaint the idle page, the character will revert to original Ironclad
art the instant an attack or death animation plays.** That may be an acceptable first milestone (get idle
working, ship attack/death later) — just don't be surprised by it. I built templates for all four so the
option is there when you're ready.

**3. `zaps1` / `zaps_2` (idle page) are lightning-squiggle special-attack VFX, not clothing** — leave them
red/orange or restyle them, but they're not "the Blade's outfit," they're a particle-style effect drawn as
a flat sprite. Same idea for `slash` / `slash2` (large white sword-slash trail shapes) and `shadow` (the
blurry grey ground-contact ellipse) — these are effects/utility art, not body parts, and probably don't need
the same fidelity as, say, the head or torso.

**4. `top mask` (idle page, ID 44) is genuinely ambiguous** — it visually resembles the `bod` torso region
(same leather-and-buckle read) but with a red mouth/warpaint stripe added, and I couldn't confirm from the
atlas alone whether it's plain visible art, a costume-variant swatch, or a shader color-mask. Worth a
five-minute check in a Godot scene view (or just paint it consistently with `bod` and see what happens)
before assuming either way.

**5. Alpha is NOT premultiplied.** Checked `ironclad.png.import`: `process/premult_alpha=false`. Paint
normal straight (non-premultiplied) alpha — you don't need to do anything special with edge colors on
semi-transparent pixels.

**6. No extra bleed/padding to worry about.** Regions are tightly packed with no shared borders between
unrelated parts — you're repainting *in place* at the exact same rectangle, not repacking the atlas, so you
don't need to add safety margin. The `offsets:` field some regions have in the raw `.atlas` file (visible if
you ever look at it directly) is trim metadata for the *original* Spine-authored artwork's bounding box —
it's not something you touch; the packed rectangle in the guide layer is the entire pixel budget you have.

**7. Multiple regions share very similar content** (e.g. `bod` on the idle page vs. `attack/bod attack` on
the attack page) but they are genuinely separate art, drawn at different foreshortening for the different
poses — repainting one does not repaint the other. Budget for that.

## What I did not do

- Did not touch `ironclad.skel` (the skeleton/animation binary) — untouched, unnecessary for a paintover.
- Did not modify the game install; extraction was read-only.
- Did not generate or alter any art myself.
- Did not attempt the actual re-import/repackaging step (spine-godot editor build, `dotnet publish`) — that's
  the separately-tracked open question in `SPINE_PAINTOVER.md` (whether upstream spine-godot's importer output
  is loadable by MegaCrit's runtime fork). This template work doesn't depend on that being solved yet — you
  can paint now and the repackaging path can be worked out in parallel.

Raw extracted atlas/skel/png (the source these templates were built from) are in scratchpad, not this repo:
`/private/tmp/claude-501/-Users-hallie-Documents-repos/7d1e52a9-8aab-40a9-b740-1167d5e01d6b/scratchpad/gdre/ironclad_test2/animations/characters/ironclad/`
