# Credits

## Art stubs — Twemoji (CC-BY 4.0)

Several status / power / curse art slots are currently filled with temporary
placeholder stubs built from **Twemoji** emoji graphics. These are coverage
stubs — they exist so nothing falls back to the base game's placeholder
textures — and are meant to be swapped for Hallie's hand-drawn art.

> Twemoji — Copyright Twitter, Inc and other contributors.
> Graphics licensed under **CC-BY 4.0**: https://creativecommons.org/licenses/by/4.0/
> Source: https://github.com/twitter/twemoji

### Stubs in use

| Slot | Emoji | Twemoji codepoint | File(s) |
|------|-------|-------------------|---------|
| Power: Wholeness | 🧵 thread | `1f9f5` | `KnifeHero/images/powers/wholeness.png` (+ `big/`) |
| Power: Become Who You Are | 🦋 butterfly | `1f98b` | `KnifeHero/images/powers/become_who_you_are_power.png` (+ `big/`) |
| Status: The Discourse | 💬 speech balloon | `1f4ac` | `KnifeHero/images/card_portraits/the_discourse.png` (+ `big/`) |
| Status: Vexing Memory | 💭 thought balloon | `1f4ad` | `KnifeHero/images/card_portraits/vexing_memory.png` (+ `big/`) |
| Curse: Festering Wound | 🩸 drop of blood | `1fa78` | `KnifeHero/images/card_portraits/festering_wound.png` (+ `big/`) |
| Curse: Throbbing Heart | 🫀 anatomical heart | `1fac0` | `KnifeHero/images/card_portraits/throbbing_heart.png` (+ `big/`) |

The status/curse card stubs are not yet wired in code (the cards still point at
`card.png`); see the Art Stubber report for the slugs the knife-hero governor
should wire. The two power stubs need no wiring — the path is auto-derived from
the power's Id by `KnifeHeroPower`.
