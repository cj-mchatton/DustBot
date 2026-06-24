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

## Complete game modes

- **Main Journey:** 6,000 deterministic rooms, including 15 hand-authored tutorial levels.
- **Daily Challenge:** one deterministic room per calendar day with streak rewards.
- **Master Clean:** unlocks after completing the Main Journey.
- **Endless Clean:** persistent room progression and best score.

Progress, settings, stars, Dust Coins, Dust Bunnies, cosmetics, daily streaks,
and endless progress are saved locally with a backup-safe JSON save system.

The Cosmetic Store contains categorized skins, trails, tile themes, docks,
animations, room backgrounds, and bundles with rarity, coin prices, and
achievement requirements.

## Release validation

Use these Unity menu commands:

- `DustBot > Configure Project` applies the iPhone release settings and app icon.
- `DustBot > Run Content Validation` compiles and simulates all 6,000 journey levels plus tutorial, daily, master, hazard, determinism, reward, and save-repair checks.
- `DustBot > Build iOS Release` validates the game and creates the Xcode project at `Build/iOS`.

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
