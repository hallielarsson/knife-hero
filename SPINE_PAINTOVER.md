# Spine paintover — can we put our art on an existing hero's skeleton?

**Question (Hallie, 2026-07-11):** can we extract enough rigging from the game's animation files to do
stand-in paintover assets on the existing skeletal animations — so the Gay Blade and the Creature get
real attack/idle/hit anims wearing *their own* art, without buying and learning Spine?

**Short answer: yes in principle, and the hard part is not the art — it's getting an importer.**
Don't spend the hour in the Godot GUI. It will fail for a reason we've now nailed down.

## What is CONFIRMED (done, not guessed)

| Step | Result |
|---|---|
| Extract atlas + skel + png from the base game | ✅ **works** |
| Atlas region map legible enough for an artist to paint into | ✅ **yes** |
| Runtime skin-swap to inject new art via code | ❌ **closed door** |
| Import raw `.atlas`/`.skel` using the extension the game ships | ❌ **impossible — see below** |
| Borrow an existing rig wholesale (the documented path) | ✅ known-working fallback |

**Extraction.** `Slay the Spire 2.pck` is an unencrypted Godot 4.5.1 pack. [GDRE Tools](https://github.com/GDRETools/gdsdecomp)
reconstructs Osty's rig into real source files at `res://animations/monsters/osty/`:
`osty.atlas` (ASCII), `osty.png` (1158×228 packed texture), `osty.skel` (binary skeleton), wired by
`osty_skel_data.tres`. Read-only; the game install was never touched.

**The atlas is human-legible — this was the make-or-break risk and it passed.** Osty's regions are named
`index1`, `index2`, `knuckle1`, `thumb2`, `webbing`, `shadow`, `glow`, `shockwave` … each with exact pixel
bounds and a rotation flag. It is *not* a scrambled bin-pack. An artist can open the png, read the atlas,
and know precisely which rectangle is which body part. **Repainting regions in place is a bounded art task.**

**Runtime skin-swap does NOT work.** `NCreatureVisuals.SetupSkins` → `MonsterModel.SetupSkins` uses
`spine.NewSkin(...)` + `data.FindSkin("...")`. `FindSkin` only finds skins **baked into that skeleton at
Spine-editor export time**. It lets a monster recombine costume variants the original artist authored; it
cannot inject new art at runtime. Verified against the API and 15+ call sites.

## THE BLOCKER (and it is not what it looked like)

A headless Megadot import of raw `.atlas`/`.skel` silently produces no `.import` files. That looked like a
headless quirk worth retesting in the GUI. **It isn't.** The shipped GDExtension at `res://addons/spine/`
declares three macOS libraries:

```
macos.editor  = "macos/libspine_godot.macos.editor.framework"
macos.debug   = "macos/libspine_godot.macos.template_debug.framework"
macos.release = "macos/libspine_godot.macos.template_release.framework"
```

All three framework bundles contain a binary literally named `libspine_godot.macos.template_release`, and
**all three are byte-identical** (`md5 5149b37964e0dfe1bdb00532f1e8cefc`). MegaCrit ships the *release
runtime* three times under three names. There is no editor build.

spine-godot's `.atlas`/`.skel` importers (`SpineAtlasResourceImportPlugin`,
`SpineSkeletonFileResourceImportPlugin`) are **editor-only**, compiled under `TOOLS_ENABLED`. They are not
in a release binary (`nm` finds zero `ImportPlugin` symbols). So **no Godot project using the shipped
extension can import raw spine files — headless or GUI.** The GUI test is a waste of an hour.

## THE PATH THAT SHOULD WORK

The gap is at **build time**, not run time. The game's *runtime* spine classes ship and work (that's how
Osty animates). We only lack the *importer*. And spine-godot is **open source** (Esoteric Software).

1. Get an **editor** build of the spine-godot GDExtension (upstream), matching **spine-cpp 4.1** and Godot
   **4.5.1** — the runtime rejects version mismatches, so this must line up.
2. Drop it into the mod's Godot project so Megadot can *import* `.atlas`/`.skel`.
3. Repaint the atlas png regions in place (same rectangles, same slots) with the Blade's / Creature's art;
   keep the original `.skel` untouched — same bones, same animation tracks.
4. `dotnet publish` bakes the imported resources into `KnifeHero.pck`. The game's shipped release runtime
   loads them.

**No Spine editor licence or training required** — we are repainting an existing rig, not authoring one.

**Next unknown, and it is the real one:** whether resources baked by *upstream* spine-godot's importer are
loadable by *MegaCrit's fork* of the runtime. If their fork diverged in the resource format, this breaks and
Spine-editor authoring becomes the honest ask. That's the thing to test next, and it's a much cheaper test
than the alternative.

## Evidence

- `scratchpad/gdre/osty_test/` — Osty's real atlas / png / skel, extracted
- `scratchpad/spine_import_test/` — the failed import experiment (now explained)

Investigated 2026-07-11 by a Claude subagent (extraction, atlas legibility, skin-swap API) and Fable
(the byte-identity finding that closed the question).
