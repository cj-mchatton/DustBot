# Cosmetic Sprite Polish Report

## Outcome

The 37 newly introduced character, object, trail, and dock cosmetics now use
polished transparent 384×384 artwork. The previous 64×64 procedural drawings
remain only as missing-resource fallbacks. Cosmetic IDs, prices, requirements,
ownership, purchasing, equipping, category flow, and save fields were not
changed.

The weakest groups were the new DustBot skins, cat skins, and crumb styles shown
in the supplied screenshots. They were addressed first. The same flat procedural
treatment was then identified in path trails and alternate docks, so those two
families received the same pass.

## Style benchmark

Classic DustBot, Orange Cat, Classic Crumbs, and the Classic Dock were used as
direct visual references. The replacement art follows these rules:

- rounded, cute silhouettes that remain clear at gameplay size;
- clean antialiased edges rather than visibly stepped procedural geometry;
- soft 2.5D cartoon shading with restrained highlights and occlusion;
- cohesive cream trim, glossy enamel, friendly faces, and readable accessories;
- full-body cat poses with expressive eyes, paws, and curled tails;
- small object clusters with dimensional color and clear negative space;
- compact trail emblems and docks with unmistakable gameplay functions;
- transparent, evenly padded square framing for predictable store presentation.

## Improved cosmetics

### DustBot skins

Crowned Gold, Moonlight Rover, Bubble Bow Bot, Honeybee Helper, Bunny-Eared Bot,
Patchwork Rover, Orbital DustBot, Cat-Eared DustBot, FrogBot, Retro Vacuum, and
Arcade Cabinet Bot.

Every variant keeps the default robot's round body, dark glass face, bright eyes,
cream trim, and friendly expression. Themes are conveyed through materials and
small accessories rather than replacing DustBot with an unrelated face icon.

### Cat skins

Tuxedo Cat, Sleepy Cat, Fancy Cat, Astro Cat, Ghost Cat, Robot Cat, and Tiny
Tiger.

The replacements are complete pursuer characters modeled on the Orange Cat's
compact anatomy and stepping pose. They no longer render as disembodied heads or
simple geometric masks.

### Crumb styles

Cookie Bits, Cereal Loops, Fluffy Dust Piles, Garden Leaves, Wrapped Candy, and
Movie Popcorn.

Each replacement is a miniature illustrated cluster with readable pieces,
controlled overlap, clean color separation, and enough internal shading to match
Classic Crumbs.

### Path trails

Bubble Dots, Falling Leaves, Paw Print Path, Lofi Pulse, Star Parade, Bubble
Trail, Comet Trail, and Prism Ribbon.

These remain deliberately simpler than character art because route nodes render
very small, but now have polished silhouettes, highlights, and cohesive depth.

### Dock designs

Gold Dock, Neon Dock, Wooden Dock, Space Dock, and Cat Bed Dock.

All five preserve the Classic Dock's open charging bay, contacts, front-facing
perspective, and compact footprint.

## Integration and presentation

`CosmeticSpriteLibrary` now loads project art through the existing cached sprite
loader before falling back to procedural rendering. It does not alter any
economy or progression code. The store and board therefore consume the same
sprite for a selected cosmetic.

Card anchor geometry was not changed. Consistent trimming, square framing, and
per-family padding solved the inconsistent preview size without risking the
responsive store layout. The existing screenshot-only QA route now captures the
DustBot and cat category pages in addition to crumb styles.

The cosmetic importer disables mipmaps, preserves the original non-power-of-two
384×384 size, clamps edges, uses bilinear filtering and texture compression, and
keeps textures non-readable at runtime. Assets are loaded and cached on demand;
there is no per-frame texture generation or allocation.

## Validation

- Unity compilation: passed.
- Cosmetic preview/resource validation: passed for every category.
- Responsive layout validation: passed for iPhone SE, iPhone 15, iPhone 16 Pro
  Max, and the narrow one-column fallback.
- Full content validation: passed 260 category levels, 95 deterministic cat
  levels, 155 large mazes, economy claims, cosmetic purchase/equip tests, save
  repair, route simulation, hazards, and progression.
- Standalone screenshot player build: passed.
- Real-player captures inspected: DustBot skins, cat skins, crumb styles, and the
  Cookie Bits detail screen.

Physical-device frame-time profiling was not run in this pass. The runtime path
is lower-allocation than the old procedural fallback after first access, and the
import settings avoid mip overhead, but final iPhone release QA should still
include a short store-scroll and gameplay profile on the oldest supported phone.

## Possible later polish

Tile and room themes remain lightweight procedural patterns. They are functional
and were not among the visibly weak sprite families in the supplied screenshots,
but their larger surfaces could benefit from a dedicated environment-art pass in
the future. Ghost Cat's pale translucency is intentionally subtle; it should be
rechecked on a dim physical display and can be made slightly more opaque if
needed.

## Generation workflow

Artwork was created with the built-in image generation path, using the original
project sprites as visual references. Five production prompt groups were used:

1. Eleven DustBot variants in a 4×3 atlas, preserving the default bot's round
   shell, face panel, eyes, trim, and soft 2.5D rendering.
2. Seven full-body cat variants in a 4×2 atlas, preserving the Orange Cat's
   anatomy, stepping pose, eyes, paws, tail, and cartoon fur rendering.
3. Six object clusters in a 3×2 atlas, matching Classic Crumbs' miniature-object
   shading and clean silhouettes.
4. Eight compact trail emblems in a 4×2 atlas, optimized for 24–40 px route-node
   readability.
5. Five alternate chargers in a 3×2 atlas, preserving the Classic Dock's open
   bay, contacts, perspective, and rounded construction.

All prompts required uniform chroma-key backgrounds, no text, borders, cast
shadows, watermarks, or cross-cell overlap. The atlases were converted to alpha,
split, trimmed, de-spilled, consistently framed, and validated by
`Tools/process_cosmetic_sprites.py`.
