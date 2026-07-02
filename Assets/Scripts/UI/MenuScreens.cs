using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DustBot
{
    public static class MenuScreens
    {
        public const int LevelsPerPage = 24;

        public static GameObject BuildMainMenu(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("Main Menu", parent);
            GenerationMode generationMode = app.Levels.ActiveGenerationMode;
            bool production = generationMode == GenerationMode.ProductionCampaign;
            bool journeyComplete = production && app.Progression.IsMainJourneyComplete();
            bool masterAvailable = app.CanAccessMasterClean;

            Text title = UIFactory.CreateText("Title", root.transform, "DUSTBOT", 76, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.22f, 0.915f), new Vector2(0.78f, 0.985f), Vector2.zero, Vector2.zero);

            Button coins = UIFactory.CreateButton(
                "Coins",
                root.transform,
                app.Economy.Coins + " COINS",
                app.UI.ShowCosmetics,
                DustBotTheme.Yellow,
                22);
            UIFactory.GetButtonText(coins).color = DustBotTheme.Ink;
            UIFactory.SetAnchors(coins.GetComponent<RectTransform>(), new Vector2(0.035f, 0.925f), new Vector2(0.235f, 0.975f), Vector2.zero, Vector2.zero);

            Button settings = UIFactory.CreateButton(
                "Settings",
                root.transform,
                "SETTINGS",
                app.UI.ShowSettings,
                DustBotTheme.MutedInk,
                20);
            UIFactory.SetAnchors(settings.GetComponent<RectTransform>(), new Vector2(0.79f, 0.925f), new Vector2(0.965f, 0.975f), Vector2.zero, Vector2.zero);

            Image hero = UIFactory.CreatePanel("Hero Card", root.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(hero.rectTransform, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.91f), Vector2.zero, Vector2.zero);
            Shadow heroShadow = hero.gameObject.AddComponent<Shadow>();
            heroShadow.effectColor = new Color(0.08f, 0.16f, 0.12f, 0.16f);
            heroShadow.effectDistance = new Vector2(0f, -7f);

            Image bot = UIFactory.CreatePanel("Bot Glow", hero.transform, DustBotTheme.PanelSoft);
            UIFactory.SetAnchors(bot.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.36f, 0.88f), Vector2.zero, Vector2.zero);
            GameObject botSpriteObject = UIFactory.CreateUIObject("Equipped DustBot", bot.transform);
            Image botSprite = botSpriteObject.AddComponent<Image>();
            botSprite.sprite = DustBotSprites.Player;
            botSprite.color = app.Cosmetics.ActiveBotTint;
            botSprite.preserveAspect = true;
            botSprite.raycastTarget = false;
            UIFactory.Stretch(botSpriteObject);
            botSprite.rectTransform.offsetMin = Vector2.one * 18f;
            botSprite.rectTransform.offsetMax = Vector2.one * -18f;
            botSpriteObject.AddComponent<MenuIdleAnimator>();

            Text greeting = UIFactory.CreateText(
                "Greeting",
                hero.transform,
                journeyComplete
                    ? "MASTER CLEAN AWAITS"
                    : production
                        ? "READY FOR THE NEXT ROOM?"
                        : LevelGenerationConfig.DisplayName(generationMode),
                32,
                DustBotTheme.Ink,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(greeting.rectTransform, new Vector2(0.4f, 0.55f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);
            greeting.fontStyle = FontStyle.Bold;
            Text progress = UIFactory.CreateText(
                "Progress Summary",
                hero.transform,
                production
                    ? string.Format(
                        "LEVEL {0}\n{1} STARS  •  {2} BUNNIES\n{3} DAY DAILY STREAK",
                        app.Progression.Data.highestUnlockedMainLevel,
                        app.Progression.Data.totalStars,
                        app.Progression.Data.totalDustBunnies,
                        app.Progression.Data.daily.currentStreak)
                    : string.Format(
                        "TEST LEVEL {0} OF {1}\n{2} COINS  •  PRODUCTION SAVE ISOLATED",
                        app.CurrentCampaignLevel,
                        app.Levels.CampaignLevelCount,
                        app.Economy.Coins),
                25,
                DustBotTheme.MutedInk,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(progress.rectTransform, new Vector2(0.4f, 0.12f), new Vector2(0.95f, 0.57f), Vector2.zero, Vector2.zero);

            Button play = UIFactory.CreateButton(
                "Primary Play",
                root.transform,
                journeyComplete
                    ? "PLAY MASTER CLEAN"
                    : "PLAY  •  LEVEL " +
                      (production
                          ? app.Progression.Data.highestUnlockedMainLevel
                          : app.CurrentCampaignLevel),
                delegate
                {
                    if (journeyComplete) app.StartMaster();
                    else app.StartMainLevel(
                        production
                            ? app.Progression.Data.highestUnlockedMainLevel
                            : app.CurrentCampaignLevel);
                },
                DustBotTheme.Mint,
                40);
            UIFactory.SetAnchors(play.GetComponent<RectTransform>(), new Vector2(0.08f, 0.575f), new Vector2(0.92f, 0.675f), Vector2.zero, Vector2.zero);

            string todayKey = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            bool dailyDone = app.Progression.Data.daily.lastCompletedDate == todayKey;
            CreateMenuCard(
                root.transform,
                "Daily Card",
                "DAILY CHALLENGE",
                dailyDone
                    ? "Complete • Replay for stars and Bunny"
                    : string.Format(
                        "Harder daily puzzle • {0}-day streak • Reward 75+",
                        app.Progression.Data.daily.currentStreak),
                app.StartDaily,
                DustBotTheme.Yellow,
                DustBotTheme.Ink,
                DustBotSprites.DustBunny,
                new Vector2(0.08f, 0.445f),
                new Vector2(0.92f, 0.56f));

            CreateMenuCard(
                root.transform,
                "Store Card",
                "COSMETIC STORE",
                "Skins, trails, themes, docks, rooms and bundles",
                app.UI.ShowCosmetics,
                DustBotTheme.Coral,
                Color.white,
                DustBotSprites.Player,
                new Vector2(0.08f, 0.325f),
                new Vector2(0.49f, 0.43f));
            CreateMenuCard(
                root.transform,
                "Levels Card",
                "LEVEL SELECT",
                "Replay rooms and chase perfect cleans",
                delegate { app.UI.ShowLevelSelect(); },
                DustBotTheme.Blue,
                Color.white,
                DustBotSprites.Crumbs,
                new Vector2(0.51f, 0.325f),
                new Vector2(0.92f, 0.43f));

            Button master = UIFactory.CreateButton(
                "Master",
                root.transform,
                journeyComplete
                    ? "MASTER CLEAN"
                    : masterAvailable ? "MASTER CLEAN • DEV" : "MASTER CLEAN • LOCKED",
                app.StartMaster,
                DustBotTheme.Coral,
                24);
            UIFactory.SetAnchors(master.GetComponent<RectTransform>(), new Vector2(0.08f, 0.225f), new Vector2(0.49f, 0.305f), Vector2.zero, Vector2.zero);

            Button endless = UIFactory.CreateButton(
                "Endless",
                root.transform,
                string.Format(
                    "ENDLESS • {0} • BEST {1}",
                    app.Progression.Data.endlessCurrentLevel,
                    app.Progression.Data.endlessBestScore),
                app.StartEndless,
                DustBotTheme.MintDark,
                22);
            UIFactory.SetAnchors(endless.GetComponent<RectTransform>(), new Vector2(0.51f, 0.225f), new Vector2(0.92f, 0.305f), Vector2.zero, Vector2.zero);

            Text footer = UIFactory.CreateText(
                "Footer",
                root.transform,
                "Tiny bot. Big mess. Impeccable standards.",
                25,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(footer.rectTransform, new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.2f), Vector2.zero, Vector2.zero);

            if (LevelGenerationConfig.DeveloperToolsEnabled)
            {
                Button developer = UIFactory.CreateButton(
                    "Developer Panel",
                    root.transform,
                    "DEV • " + LevelGenerationConfig.DisplayName(generationMode),
                    app.UI.ShowDeveloperPanel,
                    DustBotTheme.MutedInk,
                    20);
                UIFactory.SetAnchors(
                    developer.GetComponent<RectTransform>(),
                    new Vector2(0.18f, 0.045f),
                    new Vector2(0.82f, 0.115f),
                    Vector2.zero,
                    Vector2.zero);
            }

            return root;
        }

        public static GameObject BuildLevelSelect(DustBotApp app, RectTransform parent, int requestedPage)
        {
            int levelCount = app.Levels.CampaignLevelCount;
            GenerationMode generationMode = app.Levels.ActiveGenerationMode;
            bool production = generationMode == GenerationMode.ProductionCampaign;
            int pageCount = (levelCount + LevelsPerPage - 1) / LevelsPerPage;
            int page = Mathf.Clamp(requestedPage, 0, pageCount - 1);
            GameObject root = CreateRoot("Level Select", parent);

            Button back = UIFactory.CreateButton("Back", root.transform, "HOME", app.UI.ShowMainMenu, DustBotTheme.MutedInk, 26);
            UIFactory.SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.91f), new Vector2(0.25f, 0.975f), Vector2.zero, Vector2.zero);

            Text title = UIFactory.CreateText(
                "Title",
                root.transform,
                production ? "MAIN JOURNEY" : LevelGenerationConfig.DisplayName(generationMode),
                production ? 54 : 42,
                DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.25f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            GameObject gridObject = UIFactory.CreateUIObject("Level Grid", root.transform);
            RectTransform gridRect = gridObject.GetComponent<RectTransform>();
            UIFactory.SetAnchors(gridRect, new Vector2(0.055f, 0.16f), new Vector2(0.945f, 0.89f), Vector2.zero, Vector2.zero);
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(205f, 135f);
            grid.spacing = new Vector2(18f, 18f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            int firstLevel = page * LevelsPerPage + 1;
            int lastLevel = Mathf.Min(levelCount, firstLevel + LevelsPerPage - 1);
            for (int levelNumber = firstLevel; levelNumber <= lastLevel; levelNumber++)
            {
                int capturedLevel = levelNumber;
                bool unlocked = !production || levelNumber <= app.Progression.Data.highestUnlockedMainLevel;
                int stars = production ? app.Progression.GetStars(levelNumber) : 0;
                bool bunny = production && app.Progression.HasDustBunny(levelNumber);
                string label = unlocked
                    ? string.Format("{0}\n{1}{2}", levelNumber, StarText(stars), bunny ? "  ◆" : string.Empty)
                    : string.Format("{0}\nLOCKED", levelNumber);
                Button button = UIFactory.CreateButton(
                    "Level " + levelNumber,
                    grid.transform,
                    label,
                    delegate { app.StartMainLevel(capturedLevel); },
                    unlocked ? DustBotTheme.Mint : new Color32(176, 187, 181, 255),
                    28);
                button.interactable = unlocked;
            }

            Text pageText = UIFactory.CreateText(
                "Page",
                root.transform,
                string.Format("Levels {0}-{1} of {2}", firstLevel, lastLevel, levelCount),
                25,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(pageText.rectTransform, new Vector2(0.28f, 0.07f), new Vector2(0.72f, 0.14f), Vector2.zero, Vector2.zero);

            Button previous = UIFactory.CreateButton("Previous", root.transform, "PREV", delegate
            {
                app.UI.ShowLevelSelect(page - 1);
            }, DustBotTheme.Blue, 26);
            UIFactory.SetAnchors(previous.GetComponent<RectTransform>(), new Vector2(0.055f, 0.055f), new Vector2(0.27f, 0.135f), Vector2.zero, Vector2.zero);
            previous.interactable = page > 0;

            Button next = UIFactory.CreateButton("Next", root.transform, "NEXT", delegate
            {
                app.UI.ShowLevelSelect(page + 1);
            }, DustBotTheme.Blue, 26);
            UIFactory.SetAnchors(next.GetComponent<RectTransform>(), new Vector2(0.73f, 0.055f), new Vector2(0.945f, 0.135f), Vector2.zero, Vector2.zero);
            next.interactable = page < pageCount - 1;

            return root;
        }

        public static GameObject BuildDeveloperPanel(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("Developer Panel", parent);
            Text title = UIFactory.CreateText("Title", root.transform, "DEVELOPER PANEL", 58, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.2f, 0.92f), new Vector2(0.8f, 0.985f), Vector2.zero, Vector2.zero);

            Button home = UIFactory.CreateButton("Home", root.transform, "HOME", app.UI.ShowMainMenu, DustBotTheme.MutedInk, 22);
            UIFactory.SetAnchors(home.GetComponent<RectTransform>(), new Vector2(0.035f, 0.925f), new Vector2(0.19f, 0.98f), Vector2.zero, Vector2.zero);

            GenerationMode[] modes =
            {
                GenerationMode.ProductionCampaign,
                GenerationMode.DevelopmentCampaign,
                GenerationMode.CatTesting,
                GenerationMode.ObstacleTesting,
                GenerationMode.TutorialTesting,
                GenerationMode.MazeTesting
            };
            for (int i = 0; i < modes.Length; i++)
            {
                GenerationMode captured = modes[i];
                bool selected = captured == app.Levels.ActiveGenerationMode;
                int row = i / 3;
                int column = i % 3;
                float left = 0.035f + column * 0.32f;
                Button mode = UIFactory.CreateButton(
                    "Mode " + captured,
                    root.transform,
                    (selected ? "✓ " : string.Empty) + ShortModeName(captured),
                    delegate
                    {
                        app.SetGenerationMode(captured);
                        app.UI.ShowDeveloperPanel();
                    },
                    selected ? DustBotTheme.MintDark : DustBotTheme.Blue,
                    19);
                UIFactory.SetAnchors(
                    mode.GetComponent<RectTransform>(),
                    new Vector2(left, 0.82f - row * 0.075f),
                    new Vector2(left + 0.29f, 0.885f - row * 0.075f),
                    Vector2.zero,
                    Vector2.zero);
            }

            Image metadataPanel = UIFactory.CreatePanel("Metadata", root.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(metadataPanel.rectTransform, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.73f), Vector2.zero, Vector2.zero);
            string metadata = app.CurrentLevel == null
                ? "No campaign level loaded. Choose a mode, then jump to a level."
                : LevelMetadata.Format(app.CurrentLevel);
            Text metadataText = UIFactory.CreateText(
                "Metadata Text",
                metadataPanel.transform,
                metadata,
                20,
                DustBotTheme.Ink,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(metadataText.rectTransform, new Vector2(0.035f, 0.04f), new Vector2(0.965f, 0.96f), Vector2.zero, Vector2.zero);

            Button previous = UIFactory.CreateButton("Previous Level", root.transform, "◀ PREV", app.PreviousCampaignLevel, DustBotTheme.Blue, 23);
            UIFactory.SetAnchors(previous.GetComponent<RectTransform>(), new Vector2(0.05f, 0.345f), new Vector2(0.29f, 0.415f), Vector2.zero, Vector2.zero);
            Button restart = UIFactory.CreateButton("Restart Level", root.transform, "REGENERATE", app.RestartCampaignLevel, DustBotTheme.Mint, 21);
            UIFactory.SetAnchors(restart.GetComponent<RectTransform>(), new Vector2(0.31f, 0.345f), new Vector2(0.69f, 0.415f), Vector2.zero, Vector2.zero);
            Button next = UIFactory.CreateButton("Next Level", root.transform, "NEXT ▶", app.NextCampaignLevel, DustBotTheme.Blue, 23);
            UIFactory.SetAnchors(next.GetComponent<RectTransform>(), new Vector2(0.71f, 0.345f), new Vector2(0.95f, 0.415f), Vector2.zero, Vector2.zero);

            Image inputBackground = UIFactory.CreatePanel("Level Input", root.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(inputBackground.rectTransform, new Vector2(0.05f, 0.26f), new Vector2(0.61f, 0.33f), Vector2.zero, Vector2.zero);
            InputField input = inputBackground.gameObject.AddComponent<InputField>();
            Text inputText = UIFactory.CreateText(
                "Text",
                inputBackground.transform,
                app.CurrentCampaignLevel.ToString(CultureInfo.InvariantCulture),
                28,
                DustBotTheme.Ink,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(inputText.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);
            input.textComponent = inputText;
            input.text = app.CurrentCampaignLevel.ToString(CultureInfo.InvariantCulture);
            input.contentType = InputField.ContentType.IntegerNumber;
            Button jump = UIFactory.CreateButton("Jump", root.transform, "JUMP / PLAY", delegate
            {
                int requested;
                if (!int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out requested))
                {
                    requested = 1;
                }
                app.StartMainLevel(requested);
            }, DustBotTheme.Coral, 24);
            UIFactory.SetAnchors(jump.GetComponent<RectTransform>(), new Vector2(0.63f, 0.26f), new Vector2(0.95f, 0.33f), Vector2.zero, Vector2.zero);

            Button coins = UIFactory.CreateButton("Add Coins", root.transform, "+10,000 COINS", delegate
            {
                app.DebugAddCoins(10000);
                app.UI.ShowDeveloperPanel();
            }, DustBotTheme.Yellow, 21);
            UIFactory.GetButtonText(coins).color = DustBotTheme.Ink;
            UIFactory.SetAnchors(coins.GetComponent<RectTransform>(), new Vector2(0.05f, 0.17f), new Vector2(0.47f, 0.24f), Vector2.zero, Vector2.zero);
            Button unlock = UIFactory.CreateButton("Unlock Campaign", root.transform, "UNLOCK LEVELS + MASTER", delegate
            {
                app.DebugUnlockCampaignAndMaster();
                app.UI.ShowDeveloperPanel();
            }, DustBotTheme.MintDark, 19);
            UIFactory.SetAnchors(unlock.GetComponent<RectTransform>(), new Vector2(0.53f, 0.17f), new Vector2(0.95f, 0.24f), Vector2.zero, Vector2.zero);

            Button cosmetics = UIFactory.CreateButton("Unlock Cosmetics", root.transform, "UNLOCK COSMETICS", delegate
            {
                app.DebugUnlockAllCosmetics();
                app.UI.ShowDeveloperPanel();
            }, DustBotTheme.Coral, 20);
            UIFactory.SetAnchors(cosmetics.GetComponent<RectTransform>(), new Vector2(0.05f, 0.08f), new Vector2(0.47f, 0.15f), Vector2.zero, Vector2.zero);
            bool resetArmed = false;
            Button reset = UIFactory.CreateButton("Reset Save", root.transform, "RESET SAVE", null, DustBotTheme.MutedInk, 20);
            reset.onClick.AddListener(delegate
            {
                if (!resetArmed)
                {
                    resetArmed = true;
                    UIFactory.GetButtonText(reset).text = "TAP AGAIN TO RESET";
                    return;
                }
                app.ResetProgress();
                app.UI.ShowDeveloperPanel();
            });
            UIFactory.SetAnchors(reset.GetComponent<RectTransform>(), new Vector2(0.53f, 0.08f), new Vector2(0.95f, 0.15f), Vector2.zero, Vector2.zero);

            Text footer = UIFactory.CreateText(
                "Footer",
                root.transform,
                "Mode selection is runtime-only. Release builds force Production Campaign.",
                18,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(footer.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.065f), Vector2.zero, Vector2.zero);
            return root;
        }

        public static GameObject BuildSettings(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("Settings", parent);
            Text title = UIFactory.CreateText("Title", root.transform, "SETTINGS", 72, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.92f), Vector2.zero, Vector2.zero);

            RectTransform buttons = CreateVerticalButtonArea(root.transform, new Vector2(0.14f, 0.20f), new Vector2(0.86f, 0.75f));
            PlayerSettingsData settings = app.Progression.Data.settings;

            Button sound = AddMenuButton(buttons, ToggleLabel("SOUND", settings.soundEnabled), null, DustBotTheme.Mint);
            sound.onClick.AddListener(delegate
            {
                settings.soundEnabled = !settings.soundEnabled;
                UIFactory.GetButtonText(sound).text = ToggleLabel("SOUND", settings.soundEnabled);
                app.ApplySettings();
                app.Audio.PlayToggle();
                app.SaveNow();
            });

            Button music = AddMenuButton(buttons, ToggleLabel("MUSIC", settings.musicEnabled), null, DustBotTheme.Blue);
            music.onClick.AddListener(delegate
            {
                settings.musicEnabled = !settings.musicEnabled;
                UIFactory.GetButtonText(music).text = ToggleLabel("MUSIC", settings.musicEnabled);
                app.ApplySettings();
                app.Audio.PlayToggle();
                app.SaveNow();
            });

            Button musicVolume = AddMenuButton(buttons, VolumeLabel("MUSIC VOL", settings.musicVolume), null, DustBotTheme.Blue);
            musicVolume.onClick.AddListener(delegate
            {
                settings.musicVolume = CycleVolume(settings.musicVolume);
                UIFactory.GetButtonText(musicVolume).text = VolumeLabel("MUSIC VOL", settings.musicVolume);
                app.ApplySettings();
                app.Audio.PlayToggle();
                app.SaveNow();
            });

            Button soundVolume = AddMenuButton(buttons, VolumeLabel("SOUND VOL", settings.soundVolume), null, DustBotTheme.Mint);
            soundVolume.onClick.AddListener(delegate
            {
                settings.soundVolume = CycleVolume(settings.soundVolume);
                UIFactory.GetButtonText(soundVolume).text = VolumeLabel("SOUND VOL", settings.soundVolume);
                app.ApplySettings();
                app.Audio.PlayToggle();
                app.SaveNow();
            });

            Button haptics = AddMenuButton(buttons, ToggleLabel("HAPTICS", settings.hapticsEnabled), null, DustBotTheme.Coral);
            haptics.onClick.AddListener(delegate
            {
                settings.hapticsEnabled = !settings.hapticsEnabled;
                UIFactory.GetButtonText(haptics).text = ToggleLabel("HAPTICS", settings.hapticsEnabled);
                app.ApplySettings();
                app.Audio.PlayToggle();
                app.SaveNow();
            });

            AddMenuButton(
                buttons,
                "HOW TO PLAY",
                app.UI.ShowHowToPlay,
                DustBotTheme.Yellow,
                DustBotTheme.Ink,
                DustBotSprites.Player);
            AddMenuButton(
                buttons,
                "COSMETICS",
                app.UI.ShowCosmetics,
                DustBotTheme.MintDark,
                null,
                DustBotSprites.DustBunny);

            bool awaitingConfirmation = false;
            Button reset = AddMenuButton(buttons, "RESET PROGRESS", null, DustBotTheme.MutedInk);
            reset.onClick.AddListener(delegate
            {
                if (!awaitingConfirmation)
                {
                    awaitingConfirmation = true;
                    UIFactory.GetButtonText(reset).text = "TAP AGAIN TO CONFIRM";
                    return;
                }

                app.ResetProgress();
                app.UI.ShowMainMenu();
            });

            Button home = UIFactory.CreateButton("Home", root.transform, "DONE", app.UI.ShowMainMenu, DustBotTheme.MintDark);
            UIFactory.SetAnchors(home.GetComponent<RectTransform>(), new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.18f), Vector2.zero, Vector2.zero);

            Text privacy = UIFactory.CreateText(
                "Privacy",
                root.transform,
                "VERSION 1.0.0  •  OFFLINE  •  NO ADS  •  NO TRACKING",
                21,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(privacy.rectTransform, new Vector2(0.05f, 0.195f), new Vector2(0.95f, 0.255f), Vector2.zero, Vector2.zero);
            return root;
        }

        public static GameObject BuildCosmetics(
            DustBotApp app,
            RectTransform parent,
            CosmeticCategory category,
            int requestedPage)
        {
            GameObject root = CreateRoot("Cosmetic Store", parent);

            Button back = UIFactory.CreateButton(
                "Back",
                root.transform,
                "HOME",
                app.UI.ShowMainMenu,
                DustBotTheme.MutedInk,
                22);
            UIFactory.SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.035f, 0.92f), new Vector2(0.19f, 0.975f), Vector2.zero, Vector2.zero);

            Text title = UIFactory.CreateText("Title", root.transform, "COSMETIC STORE", 56, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.2f, 0.915f), new Vector2(0.8f, 0.98f), Vector2.zero, Vector2.zero);

            Text wallet = UIFactory.CreateText(
                "Wallet",
                root.transform,
                string.Format(
                    "{0} COINS  •  {1} BUNNIES  •  {2} STARS",
                    app.Economy.Coins,
                    app.Progression.Data.totalDustBunnies,
                    app.Progression.Data.totalStars),
                23,
                DustBotTheme.MintDark);
            UIFactory.SetAnchors(wallet.rectTransform, new Vector2(0.19f, 0.86f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            Text hint = UIFactory.CreateText(
                "Scroll Hint",
                root.transform,
                "Swipe vertically to browse cards. Cosmetics never affect gameplay.",
                21,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(hint.rectTransform, new Vector2(0.07f, 0.815f), new Vector2(0.93f, 0.865f), Vector2.zero, Vector2.zero);

            ScrollRect scroll = CreateStoreScroll(root.transform);
            RectTransform content = scroll.content;
            List<CosmeticCategory> categories = VisibleStoreCategories(category);
            for (int i = 0; i < categories.Count; i++)
            {
                AddStoreSection(app, content, categories[i]);
            }

            Text footer = UIFactory.CreateText(
                "Store Footer",
                content,
                "No fail cosmetics are sold here — DustBot prefers optimism.",
                22,
                DustBotTheme.MutedInk);
            LayoutElement footerLayout = footer.gameObject.AddComponent<LayoutElement>();
            footerLayout.minHeight = 70f;
            return root;
        }

        public static GameObject BuildHowToPlay(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("How To Play", parent);
            Text title = UIFactory.CreateText("Title", root.transform, "HOW TO PLAY", 68, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

            Image card = UIFactory.CreatePanel("Instructions", root.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(card.rectTransform, new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.81f), Vector2.zero, Vector2.zero);
            Shadow shadow = card.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.16f, 0.12f, 0.16f);
            shadow.effectDistance = new Vector2(0f, -7f);

            CreateHowToIcon(card.transform, DustBotSprites.Player, new Vector2(0.08f, 0.82f), new Vector2(0.27f, 0.97f));
            CreateHowToIcon(card.transform, DustBotSprites.Crumbs, new Vector2(0.31f, 0.82f), new Vector2(0.50f, 0.97f));
            CreateHowToIcon(card.transform, DustBotSprites.Sock, new Vector2(0.54f, 0.82f), new Vector2(0.73f, 0.97f));
            CreateHowToIcon(card.transform, DustBotSprites.Dock, new Vector2(0.77f, 0.82f), new Vector2(0.96f, 0.97f));

            string copy =
                "1   TOUCH DUSTBOT\n" +
                "Start every route on the little cleaner.\n\n" +
                "2   DRAW ONE ROUTE\n" +
                "Drag through neighboring tiles. Drag backward to erase.\n\n" +
                "3   CLEAN EVERY CRUMB\n" +
                "Furniture, socks, cords, and wet spots block the way.\n\n" +
                "4   FINISH AT THE DOCK\n" +
                "Press Play when the full route is ready.\n\n" +
                "Earn up to three stars for an efficient, independent clean.";
            Text body = UIFactory.CreateText(
                "Instructions Copy",
                card.transform,
                copy,
                30,
                DustBotTheme.Ink,
                TextAnchor.UpperLeft);
            UIFactory.Stretch(body.gameObject);
            body.rectTransform.offsetMin = new Vector2(52f, 42f);
            body.rectTransform.offsetMax = new Vector2(-52f, -245f);

            Button done = UIFactory.CreateButton("Done", root.transform, "GOT IT", app.UI.ShowSettings, DustBotTheme.Mint, 34);
            UIFactory.SetAnchors(done.GetComponent<RectTransform>(), new Vector2(0.2f, 0.07f), new Vector2(0.8f, 0.16f), Vector2.zero, Vector2.zero);
            return root;
        }

        private static Button CreateMenuCard(
            Transform parent,
            string name,
            string heading,
            string detail,
            UnityEngine.Events.UnityAction action,
            Color color,
            Color textColor,
            Sprite icon,
            Vector2 min,
            Vector2 max)
        {
            Button card = UIFactory.CreateButton(name, parent, string.Empty, action, color, 30);
            UIFactory.SetAnchors(card.GetComponent<RectTransform>(), min, max, Vector2.zero, Vector2.zero);

            GameObject iconObject = UIFactory.CreateUIObject("Card Icon", card.transform);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            UIFactory.SetAnchors(
                iconImage.rectTransform,
                new Vector2(0.035f, 0.16f),
                new Vector2(0.25f, 0.84f),
                Vector2.zero,
                Vector2.zero);

            Text title = UIFactory.CreateText(
                "Card Heading",
                card.transform,
                heading,
                max.x - min.x < 0.5f ? 22 : 26,
                textColor,
                TextAnchor.LowerLeft);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.27f, 0.54f), new Vector2(0.97f, 0.9f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;

            Text subtitle = UIFactory.CreateText(
                "Card Detail",
                card.transform,
                detail,
                max.x - min.x < 0.5f ? 15 : 17,
                new Color(textColor.r, textColor.g, textColor.b, 0.9f),
                TextAnchor.UpperLeft);
            UIFactory.SetAnchors(subtitle.rectTransform, new Vector2(0.27f, 0.08f), new Vector2(0.97f, 0.43f), Vector2.zero, Vector2.zero);
            return card;
        }

        private static GameObject CreateRoot(string name, RectTransform parent)
        {
            GameObject root = UIFactory.CreateUIObject(name, parent);
            UIFactory.Stretch(root);
            return root;
        }

        private static RectTransform CreateVerticalButtonArea(Transform parent, Vector2 min, Vector2 max)
        {
            GameObject area = UIFactory.CreateUIObject("Buttons", parent);
            RectTransform rect = area.GetComponent<RectTransform>();
            UIFactory.SetAnchors(rect, min, max, Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = area.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            return rect;
        }

        private static ScrollRect CreateStoreScroll(Transform parent)
        {
            GameObject scrollObject = UIFactory.CreateUIObject("Vertical Store Scroll", parent);
            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            UIFactory.SetAnchors(scrollRect, new Vector2(0.055f, 0.075f), new Vector2(0.945f, 0.81f), Vector2.zero, Vector2.zero);

            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.13f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 38f;

            Image viewport = UIFactory.CreatePanel("Viewport", scrollObject.transform, new Color(1f, 1f, 1f, 0.02f));
            UIFactory.Stretch(viewport.gameObject);
            viewport.raycastTarget = true;
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewport.rectTransform;

            GameObject contentObject = UIFactory.CreateUIObject("Store Content", viewport.transform);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.padding = new RectOffset(10, 10, 10, 34);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            return scroll;
        }

        private static void AddStoreSection(
            DustBotApp app,
            RectTransform content,
            CosmeticCategory category)
        {
            IReadOnlyList<CosmeticDefinition> items = CosmeticCatalog.ForCategory(category);
            if (items.Count == 0)
            {
                return;
            }

            Image header = UIFactory.CreatePanel(
                "Section " + category,
                content,
                new Color(DustBotTheme.Panel.r, DustBotTheme.Panel.g, DustBotTheme.Panel.b, 0.94f));
            LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
            headerLayout.minHeight = 104f;

            Text title = UIFactory.CreateText(
                "Section Title",
                header.transform,
                CategorySectionTitle(category),
                31,
                DustBotTheme.Ink,
                TextAnchor.LowerLeft);
            title.fontStyle = FontStyle.Bold;
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.045f, 0.46f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);

            Text detail = UIFactory.CreateText(
                "Section Detail",
                header.transform,
                CategoryDescription(category),
                20,
                DustBotTheme.MutedInk,
                TextAnchor.UpperLeft);
            UIFactory.SetAnchors(detail.rectTransform, new Vector2(0.045f, 0.12f), new Vector2(0.95f, 0.48f), Vector2.zero, Vector2.zero);

            GameObject gridObject = UIFactory.CreateUIObject("Grid " + category, content);
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(455f, 350f);
            grid.spacing = new Vector2(22f, 22f);
            grid.childAlignment = TextAnchor.UpperCenter;

            int rows = (items.Count + 1) / 2;
            LayoutElement gridLayout = gridObject.AddComponent<LayoutElement>();
            gridLayout.minHeight = rows * grid.cellSize.y + Math.Max(0, rows - 1) * grid.spacing.y + 8f;

            for (int i = 0; i < items.Count; i++)
            {
                CreateCosmeticCard(app, grid.transform, items[i], category);
            }
        }

        private static void CreateCosmeticCard(
            DustBotApp app,
            Transform parent,
            CosmeticDefinition definition,
            CosmeticCategory refreshCategory)
        {
            CosmeticDefinition captured = definition;
            string lockReason = app.Cosmetics.LockReason(definition);
            bool owned = app.Cosmetics.Owns(definition.id);
            bool requirementsMet = string.IsNullOrEmpty(lockReason);
            bool canAfford = app.Economy.Coins >= definition.coinPrice;
            bool interactable = owned || (requirementsMet && canAfford);
            Button card = UIFactory.CreateButton(
                "Cosmetic " + definition.id,
                parent,
                string.Empty,
                delegate
                {
                    bool wasOwned = app.Cosmetics.Owns(captured.id);
                    if (app.Cosmetics.TryUnlockOrSelect(captured.id))
                    {
                        if (wasOwned)
                        {
                            app.Audio.PlayStoreItemSelected();
                        }
                        else
                        {
                            app.Audio.PlayPurchaseSuccess();
                        }
                        app.SaveNow();
                        app.UI.ShowCosmetics(refreshCategory, 0);
                    }
                },
                new Color(1f, 0.995f, 0.965f, 0.98f),
                18);
            card.interactable = interactable;
            Text emptyLabel = UIFactory.GetButtonText(card);
            if (emptyLabel != null)
            {
                emptyLabel.gameObject.SetActive(false);
            }

            Outline[] outlines = card.GetComponents<Outline>();
            if (outlines.Length > 0)
            {
                outlines[0].effectColor = RarityColor(definition.rarity);
                outlines[0].effectDistance = new Vector2(4f, -4f);
            }

            Image preview = UIFactory.CreatePanel(
                "Preview",
                card.transform,
                PreviewBackground(definition));
            UIFactory.SetAnchors(preview.rectTransform, new Vector2(0.07f, 0.54f), new Vector2(0.93f, 0.94f), Vector2.zero, Vector2.zero);
            preview.raycastTarget = false;
            AddCosmeticPreview(preview.transform, definition);

            Text rarity = UIFactory.CreateText(
                "Rarity",
                card.transform,
                definition.rarity.ToString().ToUpperInvariant(),
                16,
                RarityTextColor(definition.rarity));
            rarity.fontStyle = FontStyle.Bold;
            Image rarityBadge = UIFactory.CreatePanel(
                "Rarity Badge",
                card.transform,
                RarityColor(definition.rarity));
            UIFactory.SetAnchors(rarityBadge.rectTransform, new Vector2(0.55f, 0.84f), new Vector2(0.91f, 0.925f), Vector2.zero, Vector2.zero);
            UIFactory.Stretch(rarity.gameObject);
            rarity.transform.SetParent(rarityBadge.transform, false);
            rarity.rectTransform.offsetMin = Vector2.zero;
            rarity.rectTransform.offsetMax = Vector2.zero;

            Text name = UIFactory.CreateText(
                "Name",
                card.transform,
                definition.displayName,
                25,
                DustBotTheme.Ink);
            name.fontStyle = FontStyle.Bold;
            UIFactory.SetAnchors(name.rectTransform, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.53f), Vector2.zero, Vector2.zero);

            Text cost = UIFactory.CreateText(
                "Cost",
                card.transform,
                CostText(definition, owned),
                20,
                DustBotTheme.MintDark);
            UIFactory.SetAnchors(cost.rectTransform, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.405f), Vector2.zero, Vector2.zero);

            Text requirement = UIFactory.CreateText(
                "Requirement",
                card.transform,
                owned ? "Owned in your closet" : RequirementSummary(definition, lockReason),
                17,
                string.IsNullOrEmpty(lockReason) ? DustBotTheme.MutedInk : DustBotTheme.Warning);
            UIFactory.SetAnchors(requirement.rectTransform, new Vector2(0.07f, 0.19f), new Vector2(0.93f, 0.305f), Vector2.zero, Vector2.zero);

            Image statePill = UIFactory.CreatePanel(
                "State Pill",
                card.transform,
                StateColor(definition, owned, requirementsMet, canAfford, app.Cosmetics.Status(definition)));
            UIFactory.SetAnchors(statePill.rectTransform, new Vector2(0.11f, 0.055f), new Vector2(0.89f, 0.17f), Vector2.zero, Vector2.zero);
            statePill.raycastTarget = false;
            Text state = UIFactory.CreateText(
                "State Text",
                statePill.transform,
                StateText(definition, owned, requirementsMet, canAfford, app.Cosmetics.Status(definition)),
                20,
                Color.white);
            state.fontStyle = FontStyle.Bold;
            UIFactory.Stretch(state.gameObject);

            if (!owned && !requirementsMet)
            {
                Image overlay = UIFactory.CreatePanel(
                    "Locked Overlay",
                    card.transform,
                    new Color(0.06f, 0.09f, 0.08f, 0.42f));
                UIFactory.SetAnchors(overlay.rectTransform, new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
                overlay.raycastTarget = false;
                Text locked = UIFactory.CreateText(
                    "Locked Text",
                    overlay.transform,
                    "LOCKED",
                    27,
                    Color.white);
                locked.fontStyle = FontStyle.Bold;
                UIFactory.Stretch(locked.gameObject);
            }
        }

        private static List<CosmeticCategory> VisibleStoreCategories(CosmeticCategory preferred)
        {
            CosmeticCategory[] order =
            {
                CosmeticCategory.BotSkin,
                CosmeticCategory.PathColor,
                CosmeticCategory.TileTheme,
                CosmeticCategory.DockDesign,
                CosmeticCategory.WinAnimation,
                CosmeticCategory.RoomBackground,
                CosmeticCategory.Bundle
            };
            List<CosmeticCategory> categories = new List<CosmeticCategory>();
            if (preferred != CosmeticCategory.FailureAnimation &&
                Array.IndexOf(order, preferred) >= 0)
            {
                categories.Add(preferred);
            }

            for (int i = 0; i < order.Length; i++)
            {
                if (!categories.Contains(order[i]))
                {
                    categories.Add(order[i]);
                }
            }

            return categories;
        }

        private static void AddCosmeticPreview(Transform parent, CosmeticDefinition definition)
        {
            if (definition.category == CosmeticCategory.PathColor)
            {
                Image track = UIFactory.CreatePanel("Trail Track", parent, new Color(1f, 1f, 1f, 0.55f));
                UIFactory.SetAnchors(track.rectTransform, new Vector2(0.13f, 0.43f), new Vector2(0.87f, 0.58f), Vector2.zero, Vector2.zero);
                track.raycastTarget = false;
                Color route;
                if (!ColorUtility.TryParseHtmlString(definition.colorHex, out route))
                {
                    route = DustBotTheme.MintDark;
                }

                Image routeLine = UIFactory.CreatePanel("Trail", track.transform, route);
                UIFactory.Stretch(routeLine.gameObject);
                routeLine.raycastTarget = false;
                return;
            }

            if (definition.category == CosmeticCategory.RoomBackground ||
                definition.category == CosmeticCategory.TileTheme)
            {
                CreateTwoTonePreview(parent, definition);
                return;
            }

            GameObject iconObject = UIFactory.CreateUIObject("Preview Icon", parent);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = CategoryIcon(definition.category);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIFactory.SetAnchors(icon.rectTransform, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f), Vector2.zero, Vector2.zero);
            Color tint;
            if (ColorUtility.TryParseHtmlString(definition.colorHex, out tint))
            {
                icon.color = tint;
            }
        }

        private static void CreateTwoTonePreview(Transform parent, CosmeticDefinition definition)
        {
            Color primary;
            if (!ColorUtility.TryParseHtmlString(definition.colorHex, out primary))
            {
                primary = DustBotTheme.PanelSoft;
            }

            Color secondary;
            if (!ColorUtility.TryParseHtmlString(definition.secondaryColorHex, out secondary))
            {
                secondary = Color.Lerp(primary, Color.white, 0.28f);
            }

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Image tile = UIFactory.CreatePanel(
                        "Preview Tile",
                        parent,
                        (x + y) % 2 == 0 ? primary : secondary);
                    UIFactory.SetAnchors(
                        tile.rectTransform,
                        new Vector2(0.12f + x * 0.255f, 0.16f + y * 0.32f),
                        new Vector2(0.33f + x * 0.255f, 0.42f + y * 0.32f),
                        Vector2.zero,
                        Vector2.zero);
                    tile.raycastTarget = false;
                }
            }
        }

        private static Color PreviewBackground(CosmeticDefinition definition)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(definition.colorHex, out color))
            {
                color = DustBotTheme.PanelSoft;
            }

            return Color.Lerp(color, Color.white, 0.58f);
        }

        private static string CostText(CosmeticDefinition definition, bool owned)
        {
            if (owned)
            {
                return "✓ OWNED";
            }

            if (definition.coinPrice <= 0)
            {
                return "FREE";
            }

            return "◎ " + definition.coinPrice + " DUST COINS";
        }

        private static string RequirementSummary(
            CosmeticDefinition definition,
            string lockReason)
        {
            if (!string.IsNullOrEmpty(lockReason))
            {
                return lockReason;
            }

            if (definition.coinPrice > 0)
            {
                return "Available now";
            }

            return "Unlocked by default";
        }

        private static Color StateColor(
            CosmeticDefinition definition,
            bool owned,
            bool requirementsMet,
            bool canAfford,
            string status)
        {
            if (status == "SELECTED")
            {
                return DustBotTheme.MintDark;
            }

            if (owned)
            {
                return DustBotTheme.Blue;
            }

            if (!requirementsMet)
            {
                return DustBotTheme.MutedInk;
            }

            return canAfford ? DustBotTheme.Coral : DustBotTheme.Warning;
        }

        private static string StateText(
            CosmeticDefinition definition,
            bool owned,
            bool requirementsMet,
            bool canAfford,
            string status)
        {
            if (status == "SELECTED")
            {
                return "EQUIPPED";
            }

            if (owned)
            {
                return definition.category == CosmeticCategory.Bundle ? "OWNED" : "EQUIP";
            }

            if (!requirementsMet)
            {
                return "LOCKED";
            }

            return canAfford ? "PURCHASE" : "NEED COINS";
        }

        private static Button AddMenuButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            Color color,
            Color? textColor = null,
            Sprite icon = null)
        {
            Button button = UIFactory.CreateButton(label, parent, label, action, color, 36);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 90f;
            if (textColor.HasValue)
            {
                UIFactory.GetButtonText(button).color = textColor.Value;
            }

            if (icon != null)
            {
                Image chip = UIFactory.CreatePanel(
                    "Icon Chip",
                    button.transform,
                    new Color(1f, 0.99f, 0.94f, 0.9f));
                UIFactory.SetAnchors(
                    chip.rectTransform,
                    new Vector2(0.025f, 0.13f),
                    new Vector2(0.145f, 0.87f),
                    Vector2.zero,
                    Vector2.zero);
                chip.raycastTarget = false;

                GameObject iconObject = UIFactory.CreateUIObject("Icon", chip.transform);
                Image iconImage = iconObject.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                UIFactory.Stretch(iconObject);
                iconImage.rectTransform.offsetMin = Vector2.one * 7f;
                iconImage.rectTransform.offsetMax = Vector2.one * -7f;

                Text buttonText = UIFactory.GetButtonText(button);
                buttonText.rectTransform.offsetMin = new Vector2(105f, 6f);
                buttonText.rectTransform.offsetMax = new Vector2(-18f, -6f);
            }

            return button;
        }

        private static string CategoryLabel(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.BotSkin: return "DUSTBOTS";
                case CosmeticCategory.PathColor: return "TRAILS";
                case CosmeticCategory.TileTheme: return "TILES";
                case CosmeticCategory.DockDesign: return "DOCKS";
                case CosmeticCategory.WinAnimation: return "CELEBRATIONS";
                case CosmeticCategory.RoomBackground: return "ROOMS";
                default: return "BUNDLES";
            }
        }

        private static string CategorySectionTitle(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.BotSkin: return "DustBot Skins";
                case CosmeticCategory.PathColor: return "Path Trails";
                case CosmeticCategory.TileTheme: return "Tile Themes";
                case CosmeticCategory.DockDesign: return "Dock Designs";
                case CosmeticCategory.WinAnimation: return "Win Celebrations";
                case CosmeticCategory.RoomBackground: return "Room Themes";
                default: return "Bundles";
            }
        }

        private static string CategoryDescription(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.BotSkin: return "Big, readable robot looks for your tiny cleaning hero.";
                case CosmeticCategory.PathColor: return "Clear route colors that keep drawing easy to follow.";
                case CosmeticCategory.TileTheme: return "Floor palettes with stronger contrast and personality.";
                case CosmeticCategory.DockDesign: return "Cute chargers for a triumphant little return home.";
                case CosmeticCategory.WinAnimation: return "Victory flair for clean, efficient routes.";
                case CosmeticCategory.RoomBackground: return "Distinct moods for the whole room backdrop.";
                default: return "Curated sets with a cleaner total price.";
            }
        }

        private static Sprite CategoryIcon(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.BotSkin: return DustBotSprites.Player;
                case CosmeticCategory.DockDesign: return DustBotSprites.Dock;
                case CosmeticCategory.TileTheme: return DustBotSprites.Toy;
                case CosmeticCategory.RoomBackground: return DustBotSprites.Wall;
                case CosmeticCategory.Bundle: return DustBotSprites.DustBunny;
                case CosmeticCategory.WinAnimation: return DustBotSprites.Crumbs;
                default: return null;
            }
        }

        private static Color RarityColor(CosmeticRarity rarity)
        {
            switch (rarity)
            {
                case CosmeticRarity.Uncommon: return new Color32(103, 179, 137, 255);
                case CosmeticRarity.Rare: return new Color32(94, 151, 215, 255);
                case CosmeticRarity.Epic: return new Color32(151, 111, 207, 255);
                case CosmeticRarity.Legendary: return new Color32(241, 185, 73, 255);
                default: return new Color32(121, 145, 137, 255);
            }
        }

        private static Color RarityTextColor(CosmeticRarity rarity)
        {
            return rarity == CosmeticRarity.Legendary ? DustBotTheme.Ink : Color.white;
        }

        private static void AddPathSwatch(Button button, string colorHex)
        {
            Color routeColor;
            if (!ColorUtility.TryParseHtmlString(colorHex, out routeColor))
            {
                routeColor = DustBotTheme.MintDark;
            }

            Image chip = UIFactory.CreatePanel(
                "Route Swatch",
                button.transform,
                new Color(1f, 0.99f, 0.94f, 0.9f));
            UIFactory.SetAnchors(
                chip.rectTransform,
                new Vector2(0.025f, 0.13f),
                new Vector2(0.145f, 0.87f),
                Vector2.zero,
                Vector2.zero);
            chip.raycastTarget = false;

            Image line = UIFactory.CreatePanel("Route Line", chip.transform, routeColor);
            UIFactory.SetAnchors(
                line.rectTransform,
                new Vector2(0.16f, 0.39f),
                new Vector2(0.84f, 0.61f),
                Vector2.zero,
                Vector2.zero);
            line.raycastTarget = false;

            Text buttonText = UIFactory.GetButtonText(button);
            buttonText.rectTransform.offsetMin = new Vector2(105f, 6f);
            buttonText.rectTransform.offsetMax = new Vector2(-18f, -6f);
        }

        private static void TintButtonPreview(Button button, string colorHex)
        {
            Color tint;
            if (!ColorUtility.TryParseHtmlString(colorHex, out tint))
            {
                return;
            }

            Image[] images = button.GetComponentsInChildren<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject.name == "Icon")
                {
                    images[i].color = tint;
                    return;
                }
            }
        }

        private static void CreateHowToIcon(
            Transform parent,
            Sprite sprite,
            Vector2 min,
            Vector2 max)
        {
            Image chip = UIFactory.CreatePanel(
                "How To Icon",
                parent,
                new Color(DustBotTheme.PanelSoft.r, DustBotTheme.PanelSoft.g, DustBotTheme.PanelSoft.b, 0.96f));
            UIFactory.SetAnchors(chip.rectTransform, min, max, Vector2.zero, Vector2.zero);
            chip.raycastTarget = false;

            GameObject iconObject = UIFactory.CreateUIObject("Sprite", chip.transform);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIFactory.Stretch(iconObject);
            icon.rectTransform.offsetMin = Vector2.one * 10f;
            icon.rectTransform.offsetMax = Vector2.one * -10f;
        }

        private static string ToggleLabel(string label, bool enabled)
        {
            return string.Format("{0}: {1}", label, enabled ? "ON" : "OFF");
        }

        private static string ShortModeName(GenerationMode mode)
        {
            switch (mode)
            {
                case GenerationMode.DevelopmentCampaign: return "DEV 30";
                case GenerationMode.CatTesting: return "CATS 24";
                case GenerationMode.ObstacleTesting: return "OBSTACLES 18";
                case GenerationMode.TutorialTesting: return "TUTORIALS 8";
                case GenerationMode.MazeTesting: return "MAZES 20";
                default: return "PRODUCTION 6000";
            }
        }

        private static string VolumeLabel(string label, float value)
        {
            return string.Format("{0}: {1}%", label, Mathf.RoundToInt(Mathf.Clamp01(value) * 100f));
        }

        private static float CycleVolume(float value)
        {
            if (value < 0.625f)
            {
                return 0.75f;
            }

            if (value < 0.875f)
            {
                return 1f;
            }

            return 0.5f;
        }

        private static string StarText(int stars)
        {
            if (stars <= 0) return "☆ ☆ ☆";
            if (stars == 1) return "★ ☆ ☆";
            if (stars == 2) return "★ ★ ☆";
            return "★ ★ ★";
        }
    }
}
