# Cozy lofi audio update

## Background music

`Cozy Lofi Loop.mp3` is imported at:

`Assets/Resources/Audio/Music/Cozy Lofi Loop.mp3`

The imported file is byte-for-byte identical to the supplied source. Unity
identifies it as a 240.05-second, stereo, 48 kHz clip. `AudioManager` loads it
from Resources at startup, assigns it to the persistent music source, enables
`AudioSource.loop`, and uses that same source for the main menu and every
current gameplay mode. Screen changes therefore do not reset the song.

The import postprocessor preserves stereo/48 kHz, preloads the clip, and uses
Decompress On Load plus PCM for stable loop timing on iOS. The base music gain
is 0.16 before the player's saved Music Volume setting, keeping it behind SFX.

## Cozy SFX palette

The old small procedural palette was replaced by named, warm procedural cues
covering:

- UI taps, back, menu open/close, store selection, purchase success, not enough
  coins, and settings toggles.
- Path start/add/backtrack, invalid route, path-too-long, reset, ready, and Play.
- DustBot start/movement, crumb cleanup, Dust Bunny, dock, level complete, star,
  Perfect Clean, hint open/use/cancel.
- Soft general failure, obstacle bonk, sock/cord/wet/fragile hazard failure.
- Cat paw, danger, near-catch, funny swat plus sad DustBot beep, and a reserved
  cat-toy cue for when that mechanic is active.

The sounds use sine/triangle bodies, gentle attack/decay envelopes, low-passed
noise for the crumb pop, and low amplitudes. No raw square waves or sharp alarm
tones are used.

## Repetition and mixing

Music, gameplay SFX, and UI SFX have separate runtime source groups and base
gains (equivalent volume separation without a scene-bound AudioMixer asset).
Gameplay has five bounded voices and UI has three, preventing unbounded
`PlayOneShot` stacking. Repeated cues have per-clip cooldowns. Path additions,
movement, crumbs, cat steps, and UI taps receive small pitch variation.

Win audio is staged: dock arrival, level-complete phrase, then star or Perfect
Clean feedback. This avoids playing all reward cues on the same frame.

## Settings and saves

The existing save schema remains unchanged. `soundEnabled`, `musicEnabled`,
`soundVolume`, and `musicVolume` still default on, are sanitized on load, are
saved immediately from Settings, and are applied to the persistent manager.
Disabling Sound stops active SFX/UI voices. Disabling Music pauses rather than
restarts the track, so re-enabling it resumes naturally.

## iPhone preparation and verification

- Unity iOS export passed at `Build/iOS`.
- The track is packed in the iOS Resources data.
- IL2CPP compilation passed.
- The native `DustBotAudioSession.mm` plugin is included.
- `AVFoundation.framework` is linked in the generated Xcode project.
- The persistent listener/source initialization and focus/resume reactivation
  remain intact.
- `AVAudioSessionCategoryPlayback` means audio is intended to play through the
  hardware silent switch; `MixWithOthers` allows the player's other audio.

A physical-iPhone listening pass is still required before release to judge the
music/SFX balance on phone speakers, hear the actual loop boundary, exercise
the silent switch, and test Bluetooth interruption/reconnection. Unity/Xcode
export verification cannot replace those hardware checks.

## Regression validation

Passed:

- Cozy audio import validation: 240.05 s, 48 kHz, stereo, preloaded PCM.
- Development modes: 30 Development Campaign, 24 Cat Testing, 18 Obstacle
  Testing, 8 Tutorial Testing, and 20 Maze Testing levels.
- Full content suite: 6,000 Production levels, 700 deterministic cat levels,
  4,947 large mazes, Daily, Master Clean, economy, cosmetics, hazards,
  progression, and save sanitation.
- iOS release export, including audio asset and native plugin integration.

## Replaceable assets

The procedural SFX are production-usable placeholders with the requested sonic
direction. They can later be swapped for mastered recordings or designed WAVs
without changing gameplay call sites. The most valuable future recording pass
would be the repeated path tap, crumb clean, cat paw/swat, and reward jingles.
