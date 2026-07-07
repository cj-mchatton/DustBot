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
                production ? "READY FOR THE NEXT ROOM?" : LevelGenerationConfig.DisplayName(generationMode),
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
                        "{0} / {1} LEVELS COMPLETE\n{2} STARS  •  {3} BUNNIES\n{4} DAY DAILY STREAK",
                        app.Progression.TotalCompleted,
                        LevelCategoryCatalog.TotalLevelCount,
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
                production
                    ? "CONTINUE  •  " + LevelCategoryCatalog.LevelName(
                        app.Progression.Data.lastPlayedCategory,
                        app.Progression.NextUnfinishedLevel(app.Progression.Data.lastPlayedCategory))
                    : "PLAY  •  LEVEL " + app.CurrentCampaignLevel,
                delegate
                {
                    if (production) app.StartRecommendedLevel();
                    else app.StartMainLevel(app.CurrentCampaignLevel);
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

        public static GameObject BuildCategorySelect(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("Category Select", parent);
            Button back = UIFactory.CreateButton("Back", root.transform, "HOME", app.UI.ShowMainMenu, DustBotTheme.MutedInk, 24);
            UIFactory.SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.92f), new Vector2(0.23f, 0.975f), Vector2.zero, Vector2.zero);
            Text title = UIFactory.CreateText("Title", root.transform, "CHOOSE A CHALLENGE", 49, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.23f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            Color[] colors = { DustBotTheme.Mint, DustBotTheme.Blue, DustBotTheme.Yellow, DustBotTheme.Coral, DustBotTheme.MintDark };
            Sprite[] icons = { DustBotSprites.Player, DustBotSprites.Crumbs, DustBotSprites.Cord, DustBotSprites.DustBunny, DustBotSprites.Cat };
            for (int i = 0; i < LevelCategoryCatalog.All.Count; i++)
            {
                LevelCategory category = LevelCategoryCatalog.All[i];
                LevelCategoryProfile profile = LevelCategoryCatalog.Get(category);
                bool unlocked = app.Progression.IsCategoryUnlocked(category);
                int completed = app.Progression.CompletedCount(category);
                int stars = app.Progression.StarsInCategory(category);
                float top = 0.88f - i * 0.15f;
                string detail = unlocked
                    ? string.Format("{0}  •  {1}/{2} complete  •  {3} stars", profile.description, completed, profile.levelCount, stars)
                    : app.Progression.CategoryLockReason(category);
                LevelCategory captured = category;
                CreateMenuCard(
                    root.transform,
                    category + " Category",
                    (unlocked ? string.Empty : "LOCKED  •  ") + profile.displayName,
                    detail,
                    delegate
                    {
                        if (app.Progression.IsCategoryUnlocked(captured)) app.UI.ShowCategoryLevelSelect(captured);
                        else app.UI.ShowCategoryLockedMessage(captured);
                    },
                    unlocked ? colors[i] : new Color32(176, 187, 181, 255),
                    i == 2 ? DustBotTheme.Ink : Color.white,
                    icons[i],
                    new Vector2(0.075f, top - 0.12f),
                    new Vector2(0.925f, top));
            }

            Text footer = UIFactory.CreateText("Footer", root.transform, "255 curated puzzles • five ways to clean", 22, DustBotTheme.MutedInk);
            UIFactory.SetAnchors(footer.rectTransform, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.09f), Vector2.zero, Vector2.zero);
            return root;
        }

        public static GameObject BuildCategoryLevelSelect(
            DustBotApp app, RectTransform parent, LevelCategory category, int requestedPage)
        {
            LevelCategoryProfile profile = LevelCategoryCatalog.Get(category);
            int pageCount = (profile.levelCount + LevelsPerPage - 1) / LevelsPerPage;
            int page = Mathf.Clamp(requestedPage, 0, pageCount - 1);
            GameObject root = CreateRoot(category + " Level Select", parent);
            Button back = UIFactory.CreateButton("Back", root.transform, "CATEGORIES", app.UI.ShowCategorySelect, DustBotTheme.MutedInk, 22);
            UIFactory.SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.035f, 0.91f), new Vector2(0.27f, 0.975f), Vector2.zero, Vector2.zero);
            Text title = UIFactory.CreateText("Title", root.transform, profile.displayName, 56, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.28f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

            GameObject gridObject = UIFactory.CreateUIObject("Level Grid", root.transform);
            RectTransform gridRect = gridObject.GetComponent<RectTransform>();
            UIFactory.SetAnchors(gridRect, new Vector2(0.055f, 0.16f), new Vector2(0.945f, 0.89f), Vector2.zero, Vector2.zero);
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(205f, 135f);
            grid.spacing = new Vector2(18f, 18f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            int first = page * LevelsPerPage + 1;
            int last = Mathf.Min(profile.levelCount, first + LevelsPerPage - 1);
            for (int level = first; level <= last; level++)
            {
                int captured = level;
                bool unlocked = app.Progression.IsLevelUnlocked(category, level);
                int stars = app.Progression.GetStars(category, level);
                bool bunny = app.Progression.HasDustBunny(category, level);
                bool perfect = app.Progression.HasPerfectClean(category, level);
                bool cat = LevelCategoryCatalog.IsCatLevel(category, level);
                string state = cat ? "CAT" : category == LevelCategory.Easy
                    ? level == 5 ? "MAZE" : "LESSON"
                    : "MAZE";
                string label = unlocked
                    ? string.Format("{0}\n{1}  {2}{3}{4}", level, state, StarText(stars), bunny ? " ◆" : string.Empty, perfect ? " ✓" : string.Empty)
                    : level + "\nLOCKED";
                Button button = UIFactory.CreateButton(
                    "Level " + level, grid.transform, label,
                    delegate { app.StartCategoryLevel(category, captured); },
                    unlocked ? DustBotTheme.Mint : new Color32(176, 187, 181, 255), 23);
                button.interactable = unlocked;
            }

            Text pageText = UIFactory.CreateText("Page", root.transform,
                string.Format("{0} {1}-{2} of {3}", profile.displayName, first, last, profile.levelCount), 24, DustBotTheme.MutedInk);
            UIFactory.SetAnchors(pageText.rectTransform, new Vector2(0.27f, 0.07f), new Vector2(0.73f, 0.14f), Vector2.zero, Vector2.zero);
            Button previous = UIFactory.CreateButton("Previous", root.transform, "PREV", delegate { app.UI.ShowCategoryLevelSelect(category, page - 1); }, DustBotTheme.Blue, 25);
            UIFactory.SetAnchors(previous.GetComponent<RectTransform>(), new Vector2(0.055f, 0.055f), new Vector2(0.25f, 0.135f), Vector2.zero, Vector2.zero);
            previous.interactable = page > 0;
            Button next = UIFactory.CreateButton("Next", root.transform, "NEXT", delegate { app.UI.ShowCategoryLevelSelect(category, page + 1); }, DustBotTheme.Blue, 25);
            UIFactory.SetAnchors(next.GetComponent<RectTransform>(), new Vector2(0.75f, 0.055f), new Vector2(0.945f, 0.135f), Vector2.zero, Vector2.zero);
            next.interactable = page < pageCount - 1;
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
                production ? "CATEGORY LEVELS" : LevelGenerationConfig.DisplayName(generationMode),
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

            for (int i = 0; i < LevelCategoryCatalog.All.Count; i++)
            {
                LevelCategory capturedCategory = LevelCategoryCatalog.All[i];
                float left = 0.035f + i * 0.19f;
                Button categoryButton = UIFactory.CreateButton(
                    "Category " + capturedCategory,
                    root.transform,
                    (capturedCategory == app.CurrentCategory ? "✓ " : string.Empty) + LevelCategoryCatalog.Name(capturedCategory),
                    delegate
                    {
                        app.DebugSelectCategory(capturedCategory);
                        app.UI.ShowDeveloperPanel();
                    },
                    capturedCategory == app.CurrentCategory ? DustBotTheme.MintDark : DustBotTheme.MutedInk,
                    15);
                UIFactory.SetAnchors(categoryButton.GetComponent<RectTransform>(),
                    new Vector2(left, 0.67f), new Vector2(left + 0.175f, 0.72f), Vector2.zero, Vector2.zero);
            }

            Image metadataPanel = UIFactory.CreatePanel("Metadata", root.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(metadataPanel.rectTransform, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.655f), Vector2.zero, Vector2.zero);
            LevelCategoryProfile currentProfile = LevelCategoryCatalog.Get(app.CurrentCategory);
            string profileSummary = string.Format(
                "PROFILE: {0} • {1} LEVELS • {2} CAT • {3} MAZE",
                currentProfile.displayName,
                currentProfile.levelCount,
                currentProfile.catLevelCount,
                currentProfile.mazeLevelCount);
            string metadata = app.CurrentLevel == null
                ? profileSummary + "\nNo level loaded. Choose a category and jump to a level."
                : profileSummary + "\n" + LevelMetadata.Format(app.CurrentLevel);
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
            UIFactory.SetAnchors(coins.GetComponent<RectTransform>(), new Vector2(0.05f, 0.17f), new Vector2(0.265f, 0.24f), Vector2.zero, Vector2.zero);
            Button completeCategory = UIFactory.CreateButton("Complete Category", root.transform, "COMPLETE", delegate
            {
                app.DebugCompleteCategory(app.CurrentCategory);
                app.UI.ShowDeveloperPanel();
            }, DustBotTheme.Mint, 16);
            UIFactory.SetAnchors(completeCategory.GetComponent<RectTransform>(), new Vector2(0.275f, 0.17f), new Vector2(0.49f, 0.24f), Vector2.zero, Vector2.zero);
            Button resetCategory = UIFactory.CreateButton("Reset Category", root.transform, "RESET CATEGORY", delegate
            {
                app.DebugResetCategory(app.CurrentCategory);
                app.UI.ShowDeveloperPanel();
            }, DustBotTheme.MutedInk, 15);
            UIFactory.SetAnchors(resetCategory.GetComponent<RectTransform>(), new Vector2(0.50f, 0.17f), new Vector2(0.715f, 0.24f), Vector2.zero, Vector2.zero);
            Button unlock = UIFactory.CreateButton("Unlock Campaign", root.transform, "UNLOCK LEVELS + MASTER", delegate
            {
                app.DebugUnlockCampaignAndMaster();
                app.UI.ShowDeveloperPanel();
            }, DustBotTheme.MintDark, 19);
            UIFactory.GetButtonText(unlock).text = "UNLOCK ALL";
            UIFactory.SetAnchors(unlock.GetComponent<RectTransform>(), new Vector2(0.725f, 0.17f), new Vector2(0.95f, 0.24f), Vector2.zero, Vector2.zero);

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
                "Developer playlists are runtime-only. Release builds use the 255 fixed curated category levels.",
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

            // Keep the button stack clear of the footer below it. The previous
            // 0.20 lower anchor placed the eighth item (Reset Progress) in the
            // same band as the privacy/version text on portrait iPhones.
            RectTransform buttons = CreateVerticalButtonArea(root.transform, new Vector2(0.14f, 0.275f), new Vector2(0.86f, 0.75f));
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
            return BuildCosmeticCategorySelect(app, parent);
        }

        public static GameObject BuildCosmeticCategorySelect(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("Cosmetic Categories", parent);

            Button home = UIFactory.CreateButton(
                "Home",
                root.transform,
                "HOME",
                app.UI.ShowMainMenu,
                DustBotTheme.MutedInk,
                22);
            UIFactory.SetAnchors(home.GetComponent<RectTransform>(), new Vector2(0.035f, 0.92f), new Vector2(0.19f, 0.975f), Vector2.zero, Vector2.zero);

            Text title = UIFactory.CreateText("Title", root.transform, "COSMETICS", 56, DustBotTheme.Ink);
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
                "Choose what you want to customize. Every change is visual only.",
                21,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(hint.rectTransform, new Vector2(0.07f, 0.815f), new Vector2(0.93f, 0.865f), Vector2.zero, Vector2.zero);

            ScrollRect scroll = CreateStoreScroll(root.transform);
            RectTransform content = scroll.content;
            CosmeticCategory[] categories = StoreCategories();
            for (int i = 0; i < categories.Length; i++)
            {
                CreateCategoryCard(app, content, categories[i]);
            }

            return root;
        }

        public static GameObject BuildCosmeticCategory(
            DustBotApp app,
            RectTransform parent,
            CosmeticCategory category)
        {
            GameObject root = CreateRoot("Cosmetic Store " + category, parent);

            Button categories = UIFactory.CreateButton(
                "Categories",
                root.transform,
                "CATEGORIES",
                app.UI.ShowCosmetics,
                DustBotTheme.MutedInk,
                20);
            UIFactory.SetAnchors(categories.GetComponent<RectTransform>(), new Vector2(0.035f, 0.92f), new Vector2(0.225f, 0.975f), Vector2.zero, Vector2.zero);

            Text title = UIFactory.CreateText("Title", root.transform, CategorySectionTitle(category).ToUpperInvariant(), 45, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.22f, 0.915f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 28;
            title.resizeTextMaxSize = 45;

            Text wallet = UIFactory.CreateText(
                "Wallet",
                root.transform,
                string.Format("{0} COINS  •  {1} BUNNIES  •  {2} STARS", app.Economy.Coins, app.Progression.Data.totalDustBunnies, app.Progression.Data.totalStars),
                23,
                DustBotTheme.MintDark);
            UIFactory.SetAnchors(wallet.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.96f, 0.92f), Vector2.zero, Vector2.zero);

            Text hint = UIFactory.CreateText("Category Description", root.transform, CategoryDescription(category), 21, DustBotTheme.MutedInk);
            UIFactory.SetAnchors(hint.rectTransform, new Vector2(0.07f, 0.815f), new Vector2(0.93f, 0.865f), Vector2.zero, Vector2.zero);

            ScrollRect scroll = CreateStoreScroll(root.transform);
            AddStoreSection(app, scroll.content, category);

            Text footer = UIFactory.CreateText(
                "Store Footer",
                scroll.content,
                "Tap any card for a larger preview, requirements, and actions.",
                22,
                DustBotTheme.MutedInk);
            LayoutElement footerLayout = footer.gameObject.AddComponent<LayoutElement>();
            footerLayout.minHeight = 70f;
            return root;
        }

        public static GameObject BuildCosmeticDetail(
            DustBotApp app,
            RectTransform parent,
            CosmeticCategory category,
            string cosmeticId)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(cosmeticId);
            if (definition == null) return BuildCosmeticCategory(app, parent, category);

            GameObject root = CreateRoot("Cosmetic Detail " + cosmeticId, parent);
            Button back = UIFactory.CreateButton("Back", root.transform, "BACK", delegate { app.UI.ShowCosmeticCategory(category); }, DustBotTheme.MutedInk, 22);
            UIFactory.SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.92f), new Vector2(0.20f, 0.975f), Vector2.zero, Vector2.zero);

            Text heading = UIFactory.CreateText("Heading", root.transform, "COSMETIC PREVIEW", 49, DustBotTheme.Ink);
            UIFactory.SetAnchors(heading.rectTransform, new Vector2(0.21f, 0.915f), new Vector2(0.94f, 0.98f), Vector2.zero, Vector2.zero);

            Image panel = UIFactory.CreatePanel("Detail Panel", root.transform, new Color(1f, 0.995f, 0.965f, 0.98f));
            UIFactory.SetAnchors(panel.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.89f), Vector2.zero, Vector2.zero);
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.12f, 0.1f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -7f);

            Image preview = UIFactory.CreatePanel("Large Preview", panel.transform, PreviewBackground(definition));
            UIFactory.SetAnchors(preview.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            preview.raycastTarget = false;
            AddCosmeticPreview(preview.transform, definition);

            Text name = UIFactory.CreateText("Name", panel.transform, definition.displayName, 42, DustBotTheme.Ink);
            name.fontStyle = FontStyle.Bold;
            UIFactory.SetAnchors(name.rectTransform, new Vector2(0.07f, 0.445f), new Vector2(0.93f, 0.525f), Vector2.zero, Vector2.zero);

            Text metadata = UIFactory.CreateText("Metadata", panel.transform, CategorySectionTitle(definition.category) + "  •  " + definition.rarity.ToString().ToUpperInvariant(), 23, RarityTextColor(definition.rarity));
            Image badge = UIFactory.CreatePanel("Metadata Badge", panel.transform, RarityColor(definition.rarity));
            UIFactory.SetAnchors(badge.rectTransform, new Vector2(0.20f, 0.395f), new Vector2(0.80f, 0.445f), Vector2.zero, Vector2.zero);
            metadata.transform.SetParent(badge.transform, false);
            UIFactory.Stretch(metadata.gameObject);

            Text description = UIFactory.CreateText("Description", panel.transform, definition.description, 24, DustBotTheme.MutedInk);
            UIFactory.SetAnchors(description.rectTransform, new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.39f), Vector2.zero, Vector2.zero);

            bool owned = app.Cosmetics.Owns(definition.id);
            string lockReason = app.Cosmetics.LockReason(definition);
            Text requirement = UIFactory.CreateText(
                "Requirement",
                panel.transform,
                owned ? "OWNED  •  " + (app.Cosmetics.Status(definition) == "SELECTED" ? "EQUIPPED" : "READY TO EQUIP") : RequirementSummary(definition, lockReason) + "\n" + CostText(definition, false),
                24,
                string.IsNullOrEmpty(lockReason) ? DustBotTheme.MintDark : DustBotTheme.Warning);
            UIFactory.SetAnchors(requirement.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);

            bool canAct = owned || (string.IsNullOrEmpty(lockReason) && app.Economy.Coins >= definition.coinPrice);
            string actionLabel = StateText(definition, owned, string.IsNullOrEmpty(lockReason), app.Economy.Coins >= definition.coinPrice, app.Cosmetics.Status(definition));
            Button action = UIFactory.CreateButton("Cosmetic Action", panel.transform, actionLabel, delegate
            {
                bool wasOwned = app.Cosmetics.Owns(definition.id);
                if (!app.Cosmetics.TryUnlockOrSelect(definition.id)) return;
                if (wasOwned) app.Audio.PlayStoreItemSelected(); else app.Audio.PlayPurchaseSuccess();
                app.SaveNow();
                app.UI.ShowCosmeticDetail(category, definition.id);
            }, canAct ? DustBotTheme.MintDark : DustBotTheme.MutedInk, 30);
            action.interactable = canAct && app.Cosmetics.Status(definition) != "SELECTED";
            UIFactory.SetAnchors(action.GetComponent<RectTransform>(), new Vector2(0.17f, 0.055f), new Vector2(0.83f, 0.15f), Vector2.zero, Vector2.zero);
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
            layout.spacing = 14f;
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
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.color = Color.clear;
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

        private static CosmeticCategory[] StoreCategories()
        {
            return new[]
            {
                CosmeticCategory.DustBotSkin,
                CosmeticCategory.PathTrail,
                CosmeticCategory.CrumbStyle,
                CosmeticCategory.CatSkin,
                CosmeticCategory.DockDesign,
                CosmeticCategory.TileTheme,
                CosmeticCategory.RoomTheme,
                CosmeticCategory.Bundle
            };
        }

        private static void CreateCategoryCard(
            DustBotApp app,
            RectTransform content,
            CosmeticCategory category)
        {
            CosmeticCategory captured = category;
            IReadOnlyList<CosmeticDefinition> items = CosmeticCatalog.ForCategory(category);
            int owned = 0;
            bool hasNew = false;
            for (int i = 0; i < items.Count; i++)
            {
                bool owns = app.Cosmetics.Owns(items[i].id);
                if (owns) owned++;
                if (items[i].isNew && !owns) hasNew = true;
            }

            Button card = UIFactory.CreateButton(
                "Category " + category,
                content,
                string.Empty,
                delegate { app.UI.ShowCosmeticCategory(captured); },
                new Color(1f, 0.995f, 0.965f, 0.98f),
                20);
            LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 154f;
            Text empty = UIFactory.GetButtonText(card);
            if (empty != null) empty.gameObject.SetActive(false);

            Image iconChip = UIFactory.CreatePanel("Icon Chip", card.transform, CategoryColor(category));
            UIFactory.SetAnchors(iconChip.rectTransform, new Vector2(0.025f, 0.13f), new Vector2(0.18f, 0.87f), Vector2.zero, Vector2.zero);
            iconChip.raycastTarget = false;
            Image icon = UIFactory.CreateUIObject("Icon", iconChip.transform).AddComponent<Image>();
            icon.sprite = CategoryIcon(category);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIFactory.SetAnchors(icon.rectTransform, new Vector2(0.14f, 0.14f), new Vector2(0.86f, 0.86f), Vector2.zero, Vector2.zero);

            Text title = UIFactory.CreateText("Category Name", card.transform, CategorySectionTitle(category), 30, DustBotTheme.Ink, TextAnchor.LowerLeft);
            title.fontStyle = FontStyle.Bold;
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.21f, 0.50f), new Vector2(0.72f, 0.88f), Vector2.zero, Vector2.zero);
            Text description = UIFactory.CreateText("Category Description", card.transform, CategoryDescription(category), 19, DustBotTheme.MutedInk, TextAnchor.UpperLeft);
            UIFactory.SetAnchors(description.rectTransform, new Vector2(0.21f, 0.11f), new Vector2(0.75f, 0.50f), Vector2.zero, Vector2.zero);
            Text count = UIFactory.CreateText("Owned Count", card.transform, owned + " / " + items.Count + " OWNED", 20, DustBotTheme.MintDark);
            count.fontStyle = FontStyle.Bold;
            UIFactory.SetAnchors(count.rectTransform, new Vector2(0.73f, 0.25f), new Vector2(0.97f, 0.69f), Vector2.zero, Vector2.zero);

            if (hasNew)
            {
                Image badge = UIFactory.CreatePanel("New Badge", card.transform, DustBotTheme.Coral);
                UIFactory.SetAnchors(badge.rectTransform, new Vector2(0.82f, 0.72f), new Vector2(0.96f, 0.91f), Vector2.zero, Vector2.zero);
                Text badgeText = UIFactory.CreateText("New", badge.transform, "NEW", 16, Color.white);
                badgeText.fontStyle = FontStyle.Bold;
                UIFactory.Stretch(badgeText.gameObject);
            }
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
            grid.cellSize = new Vector2(300f, ResponsiveStoreGrid.DefaultCardHeight);
            grid.spacing = new Vector2(22f, 22f);
            grid.childAlignment = TextAnchor.UpperCenter;

            LayoutElement gridLayout = gridObject.AddComponent<LayoutElement>();
            StoreGridMetrics initialMetrics = ResponsiveStoreGrid.Calculate(0f, items.Count);
            gridLayout.minHeight = initialMetrics.preferredHeight;
            gridLayout.preferredHeight = initialMetrics.preferredHeight;
            ResponsiveStoreGrid responsiveGrid = gridObject.AddComponent<ResponsiveStoreGrid>();

            for (int i = 0; i < items.Count; i++)
            {
                CreateCosmeticCard(app, grid.transform, items[i], category);
            }
            responsiveGrid.Refresh();
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
            Button card = UIFactory.CreateButton(
                "Cosmetic " + definition.id,
                parent,
                string.Empty,
                delegate { app.UI.ShowCosmeticDetail(refreshCategory, captured.id); },
                new Color(1f, 0.995f, 0.965f, 0.98f),
                18);
            card.interactable = true;
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

        private static void AddCosmeticPreview(Transform parent, CosmeticDefinition definition)
        {
            if (definition.category == CosmeticCategory.PathTrail)
            {
                Image track = UIFactory.CreatePanel("Trail Track", parent, new Color(1f, 1f, 1f, 0.55f));
                UIFactory.SetAnchors(track.rectTransform, new Vector2(0.10f, 0.35f), new Vector2(0.90f, 0.65f), Vector2.zero, Vector2.zero);
                track.raycastTarget = false;
                for (int i = 0; i < 5; i++)
                {
                    Image node = UIFactory.CreateUIObject("Trail Node", track.transform).AddComponent<Image>();
                    node.sprite = CosmeticSpriteLibrary.PathNode(definition.id);
                    node.preserveAspect = true;
                    node.raycastTarget = false;
                    UIFactory.SetAnchors(node.rectTransform, new Vector2(0.02f + i * 0.20f, 0.02f), new Vector2(0.18f + i * 0.20f, 0.98f), Vector2.zero, Vector2.zero);
                }
                return;
            }

            if (definition.category == CosmeticCategory.RoomTheme ||
                definition.category == CosmeticCategory.TileTheme)
            {
                CreateTwoTonePreview(parent, definition);
                return;
            }

            if (definition.category == CosmeticCategory.Bundle)
            {
                int count = definition.bundleItemIds == null ? 0 : Mathf.Min(3, definition.bundleItemIds.Length);
                for (int i = 0; i < count; i++)
                {
                    CosmeticDefinition item = CosmeticCatalog.Find(definition.bundleItemIds[i]);
                    Image bundleIcon = UIFactory.CreateUIObject("Bundle Item", parent).AddComponent<Image>();
                    bundleIcon.sprite = CosmeticSpriteLibrary.Preview(item);
                    bundleIcon.preserveAspect = true;
                    bundleIcon.raycastTarget = false;
                    UIFactory.SetAnchors(bundleIcon.rectTransform, new Vector2(0.08f + i * 0.29f, 0.18f), new Vector2(0.34f + i * 0.29f, 0.82f), Vector2.zero, Vector2.zero);
                }
                return;
            }

            GameObject iconObject = UIFactory.CreateUIObject("Preview Icon", parent);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = CosmeticSpriteLibrary.Preview(definition);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UIFactory.SetAnchors(icon.rectTransform, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.88f), Vector2.zero, Vector2.zero);
            icon.color = Color.white;
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
                        Color.white);
                    tile.sprite = definition.category == CosmeticCategory.RoomTheme
                        ? CosmeticSpriteLibrary.Room(definition.id)
                        : CosmeticSpriteLibrary.Tile(definition.id, (x + y) % 2 != 0);
                    tile.type = Image.Type.Simple;
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
                return definition.category == CosmeticCategory.Bundle ? "APPLY SET" : "EQUIP";
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
                case CosmeticCategory.DustBotSkin: return "DUSTBOTS";
                case CosmeticCategory.PathTrail: return "TRAILS";
                case CosmeticCategory.CrumbStyle: return "CRUMBS";
                case CosmeticCategory.CatSkin: return "CATS";
                case CosmeticCategory.TileTheme: return "TILES";
                case CosmeticCategory.DockDesign: return "DOCKS";
                case CosmeticCategory.RoomTheme: return "ROOMS";
                default: return "BUNDLES";
            }
        }

        private static string CategorySectionTitle(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.DustBotSkin: return "DustBot Skins";
                case CosmeticCategory.PathTrail: return "Path Trails";
                case CosmeticCategory.CrumbStyle: return "Crumb Styles";
                case CosmeticCategory.CatSkin: return "Cat Skins";
                case CosmeticCategory.TileTheme: return "Tile / Floor Themes";
                case CosmeticCategory.DockDesign: return "Dock Designs";
                case CosmeticCategory.RoomTheme: return "Room Themes";
                default: return "Bundles";
            }
        }

        private static string CategoryDescription(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.DustBotSkin: return "Change your cleaning hero.";
                case CosmeticCategory.PathTrail: return "Customize your route.";
                case CosmeticCategory.CrumbStyle: return "Change what DustBot cleans.";
                case CosmeticCategory.CatSkin: return "Customize the chase.";
                case CosmeticCategory.DockDesign: return "Change your charging station.";
                case CosmeticCategory.TileTheme: return "Change the board.";
                case CosmeticCategory.RoomTheme: return "Change the background.";
                default: return "Matching cosmetic sets.";
            }
        }

        private static Sprite CategoryIcon(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.DustBotSkin: return DustBotSprites.Player;
                case CosmeticCategory.PathTrail: return CosmeticSpriteLibrary.PathNode("path_gold");
                case CosmeticCategory.CrumbStyle: return DustBotSprites.Crumbs;
                case CosmeticCategory.CatSkin: return DustBotSprites.Cat;
                case CosmeticCategory.DockDesign: return DustBotSprites.Dock;
                case CosmeticCategory.TileTheme: return DustBotSprites.Toy;
                case CosmeticCategory.RoomTheme: return DustBotSprites.Wall;
                case CosmeticCategory.Bundle: return DustBotSprites.DustBunny;
                default: return null;
            }
        }

        private static Color CategoryColor(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.DustBotSkin: return new Color32(195, 232, 220, 255);
                case CosmeticCategory.PathTrail: return new Color32(198, 220, 244, 255);
                case CosmeticCategory.CrumbStyle: return new Color32(244, 220, 174, 255);
                case CosmeticCategory.CatSkin: return new Color32(246, 203, 182, 255);
                case CosmeticCategory.DockDesign: return new Color32(218, 209, 244, 255);
                case CosmeticCategory.TileTheme: return new Color32(213, 229, 196, 255);
                case CosmeticCategory.RoomTheme: return new Color32(242, 205, 226, 255);
                default: return new Color32(250, 224, 145, 255);
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
                default: return "PRODUCTION 255";
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
