# DustBot gameplay, cat, movement, and audio polish report

> Cat mechanics in this earlier report have been superseded by
> `CAT_CHASE_TURN_BASED_REPORT.md`. Normal movement and iOS audio sections remain
> applicable.

## Result

This pass adds a complete deterministic pet-cat obstacle system, more expressive
movement and feedback, a distinct invalid-route language, and an explicit iOS
audio setup. Existing path drawing, progression, economy, cosmetics, Daily
Challenge, Master Clean, saves, menus, hints, win/fail flow, and canonical level
generation remain intact.

Validation passes for all 6,000 journey levels. The current journey contains
3,178 deterministic cat levels whose canonical routes are both safe and
strategically active.

## DustBot movement

DustBot remains grid-based and deterministic, but presentation now cruises
continuously through the route instead of treating every tile like a separate
hop.

- Travel uses a constant 0.15 seconds per tile.
- Position interpolation is linear, so speed does not ease down at tile
  centers.
- There is no turn lead-in, vertical hop, squash/stretch, landing settle, or
  inter-tile delay.
- Rotation blends during forward movement and completes before the end of the
  tile segment.
- Cat reactions animate within the same movement beat instead of pausing
  DustBot between tiles.
- Crumbs pop, rotate, enlarge, and fade when cleaned.
- Docking adds a 0.28-second settling animation before the selected win
  animation.
- Cat collisions use a dedicated pounce and flattened DustBot impact.
- Other failures retain the selected cosmetic animation with a stronger,
  reserved error color.

Retry still returns directly to route editing.

## Trail and invalid-route readability

The former red `Coral Route` cosmetic is now the muted purple-brown
`Rosewood Trail` (`#9B5A72`). The orange Sunset trail remains a warm cosmetic,
but neither trail is used as the invalid-state language.

Invalid route feedback now combines:

- Reserved error red (`#DA4247`)
- A large white-outlined red ×
- Horizontal tile shake
- Status text explaining the block
- Existing error feedback hook

Losing perfect-clean eligibility uses amber warning color instead of error red.

## Pet cat obstacle

The cat is a separate moving-obstacle definition rather than a static cell, so
it does not replace crumbs, docks, hazards, or furniture.

DustBot always moves first. The cat then reacts:

- Sleepy Cat moves one tile after every second DustBot step.
- Curious Cat moves one tile after every DustBot step.
- Pouncy Cat moves one tile normally and two tiles on every second turn.

The cat greedily reduces Manhattan distance with a deterministic horizontal or
vertical tie-break stored in the level definition. Walls, toys, socks, cords,
and wet spots block its movement. A collision produces `CatPounce` failure.

While editing, predicted cat positions appear as amber paw prints. The HUD says
`CAT SAFE` or identifies the first dangerous step. The same pure simulator is
used by the editor preview, runtime session, generator, validator, and
engagement scoring.

## Introduction and generation

- No cats appear in Levels 1–25.
- Level 30 introduces Sleepy Cat.
- Level 45 introduces Curious Cat.
- Level 180 introduces Pouncy Cat.
- Cats appear regularly from Level 41 onward, in roughly half of Levels 76–150,
  and are attempted in most later Journey rooms. They are also prevalent in
  Master, Endless, and eligible Daily layouts.
- Optional cat slots deterministically omit the cat when the room has no fair,
  strategically active placement.

Generation version 5 includes cat behavior, start tile, tie-break direction,
and any required furniture in the level signature. A candidate is accepted
only when:

- The canonical route is not caught.
- The cat has an open neighboring tile and at least four reachable tiles.
- It moves on at least three turns and visits at least three unique positions.
- It comes within two tiles of DustBot for at least two turns, producing
  meaningful visible pressure instead of acting as decoration.
- Start and all generated content are valid.
- The normal engagement and solvability constraints still pass.

The Daily seed continues to derive from the date, so the same date produces the
same cat layout and behavior whenever a cat is present.

## Obstacle architecture

The cat is the new obstacle family, with three behaviors sharing one pure,
deterministic simulation interface. Existing room clutter now also participates
in cat navigation, creating combinations such as furniture traps and blocked
chase lanes.

This architecture cleanly supports future moving variants without adding
special cases to route drawing or save data. No additional conveyor, switch, or
fragile-tile mechanic was added in this pass; those would need their own route
cost and tutorial work rather than being cosmetic reskins.

## iPhone audio

The empty boot scene did not guarantee an `AudioListener`, even though the game
created procedural audio sources. DustBot now creates exactly one persistent
listener at startup, restores listener state after resume, and gives menu,
music, and effects explicit 2D source settings.

On iOS:

- `DustBotAudioSession.mm` configures `AVAudioSessionCategoryPlayback`.
- `MixWithOthers` allows existing player audio to continue.
- The audio session is reactivated after focus/resume.
- The post-build processor links `AVFoundation.framework`.

The Unity iOS export confirms that the native source and framework are present.
See `IOS_AUDIO.md` for the device checklist.

## Generated cat artwork

Final asset: `Assets/Resources/Sprites/cat.png` (512 × 512 transparent PNG).

It was generated with the built-in image tool as a cozy, polished, readable
orange-tabby mobile-game sprite on a flat magenta chroma-key background, with
one centered cat, no text, props, shadow, or watermark. The background was
removed locally with a soft matte and despill, then the result was downscaled
for runtime use.

## Automated verification

- Unity script compilation: passed.
- 6,000 canonical journey levels: passed.
- Deterministic signatures, including cat introductions: passed.
- Independent cat relevance audit: 3,178 cats, 0 weak, 0 trapped.
- Minimum roaming area: 4 reachable tiles.
- Minimum unique visited positions: 3.
- Minimum movement turns: 3.
- Maximum closest distance from DustBot: 2 tiles.
- Minimum pressure turns within two tiles: 2.
- Canonical route simulation for every level: passed.
- Daily and Master deterministic validation: passed.
- Economy, cosmetic, progression, hazard, and save-repair validation: passed.
- macOS player build: passed.
- Screenshot capture and cat-level visual QA: passed.
- Unity iOS export: passed (554.7 MB).
- Native iOS audio plugin copied and AVFoundation linked: verified.

## Manual iPhone checks

1. Test with the hardware silent switch both on and off.
2. Test Sound and Music toggles independently, including after relaunch.
3. Verify menu, route, move, crumb, cat, dock, win, and fail sounds.
4. Background and resume the app while other audio is playing.
5. Connect and disconnect Bluetooth headphones during play.
6. Play Levels 30, 45, and 180 and confirm paw previews match movement.
7. Draw a deliberately unsafe cat route and confirm the HUD warning and failure.
8. Try invalid tiles with several trail cosmetics and confirm the × remains
   obvious.
9. Check movement pacing on an older supported iPhone at a stable 60 FPS.

## Remaining balance considerations

The cat behaviors are intentionally deterministic and use greedy pursuit rather
than full pathfinding. This keeps them readable, but advanced players may learn
to exploit tie-break direction and clutter. Cat frequency and Pouncy Cat
pressure should be tuned using device playtests and completion analytics.

The iOS project export is verified, but final confirmation of silent-switch,
Bluetooth, interruption, and speaker behavior requires a physical signed iPhone
build.
