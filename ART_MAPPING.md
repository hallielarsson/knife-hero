# Art Mapping — Hallie's `Untitled_Artwork*` exports → knife-hero card portraits

Mapped by the Art Mapper (vision pass over every file in `~/Downloads`), 2026-06-15.
Confident matches were renamed into `~/Downloads/knife-hero-art-mapped/` and rigged into
`KnifeHero/images/card_portraits/` (+ `big/`) via `tools/rig-card.sh`. **No `.cs` files were touched** —
the governor wires `PortraitPath` overrides from the slugs below.

## The big finding
The ~69 exports are **not** all knife-hero card portraits. They fall into four buckets:

1. **A different card game (rhetoric / debate)** — most of `Untitled_Artwork 1.png`–`24.png`: full card
   *template mockups* (orange title bar, empty white art box, red "energy" bubble) for cards like
   *Gish Gallop, Ad Hominem, Devil's Advocate, Straw Man, False Premise, Objection, Reply Guy,
   Marbury v. Madison, Miranda v. Arizona, Dish v. Spoon*. Empty art boxes — nothing to rig. NOT knife-hero.
2. **An analog "knife" tabletop prototype** — `Untitled_Artwork 25.png`–`51.png`: hand-drawn stick-figure
   sketches inside the same mockup frames (*Longsword, Skeleton, Knife to Meet You, Surefan of Knives,
   Swiss Army Sword, Taste the Knife-Bow, Kunaii, Rico-GAY-t, Scalpel, Looking Sharp, Commander Sharpie,
   Dagger Whip, Portal to the Knife Dimension, Knife Caddy, …*). These share *themes* with knife-hero
   cards, but they are rough sketches **inside card frames with handwritten rules text and a red blob** —
   not clean portrait art. Rigging them would crop in frame + text. Left in place for Hallie; thematic
   slug guesses noted in UNKNOWNS.
3. **Finished, frameless color portraits** — the genuinely riggable art (see table).
4. **Layer fragments** — `Untitled_Artwork-1.png`, `-3`…`-12.png` and `Untitled_Artwork 1.jpg`,
   `2.jpg`, `3.jpg`: single-color exported layers (a pink limb, a blue scribble, an orange hair swoosh,
   a purple rapier, blue jeans, the rainbow-striped shirt, a grey cloud, a blade alpha-mask, background
   textures). These composite back into the finished hero (`5.jpg`) / discourse figure (`-2.png`). Not
   standalone cards.

## Mapped (rigged) — confident
| Source file | Depicts | Slug | Confidence |
|---|---|---|---|
| `Untitled_Artwork 4.jpg` | Green-bladed rapier with a rainbow smiling cup-hilt | `rapier` | HIGH |
| `Untitled_Artwork.jpg` | Roller-skater charging with a rainbow pride flag raised | `rainbow_strike` | MEDIUM |

Both rigged to `card_portraits/<slug>.png` (250×190) and `card_portraits/big/<slug>.png` (1000×760).

**Rig note (`rainbow_strike`):** the source is tall-portrait; the rig tool center-crops to the 250:190
landscape slot, which trims the **rainbow flag at the top** and the skate wheels at the bottom — the body
+ skate read fine but the identifying flag is lost in the crop. Worth a hand re-center / a wider source
if the flag should stay in frame.

## UNKNOWNS / for Hallie (left in `~/Downloads`, not rigged)
- **`Untitled_Artwork 5.jpg`** — the **finished hero splash**: orange hair, blue sunglasses, white shirt,
  rainbow sash + grey cloud, blue flares, black boots, holding a pink rapier. This is the Gay Blade
  **character**, not a single card. Best used as char-select / splash (`charui`), not a card portrait —
  needs your call on the slot. (HIGH confidence on *what it is*, just not a card slug.)
- **`Untitled_Artwork-2.png`** — dark silhouette of that same figure (sunglasses, sash) over a tangled
  rainbow-threads background. Reads as **the_discourse** / **extremely_online** (the "discourse threads"
  motif), but genuinely ambiguous between those two and "a moody character variant." Your pick.
- **Analog knife sketches with clear themes** (sketches-in-frames, not clean art — your call whether to
  redraw frameless):
  - `Untitled_Artwork 34.png` "Kunaii" → likely `kunai`
  - `Untitled_Artwork 33.png` "Taste the Knife-Bow" (rainbow bow of knives) → likely `long_bow`
  - `Untitled_Artwork 35.png` "Rico-GAY-t / Ricochet" (rainbow bouncing arrow) → likely `ricochet`
  - `Untitled_Artwork 30.png` "Surefan of Knives" → likely `superfan_of_knives`
  - `Untitled_Artwork 44.png` "Dagger Whip" → theme of `knife_whip` (already rigged)
  - `Untitled_Artwork 49.png` "Be Gay Do Crimes" (rainbow switchblade) → theme of `switch_blade` (already rigged)
  - `Untitled_Artwork 46.png` "Portal to the Knife Dimension" → `portal_to_the_knife_dimension`
  - `Untitled_Artwork 32.png` "Swiss Army Sword" → possible `switch_blade` variant
- **Pure layer fragments** (no standalone meaning): `Untitled_Artwork-1.png`, `-3` … `-12.png`,
  `Untitled_Artwork 1.jpg` (blade alpha-mask), `2.jpg` (texture), `3.jpg` (threads bg).

## Tally
- **Files surveyed:** 69 (all opened and looked at).
- **Rigged (confident card portraits):** 2 — `rapier`, `rainbow_strike`.
- **Finished art, needs-a-decision (not a card slug):** 2 — hero splash (`5.jpg`), discourse figure (`-2.png`).
- **Themed analog sketches (not clean art):** ~8, slug guesses above.
- **Off-project (rhetoric game mockups) + empty frames + layer fragments:** the rest.
