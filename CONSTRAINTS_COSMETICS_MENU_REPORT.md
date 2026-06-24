# DustBot constraint, cosmetics, and menu update

## Stronger puzzle constraints

- Generation version 4 separates soft star targets from true hard path caps.
- Levels 1–3 remain penalty-free onboarding.
- Levels 4–7 introduce turns, multiple crumbs, and small route decisions through
  the hand-authored tutorial sequence.
- Levels 8–12 use blockers, hazards, backtracking, and more deliberate routing.
- Levels 13–20 use efficient-route star targets, occasional three-crumb rooms,
  wrong-looking branches, and the first optional Dust Bunnies.
- Levels 21–35 use real route-order pressure, blockers, hazards, and stricter
  perfect-clean targets.
- Levels 36–60 introduce occasional hard maximum path lengths.
- Levels 61+ normally use hard caps except intentional breather rooms.
- Later tiers progressively tighten path slack, increase board size, and combine
  crumb order, hazards, blockers, Bunny tradeoffs, and near-optimal routing.

The player can still draw freely on soft-target levels. Exceeding the perfect
or two-star target only lowers the rating. On hard-cap levels, the game refuses
to extend the route past the displayed maximum and explains how to backtrack.

The gameplay HUD now shows:

- planned crumbs remaining
- Dust Bunny routed or waiting
- current path length
- perfect path target
- two-star target or hard maximum
- whether a perfect clean is still available

The route turns coral when hints, Undo, excess length, or a missed required
Bunny make a three-star clear unavailable.

## Difficulty scoring and rejection

`LevelEngagementEvaluator` now scores:

- solution length and turns
- endpoint detour
- spatial crumb spread
- crumb-order complexity
- real route branches and decisions
- blockers and hazards touching meaningful route choices
- tempting wrong branches
- actual Dust Bunny shortcut-versus-detour cost
- soft versus hard path pressure

Tutorial-like candidates are rejected increasingly aggressively after Levels 7,
10, 15, and 25. Dense Tight Path and Blocker Maze rooms can qualify through
their hard caps and route-order pressure rather than being forced to contain
artificial open branches.

Dust Bunny rooms can now contain a real bypass: skipping the detour produces a
shorter completion route, while following it collects the Bunny and preserves
the perfect-clean objective.

The six-room rhythm still includes a deliberate Breather archetype. Breathers
keep at least two crumbs after the tutorial but relax hard caps and density.

## Daily Challenge

Daily Challenge now uses:

- medium/hard deterministic metadata
- four to five spatially separated crumbs
- a real Dust Bunny detour
- meaningful blockers and hazards
- at least eight solution turns
- a minimum endpoint detour
- a hard maximum path length
- stronger engagement and route-choice thresholds

Daily rewards, hints, streaks, replay improvements, and one-time claim
protections are unchanged.

## Determinism

Generation rules were moved to version 4. Versions 1–3 remain supported.

Main Journey remains a pure function of level number and version. Daily remains
a pure function of date. Master Clean remains a pure function of Master level
number. Endless remains a pure function of run seed and room number.

Automated signature checks cover Levels 20, 50, 100, 500, 1000, 2000, 4000,
and 6000, plus Daily and Master Clean.

## Expanded cosmetic store

The store now contains more than 50 entries across eight categories:

- DustBot skins
- path trails
- tile themes
- dock designs
- win animations
- failure animations
- room backgrounds
- bundles

Rarity tiers are Common, Uncommon, Rare, Epic, and Legendary. Prices span from
250 Dust Coins for entry-level cosmetics to 20,000 Dust Coins for legendary
items.

Achievement gates include:

- reaching specific Main Journey levels
- total stars
- total Dust Bunnies
- Daily Challenge completions
- no-hint Daily completions
- best Daily streak
- reaching Master Clean

Purchases require both the achievement gate and enough Dust Coins. Ownership,
purchase history, and equipped selections are saved. Bundles unlock every
included item. Legacy Honey, Lavender, and Coral cosmetic IDs remain supported
for older saves.

Visible cosmetic effects currently include:

- DustBot tint/skin
- path color
- board tile palette
- dock tint/design
- room background color
- win animation style/color
- failure animation behavior

Cosmetics never modify generation, route rules, scoring, or rewards.

## Main menu organization

The main menu now prioritizes:

1. large Play/Continue button
2. Daily Challenge card with streak and reward preview
3. Cosmetic Store
4. Level Select
5. Master Clean and Endless

The top area contains the title, Dust Coin balance, and Settings. A lightweight
animated DustBot hero card displays current level, stars, Bunnies, and Daily
streak. The layout remains portrait-safe and uses only inexpensive UI
animations.

## Manual testing

Difficulty:

- Levels 1–3, 4–7, 8–12, and 13–20
- Levels 21, 25, 35, 36, 42, 50, 60, 61, 75, and 100
- Levels 101, 250, 500, 1000, 2000, 4000, and 6000
- several Breather, Tight Path, Trick Route, and Dust Bunny Detour rooms
- Master Clean rooms 1, 10, and 100

Constraints and scoring:

- exceed a soft perfect target and verify stars drop without failure
- reach a hard maximum and verify route extension is blocked
- backtrack from the maximum and continue drawing
- complete with one, two, and three stars
- miss a required Bunny and replay
- use a hint and verify the perfect/no-hint messaging

Economy:

- first clear and replay reward protection
- star improvement rewards
- Bunny collection on replay
- normal and Daily hint purchases
- milestone and Daily streak rewards

Cosmetics:

- browse every category and page
- attempt locked purchases
- attempt purchases without enough coins
- purchase and equip each visible category
- restart and confirm ownership/equipment
- buy a bundle and verify included ownership
- confirm legacy cosmetics still load

Menu/UI:

- safe area on several portrait iPhones
- main Play, Daily, Store, Level Select, Settings, Master, and Endless routes
- Daily completed/uncompleted card states
- long store requirement labels and all rarity colors

## Remaining balance concerns

- Generation constraints are structurally much stronger, but the ideal hard-cap
  frequency between Levels 61–250 should be tuned through human play sessions.
- Legendary prices deliberately represent long-term goals; real retention data
  may justify reducing individual prices or adding more milestone coin rewards.
- The generated Bunny shortcut is deterministic and validated, but specific
  detours should be sampled manually to ensure they read clearly at a glance.
