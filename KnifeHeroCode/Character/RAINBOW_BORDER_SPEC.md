# Rainbow card-border — spec (ready to build)

Goal (Hallie, earlier): the rainbow should be the **color of the card border/frame**, not painted on
the card art. Research done; the control is already in our own `KnifeHeroCardPool.cs`, just stubbed.

## How card frames work here
`CustomCardPoolModel` / `CardPoolModel` drive the frame for every card in the pool:
- **`H` / `S` / `V`** (in `KnifeHeroCardPool`, currently `1, 1, 1`): a hue/sat/brightness **shader**
  applied over the base frame image. This is a **single hue** — good for "all my cards are purple,"
  useless for a rainbow (a rainbow is many hues at once). Leave these neutral if we go the PNG route.
- **`CardFrameMaterialPath`** (abstract, `CardPoolModel.cs:22`): names a material
  `res://materials/cards/frames/<name>_mat.tres` (default examples like `card_frame_red`). A custom
  one could be a procedural rainbow **shader**, but it means authoring a `.gdshader` + `_mat.tres`
  and version-matching the resource — heavier.
- **`CustomFrame(CustomCardModel card)`** (already present, commented out in `KnifeHeroCardPool.cs`):
  return a `Texture2D` and it **replaces the frame image** for every card. This is the rainbow path.

## Recommended route: CustomFrame PNG (a drawn rainbow frame)
Simplest, and it fits the human-sourced-art principle — Hallie draws/owns the rainbow frame; we just
wire it. It's literally the path BaseLib documents in the stub.

### Build steps (when out of playtest)
1. Add `KnifeHero/images/cards/frame.png` — a rainbow version of the card frame. It must match the
   base frame's **dimensions and transparency** (the frame is a border overlay with a transparent
   center where the art shows). **Confirm the exact size first** by inspecting the base frame texture
   the default material uses (read `card_frame_red`'s texture in `materials/cards/frames/`), so the
   rainbow overlay registers pixel-for-pixel.
2. Uncomment the override in `KnifeHeroCardPool.cs`:
   ```csharp
   public override Texture2D CustomFrame(CustomCardModel card)
   {
       return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
   }
   ```
3. Keep `H = S = V = 1` (neutral) so the HSV shader doesn't tint the rainbow away.

### Placeholder option (if Hallie wants to see it before drawing)
Generate a stand-in rainbow frame procedurally with ImageMagick — but only AFTER step 1's geometry is
known (border thickness, corner radius, transparent center), or it won't line up. A naive rainbow
rectangle won't match the frame silhouette. So: get the base frame's shape first, then either Hallie
draws over it or we ImageMagick a gradient masked to that silhouette.

## If we later want it animated (shimmer/scroll)
Switch to the shader route: author a custom `_mat.tres` for `CardFrameMaterialPath` (or override
BaseLib's `CreateCustomFrameMaterial` on the card) with a hue-cycling gradient over UV.x. More work,
needs resource version-matching; only worth it if a static rainbow isn't enough.

## Status
Not built (no source change yet — the override stays commented until the frame PNG exists and Hallie
is out of the game). This spec + the existing stub mean it's a ~3-line wire-up once the art lands.
