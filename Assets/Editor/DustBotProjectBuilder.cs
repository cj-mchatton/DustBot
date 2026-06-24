#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.iOS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustBot.Editor
{
    public static class DustBotProjectBuilder
    {
        private const string AppIconPath = "Assets/Resources/DustBotIcon.png";
        private const string UIMaterialPath = "Assets/Resources/DustBotUIMaterial.mat";
        private const string BundleIdentifier = "com.dustbotgames.dustbot";

        [MenuItem("DustBot/Configure Project")]
        public static void ConfigureProject()
        {
            EnsureFolder("Assets/Scenes");
            if (!File.Exists("Assets/Scenes/Boot.unity"))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boot.unity");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Boot.unity", true)
            };

            PlayerSettings.companyName = "DustBot Games";
            PlayerSettings.productName = "DustBot";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleIdentifier);
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.iOS,
                ManagedStrippingLevel.Medium);
            PlayerSettings.SetApiCompatibilityLevel(
                NamedBuildTarget.iOS,
                ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.runInBackground = false;
            PlayerSettings.muteOtherAudioSources = false;

            ConfigureUIMaterial();
            ConfigureAppIcon();

            AssetDatabase.SaveAssets();
        }

        [MenuItem("DustBot/Run Content Validation")]
        public static void RunContentValidation()
        {
            ConfigureProject();
            LevelLoader loader = new LevelLoader();

            for (int i = 1; i <= LevelManifest.TutorialLevelCount; i++)
            {
                ValidateAndSimulate(loader.LoadMain(i), "Tutorial " + i);
            }

            int[] deterministicChecks = { 16, 20, 30, 45, 50, 100, 180, 500, 1000, 2000, 4000, 6000 };
            int catLevelCount = 0;
            for (int number = LevelManifest.TutorialLevelCount + 1;
                 number <= LevelManifest.MainJourneyLevelCount;
                 number++)
            {
                LevelDefinition first = loader.LoadMain(number);
                if (Array.IndexOf(deterministicChecks, number) >= 0)
                {
                    LevelDefinition second = loader.LoadMain(number);
                    Require(
                        LevelValidator.Signature(first) == LevelValidator.Signature(second),
                        "Main level " + number + " was not deterministic.");
                }

                if (number > 25 && number != 30)
                {
                    LevelEngagementReport engagement = LevelEngagementEvaluator.Analyze(first);
                    Require(!engagement.tooTrivial, "Main level " + number + " was accepted as trivial.");
                    Require(!engagement.tooDense, "Main level " + number + " was accepted as overly dense.");
                }

                if (first.generationVersion >= 4)
                {
                    Require(
                        first.twoStarMoveTarget >= first.threeStarMoveTarget,
                        "Main level " + number + " has inverted star targets.");
                    if (first.hardPathLimit)
                    {
                        Require(
                            first.moveLimit >= first.parMoves,
                            "Main level " + number + " has an unfair hard path cap.");
                    }
                    else
                    {
                        Require(
                            first.moveLimit == 0,
                            "Main level " + number + " exposes a hidden hard path cap.");
                    }
                }

                if (first.cat != null && first.cat.IsEnabled)
                {
                    catLevelCount++;
                    List<GridPosition> catRoute = CatObstacleSimulator.BuildExpectedRoute(first);
                    CatRoutePreview preview = CatObstacleSimulator.SimulateRoute(
                        first,
                        catRoute);
                    Require(
                        !preview.collided,
                        "Main level " + number + " has an unsafe canonical cat route.");
                    CatRelevanceReport relevance =
                        CatObstacleSimulator.AnalyzeRelevance(first, catRoute);
                    Require(
                        relevance.IsStrategicallyActive,
                        string.Format(
                            "Main level {0} has a trapped or negligible cat: open {1}, reachable {2}, movement {3}, unique {4}, closest {5}, pressure {6}.",
                            number,
                            relevance.openStartNeighbors,
                            relevance.reachableTiles,
                            relevance.movementTurns,
                            relevance.uniqueVisitedTiles,
                            relevance.closestDistance,
                            relevance.pressureTurns));
                }

                ValidateAndSimulate(first, "Main " + number);
            }

            Require(catLevelCount >= 500, "Too few canonical levels use the cat mechanic.");
            Require(
                loader.LoadMain(30).cat.behavior == CatBehavior.Curious,
                "The Curious Cat introduction level is missing.");

            DateTime date = new DateTime(2026, 6, 22);
            LevelDefinition dailyA = loader.LoadDaily(date);
            LevelDefinition dailyB = loader.LoadDaily(date);
            Require(
                LevelValidator.Signature(dailyA) == LevelValidator.Signature(dailyB),
                "Daily challenge was not deterministic.");
            LevelEngagementReport dailyEngagement = LevelEngagementEvaluator.Analyze(dailyA);
            Require(dailyA.objectives.collectBonus, "Daily challenge did not include a Dust Bunny.");
            Require(dailyA.Count(CellContent.Crumb) >= 4, "Daily challenge did not include enough crumbs.");
            Require(dailyEngagement.score >= 38, "Daily challenge engagement score was too low.");
            Require(dailyEngagement.turns >= 8, "Daily challenge route did not require enough planning.");
            Require(dailyEngagement.bonusDetourCost >= 2, "Daily Dust Bunny was not a real detour.");
            Require(dailyA.hardPathLimit, "Daily challenge did not enforce a hard path limit.");
            ValidateAndSimulate(dailyA, "Daily 2026-06-22");

            LevelDefinition masterA = loader.LoadMaster(500);
            LevelDefinition masterB = loader.LoadMaster(500);
            Require(
                LevelValidator.Signature(masterA) == LevelValidator.Signature(masterB),
                "Master level 500 was not deterministic.");
            Require(masterA.hardPathLimit, "Master Clean reset to a soft early-game profile.");
            ValidateAndSimulate(masterA, "Master 500");

            ValidateHazardBlocking(loader.LoadMain(7));
            ValidateCatTurnMechanic(loader.LoadMain(30));
            ValidateProgression();
            Debug.Log(
                "DUSTBOT_VALIDATION_PASSED: 6,000 canonical journey levels, " +
                catLevelCount +
                " deterministic cat levels, engagement pacing, archetypes, tutorials, " +
                "daily, master, economy claims, cosmetics, route simulation, hazards, and progression.");
        }

        [MenuItem("DustBot/Audit Cat Relevance")]
        public static void AuditCatRelevance()
        {
            LevelLoader loader = new LevelLoader();
            int catLevels = 0;
            int weakLevels = 0;
            int trappedLevels = 0;
            int minimumReachable = int.MaxValue;
            int minimumUnique = int.MaxValue;
            int minimumMovement = int.MaxValue;
            int maximumClosestDistance = 0;
            int minimumPressureTurns = int.MaxValue;

            for (int number = 1; number <= LevelManifest.MainJourneyLevelCount; number++)
            {
                LevelDefinition level = loader.LoadMain(number);
                if (level.cat == null || !level.cat.IsEnabled)
                {
                    continue;
                }

                catLevels++;
                CatRelevanceReport relevance = CatObstacleSimulator.AnalyzeRelevance(
                    level,
                    CatObstacleSimulator.BuildExpectedRoute(level));
                minimumReachable = Math.Min(minimumReachable, relevance.reachableTiles);
                minimumUnique = Math.Min(minimumUnique, relevance.uniqueVisitedTiles);
                minimumMovement = Math.Min(minimumMovement, relevance.movementTurns);
                maximumClosestDistance = Math.Max(maximumClosestDistance, relevance.closestDistance);
                minimumPressureTurns = Math.Min(minimumPressureTurns, relevance.pressureTurns);
                if (relevance.openStartNeighbors == 0 || relevance.reachableTiles < 4)
                {
                    trappedLevels++;
                }

                if (!relevance.IsStrategicallyActive)
                {
                    weakLevels++;
                    Debug.LogWarning(
                        string.Format(
                            "CAT_RELEVANCE_WEAK level {0}: behavior={1}, open={2}, reachable={3}, moves={4}, unique={5}, closest={6}, pressure={7}",
                            number,
                            level.cat.behavior,
                            relevance.openStartNeighbors,
                            relevance.reachableTiles,
                            relevance.movementTurns,
                            relevance.uniqueVisitedTiles,
                            relevance.closestDistance,
                            relevance.pressureTurns));
                }
            }

            Debug.Log(
                string.Format(
                    "DUSTBOT_CAT_AUDIT: cats={0}, weak={1}, trapped={2}, minimum reachable={3}, minimum unique={4}, minimum movement turns={5}, maximum closest distance={6}, minimum pressure turns={7}",
                    catLevels,
                    weakLevels,
                    trappedLevels,
                    minimumReachable,
                    minimumUnique,
                    minimumMovement,
                    maximumClosestDistance,
                    minimumPressureTurns));
            Require(weakLevels == 0, weakLevels + " cat levels failed the relevance audit.");
            Require(trappedLevels == 0, trappedLevels + " cat levels were trapped.");
        }

        [MenuItem("DustBot/Build iOS Release")]
        public static void BuildIOSRelease()
        {
            ConfigureProject();
            RunContentValidation();

            string buildPath = Path.GetFullPath("Build/iOS");
            Directory.CreateDirectory(buildPath);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Boot.unity" },
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "iOS release build failed: {0} errors, {1} warnings.",
                        report.summary.totalErrors,
                        report.summary.totalWarnings));
            }

            Debug.Log(
                string.Format(
                    "DUSTBOT_IOS_BUILD_PASSED: {0} ({1:0.0} MB)",
                    buildPath,
                    report.summary.totalSize / (1024f * 1024f)));
        }

        [MenuItem("DustBot/Build Store Screenshot Player")]
        public static void BuildStoreScreenshotPlayer()
        {
            ConfigureProject();
            string buildDirectory = Path.GetFullPath("Build/ScreenshotPlayer");
            Directory.CreateDirectory(buildDirectory);
            string appPath = Path.Combine(buildDirectory, "DustBot.app");
            bool previousRunInBackground = PlayerSettings.runInBackground;
            PlayerSettings.runInBackground = true;
            try
            {
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/Boot.unity" },
                    locationPathName = appPath,
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                };

                BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException("Store screenshot player build failed.");
                }
            }
            finally
            {
                PlayerSettings.runInBackground = previousRunInBackground;
            }

            Debug.Log("DUSTBOT_SCREENSHOT_PLAYER_PASSED: " + appPath);
        }

        private static void ValidateAndSimulate(LevelDefinition level, string label)
        {
            string message;
            Require(LevelValidator.TryValidate(level, out message), label + " invalid: " + message);

            GameSession session = new GameSession(level);
            if (session.HasCat)
            {
                Require(
                    session.State == GameSessionState.CatTurn,
                    label + " did not start in cat-turn mode.");
                for (int i = 0; i < level.expectedSolution.Count; i++)
                {
                    StepOutcome outcome;
                    Require(
                        session.TryCatTurn(
                            level.expectedSolution[i].direction,
                            out outcome),
                        label + " rejected expected cat turn " + i +
                        " while " + session.State +
                        " after " + session.FailureReason + ".");
                    if (session.State == GameSessionState.Failed)
                    {
                        break;
                    }
                }

                Require(
                    session.State == GameSessionState.Won,
                    label + " expected cat-turn solution did not win.");
                return;
            }

            Require(
                session.BeginPath(session.Grid.Start) == PathEditResult.Started,
                label + " could not begin its route.");
            for (int i = 0; i < level.expectedSolution.Count; i++)
            {
                SolutionStep step = level.expectedSolution[i];
                GridPosition next = step.position + DirectionUtility.ToOffset(step.direction);
                Require(
                    session.TryExtendPath(next) == PathEditResult.Added,
                    label + " could not draw expected step " + i + ".");
            }
            session.EndPath();

            Require(session.StartSimulation(), label + " could not start.");
            int guard = level.width * level.height * 4;
            while (session.State == GameSessionState.Simulating && guard-- > 0)
            {
                session.Advance();
            }

            Require(session.State == GameSessionState.Won, label + " expected route did not win.");
        }

        private static void ValidateHazardBlocking(LevelDefinition tutorial)
        {
            GameSession session = new GameSession(tutorial);
            Require(
                session.BeginPath(session.Grid.Start) == PathEditResult.Started,
                "Hazard test route could not begin.");
            Require(
                session.TryExtendPath(new GridPosition(1, 3)) == PathEditResult.Invalid,
                "Sock hazard did not block path drawing.");
        }

        private static void ValidateCatTurnMechanic(LevelDefinition tutorial)
        {
            GameSession session = new GameSession(tutorial);
            GridPosition initialCat = session.CatPosition;
            StepOutcome blockedOutcome;
            Require(
                !session.TryCatTurn(Direction.Left, out blockedOutcome),
                "Cat tutorial consumed a blocked swipe.");
            Require(
                session.Moves == 0 && session.CatPosition == initialCat,
                "A blocked swipe moved DustBot or the cat.");

            GridPosition hintTarget;
            Require(
                session.ApplyNextHint(out hintTarget),
                "Cat tutorial could not provide a safe one-move hint.");
            Require(
                session.UsedHint &&
                session.BotPosition == session.Grid.Start &&
                hintTarget != session.Grid.Start,
                "A Cat Chase hint moved DustBot or failed to mark hint use.");

            GridPosition destination;
            CatStepResult preview;
            FailureReason danger;
            Require(
                session.TryPreviewCatTurn(
                    Direction.Up,
                    out destination,
                    out preview,
                    out danger) &&
                danger == FailureReason.None &&
                preview.moveCount == 2,
                "Cat tutorial did not preview two deterministic cat moves.");

            StepOutcome firstTurn;
            Require(
                session.TryCatTurn(Direction.Up, out firstTurn) &&
                firstTurn.moved &&
                firstTurn.catMoveCount == 2 &&
                firstTurn.catFrom == initialCat,
                "Cat tutorial did not execute DustBot first and two cat moves second.");
        }

        private static void ValidateProgression()
        {
            DateTime date = new DateTime(2026, 6, 22);
            ProgressionSystem progression = new ProgressionSystem(new PlayerProgressData());
            int firstAward = progression.ApplyResult(new LevelResult
            {
                levelId = "MainJourney_1",
                mode = GameMode.MainJourney,
                levelNumber = 1,
                stars = 3,
                coinsEarned = 999
            }, date);
            int repeatAward = progression.ApplyResult(new LevelResult
            {
                levelId = "MainJourney_1",
                mode = GameMode.MainJourney,
                levelNumber = 1,
                stars = 3,
                coinsEarned = 999
            }, date);

            Require(firstAward == 20, "First-clear rewards changed unexpectedly.");
            Require(repeatAward == 0, "Completed levels can be farmed for coins.");
            Require(progression.Data.highestUnlockedMainLevel == 2, "Level completion did not unlock the next room.");
            Require(progression.Data.totalStars == 3, "Stars were not recorded.");

            int bunnyAward = progression.ApplyResult(new LevelResult
            {
                levelId = "MainJourney_1",
                mode = GameMode.MainJourney,
                levelNumber = 1,
                stars = 3,
                collectedBonus = true
            }, date);
            int duplicateBunny = progression.ApplyResult(new LevelResult
            {
                levelId = "MainJourney_1",
                mode = GameMode.MainJourney,
                levelNumber = 1,
                stars = 3,
                collectedBonus = true
            }, date);
            Require(
                bunnyAward == EconomyConfig.DustBunnyBonusCoins && duplicateBunny == 0,
                "Dust Bunny rewards can be farmed.");
            Require(progression.Data.totalDustBunnies == 1, "Dust Bunny total was not recorded.");

            int milestoneAward = progression.ApplyResult(new LevelResult
            {
                levelId = "MainJourney_25",
                mode = GameMode.MainJourney,
                levelNumber = 25,
                stars = 2
            }, date);
            Require(milestoneAward == 115, "Milestone reward was not applied correctly.");

            Require(progression.RegisterDailyAttempt(date) == 1, "First daily attempt was not recorded.");
            int firstDaily = progression.ApplyResult(new LevelResult
            {
                levelId = "DailyChallenge_20260622",
                mode = GameMode.DailyChallenge,
                stars = 3,
                collectedBonus = true,
                firstAttempt = true
            }, date);
            int duplicateDaily = progression.ApplyResult(new LevelResult
            {
                levelId = "DailyChallenge_20260622",
                mode = GameMode.DailyChallenge,
                stars = 3,
                collectedBonus = true,
                firstAttempt = true
            }, date);
            Require(progression.RegisterDailyAttempt(date.AddDays(1)) == 1, "Next daily attempt did not reset.");
            int nextDaily = progression.ApplyResult(new LevelResult
            {
                levelId = "DailyChallenge_20260623",
                mode = GameMode.DailyChallenge,
                stars = 1,
                usedHint = true,
                firstAttempt = true
            }, date.AddDays(1));
            Require(
                firstDaily == 275 && duplicateDaily == 0 && nextDaily == 190,
                "Daily challenge streak rewards are incorrect.");

            PlayerProgressData malformed = new PlayerProgressData
            {
                mainLevels = null,
                collectedDustBunnyLevelIds = null,
                rewardedCompletionLevelIds = null,
                daily = null,
                cosmetics = null,
                settings = null,
                endlessRunSeed = null,
                dustCoins = -50
            };
            ProgressionSystem repaired = new ProgressionSystem(malformed);
            Require(repaired.Data.mainLevels != null, "Null level records were not repaired.");
            Require(repaired.Data.daily != null && repaired.Data.settings != null, "Null nested save data was not repaired.");
            Require(
                repaired.Data.cosmetics != null &&
                repaired.Data.cosmetics.ownedCosmeticIds.Contains(CosmeticCatalog.DefaultBot),
                "Cosmetic inventory was not repaired.");
            Require(repaired.Data.dustCoins == 0, "Negative currency was not repaired.");
            Require(!string.IsNullOrEmpty(repaired.Data.endlessRunSeed), "Endless run seed was not repaired.");

            EconomySystem economy = new EconomySystem(progression);
            CosmeticSystem cosmetics = new CosmeticSystem(progression, economy);
            progression.Data.dustCoins = 1000;
            Require(cosmetics.TryUnlockOrSelect("path_coral"), "Coin cosmetic could not be unlocked.");
            Require(
                progression.Data.cosmetics.activePathColorId == "path_coral",
                "Unlocked cosmetic could not be selected.");
            Require(
                !cosmetics.TryUnlockOrSelect("bot_gold"),
                "Achievement-locked legendary cosmetic could be purchased early.");
            Require(
                CosmeticCatalog.ForCategory(CosmeticCategory.Bundle).Count >= 6,
                "Cosmetic bundles were not populated.");
            Require(
                CosmeticCatalog.All.Count >= 50,
                "Cosmetic catalog did not expand meaningfully.");
        }

        private static void ConfigureAppIcon()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
            {
                Debug.LogWarning("DustBot app icon is missing at " + AppIconPath);
                return;
            }

            SetIconKind(icon, iOSPlatformIconKind.Application);
            SetIconKind(icon, iOSPlatformIconKind.Settings);
            SetIconKind(icon, iOSPlatformIconKind.Notification);
            SetIconKind(icon, iOSPlatformIconKind.Spotlight);
            SetIconKind(icon, iOSPlatformIconKind.Marketing);
        }

        private static void ConfigureUIMaterial()
        {
            Shader shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("Unity's UI/Default shader could not be found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(UIMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "DustBot UI Material"
                };
                AssetDatabase.CreateAsset(material, UIMaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
            }
        }

        private static void SetIconKind(Texture2D icon, PlatformIconKind kind)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i].SetTexture(icon, 0);
            }

            PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
