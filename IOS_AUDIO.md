# DustBot iPhone audio

DustBot now creates one persistent `AudioListener` at startup. The empty boot
scene previously created procedural `AudioSource` components without
guaranteeing that a listener existed, which can make all game audio inaudible.

On iOS, `DustBotAudioSession.mm` configures `AVAudioSession` with the Playback
category and `MixWithOthers`. This makes game audio audible when the hardware
silent switch is enabled while allowing the player's existing audio to
continue. The session is reactivated after the app regains focus or resumes.

The iOS post-build step explicitly links `AVFoundation.framework`.

Manual device checks:

1. Launch with the hardware silent switch both on and off.
2. Verify menu music, button taps, route sounds, movement, crumbs, cat steps,
   docking, victory, and failure.
3. Toggle Sound and Music independently, relaunch, and verify both saved states.
4. Background the app, play other audio, return to DustBot, and verify the mix.
5. Connect and disconnect Bluetooth headphones during play.
