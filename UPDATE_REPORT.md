# DustBot cat variety and campaign maze integration report

## Screenshot-driven changes

The supplied Cat Chase 46 and 49 screenshots exposed the production fallback:
both were the same 6×5, one-cat, one-crumb, one-dock template with only mirrored
wall and endpoint positions. Production cat levels after the level-21 tutorial
now use deterministic procedural arenas instead. In the new generation,
representative levels 46 and 49 are 8×8 multi-objective cat puzzles with
different declared strategies (Corridor Delay and Safe Pocket).

Level 21 remains hand-authored because it teaches the one-tile swipe and the
cat's two-step, horizontal-first response safely.

## Cat mechanics and archetypes

The movement rule did not change: DustBot moves one tile, then the cat takes
two deterministic steps toward DustBot, trying horizontal movement before
vertical movement and respecting furniture. The solver still proves that all
crumbs (and a required Dust Bunny) can be collected before docking.

Generated cat levels now declare one of 19 archetypes:

- Horizontal Priority Trap
- Loop Around Furniture
- Corridor Delay
- Chokepoint Timing
- Safe Pocket
- Split Room
- Dock Pressure
- Dust Bunny Risk
- Crumb Order Chase
- Near Catch
- Cat at the Chokepoint
- Long Route vs Safe Route
- Central Island
- Lure Away from Crumb
- Lure Away from Dock
- Backtrack Bait
- Multi-Corridor Pursuit
- Furniture Delay
- Multi-Crumb Route Planning

The archetype changes crumb count, blocker density, objective placement,
route length, preferred cat start zone, and the placement score used when
choosing a fair cat start. The generator rejects a candidate when the solved
route does not exhibit the promised strategy.

## Anti-repetition and pressure validation

The campaign uses a coprime 19-profile schedule, so it visits every cat
archetype before repeating. A second deterministic five-zone schedule rotates
the cat start among corners, sides, and central pressure. Board sizes, path
archetype, crumb counts, and campaign difficulty add further variation.

`CatLevelVarietyEvaluator` records route change versus the cat-free solution,
intentional backtracking, near-catches, horizontal/vertical cat movement,
crumb/dock/Dust Bunny pressure, corridors, branches, chokepoints, loops, safe
pockets, nearby furniture, and a compact solution-pattern fingerprint. The
cat validation runner keeps a recent window and fails on repeated archetypes
or excessive strategic fingerprint repetition.

Pressure scoring now includes route threats, route changes, backtracking,
near-catches, objective pressure, dock return pressure, optional Dust Bunny
risk, cat mobility, and furniture interaction. Levels are rejected when the
cat is trapped, disconnected, negligible, unsafe on the canonical solution,
unsolvable, below pressure, or inconsistent with the declared archetype.

This adapts the useful predictable-pursuer idea to DustBot without copying a
reference game's maps, wording, theme, art, or solutions. The player cleans a
home, manipulates a pet cat around furniture, and must return to a dock.

## Main-campaign maze integration

Generation version 8 enables production large mazes instead of reserving them
for development playlists. Scaling is gradual:

- Levels 1–3: onboarding
- Levels 4–20: 6×6/7×7 light maze and route ideas
- Levels 21–35: 6×6/7×7 cat introduction and early strategy
- Levels 36–60: selected 8×8/9×9 production mazes
- Levels 61–100: selected 9×9/10×10 mazes
- Levels 101–250: selected 12×12/13×13 mazes
- Levels 251–500: selected 13×13/14×14 mazes
- Levels 501–1000: selected 14×14/15×15 mazes
- Levels 1001–4000: selected 15×15 through 18×18 expert mazes
- Levels 4001–6000: 18×18 through 21×21 Master-tier mazes
- Master Clean: 18×18 through 22×22 mazes

Depth-first corridors are enriched with rooms, loops, branches, dead ends,
chokepoints, decoys, separated crumbs, a required Dust Bunny detour, route
modifiers, and tight star/path-cost targets. Production generation now carves
spaced connected edge branches before choosing the maze diameter. Validation
rejects a full blocked perimeter or too few playable edge cells, so the board
boundary is used instead of wasting every outside tile on a wall ring.

`LargeMazeEvaluator` validates reachability, open-cell ratio, linearity,
route length, branches, dead ends, loops, chokepoints, rooms, decoys, crumb
separation, final-crumb-to-dock distance, bonus detour cost, optimal-route gap,
path-cost pressure, and playable edges.

The large-board UI supplies viewport clipping, pinch zoom, two-finger pan,
zoom/reset controls, edge auto-pan while drawing, thicker large-maze path
presentation, and bot-follow during simulation. Production mazes now open and
reset at a full-board fitted overview before the player zooms in. No Settings,
audio, SFX, economy, save, cosmetics, or menu code was changed.

## Determinism and validation

Generation version was intentionally advanced from 7 to 8. Main Journey is a
pure function of level number and version; Daily remains a pure function of
date; Master Clean remains a pure function of its level number; Endless also
retains its run seed. Candidate rerolls use deterministic candidate salts.

Automated checks:

- Large-maze batch: 31/31 passed, including 12×12–40×40 development stress
  boards and a 19×21 production Master Clean board.
- Cat variety sample (levels 22–180): 55 cat levels, 19 archetypes, 8 board
  sizes, 55 multi-crumb levels, and 4 recent strategic-fingerprint matches.
- Full content validation exercises all 6,000 campaign levels twice at
  checkpoints, simulates canonical routes, checks cat relevance, maze topology,
  Daily, Master Clean, progression/economy, and audio resource availability.
  It passed with 851 deterministic cat levels and 4,947 production mazes.

## Recommended manual tests

Start with:

- Level 21 for the unchanged cat tutorial.
- Levels 24, 27, 30, 33, and 35 for early cat variety.
- Levels 46 and 49 to compare directly with the supplied screenshots.
- Levels 50, 55, 73, 86, 121, and 175 for long/safe route, crumb order, dock
  pressure, Dust Bunny risk, near-catch, and higher-level Dust Bunny pressure.
- Levels 36–60 for the first production maze appearances.
- Levels 100, 180, 500, 1000, 2000, 4000, and 6000 for maze scaling.
- Master Clean 2 and 500, plus the same Daily Challenge before and after an app
  restart, for mode determinism.

## Remaining concerns

- Structural scoring prevents weak layouts, but human playtests should tune
  how often a safe but longer route deserves two rather than three stars.
- Cat pressure is intentionally readable; later telemetry may justify a higher
  minimum near-catch frequency in Expert levels.
- The 18×18–22×22 iPhone experience should be tested on the smallest supported
  screen for icon recognition and comfortable pinch/pan sensitivity.
