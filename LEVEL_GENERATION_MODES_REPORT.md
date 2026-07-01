# DustBot Level Generation Modes Report

## Modes and switching

`GenerationMode` now provides six centrally configured modes:

- `ProductionCampaign`: 6,000 canonical journey levels.
- `DevelopmentCampaign`: 30 deterministic integration-test levels.
- `CatTesting`: 24 deterministic cat levels.
- `ObstacleTesting`: 18 deterministic obstacle levels.
- `TutorialTesting`: 8 short onboarding levels.
- `MazeTesting`: 20 advanced dev-only maze levels.

Unity Editor and development builds default to Development Campaign. Release
builds force Production Campaign. In a development configuration, use the
**DEV** button on the main menu or game header, select a mode, and use
**JUMP / PLAY**. Mode choice is intentionally runtime-only, so a relaunch
returns to the build-appropriate default.

## Development Campaign structure

- 1-3: hand-authored onboarding.
- 4-6: easy path planning.
- 7-9: medium routes, ending with a carved 9×9 maze.
- 10-12: advanced 10×10, 12×12, and 14×14 branch/loop/dock-return mazes.
- 13-15: advanced 13×13, 15×15, and 16×16 Sticky, One-Way, and Fragile mazes.
- 16-25: varied deterministic Cat Chase levels.
- 26-27: hard/expert cat pressure levels.
- 28: carved Expert 17×17 maze.
- 29: carved Daily Challenge-style 18×18 Dust Bunny detour maze and isolated simulated streak/reward UI.
- 30: carved Master Clean-style 20×20 maze.

All test seeds use a separate `DustBot_<Mode>_v<Version>_<Level>` namespace.
The same mode, generation version, and level number therefore reproduce the
same puzzle without changing production seeds.

## Focused testing modes

Cat Testing uses cats on all 24 levels, mixing proven template arenas with
procedural arenas and named pressure archetypes. Normal validation rejects a
cat that is trapped, disconnected, strategically negligible, catches the
canonical route, or leaves the level unsolvable.

Obstacle Testing covers Sticky path cost, One-Way direction commitments,
Fragile no-backtracking behavior, and combined modifier levels. The existing
validator checks move cost, arrow direction, fragile reuse, route continuity,
and canonical solvability. Gameplay briefing text explains each tile effect.

Tutorial Testing covers movement/docking, crumbs, furniture, hazards, the
turn-based cat introduction, Dust Bunnies, stars/path targets, and hints. It
uses short hand-authored rooms for stable first-time-player QA.

Maze Testing contains 20 deterministic mazes from 10×10 to 26×26. Its named
profiles cover dead-end branches, loops, dock return, Sticky shortcuts,
chokepoints, multi-room routing, Dust Bunny detours, One-Way commitments,
Fragile corridors, Slippery momentum, and combined expert-large layouts. This playlist and its
stronger complexity thresholds are never selected by Production Campaign.

## Developer tools and metadata

The development-only panel provides mode switching, previous/next/restart,
deterministic regeneration, numeric level jumping, save reset confirmation,
10,000-coin grants, production level/Master unlock, and cosmetic unlock.

Every test level logs metadata during interactive development and exposes it
in the panel: mode, level number, playlist size, seed, generation version,
path-vs-cat type, difficulty tier/score, archetype, obstacles, strategic depth,
cat pressure, maze topology/complexity, solver/validator result, ideal cost,
and hard maximum.

Test wins exercise rewards and economy but do not write journey stars/unlocks,
Daily streak state, or Master/Endless progression. Production, Daily, Master,
and Endless loading retain their dedicated production manifests.

## Validation performed

- `Run Development Mode Validation`: passed all 100 compact levels, loading
  each twice for determinism and simulating every canonical solution.
- `Validate Large Mazes`: passed 31 fixtures: the Dev Level 9 baseline, nine
  advanced Development Campaign mazes, all 20 Maze Testing levels, and the
  existing 19×21 production Master fixture. Long-path input passed through
  212 route steps and the batch completed in under 3 seconds on the test Mac.
- `Run Content Validation`: passed all 6,000 production journey levels, 4,947
  large mazes, 700 deterministic cat levels, Daily Challenge, Master Clean,
  economy, cosmetics, save repair, hazards, and progression.

## Manual test order

1. Open DEV, switch modes, jump/restart/next/previous, and inspect metadata.
2. Play Development levels 9, 10, 12, 14, 15, 28, 29, and 30; test overview zoom, detailed
   zoom, one-finger pan away from DustBot, pinch, recenter, edge-pan drawing,
   Reset, Undo, and simulation follow.
3. In Cat Testing, compare a template level (3 or 6) with procedural levels
   (5 or 7), including danger preview, hint, restart, win, and fail states.
4. Play Obstacle levels 1, 7, 13, and 17; verify cost, arrows, fragile crossing,
   path preview, star targets, and combined effects.
5. Run Maze Testing 1, 4, 8, 9, 12, 16, and 20 for increasing size and each
   high-value archetype, then run Tutorial Testing 1-8 as a fresh-player flow.
6. Switch to Production, compare a known level across relaunches, then test a
   real Daily Challenge and Master Clean separately.
7. Make a non-development build and confirm Production is forced and no DEV
   entry point is visible.

## Current limitations and recommended follow-ups

- Slippery/conveyor, gate/switch, and cat-toy distraction systems do not exist
  in the current gameplay model, so Obstacle Testing covers only implemented
  route modifiers and blockers. Add focused profiles when those mechanics land.
- Named cat archetypes currently describe deterministic template/procedural
  layout goals; bespoke hand-authored boards for every name would improve
  targeted regression testing further.
- Compact-playlist star history is intentionally not persisted. If long-running
  QA tracking becomes useful, add a separate development-only progress store.
