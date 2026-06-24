# Cat Chase turn-based revision

## Result

Cat rooms are now a distinct turn-based evasion mode. Normal rooms retain the
existing full-route drawing and Play simulation.

The validated Main Journey contains 1,052 deterministic Cat Chase rooms. All
6,000 canonical levels pass validation, and the independent cat audit reports
zero weak and zero trapped cats.

## Turn structure

1. The player swipes up, down, left, or right.
2. DustBot slides one tile.
3. The destination cleans a crumb or collects a Dust Bunny.
4. After a 0.035-second readability beat, the cat makes its first move.
5. The cat then makes its second move.
6. The board waits for the next player swipe.

Invalid swipes do not consume a turn.

## Cat algorithm

The active generated behavior is Curious Cat:

- Exactly two movement attempts follow each valid DustBot move.
- Each attempt first tries one horizontal step toward DustBot.
- If that step is aligned, blocked, outside the board, or does not reduce
  Manhattan distance, it tries one vertical step toward DustBot.
- If neither step reduces distance legally, the cat stays for that attempt.
- The same rule is used by gameplay, previews, generation, validation, and the
  solver.

There is no runtime randomness. `horizontalFirst` is fixed and serialized in
the level signature.

## Furniture and hazards

Walls and toys block both characters. Socks, cords, and wet spots are fatal to
DustBot and also block cat navigation. This creates corners, narrow lanes, and
delays that let players exploit the cat's horizontal-first priority.

The cat is never allowed through blockers or outside the grid.

## Cat rooms versus normal rooms

Normal rooms:

- Drag the complete route.
- Press Play.
- DustBot cruises through the route without tile-center pauses.

Cat Chase rooms:

- No full-route drawing or Play button.
- Swipe exactly one tile per turn.
- DustBot and cat animate sequentially.
- HUD shows crumbs, moves, ideal target, optional maximum, bunny state, and
  perfect-clear availability.
- Neighboring move previews show predicted paw positions; dangerous catches
  use the reserved error color.

## Generation and validation

Cat candidates use a dedicated solver over:

- DustBot position
- Cat position
- Collected-crumb bitmask
- Dust Bunny state when required for the perfect route

The breadth-first solver permits necessary tile revisits and rejects moves that
hit blockers, hazards, the cat, an early dock, or a cat response collision.
Generated expected solutions are shortest valid turn sequences under these
rules.

Each accepted cat room must:

- Have a solver-proven cleaning-and-docking solution.
- Keep the cat in a useful navigable area.
- Move the cat on at least three turns.
- Visit at least three distinct cat positions.
- Bring the cat within two tiles of DustBot for at least two turns.
- Preserve deterministic level, Daily, Master, and Endless seeds.

Optional cat slots are omitted when a generated room cannot satisfy both
solvability and pressure requirements.

## Tutorial

Level 30 is a hand-authored Cat Chase tutorial. Its short briefing teaches:

- One-tile swipes
- Two cat moves
- Horizontal-first pursuit
- Using a corner and furniture
- Cleaning before docking

The authored solution deliberately revisits tiles, demonstrating that Cat
Chase is an evasion puzzle rather than a pre-drawn route.

## Hints

A Cat Chase hint spends the normal configured hint cost and highlights one
safe neighboring move. The choice prioritizes:

- Avoiding immediate cat or hazard failure
- Cleaning a crumb
- Collecting a Dust Bunny
- Reaching the dock after cleaning
- Maintaining distance from the cat

It reveals one move, not the full solution, and still removes the no-hint star
condition where applicable.

## Stars and rewards

- Completion without being caught earns the clear.
- Two and three stars use solver-derived move targets.
- Three stars additionally respect the no-hint and required Dust Bunny goals.
- Existing coins, first-clear rewards, streaks, progression, cosmetics, and
  saves are unchanged.

## Automated results

- 6,000 canonical Journey levels: passed.
- Turn-based expected solutions: simulated to victory.
- Cat Chase rooms: 1,052.
- Weak cats: 0.
- Trapped cats: 0.
- Minimum reachable cat area: 4 tiles.
- Minimum unique cat positions: 3.
- Minimum cat movement turns: 3.
- Maximum closest approach: 2 tiles.
- Minimum pressure turns: 2.
- Daily and Master determinism: passed.
- Normal route drawing and hazard blocking: passed.

## Manual iPhone tests

1. Swipe rapidly in all four directions and verify one move is accepted per
   completed turn.
2. Confirm invalid swipes do not move the cat or increase the move count.
3. Compare paw previews with the cat's two animated moves.
4. Move directly onto the cat and confirm immediate failure.
5. Enter each hazard type and confirm its specific failure.
6. Use Level 30's corner to observe horizontal-first pursuit.
7. Test a Cat Chase with a Dust Bunny and all star outcomes.
8. Use hints after deliberately diverging from the ideal solution.
9. Retry repeatedly and verify instant, clean state resets.
10. Test Daily and Master Cat Chases across relaunches for identical layouts.

## Balance considerations

A two-step cat is deliberately aggressive. Some generated rooms therefore omit
the cat rather than weakening the rules or publishing an impossible puzzle.
Completion times and hint usage should be monitored to tune cat frequency,
blocker density, and move-target slack.
