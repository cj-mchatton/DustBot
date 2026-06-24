using System;
using System.Collections.Generic;

namespace DustBot
{
    public sealed class LevelLoader
    {
        private readonly LevelManifest manifest = new LevelManifest();
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
