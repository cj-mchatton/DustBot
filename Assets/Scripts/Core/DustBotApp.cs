using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DustBot
{
    public sealed class DustBotApp : MonoBehaviour
    {
        public LevelLoader Levels { get; private set; }
        public SaveSystem Save { get; private set; }
        public ProgressionSystem Progression { get; private set; }
        public EconomySystem Economy { get; private set; }
        public CosmeticSystem Cosmetics { get; private set; }
        public IRewardedHintProvider Ads { get; private set; }
        public AudioManager Audio { get; private set; }
        public HapticsManager Haptics { get; private set; }
        public UIManager UI { get; private set; }
        public int CurrentCampaignLevel { get; private set; } = 1;
        public LevelDefinition CurrentLevel { get; private set; }
        public bool CanAccessMasterClean
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return Progression != null && Progression.IsMainJourneyComplete();
#endif
            }
        }

        private void Awake()
        {
            Levels = new LevelLoader();
            Save = new SaveSystem();
            Progression = new ProgressionSystem(Save.Load());
            Economy = new EconomySystem(Progression);
            Cosmetics = new CosmeticSystem(Progression, Economy);
            Ads = new AdsStub();
            Audio = gameObject.AddComponent<AudioManager>();
            Haptics = gameObject.AddComponent<HapticsManager>();
            UI = gameObject.AddComponent<UIManager>();

            ApplySettings();
            UI.Initialize(this);
            AnalyticsStub.Track("game_start");
        }

        private void Start()
        {
            string capturePath;
            if (TryGetCapturePath(out capturePath))
            {
                StartCoroutine(CaptureStoreScreenshots(capturePath));
            }
            else
            {
                UI.ShowMainMenu();
            }
        }

        public void StartMainLevel(int levelNumber)
        {
            CurrentCampaignLevel = Math.Max(1, Math.Min(Levels.CampaignLevelCount, levelNumber));
            CurrentLevel = Levels.LoadCampaign(CurrentCampaignLevel);
            UI.ShowGame(CurrentLevel);
        }

        public void StartDaily()
        {
            CurrentLevel = Levels.LoadDaily(DateTime.Today);
            UI.ShowGame(CurrentLevel);
        }

        public void StartMaster()
        {
            if (!CanAccessMasterClean)
            {
                UI.ShowMasterCleanLockedMessage();
                return;
            }

            CurrentLevel = Levels.LoadMaster(Progression.Data.masterCleanProgress);
            UI.ShowGame(CurrentLevel);
        }

        public void StartEndless()
        {
            CurrentLevel = Levels.LoadEndless(
                Progression.Data.endlessRunSeed,
                Progression.Data.endlessCurrentLevel);
            UI.ShowGame(CurrentLevel);
        }

        public void SetGenerationMode(GenerationMode mode)
        {
            if (!LevelGenerationConfig.DeveloperToolsEnabled)
            {
                return;
            }

            Levels.SetGenerationMode(mode);
            CurrentCampaignLevel = 1;
            CurrentLevel = null;
        }

        public void RestartCampaignLevel()
        {
            StartMainLevel(CurrentCampaignLevel);
        }

        public void PreviousCampaignLevel()
        {
            StartMainLevel(Math.Max(1, CurrentCampaignLevel - 1));
        }

        public void NextCampaignLevel()
        {
            StartMainLevel(Math.Min(Levels.CampaignLevelCount, CurrentCampaignLevel + 1));
        }

        public void DebugAddCoins(int amount)
        {
            if (!LevelGenerationConfig.DeveloperToolsEnabled) return;
            Economy.Add(amount);
            SaveNow();
        }

        public void DebugUnlockCampaignAndMaster()
        {
            if (!LevelGenerationConfig.DeveloperToolsEnabled) return;
            PlayerProgressData data = Progression.Data;
            data.highestUnlockedMainLevel = LevelManifest.MainJourneyLevelCount;
            bool foundFinal = false;
            for (int i = 0; i < data.mainLevels.Count; i++)
            {
                if (data.mainLevels[i].levelNumber == LevelManifest.MainJourneyLevelCount)
                {
                    data.mainLevels[i].completed = true;
                    if (data.mainLevels[i].stars <= 0)
                    {
                        data.totalStars++;
                    }
                    data.mainLevels[i].stars = Math.Max(1, data.mainLevels[i].stars);
                    foundFinal = true;
                    break;
                }
            }

            if (!foundFinal)
            {
                data.mainLevels.Add(new LevelProgressRecord
                {
                    levelNumber = LevelManifest.MainJourneyLevelCount,
                    completed = true,
                    stars = 1
                });
                data.totalStars++;
            }
            SaveNow();
        }

        public void DebugUnlockAllCosmetics()
        {
            if (!LevelGenerationConfig.DeveloperToolsEnabled) return;
            IReadOnlyList<CosmeticDefinition> all = CosmeticCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (!Progression.Data.cosmetics.ownedCosmeticIds.Contains(all[i].id))
                {
                    Progression.Data.cosmetics.ownedCosmeticIds.Add(all[i].id);
                }
            }
            SaveNow();
        }

        public void CommitResult(LevelResult result)
        {
            result.coinsEarned = Progression.ApplyResult(result, DateTime.Today);
            result.cosmeticUnlocked = Cosmetics.GrantMilestoneCosmetics();
            SaveNow();
        }

        public int RegisterDailyAttempt()
        {
            int attempts = Progression.RegisterDailyAttempt(DateTime.Today);
            SaveNow();
            return attempts;
        }

        public void SaveNow()
        {
            Save.Save(Progression.Data);
        }

        public void ResetProgress()
        {
            Save.Delete();
            Progression.Reset();
            Economy = new EconomySystem(Progression);
            Cosmetics = new CosmeticSystem(Progression, Economy);
            ApplySettings();
            SaveNow();
        }

        public void ApplySettings()
        {
            PlayerSettingsData settings = Progression.Data.settings;
            if (Audio != null)
            {
                Audio.SoundVolume = settings.soundVolume;
                Audio.MusicVolume = settings.musicVolume;
                Audio.SoundEnabled = settings.soundEnabled;
                Audio.MusicEnabled = settings.musicEnabled;
            }

            if (Haptics != null)
            {
                Haptics.Enabled = settings.hapticsEnabled;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveNow();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }

        private bool TryGetCapturePath(out string path)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++)
            {
                if (arguments[i] == "-dustbotCaptureStore")
                {
                    path = arguments[i + 1];
                    return !string.IsNullOrEmpty(path);
                }
            }

            path = string.Empty;
            return false;
        }

        private IEnumerator CaptureStoreScreenshots(string path)
        {
            Directory.CreateDirectory(path);
            PrepareShowcaseProgress();

            UI.ShowMainMenu();
            yield return CaptureAfterLayout(Path.Combine(path, "01-main-menu.png"));

            UI.ShowLevelSelect(0);
            yield return CaptureAfterLayout(Path.Combine(path, "02-level-select.png"));

            UI.ShowGame(Levels.LoadMain(15));
            yield return null;
            GameScreen journey = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            if (journey != null)
            {
                journey.PrepareForStoreScreenshot(8);
            }
            yield return CaptureAfterLayout(Path.Combine(path, "03-route-puzzle.png"));

            UI.ShowGame(Levels.LoadDaily(new DateTime(2026, 6, 23)));
            yield return null;
            GameScreen daily = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            if (daily != null)
            {
                daily.PrepareForStoreScreenshot(6);
            }
            yield return CaptureAfterLayout(Path.Combine(path, "04-daily-challenge.png"));

            UI.ShowGame(Levels.LoadCampaign(GenerationMode.DevelopmentCampaign, 30));
            yield return null;
            GameScreen largeMaze = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            if (largeMaze != null)
            {
                largeMaze.PrepareLargeMazeScreenshot(24);
            }
            yield return CaptureAfterLayout(Path.Combine(path, "07-large-maze.png"));

            UI.ShowHowToPlay();
            yield return CaptureAfterLayout(Path.Combine(path, "05-how-to-play.png"));

            UI.ShowCosmetics();
            yield return CaptureAfterLayout(Path.Combine(path, "06-cosmetics.png"));

            Debug.Log("DUSTBOT_SCREENSHOTS_CAPTURED: " + path);
            Application.Quit();
        }

        private static IEnumerator CaptureAfterLayout(string filePath)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            UnityEngine.ScreenCapture.CaptureScreenshot(filePath, 1);
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.35f);
        }

        private void PrepareShowcaseProgress()
        {
            PlayerProgressData data = Progression.Data;
            data.highestUnlockedMainLevel = 43;
            data.dustCoins = 245;
            data.roomsCleaned = 48;
            data.totalDustBunnies = 12;
            data.endlessCurrentLevel = 13;
            data.endlessBestScore = 12;
            data.daily.currentStreak = 4;
            data.daily.bestStreak = 7;
            data.cosmetics.ownedCosmeticIds = new List<string>
            {
                CosmeticCatalog.DefaultBot,
                CosmeticCatalog.DefaultPath,
                "bot_lavender",
                "path_coral"
            };
            data.mainLevels = new List<LevelProgressRecord>();
            data.totalStars = 0;
            for (int levelNumber = 1; levelNumber <= 24; levelNumber++)
            {
                int stars = 1 + levelNumber % 3;
                data.mainLevels.Add(new LevelProgressRecord
                {
                    levelNumber = levelNumber,
                    stars = stars,
                    completed = true,
                    dustBunnyCollected = levelNumber % 6 == 0
                });
                data.totalStars += stars;
            }
        }
    }
}
