using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustBot
{
    public sealed class UIManager : MonoBehaviour
    {
        private DustBotApp app;
        private RectTransform safeArea;
        private GameObject currentScreen;
        private GameObject modalOverlay;
        private Image background;

        public void Initialize(DustBotApp application)
        {
            if (application == null)
            {
                throw new System.ArgumentNullException("application");
            }

            app = application;
            BuildCanvas();
        }

        public void ShowMainMenu()
        {
            if (app.Audio != null) app.Audio.PlayMenuMusic();
            SwitchTo(MenuScreens.BuildMainMenu(app, safeArea));
        }

        public void ShowLevelSelect(int page = -1)
        {
            if (app.Audio != null) app.Audio.PlayMenuMusic();
            if (page < 0)
            {
                int level = app.Levels.ActiveGenerationMode == GenerationMode.ProductionCampaign
                    ? app.Progression.Data.highestUnlockedMainLevel
                    : app.CurrentCampaignLevel;
                page = (level - 1) / MenuScreens.LevelsPerPage;
            }

            SwitchTo(MenuScreens.BuildLevelSelect(app, safeArea, page));
        }

        public void ShowSettings()
        {
            if (app.Audio != null) app.Audio.PlayMenuMusic();
            SwitchTo(MenuScreens.BuildSettings(app, safeArea));
        }

        public void ShowHowToPlay()
        {
            if (app.Audio != null) app.Audio.PlayMenuMusic();
            SwitchTo(MenuScreens.BuildHowToPlay(app, safeArea));
        }

        public void ShowCosmetics()
        {
            ShowCosmetics(CosmeticCategory.BotSkin, 0);
        }

        public void ShowCosmetics(CosmeticCategory category, int page = 0)
        {
            if (app.Audio != null) app.Audio.PlayMenuMusic();
            SwitchTo(MenuScreens.BuildCosmetics(app, safeArea, category, page));
        }

        public void ShowDeveloperPanel()
        {
            if (!LevelGenerationConfig.DeveloperToolsEnabled)
            {
                return;
            }

            if (app.Audio != null) app.Audio.PlayMenuMusic();
            SwitchTo(MenuScreens.BuildDeveloperPanel(app, safeArea));
        }

        public void ShowGame(LevelDefinition level)
        {
            if (level == null)
            {
                throw new System.ArgumentNullException("level");
            }

            GameObject screen = UIFactory.CreateUIObject("Game Screen", safeArea);
            UIFactory.Stretch(screen);
            GameScreen gameScreen = screen.AddComponent<GameScreen>();
            gameScreen.Initialize(app, level);
            if (app.Audio != null) app.Audio.PlayGameplayMusic(level);
            SwitchTo(screen);
        }

        public void ShowMasterCleanLockedMessage()
        {
            ShowInfoModal(
                "MASTER CLEAN LOCKED",
                "Complete Level " + LevelManifest.MainJourneyLevelCount + " to unlock Master Clean.\n\nKeep cleaning!");
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject(
                "DustBot Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backgroundObject = UIFactory.CreateUIObject("Background", canvas.transform);
            background = backgroundObject.AddComponent<Image>();
            background.color = DustBotTheme.Background;
            UIFactory.Stretch(background.gameObject);

            CreateBackgroundBubble(canvas.transform, new Vector2(0.08f, 0.84f), 330f, DustBotTheme.Mint, 0.08f);
            CreateBackgroundBubble(canvas.transform, new Vector2(0.92f, 0.66f), 430f, DustBotTheme.Yellow, 0.09f);
            CreateBackgroundBubble(canvas.transform, new Vector2(0.12f, 0.18f), 380f, DustBotTheme.Blue, 0.06f);
            CreateBackgroundSprite(canvas.transform, DustBotSprites.Crumbs, new Vector2(0.09f, 0.72f), 230f, -16f, 0.055f);
            CreateBackgroundSprite(canvas.transform, DustBotSprites.Cord, new Vector2(0.92f, 0.84f), 280f, 18f, 0.04f);
            CreateBackgroundSprite(canvas.transform, DustBotSprites.DustBunny, new Vector2(0.86f, 0.13f), 300f, 8f, 0.055f);

            GameObject safeAreaObject = UIFactory.CreateUIObject("Safe Area", canvas.transform);
            safeArea = UIFactory.Stretch(safeAreaObject);
            safeAreaObject.AddComponent<SafeAreaFitter>();

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        private static void CreateBackgroundBubble(
            Transform parent,
            Vector2 anchor,
            float size,
            Color color,
            float alpha)
        {
            GameObject bubbleObject = UIFactory.CreateUIObject("Background Bubble", parent);
            Image bubble = bubbleObject.AddComponent<Image>();
            bubble.sprite = SpriteFactory.Circle;
            bubble.color = new Color(color.r, color.g, color.b, alpha);
            bubble.raycastTarget = false;
            RectTransform rect = bubble.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * size;
        }

        private static void CreateBackgroundSprite(
            Transform parent,
            Sprite sprite,
            Vector2 anchor,
            float size,
            float rotation,
            float alpha)
        {
            GameObject objectSprite = UIFactory.CreateUIObject("Background Illustration", parent);
            Image image = objectSprite.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, alpha);
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        private void SwitchTo(GameObject screen)
        {
            if (background != null && app != null && app.Cosmetics != null)
            {
                background.color = app.Cosmetics.ActiveRoomBackground;
            }

            if (currentScreen != null && currentScreen != screen)
            {
                currentScreen.SetActive(false);
                Destroy(currentScreen);
            }

            if (modalOverlay != null)
            {
                Destroy(modalOverlay);
                modalOverlay = null;
            }

            currentScreen = screen;
        }

        private void ShowInfoModal(string title, string body)
        {
            if (safeArea == null)
            {
                return;
            }

            if (modalOverlay != null)
            {
                Destroy(modalOverlay);
            }

            modalOverlay = UIFactory.CreateUIObject("Info Modal", safeArea);
            UIFactory.Stretch(modalOverlay);
            Image shade = modalOverlay.AddComponent<Image>();
            shade.color = DustBotTheme.Overlay;

            Image panel = UIFactory.CreatePanel("Modal Panel", modalOverlay.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(panel.rectTransform, new Vector2(0.105f, 0.34f), new Vector2(0.895f, 0.66f), Vector2.zero, Vector2.zero);
            Shadow shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.07f, 0.06f, 0.32f);
            shadow.effectDistance = new Vector2(0f, -8f);

            Text titleText = UIFactory.CreateText(
                "Modal Title",
                panel.transform,
                title,
                38,
                DustBotTheme.Ink);
            titleText.fontStyle = FontStyle.Bold;
            UIFactory.SetAnchors(titleText.rectTransform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

            Text bodyText = UIFactory.CreateText(
                "Modal Body",
                panel.transform,
                body,
                27,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(bodyText.rectTransform, new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);

            Button ok = UIFactory.CreateButton(
                "Modal OK",
                panel.transform,
                "GOT IT",
                delegate
                {
                    if (modalOverlay != null)
                    {
                        Destroy(modalOverlay);
                        modalOverlay = null;
                    }
                },
                DustBotTheme.MintDark,
                28);
            UIFactory.SetAnchors(ok.GetComponent<RectTransform>(), new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.24f), Vector2.zero, Vector2.zero);
        }
    }
}
