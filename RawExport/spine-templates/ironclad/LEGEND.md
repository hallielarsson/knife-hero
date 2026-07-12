# Ironclad spine atlas — region legend

Extracted from `res://animations/characters/ironclad/ironclad.atlas` (base game, Slay the Spire 2, via GDRE Tools). Coordinates are top-left-origin pixel rects, verified against the source PNGs (not assumed from the libgdx spec).

Rotate = Y means the art is stored turned 90° clockwise in the packed page relative to how it displays in-game. **Paint in the stored (rotated) orientation shown on the guide layer, not the orientation you'd expect from the region name.**


## Idle / base pose (default combat stance)  —  `ironclad_page1_main_1000x269.psd`  (1000x269px, 48 regions)

ID map for cross-reference: `ironclad_page1_main_IDMAP_3x.png`

| ID | Region name | x,y | w×h | Rotated | Notes |
|---|---|---|---|---|---|
| 0 | `back mask` | 384,69 | 48×42 | **Y** | Small triangular cloth/hood flap, back side |
| 1 | `belt` | 936,82 | 72×28 | **Y** | Belt strap |
| 2 | `belt 2` | 976,59 | 22×30 | n | Belt strap segment |
| 3 | `belt 3` | 648,2 | 23×39 | n | Belt strap segment |
| 4 | `bod` | 200,35 | 82×96 | **Y** | Main torso/body art — includes the belt-buckle detail. This is the single biggest "read this character's outfit" region |
| 5 | `bottom lower arm` | 617,50 | 42×33 | n | Forearm (rear arm) |
| 6 | `bottom upper arm` | 966,91 | 28×63 | n | Upper arm (rear arm) |
| 7 | `bottom_hand` | 264,6 | 29×27 | n | Rear hand |
| 8 | `bottom_hand_fingers` | 756,2 | 29×27 | **Y** | Rear hand fingers overlay |
| 9 | `collar_back` | 785,7 | 39×15 | **Y** | Neck collar, back flap |
| 10 | `eye glow` | 976,33 | 24×22 | **Y** | Small red emissive streak painted over the eye — VFX highlight, not cloth/skin. Verified by eye: it's a red glow smear |
| 11 | `hair` | 501,122 | 24×39 | n | Hair |
| 12 | `head` | 936,33 | 38×47 | n | Head / hood, back-of-head view (confirmed by crop) |
| 13 | `hip armor right back` | 889,12 | 16×19 | n | Small hip armor plate, back side |
| 14 | `hip bottom armor` | 866,12 | 19×21 | **Y** | Hip armor, lower edge |
| 15 | `hip top bottom` | 497,33 | 9×13 | n | Tiny hip armor sliver |
| 16 | `hips` | 298,2 | 76×48 | n | Hip/pelvis base |
| 17 | `l foot` | 661,46 | 48×37 | n | Left foot |
| 18 | `l hip armor bottom` | 557,50 | 58×33 | n | Left hip armor, lower plate |
| 19 | `l hip armor top` | 497,48 | 58×35 | n | Left hip armor, upper plate |
| 20 | `l knee` | 563,2 | 46×45 | **Y** | Left knee |
| 21 | `l lower leg` | 949,187 | 43×80 | n | Left shin |
| 22 | `l neck armor` | 428,69 | 20×24 | **Y** | Left side neck/shoulder armor bit |
| 23 | `l shoulder armor bottom` | 949,156 | 35×29 | n | Left shoulder armor, lower |
| 24 | `l shoulder armor top` | 802,4 | 34×27 | n | Left shoulder armor, upper |
| 25 | `l upper leg` | 428,91 | 70×71 | **Y** | Left thigh |
| 26 | `neck` | 610,2 | 36×46 | n | Neck |
| 27 | `r foot` | 673,10 | 31×34 | n | Right foot |
| 28 | `r hip armor bottom` | 193,2 | 69×31 | n | Right hip armor, lower plate |
| 29 | `r hip armor top` | 454,2 | 71×29 | n | Right hip armor, upper plate |
| 30 | `r knee` | 711,43 | 45×40 | n | Right knee |
| 31 | `r lower leg` | 2,4 | 53×63 | **Y** | Right shin |
| 32 | `r neck armor` | 501,90 | 30×24 | **Y** | Right side neck/shoulder armor bit |
| 33 | `r shoulder armor bottom` | 758,48 | 44×35 | n | Right shoulder armor, lower |
| 34 | `r shoulder armor top` | 706,14 | 48×27 | n | Right shoulder armor, upper |
| 35 | `r upper leg` | 298,52 | 65×84 | **Y** | Right thigh |
| 36 | `shadow` | 200,119 | 226×42 | n | Blurry grey ground-contact shadow ellipse. Not costume art — a soft blob, generic to any character. Probably leave as-is or lightly restyle; doesn't need "the Blade's art" |
| 37 | `shine` | 838,16 | 26×15 | n | Small blue-white gem/highlight sprite (looks like a jewel or metal glint, possibly on the sword or a belt buckle) |
| 38 | `slash` | 528,85 | 282×182 | n | Large white sword-slash trail VFX (attack effect), not body art |
| 39 | `slash2` | 2,163 | 524×104 | n | Second white sword-slash trail VFX, not body art |
| 40 | `sword blade` | 2,59 | 102×196 | **Y** | Sword blade — the Blade's actual sword should go here |
| 41 | `sword_handle` | 67,7 | 50×65 | **Y** | Sword hilt/handle |
| 42 | `top hand` | 527,2 | 34×29 | n | Front hand (sword-holding hand) |
| 43 | `top lower arm` | 134,10 | 57×47 | n | Forearm (front/sword arm) |
| 44 | `top mask` | 454,33 | 41×50 | n | **Uncertain purpose** — visually a torso panel with a red mouth/warpaint stripe, similar composition to `bod`. May be a secondary skin layer, a color-mask used by a shader, or a costume variant swatch not normally shown. Worth testing in-game (or asking in a Godot scene view) before assuming it's plain visible art |
| 45 | `top upper arm` | 384,2 | 68×65 | n | Upper arm (front/sword arm) |
| 46 | `zaps1` | 812,156 | 135×111 | n | Red/orange lightning-squiggle VFX (special-attack aura), not body art |
| 47 | `zaps_2` | 812,33 | 122×121 | n | Red/orange lightning-squiggle VFX (special-attack aura), not body art |

## Attack pose parts (regions prefixed attack/)  —  `ironclad_page2_attack_632x82.psd`  (632x82px, 21 regions)

ID map for cross-reference: `ironclad_page2_attack_IDMAP_4x.png`

| ID | Region name | x,y | w×h | Rotated | Notes |
|---|---|---|---|---|---|
All 21 regions here are body-part art specific to the attack pose (arms/legs/torso are drawn from a different
angle than the idle page — confirmed by eye against `ironclad_2_IDSHEET`-style crops, they are recognizably
arm/leg/torso chunks, same character, different foreshortening). No VFX regions on this page.

| 0 | `attack/bod attack` | 2,3 | 77×84 | **Y** | Torso, attack-pose foreshortening |
| 1 | `attack/bottom hand attack` | 598,6 | 35×32 | **Y** | Rear hand |
| 2 | `attack/bottom lower arm attack` | 325,19 | 61×41 | **Y** | Rear forearm |
| 3 | `attack/bottom upper arm attack` | 133,8 | 64×72 | n | Rear upper arm |
| 4 | `attack/front arm attack` | 199,12 | 68×64 | **Y** | Front (sword) arm, combined |
| 5 | `attack/front bracer attack` | 480,2 | 42×32 | n | Front arm bracer/vambrace |
| 6 | `attack/front hand attack` | 419,2 | 29×28 | n | Front (sword) hand |
| 7 | `attack/l ankle attack` | 601,46 | 34×25 | **Y** | Left ankle |
| 8 | `attack/l foot attack` | 494,38 | 33×42 | n | Left foot |
| 9 | `attack/l knee attack` | 368,31 | 49×45 | **Y** | Left knee |
| 10 | `attack/l leg attack` | 88,5 | 43×75 | n | Left leg |
| 11 | `attack/l neck armor attack` | 450,2 | 28×28 | n | Left neck armor |
| 12 | `attack/l shoulder bottom attack` | 458,36 | 44×34 | **Y** | Left shoulder, lower |
| 13 | `attack/l shoulder bottom back attack` | 524,2 | 34×37 | **Y** | Left shoulder, lower back plate |
| 14 | `attack/l shoulder top attack` | 368,2 | 49×27 | n | Left shoulder, upper |
| 15 | `attack/r foot attack` | 529,42 | 38×35 | **Y** | Right foot |
| 16 | `attack/r knee attack` | 415,32 | 48×41 | **Y** | Right knee |
| 17 | `attack/r lower leg attack` | 265,16 | 58×64 | n | Right shin |
| 18 | `attack/r neck armor attack` | 325,3 | 14×20 | **Y** | Right neck armor |
| 19 | `attack/r shoulder bottom attack` | 563,2 | 33×38 | n | Right shoulder, lower |
| 20 | `attack/r shoulder top attack` | 566,43 | 33×37 | n | Right shoulder, upper |

## Death pose parts (regions prefixed death/)  —  `ironclad_page3_death_260x153.psd`  (260x153px, 12 regions)

ID map for cross-reference: `ironclad_page3_death_IDMAP_4x.png`

| ID | Region name | x,y | w×h | Rotated | Notes |
|---|---|---|---|---|---|
All 12 regions are body-part art for the death-pose rig (a separate, simplified set of parts — fewer pieces
than idle/attack, presumably because the death animation doesn't need per-limb articulation). Confirmed by
eye: recognizable torso/head/limb chunks, no VFX regions on this page.

| 0 | `death/body death` | 2,42 | 109×89 | **Y** | Torso, death pose |
| 1 | `death/bottom shoulder death` | 193,108 | 43×37 | **Y** | Rear shoulder |
| 2 | `death/bracer_death` | 89,2 | 72×38 | n | Arm bracer |
| 3 | `death/hair death` | 93,42 | 36×46 | n | Hair |
| 4 | `death/hand death` | 131,42 | 46×28 | **Y** | Hand |
| 5 | `death/head death` | 143,92 | 59×48 | **Y** | Head |
| 6 | `death/knee_death` | 214,2 | 38×44 | **Y** | Knee |
| 7 | `death/lower arm_death` | 161,42 | 57×48 | n | Lower arm |
| 8 | `death/lower leg_death` | 2,2 | 38×85 | **Y** | Lower leg |
| 9 | `death/open hand_death` | 220,70 | 35×36 | n | Open hand (limp, dropped) |
| 10 | `death/top shoulder death` | 163,2 | 49×38 | n | Front shoulder |
| 11 | `death/upper arm_death` | 93,90 | 48×61 | n | Upper arm |

## Single placeholder region (slash/slash_placeholder)  —  `ironclad_page4_slash_placeholder_17x42.psd`  (17x42px, 1 regions)

ID map for cross-reference: `ironclad_page4_slash_placeholder_IDMAP_4x.png`

| ID | Region name | x,y | w×h | Rotated | Notes |
|---|---|---|---|---|---|
| 0 | `slash/slash_placeholder` | 2,2 | 13×38 | n | A tiny (13×38px) near-blank rect. Almost certainly a technical placeholder/stub, not visible art — check in-game before spending painting time here. Included for completeness only |