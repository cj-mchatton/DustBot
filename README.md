# DustBot

DustBot is a polished, portrait-first Unity route puzzle for iPhone. It is an offline game with no ads, no in-app purchases, no accounts, no tracking, and no network dependency.

## Play

To play the standalone game immediately, double-click `Play DustBot.command`.

To run and edit the latest project in Unity:

1. Double-click `Open DustBot.command`.
2. If macOS asks, choose **Open**.
3. In Unity, open `Assets/Scenes/Boot.unity` if it is not already open.
4. Press the ▶ Play button at the top of the Unity window.

Alternatively, open Unity Hub, choose **Add project from disk**, select this
folder, and open it with Unity `6000.5.0f1`.

Touch DustBot and drag through orthogonally adjacent tiles to draw one continuous route. Clean every crumb and finish at the charging dock. Drag backward to erase, or use Undo, Reset, and earned-coin hints.

Development builds include a separate Maze Testing playlist with carved mazes
from 10×10 through 26×26. Drag anywhere away from DustBot to pan, pinch or use
−/+ to zoom, and tap ◎ to return to the readable overview. The camera follows
DustBot during long simulations and edge-pans while a route is being drawn.

Cat Chase rooms switch to one-tile directional swipes. DustBot moves first,
then a predictable Curious Cat makes two horizontal-first pursuit moves.
Furniture, corners, and corridors become tools for cleaning safely.

## Complete game modes

- **Main levels:** 260 deterministic rooms split into Easy (10), Medium (50), Hard (50), Expert (100), and Cat Chase (50).
- **Daily Challenge:** one deterministic room per calendar day with streak rewards.
- **Master Clean:** unlocks after completing all Expert levels.
- **Endless Clean:** persistent room progression and best score.

## Development generation modes

Unity Editor and development builds default to a deterministic 30-level
Development Campaign. Open **DEV** from the main menu or any level header to
switch among Production Categories (260), Development Campaign (30), Maze
Testing (20), Cat Testing (24), Obstacle Testing (18), and Tutorial Testing (8).
Non-development release builds always force Production Categories and do not
expose the panel.

The panel supports level jumping/navigation, deterministic regeneration, save
reset, progression/cosmetic unlocks, coin grants, and detailed seed, solver,
difficulty, obstacle, and cat-pressure metadata. Test campaign completions do
not advance production journey, Daily Challenge, or Master Clean progress.

Progress, settings, stars, Dust Coins, Dust Bunnies, cosmetics, daily streaks,
and endless progress are saved locally with a backup-safe JSON save system.

The Cosmetic Store contains categorized skins, trails, tile themes, docks,
animations, room backgrounds, and bundles with rarity, coin prices, and
achievement requirements.

## Release validation

Use these Unity menu commands:

- `DustBot > Configure Project` applies the iPhone release settings and app icon.
- `DustBot > Run Content Validation` compiles and simulates all 260 category levels plus tutorial, daily, master, hazard, determinism, reward, and save-repair checks.
- `DustBot > Run Development Mode Validation` checks and simulates every compact testing playlist twice for determinism.
- `DustBot > Validate Large Mazes` checks the Dev Level 9 baseline, nine advanced Development Campaign mazes, all 20 Maze Testing fixtures through 26×26, and the existing production Master fixture. It audits topology and draws each canonical long route through the gameplay input system.
- `DustBot > Build iOS Release` validates the game and creates the Xcode project at `Build/iOS`.

Implementation details and physical-device checks are documented in
`GAMEPLAY_POLISH_CAT_AUDIO_REPORT.md` and `IOS_AUDIO.md`.

Command-line validation:

```sh
/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "/path/to/DustBot" \
  -executeMethod DustBot.Editor.DustBotProjectBuilder.RunContentValidation \
  -quit -logFile -
```

## App Store package

The `AppStore` folder contains the English product-page copy, review notes, privacy disclosures, age-rating guidance, support page, privacy policy, and final submission checklist.

The remaining account-specific steps are:

1. Replace `com.dustbotgames.dustbot` if that bundle identifier is not owned by your Apple Developer account.
2. Select your development team/signing certificate in Xcode.
3. Host `AppStore/privacy-policy.html` and `AppStore/support.html` on public HTTPS URLs.
4. Create the App Store Connect record, enter the supplied metadata, upload screenshots, archive, and upload.

The project targets iPhone only, iOS 15 or later, and is configured for IL2CPP release builds. Unity 6 supplies the required iOS privacy manifest for its runtime APIs.
