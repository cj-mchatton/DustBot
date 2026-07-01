using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    public sealed class LevelLoader
    {
        private readonly LevelManifest manifest = new LevelManifest();
        private readonly DevelopmentLevelManifest developmentManifest =
            new DevelopmentLevelManifest();
        private readonly LevelGenerator generator = new LevelGenerator();
        private readonly Dictionary<int, LevelDefinition> mainOverrides =
            new Dictionary<int, LevelDefinition>();

        public LevelLoader()
        {
            for (int levelNumber = 1; levelNumber <= LevelManifest.TutorialLevelCount; levelNumber++)
            {
                LevelDefinition tutorial = TutorialLevelCatalog.Get(levelNumber);
                ValidateOrThrow(tutorial);
                mainOverrides[levelNumber] = tutorial;
            }

            LevelDefinition catTutorial = CatTutorialLevelCatalog.GetIntroduction();
            ValidateOrThrow(catTutorial);
            mainOverrides[catTutorial.levelNumber] = catTutorial;
        }

        public LevelDefinition LoadMain(int levelNumber)
        {
            levelNumber = Math.Max(1, Math.Min(LevelManifest.MainJourneyLevelCount, levelNumber));
            LevelDefinition overrideLevel;
            if (mainOverrides.TryGetValue(levelNumber, out overrideLevel))
            {
                return overrideLevel;
            }

            return generator.Generate(manifest.GetMainEntry(levelNumber), GameMode.MainJourney);
        }

        public GenerationMode ActiveGenerationMode
        {
            get { return LevelGenerationConfig.ActiveMode; }
        }

        public int CampaignLevelCount
        {
            get { return LevelGenerationConfig.LevelCount(ActiveGenerationMode); }
        }

        public void SetGenerationMode(GenerationMode mode)
        {
            LevelGenerationConfig.SetActiveMode(mode);
        }

        public LevelDefinition LoadCampaign(int levelNumber)
        {
            return LoadCampaign(ActiveGenerationMode, levelNumber);
        }

        public LevelDefinition LoadCampaign(GenerationMode generationMode, int levelNumber)
        {
            if (generationMode == GenerationMode.ProductionCampaign)
            {
                return LoadMain(levelNumber);
            }

            int count = LevelGenerationConfig.LevelCount(generationMode);
            levelNumber = Math.Max(1, Math.Min(count, levelNumber));
            LevelDefinition level;
            if (generationMode == GenerationMode.DevelopmentCampaign && levelNumber <= 3)
            {
                level = PrepareHandAuthored(
                    TutorialLevelCatalog.Get(levelNumber),
                    generationMode,
                    levelNumber,
                    "Onboarding");
            }
            else if (generationMode == GenerationMode.TutorialTesting)
            {
                level = LoadTutorialTest(levelNumber);
            }
            else
            {
                level = generator.Generate(
                    developmentManifest.GetEntry(generationMode, levelNumber),
                    GameMode.MainJourney);
            }

            ValidateOrThrow(level);
            level.validationResult = "Valid";
            LevelEngagementReport report = LevelEngagementEvaluator.Analyze(level);
            level.engagementScore = report.score;
            level.strategicDepthScore = report.strategicDepthScore;
            level.catPressureScore = report.catPressureScore;
            if (LevelGenerationConfig.DeveloperToolsEnabled && !Application.isBatchMode)
            {
                Debug.Log("DUSTBOT_LEVEL_METADATA\n" + LevelMetadata.Format(level));
            }

            return level;
        }

        public LevelDefinition LoadDaily(DateTime date)
        {
            return generator.Generate(manifest.GetDailyEntry(date.Date), GameMode.DailyChallenge);
        }

        public LevelDefinition LoadMaster(int levelNumber)
        {
            return generator.Generate(manifest.GetMasterEntry(levelNumber), GameMode.MasterClean);
        }

        public LevelDefinition LoadEndless(string runSeed, int levelNumber)
        {
            return generator.Generate(manifest.GetEndlessEntry(runSeed, levelNumber), GameMode.EndlessClean);
        }

        public void RegisterMainOverride(int levelNumber, LevelDefinition definition)
        {
            if (levelNumber < 1 || levelNumber > LevelManifest.MainJourneyLevelCount)
            {
                throw new ArgumentOutOfRangeException("levelNumber");
            }

            if (definition == null || definition.levelNumber != levelNumber)
            {
                throw new ArgumentException("Override metadata must match its manifest level number.", "definition");
            }

            ValidateOrThrow(definition);
            mainOverrides[levelNumber] = definition;
        }

        private static LevelDefinition LoadTutorialTest(int levelNumber)
        {
            if (levelNumber == 5)
            {
                return PrepareHandAuthored(
                    CatTutorialLevelCatalog.GetIntroduction(),
                    GenerationMode.TutorialTesting,
                    levelNumber,
                    "Cat Turn-Based Introduction");
            }

            int[] sourceLevels = { 1, 3, 6, 7, 14, 10, 13 };
            int sourceIndex = levelNumber < 5 ? levelNumber - 1 : levelNumber - 2;
            LevelDefinition level = TutorialLevelCatalog.Get(sourceLevels[sourceIndex]);
            string[] labels =
            {
                "Basic Movement and Docking",
                "Cleaning Crumbs",
                "Drawing Around Furniture",
                "Obstacle Introduction",
                "Dust Bunny Introduction",
                "Stars and Path Targets",
                "Hints Introduction"
            };
            level = PrepareHandAuthored(
                level,
                GenerationMode.TutorialTesting,
                levelNumber,
                labels[sourceIndex]);
            if (levelNumber == 7)
            {
                level.tutorialMessage =
                    "STARS • Follow the clean route, then replay and shorten it to hit the 3-star target.";
            }
            return level;
        }

        private static LevelDefinition PrepareHandAuthored(
            LevelDefinition level,
            GenerationMode generationMode,
            int levelNumber,
            string testArchetype)
        {
            level.id = string.Format("{0}_{1}", generationMode, levelNumber);
            level.mode = GameMode.MainJourney;
            level.generationMode = generationMode;
            level.levelNumber = levelNumber;
            level.seed = string.Format(
                "DustBot_{0}_v{1}_{2:00}_HandAuthored",
                generationMode,
                LevelManifest.CurrentGenerationVersion,
                levelNumber);
            level.generationVersion = LevelManifest.CurrentGenerationVersion;
            level.testArchetype = testArchetype;
            return level;
        }

        private static void ValidateOrThrow(LevelDefinition definition)
        {
            string message;
            if (!LevelValidator.TryValidate(definition, out message))
            {
                throw new InvalidOperationException("Invalid level definition: " + message);
            }
        }
    }
}
