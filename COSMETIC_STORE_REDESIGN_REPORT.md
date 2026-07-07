# Cosmetic Store Redesign Report

## Store organization

Cosmetics now opens to a dedicated category selector instead of one long mixed
catalog. The selector shows an icon, short description, owned/total count, and
new-item badge for each category. It returns to Settings and keeps the wallet
summary (Dust Coins, Dust Bunnies, and stars) visible.

The eight storefront categories are:

1. DustBot Skins
2. Path Trails
3. Crumb Styles
4. Cat Skins
5. Dock Designs
6. Tile / Floor Themes
7. Room Themes
8. Bundles

There is no Fails category. Existing win/failure celebration IDs remain readable
for save compatibility but are not exposed as store categories.

## Category stores and details

Each category opens a separate vertically scrollable store containing only its
own items. A responsive grid uses two columns on supported iPhone widths and a
single-column fallback on unusually narrow layouts. Cards show a large preview,
name, rarity, cost, unlock requirement, and clear locked/owned/equipped state.

Tapping a card opens a dedicated item detail screen with a much larger preview,
category and rarity metadata, description, price, requirement, ownership state,
and purchase/equip action. Locked cards remain tappable so their full requirement
can always be read. Bundles unlock their contents and `APPLY SET` equips every
owned piece in the bundle; an individual item can still override any slot later.

## Cosmetic system and save compatibility

The data model now supports IDs, display data, category, rarity, price, all
existing progression requirements, preview/asset keys, animation keys, particle
and material/effect keys, visual style, new-item state, affected asset type, and
bundle contents.

One active cosmetic is saved for each gameplay-facing slot:

- DustBot skin
- Path trail
- Crumb style
- Cat skin
- Dock design
- Tile/floor theme
- Room theme

Crumb and cat selections were added as additive save fields. Save repair grants
and selects safe defaults when older data does not contain them. Existing IDs,
ownership, purchase history, Dust Coins, stars, Dust Bunnies, active legacy
slots, and economy behavior are preserved.

## Meaningful visual variety

The gameplay renderer now reads the active asset slot rather than applying only
a tint. Polished transparent Resources art is loaded and cached on demand for
character, crumb, path, and dock cosmetics. Lightweight procedural drawing is
retained only as a missing-resource fallback, avoiding per-frame texture
allocation on iPhone.

- DustBot skins can use different bodies, silhouettes, accessories, faces, and
  idle personalities (for example crowns, domes, ears, wings, or cabinet forms).
- Trails use distinct route-node shapes such as paws, stars, leaves, bubbles,
  and comets, with optional pulse or animated prism color behavior.
- Crumb styles replace crumb sprites with cookies, cereal loops, dust piles,
  leaves, wrapped candy, or popcorn.
- Cat skins replace the pet sprite with distinct readable cats, including
  accessory, helmet, ghost, and robot constructions while retaining the danger
  glow and chase readability.
- Docks use distinct bed, wood, neon/gold charger, and space-station shapes.
- Tile themes use cached board patterns such as grout, diagonal metal, patio,
  stars, and candy rings instead of palette swaps alone.
- Room themes apply a full patterned atmosphere behind every screen.

Cosmetics remain presentation-only. No cosmetic value is read by pathfinding,
level generation, scoring, hazards, cat movement, objectives, or economy math.

## Validation

- Unity script/player compilation: passed.
- Cosmetic store layout validator: passed for iPhone SE, iPhone 15, iPhone 16
  Pro Max, and a narrow single-column fallback.
- Full content validation: passed all 260 category levels, 95 deterministic cat
  levels, 155 large mazes, economy claims, cosmetic purchase/equip tests, save
  repair, route simulation, hazards, and progression.
- Standalone macOS screenshot player build: passed.
- Visual captures verified the category selector, Crumb Styles grid, item detail
  screen, safe margins, readable locked overlays, and vertically scrollable cards.
