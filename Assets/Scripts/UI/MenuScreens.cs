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
            bool journeyComplete = app.Progression.IsMainJourneyComplete();
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
                journeyComplete ? "MASTER CLEAN AWAITS" : "READY FOR THE NEXT ROOM?",
                32,
                DustBotTheme.Ink,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(greeting.rectTransform, new Vector2(0.4f, 0.55f), new Vector2(0.95f, 0.84f), Vector2.zero, Vector2.zero);
            greeting.fontStyle = FontStyle.Bold;
            Text progress = UIFactory.CreateText(
                "Progress Summary",
                hero.transform,
                string.Format(
                    "LEVEL {0}\n{1} STARS  •  {2} BUNNIES\n{3} DAY DAILY STREAK",
                    app.Progression.Data.highestUnlockedMainLevel,
                    app.Progression.Data.totalStars,
                    app.Progression.Data.totalDustBunnies,
                    app.Progression.Data.daily.currentStreak),
                25,
                DustBotTheme.MutedInk,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(progress.rectTransform, new Vector2(0.4f, 0.12f), new Vector2(0.95f, 0.57f), Vector2.zero, Vector2.zero);

            Button play = UIFactory.CreateButton(
                "Primary Play",
                root.transform,
                journeyComplete
                    ? "PLAY MASTER CLEAN"
                    : "PLAY  •  LEVEL " + app.Progression.Data.highestUnlockedMainLevel,
                delegate
                {
                    if (journeyComplete) app.StartMaster();
                    else app.StartMainLevel(app.Progression.Data.highestUnlockedMainLevel);
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
                "Skins, trails, themes, docks, animations and bundles",
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
            master.interactable = masterAvailable;

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

            return root;
        }

        public static GameObject BuildLevelSelect(DustBotApp app, RectTransform parent, int requestedPage)
        {
            int pageCount = (LevelManifest.MainJourneyLevelCount + LevelsPerPage - 1) / LevelsPerPage;
            int page = Mathf.Clamp(requestedPage, 0, pageCount - 1);
            GameObject root = CreateRoot("Level Select", parent);

            Button back = UIFactory.CreateButton("Back", root.transform, "HOME", app.UI.ShowMainMenu, DustBotTheme.MutedInk, 26);
            UIFactory.SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.04f, 0.91f), new Vector2(0.25f, 0.975f), Vector2.zero, Vector2.zero);

            Text title = UIFactory.CreateText("Title", root.transform, "MAIN JOURNEY", 54, DustBotTheme.Ink);
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
            int lastLevel = Mathf.Min(LevelManifest.MainJourneyLevelCount, firstLevel + LevelsPerPage - 1);
            for (int levelNumber = firstLevel; levelNumber <= lastLevel; levelNumber++)
            {
                int capturedLevel = levelNumber;
                bool unlocked = levelNumber <= app.Progression.Data.highestUnlockedMainLevel;
                int stars = app.Progression.GetStars(levelNumber);
                bool bunny = app.Progression.HasDustBunny(levelNumber);
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
                string.Format("Levels {0}-{1} of 6000", firstLevel, lastLevel),
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

        public static GameObject BuildSettings(DustBotApp app, RectTransform parent)
        {
            GameObject root = CreateRoot("Settings", parent);
            Text title = UIFactory.CreateText("Title", root.transform, "SETTINGS", 72, DustBotTheme.Ink);
            UIFactory.SetAnchors(title.rectTransform, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.92f), Vector2.zero, Vector2.zero);

            RectTransform buttons = CreateVerticalButtonArea(root.transform, new Vector2(0.14f, 0.28f), new Vector2(0.86f, 0.75f));
            PlayerSettingsData settings = app.Progression.Data.settings;

            Button sound = AddMenuButton(buttons, ToggleLabel("SOUND", settings.soundEnabled), null, DustBotTheme.Mint);
            sound.onClick.AddListener(delegate
            {
                settings.soundEnabled = !settings.soundEnabled;
                UIFactory.GetButtonText(sound).text = ToggleLabel("SOUND", settings.soundEnabled);
                app.ApplySettings();
                app.SaveNow();
            });

            Button music = AddMenuButton(buttons, ToggleLabel("MUSIC", settings.musicEnabled), null, DustBotTheme.Blue);
            music.onClick.AddListener(delegate
            {
                settings.musicEnabled = !settings.musicEnabled;
                UIFactory.GetButtonText(music).text = ToggleLabel("MUSIC", settings.musicEnabled);
                app.ApplySettings();
                app.SaveNow();
            });

            Button haptics = AddMenuButton(buttons, ToggleLabel("HAPTICS", settings.hapticsEnabled), null, DustBotTheme.Coral);
            haptics.onClick.AddListener(delegate
            {
                settings.hapticsEnabled = !settings.hapticsEnabled;
                UIFactory.GetButtonText(haptics).text = ToggleLabel("HAPTICS", settings.hapticsEnabled);
                app.ApplySettings();
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
            const int itemsPerPage = 4;
            IReadOnlyList<CosmeticDefinition> categoryItems = CosmeticCatalog.ForCategory(category);
            int pageCount = Mathf.Max(1, (categoryItems.Count + itemsPerPage - 1) / itemsPerPage);
            int page = Mathf.Clamp(requestedPage, 0, pageCount - 1);
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

            GameObject tabsObject = UIFactory.CreateUIObject("Category Tabs", root.transform);
            RectTransform tabsRect = tabsObject.GetComponent<RectTransform>();
            UIFactory.SetAnchors(tabsRect, new Vector2(0.055f, 0.70f), new Vector2(0.945f, 0.855f), Vector2.zero, Vector2.zero);
            GridLayoutGroup tabs = tabsObject.AddComponent<GridLayoutGroup>();
            tabs.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tabs.constraintCount = 4;
            tabs.cellSize = new Vector2(225f, 65f);
            tabs.spacing = new Vector2(14f, 12f);
            tabs.childAlignment = TextAnchor.MiddleCenter;

            Array categories = Enum.GetValues(typeof(CosmeticCategory));
            for (int i = 0; i < categories.Length; i++)
            {
                CosmeticCategory capturedCategory = (CosmeticCategory)categories.GetValue(i);
                Button tab = UIFactory.CreateButton(
                    "Tab " + capturedCategory,
                    tabs.transform,
                    CategoryLabel(capturedCategory),
                    delegate
                    {
                        app.UI.ShowCosmetics(capturedCategory, 0);
                    },
                    capturedCategory == category ? DustBotTheme.MintDark : DustBotTheme.MutedInk,
                    18);
                UIFactory.GetButtonText(tab).resizeTextMinSize = 12;
            }

            Text categoryTitle = UIFactory.CreateText(
                "Category Title",
                root.transform,
                CategoryLabel(category) + "  •  " + categoryItems.Count + " ITEMS",
                30,
                DustBotTheme.Ink,
                TextAnchor.MiddleLeft);
            UIFactory.SetAnchors(categoryTitle.rectTransform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
            categoryTitle.fontStyle = FontStyle.Bold;

            RectTransform items = CreateVerticalButtonArea(
                root.transform,
                new Vector2(0.08f, 0.20f),
                new Vector2(0.92f, 0.64f));
            int first = page * itemsPerPage;
            int last = Mathf.Min(categoryItems.Count, first + itemsPerPage);
            for (int i = first; i < last; i++)
            {
                CosmeticDefinition definition = categoryItems[i];
                CosmeticDefinition captured = definition;
                string lockReason = app.Cosmetics.LockReason(definition);
                bool owned = app.Cosmetics.Owns(definition.id);
                string status = app.Cosmetics.Status(definition);
                string detail = owned
                    ? status
                    : !string.IsNullOrEmpty(lockReason)
                        ? lockReason + "  •  " + definition.coinPrice + " Coins"
                        : definition.coinPrice + " Dust Coins";
                Button button = AddMenuButton(
                    items,
                    string.Format(
                        "{0}  •  {1}\n{2}",
                        definition.displayName.ToUpperInvariant(),
                        definition.rarity.ToString().ToUpperInvariant(),
                        detail),
                    delegate
                    {
                        if (app.Cosmetics.TryUnlockOrSelect(captured.id))
                        {
                            app.SaveNow();
                            app.UI.ShowCosmetics(category, page);
                        }
                    },
                    RarityColor(definition.rarity),
                    RarityTextColor(definition.rarity),
                    CategoryIcon(definition.category));
                UIFactory.GetButtonText(button).fontSize = 25;
                button.interactable =
                    owned ||
                    (string.IsNullOrEmpty(lockReason) && app.Economy.Coins >= definition.coinPrice);
                if (definition.category == CosmeticCategory.PathColor)
                {
                    AddPathSwatch(button, definition.colorHex);
                }
                else
                {
                    TintButtonPreview(button, definition.colorHex);
                }
            }

            Button previous = UIFactory.CreateButton(
                "Previous",
                root.transform,
                "PREV",
                delegate { app.UI.ShowCosmetics(category, page - 1); },
                DustBotTheme.Blue,
                24);
            UIFactory.SetAnchors(previous.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.27f, 0.16f), Vector2.zero, Vector2.zero);
            previous.interactable = page > 0;

            Text pageText = UIFactory.CreateText(
                "Page",
                root.transform,
                string.Format("PAGE {0}/{1}\nCosmetics never affect gameplay.", page + 1, pageCount),
                22,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(pageText.rectTransform, new Vector2(0.29f, 0.075f), new Vector2(0.71f, 0.165f), Vector2.zero, Vector2.zero);

            Button next = UIFactory.CreateButton(
                "Next",
                root.transform,
                "NEXT",
                delegate { app.UI.ShowCosmetics(category, page + 1); },
                DustBotTheme.Blue,
                24);
            UIFactory.SetAnchors(next.GetComponent<RectTransform>(), new Vector2(0.73f, 0.08f), new Vector2(0.92f, 0.16f), Vector2.zero, Vector2.zero);
            next.interactable = page < pageCount - 1;
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
                case CosmeticCategory.BotSkin: return "BOTS";
                case CosmeticCategory.PathColor: return "TRAILS";
                case CosmeticCategory.TileTheme: return "TILES";
                case CosmeticCategory.DockDesign: return "DOCKS";
                case CosmeticCategory.WinAnimation: return "WINS";
                case CosmeticCategory.FailureAnimation: return "FAILS";
                case CosmeticCategory.RoomBackground: return "ROOMS";
                default: return "BUNDLES";
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
                case CosmeticCategory.FailureAnimation: return DustBotSprites.Sock;
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

        private static string StarText(int stars)
        {
            if (stars <= 0) return "☆ ☆ ☆";
            if (stars == 1) return "★ ☆ ☆";
            if (stars == 2) return "★ ★ ☆";
            return "★ ★ ★";
        }
    }
}
