# Static Curated Campaign Report

Date: July 6, 2026

## Result

The production campaign now contains exactly 255 immutable level records:

- Easy: 5 (four lessons and one cat lesson; Easy 5 is the first small maze)
- Medium: 50 (35 maze/path, 15 cat)
- Hard: 50 (45 maze/path, 5 cat)
- Expert: 100 (75 maze/path, 25 cat)
- Cat Chase: 50 (50 cat)

`LevelLoader.LoadCategory` now reads only from `CuratedLevelCatalog`. It does not call `LevelGenerator`, `LevelManifest`, a random source, the clock, or save data. The old campaign manifest entry points were removed. Daily Challenge, Master Clean, Endless Clean, and development-only playlists may still use generation.

Every catalog record explicitly stores its category and number, board dimensions, complete tile map, start, dock, crumbs, furniture, optional Dust Bunny, optional cat, expected route, par, two-star and three-star targets, hard limit, difficulty, archetype, cat archetype, tutorial copy, design purpose, and intended strategy. All records use the fixed revision key `CuratedCampaign_2026_07_06`.

## Easy onboarding

1. Easy 1 teaches touching DustBot, drawing a path, starting movement, and docking.
2. Easy 2 introduces required crumbs and the clean-before-dock rule.
3. Easy 3 introduces an optional Dust Bunny and a visible detour decision.
4. Easy 4 introduces one-tile cat turns, the cat's two predictable moves, and using furniture for safety.
5. Easy 5 introduces a small furniture maze, crumb order, wrong routes, and dock-return planning.

All five levels include focused tutorial text. Cats do not appear before Easy 4, and maze planning is not introduced before Easy 5.

## Difficulty and variety

Medium cat levels are dispersed at 3, 6, 10, 13, 17, 20, 23, 27, 30, 33, 37, 40, 43, 47, and 50. Hard cat levels occur at 10-level intervals. Expert cat levels occur every fourth level. Cat Chase is entirely cat-based and follows the requested 4 Easy / 10 Medium / 10 Hard / 26 Expert difficulty distribution.

The catalog uses all 19 cat puzzle archetypes. Cat validation simulates the authored route, rejects collisions, verifies that the cat shares the playable region, measures movement and pressure, rejects trapped or negligible cats, verifies the declared strategy, and independently solves the turn-based state space.

The 155 large path mazes pass topology checks for objective reachability, branches, dead ends, loops, chokepoints, decoys, playable edges, Dust Bunny detours, and category-specific complexity thresholds. Every authored gameplay map is unique across the full catalog.

## Removed main-game tiles

Production category definitions contain only Start, Dock, Crumb, Wall, Toy/furniture, normal unlisted floor, Dust Bunny overlays, and cat overlays. Sticky, two-cost, one-way, slippery, fragile, sock, cord, wet-spot, switch, gate, conveyor, and other movement-modifier tiles are absent. A production validation guard rejects any future curated record that contains one.

The legacy special-tile code remains available only to the explicit development obstacle/testing playlists. The cat still moves twice after each valid DustBot turn.

## Progression and UI

The category catalog, level-select paging, progress totals, completion tracking, star tracking, Dust Bunny tracking, Perfect Clean tracking, Master Clean gate, developer labels, and completion-dependent cosmetic threshold now use 255 levels and Easy's five-level count. Save repair drops out-of-range retired Easy 6–10 records; save data continues to store results and settings only and is never consulted by the catalog decoder.

Cat Chase now unlocks after the Easy 4 cat lesson. The final 260-level cosmetic threshold was adjusted to 255.

## Validation result

`DustBot > Run Content Validation` passed:

- all 255 records loaded twice with identical signatures;
- 255 unique gameplay layouts;
- 96 total cat levels including Easy 4;
- 155 large maze levels;
- all expected routes simulated to a win;
- every required crumb and dock reachable;
- every Dust Bunny on its expected route;
- star and hard-move targets internally valid;
- every cat level fair, strategically active, archetype-matched, and solver-confirmed;
- no retired special content in any main level;
- Daily, Master, development playlists, progression repair, economy, and cosmetics still pass.

## Test first

1. Play Easy 1–5 in sequence on an iPhone-sized screen and confirm each lesson teaches only its intended concept.
2. Play Medium 2–3 and 5–6 to verify the transition into dispersed cat levels.
3. Play Medium 50, Hard 50, Expert 80, Expert 100, and Cat Chase 50 to assess strict late-game targets and board readability.
4. Confirm two devices or fresh saves load matching map signatures for Easy 4, Medium 32, Hard 14, Expert 80, and Cat Chase 22.
5. Complete Easy, inspect category totals/stars/Dust Bunnies/Perfect Clean, relaunch, and verify progression survives without changing a layout.

## Remaining balance concerns

Automated structural, route, topology, and cat-pressure validation is green, but subjective pacing still needs human playtesting. The highest-risk areas are visual readability on the largest Expert boards, whether adjacent late-game levels feel distinct in play rather than only structurally distinct, and whether the strictest three-star targets feel satisfying instead of brittle. Those are balance edits to fixed records; they do not require reintroducing runtime generation.
