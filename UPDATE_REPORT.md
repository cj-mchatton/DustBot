# DustBot difficulty, economy, and Daily Challenge update

## Difficulty and engagement pacing

- Levels 1–15 remain the hand-authored tutorial sequence.
- Levels 16–25 use short 5×5 puzzles with earlier blockers, two crumbs from
  Level 20, and gentle hazards from roughly Level 20 onward.
- Levels 26–50 use longer routes, two to three crumbs, meaningful blockers,
  gentle hazards, and rare Dust Bunny detours.
- Levels 51–100 commonly use three crumbs, occasionally four, with stronger
  route-shaping obstacles and meaningful three-star requirements.
- Levels 101–200 use more 6×7 boards, three to four crumbs, tighter move
  targets, and more frequent Dust Bunnies.
- Levels 201+ continue into larger boards and combined mechanics.

Main Journey follows a deterministic six-level hook rhythm:

1. normal/simple or crumb-order puzzle
2. blocker maze
3. trickier routing puzzle
4. breather
5. Dust Bunny detour
6. hazard-avoidance or tight-path puzzle

The implemented archetypes are Simple Route, Crumb Order, Blocker Maze,
Hazard Avoidance, Dust Bunny Detour, Tight Path, Breather, Trick Route, and
Challenge Route.

## Boring-level rejection

`LevelEngagementEvaluator` scores every generated candidate using:

- solution length
- route turns
- endpoint detour
- crumb count
- alternate route decisions
- blockers adjacent to the intended route
- hazards adjacent to the intended route
- Dust Bunny objective presence

After Level 25, candidates are rejected when they are too short, too straight,
too sparse, too dense, or below the level range’s engagement target. Rejected
candidates advance through deterministic candidate seeds, so all players still
receive the same accepted puzzle.

## Canonical determinism

Generation is now version 3. Main Journey metadata is still a pure function of
level number; Daily metadata is a pure function of calendar date; Master Clean
is a pure function of Master level number; Endless is a pure function of run
seed and room number.

The validator compares full serialized signatures and simulations for the
canonical checkpoints, including Levels 20, 100, 500, and 6000. The generator
still supports versions 1 and 2 when supplied by older metadata.

## Economy

All tunable values live in `EconomyConfig`:

- normal completion: 10 Dust Coins
- two-star total bonus: 5
- three-star total bonus: 10
- Dust Bunny: 15
- milestone every 25 levels: 100
- normal hint: 50
- Daily hint: 75
- tutorials 1–3: free hints

Normal retries and progression never cost coins.

Dust Bunnies are optional, saved per level, displayed in level select, counted
globally, and awarded only once per puzzle. They appear rarely at Levels 26–50,
occasionally from Level 51 onward, frequently later, and almost always in
Daily/Master challenge profiles.

Stars are calculated as:

- one star: complete
- two stars: meet the forgiving two-star move target
- three stars: meet the near-optimal target, satisfy no-hint/no-undo rules,
  and collect the Dust Bunny when that level requires it

## Cosmetics

The save now includes owned cosmetics and active selections. Implemented
entries:

- Classic Mint bot
- Honey Polish bot, 400 coins
- Bunny Lavender bot, unlocked at 10 Dust Bunnies
- Mint Route
- Coral Route, 250 coins
- Sky Route, 300 coins

Bot tint and route color visibly update during gameplay. Cosmetics never alter
rules or outcomes. A non-functional `AdsStub` interface remains available for
a future consent-aware rewarded-hint integration; no ad SDK is installed.

## Daily Challenge

Daily Challenge has a separate generation profile:

- deterministic date seed
- usually 7×7, 7×8, 8×7, or 8×8
- four to five crumbs
- meaningful blockers and hazards
- Dust Bunny enabled
- at least six turns
- at least four tiles of endpoint detour
- engagement target of at least 28
- challenge-focused archetypes only

Daily rewards:

- base completion: 75
- two-star bonus: 20
- three-star bonus: 50
- Dust Bunny: 25
- no-hint bonus: 25
- first-attempt bonus: 25
- seven-day repeating streak schedule: 75, 90, 110, 130, 150, 175, 225

Base and streak rewards can be claimed once per date. Bunny and no-hint rewards
can be claimed once when achieved. Star improvement rewards only pay the
difference between the previous and improved rating. Replay remains free and
can improve stars or collect a missed Bunny without enabling infinite farming.

## Manual test recommendations

Difficulty:

- Levels 1–15 for tutorial clarity
- Levels 16, 20, and 25 for early hooks
- Levels 26–32 and 45–55 for archetype rhythm
- Levels 75, 100, 150, 200, 500, and 6000 for scaling

Economy:

- first clear versus replay of the same Main level
- one-, two-, and three-star improvements
- collecting a Bunny on replay
- Levels 25 and 50 milestone rewards
- free tutorial hint, 50-coin normal hint, insufficient coins
- buying/selecting path colors and bot skins
- unlocking Bunny Lavender at 10 Bunnies
- save, restart, and confirm ownership/selections

Daily:

- complete once, then replay on the same date
- improve from one star to three
- collect a missed Bunny on replay
- use a hint and verify no no-hint bonus
- fail once, then complete and verify no first-attempt bonus
- complete on consecutive dates and verify streak progression
- restart the app and verify the same date produces the same board

## Remaining balance concerns

- A perfect first-attempt Daily clear is intentionally generous. Retention data
  may justify reducing the combined Daily payout later.
- Engagement scoring is a strong structural filter, but human playtesting
  should still tune individual thresholds and the frequency of breather levels.
- The first 50 generated levels deserve the most hands-on testing because they
  determine whether “interesting sooner” still feels welcoming.
