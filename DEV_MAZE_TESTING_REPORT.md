# Dev Maze Testing Update

## Direction and scope

Dev Level 9 remains the baseline example: blockers form corridors instead of
being scattered decoration. The advanced profile extends that language with
longer carved corridors, required side loops, direct-looking skip traps,
dead-end decoys, rooms, chokepoints, route modifiers, separated crumbs, and a
substantial final-crumb-to-dock segment.

This work is gated by `useAdvancedDevMaze` plus a non-production generation
mode. Production Campaign never sets that flag. Its 6,000-entry manifest,
seeds, level count, distribution, and generation branch were not changed by
this update.

## Development content

- Dev Level 9: unchanged baseline 9×9 maze direction.
- Dev 10-15: 10×10 through 16×16 Dead-End Branch, Loop, Dock Return, Sticky
  Shortcut, One-Way Commitment, and Fragile Corridor mazes.
- Dev 28: Expert 17×17 Multi-Room maze.
- Dev 29: Daily-style 18×18 Dust Bunny Detour maze.
- Dev 30: Master-style 20×20 combined Expert Large maze.
- Maze Testing 1-20: 10×10 through 26×26, including rectangular 18×20,
  20×18, 22×20, and 24×22 variants.

Required detours are carved beside an apparently direct corridor edge. The
expected route enters the detour for a crumb while the original edge stays
open as a tempting mistake. Taking that shortcut can miss a crumb, consume a
limited route cell too early, worsen dock return, or spend the hard cost
budget. Separate optional detours make the Dust Bunny a star-risk decision.

## Scoring and validation

Three stars require the canonical perfect cost, no hint, and the Dust Bunny
when present. Two stars have only a small tier-scaled allowance. Hard, Expert,
and Extreme dev mazes have a hard maximum only a few moves above perfect.
Undo remains functional and does not itself remove the third star in this
dev-only profile.

Combined advanced profiles also include readable wave-marked Slippery tiles.
The path preview rejects turns made while crossing them, so momentum must
continue straight; the same rule is enforced by canonical validation.

The advanced validator layers dev-only checks over the existing maze audit. It
measures branches, dead ends, loops, chokepoints, rooms, decoys, turns, route
choices, nonconsecutive shortcut edges, crumb route spread, final crumb-to-dock
cost, Dust Bunny detour cost, commitment tiles, and hard-limit slack. Large but
empty, linear, generous, clustered, or low-choice candidates are rejected and
regenerated from the same deterministic seed namespace.

## Large-grid interaction

Advanced boards use an overview zoom based on grid size instead of squeezing
the whole maze into tiny tiles. They support pinch zoom, two-finger pan,
one-finger pan when starting away from DustBot, explicit −/+/recenter controls,
edge pan while drawing, interpolated drag sampling, a thicker dev-maze path,
Undo, Reset, live cost/sticky pressure, and camera follow during simulation.

## Verification and first tests

Unity 6000.5.0f1 compiled cleanly. `Validate Large Mazes` passed all 31 fixtures
in under 3 seconds, including canonical input routes up to 212 steps.
`Run Development Mode Validation` passed all 100 dev/test levels twice for
determinism and simulated every canonical solution.

Start with Maze Testing 1 (basic branches), 4 (dock return), 8 (Dust Bunny),
9 (One-Way), 12 (22×22 combined), 16 (26×26 extreme), and 20 (large Dust
Bunny). On a physical iPhone, pay closest attention to 24×24-26×26 icon size,
edge-pan speed, long-drag accuracy, and whether the strict limits feel tough
but legible.
