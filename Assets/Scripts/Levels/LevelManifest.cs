using System;
using System.Globalization;

namespace DustBot
{
    public sealed class LevelManifest
    {
        public const int MainJourneyLevelCount = 6000;
        public const int TutorialLevelCount = 15;
        public const int CurrentGenerationVersion = 5;

        public LevelManifestEntry GetMainEntry(int levelNumber)
        {
            levelNumber = Math.Max(1, Math.Min(MainJourneyLevelCount, levelNumber));
            DifficultyTier tier;
            int width;
            int height;

            if (levelNumber <= TutorialLevelCount)
            {
                tier = DifficultyTier.Tutorial;
                width = 4;
                height = 4;
            }
            else if (levelNumber <= 20)
            {
                tier = DifficultyTier.Beginner;
                width = 5;
                height = 5;
            }
            else if (levelNumber <= 35)
            {
                tier = DifficultyTier.Easy;
                width = levelNumber % 3 == 0 ? 6 : 5;
                height = levelNumber % 4 == 0 ? 6 : 5;
            }
            else if (levelNumber <= 60)
            {
                tier = DifficultyTier.Medium;
                width = 6;
                height = 6;
            }
            else if (levelNumber <= 100)
            {
                tier = DifficultyTier.Medium;
                width = levelNumber % 3 == 0 ? 7 : 6;
                height = levelNumber % 4 == 0 ? 7 : 6;
            }
            else if (levelNumber <= 250)
            {
                tier = DifficultyTier.Hard;
                width = levelNumber % 3 == 0 ? 7 : 6;
                height = 7;
            }
            else if (levelNumber <= 500)
            {
                tier = DifficultyTier.Hard;
                width = 7;
                height = levelNumber % 4 == 0 ? 8 : 7;
            }
            else if (levelNumber <= 1000)
            {
                tier = DifficultyTier.Hard;
                width = 7;
                height = 8;
            }
            else if (levelNumber <= 4000)
            {
                tier = DifficultyTier.Expert;
                width = 8;
                height = levelNumber <= 2000 ? 8 : 9;
            }
            else
            {
                tier = DifficultyTier.Master;
                width = 8;
                height = 9;
            }

            return CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_Main_{0:0000}", levelNumber),
                tier,
                width,
                height,
                SelectMainArchetype(levelNumber),
                false);
        }

        public LevelManifestEntry GetMasterEntry(int levelNumber)
        {
            levelNumber = Math.Max(1, levelNumber);
            int variant = (levelNumber - 1) % 3;
            return CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_Master_{0:0000}", levelNumber),
                DifficultyTier.Master,
                variant == 0 ? 7 : 8,
                variant == 2 ? 9 : 8,
                SelectChallengeArchetype(DeterministicRandom.StableHash("Master_" + levelNumber)),
                false);
        }

        public LevelManifestEntry GetDailyEntry(DateTime date)
        {
            string dateKey = date.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            uint hash = DeterministicRandom.StableHash(dateKey);
            DifficultyTier tier = hash % 4 == 0 ? DifficultyTier.Hard : DifficultyTier.Medium;
            return CreateEntry(
                int.Parse(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                "DustBot_Daily_" + dateKey,
                tier,
                7 + (int)(hash % 2),
                7 + (int)((hash >> 2) % 2),
                SelectChallengeArchetype(hash),
                true);
        }

        public LevelManifestEntry GetEndlessEntry(string runSeed, int levelNumber)
        {
            levelNumber = Math.Max(1, levelNumber);
            DifficultyTier tier = levelNumber < 5
                ? DifficultyTier.Easy
                : levelNumber < 15 ? DifficultyTier.Medium : DifficultyTier.Hard;
            return CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_Endless_{0}_{1:0000}", runSeed, levelNumber),
                tier,
                levelNumber < 10 ? 6 : 7,
                levelNumber < 20 ? 7 : 8,
                SelectChallengeArchetype(DeterministicRandom.StableHash(runSeed + "_" + levelNumber)),
                false);
        }

        private static LevelManifestEntry CreateEntry(
            int levelNumber,
            string seed,
            DifficultyTier tier,
            int width,
            int height,
            LevelArchetype archetype,
            bool dailyProfile)
        {
            return new LevelManifestEntry
            {
                levelNumber = levelNumber,
                seed = seed,
                generationVersion = CurrentGenerationVersion,
                difficultyTier = tier,
                boardWidth = width,
                boardHeight = height,
                archetype = archetype,
                useDailyChallengeProfile = dailyProfile,
                mechanicSet = "DrawPath",
                objectiveSet = "CleanAllAndDock",
                themeId = "CozyHome"
            };
        }

        private static LevelArchetype SelectMainArchetype(int levelNumber)
        {
            if (levelNumber <= TutorialLevelCount)
            {
                return LevelArchetype.SimpleRoute;
            }

            int slot = (levelNumber - TutorialLevelCount - 1) % 6;
            uint hash = DeterministicRandom.StableHash("Archetype_" + levelNumber);
            switch (slot)
            {
                case 0:
                    return hash % 2 == 0 ? LevelArchetype.SimpleRoute : LevelArchetype.CrumbOrder;
                case 1:
                    return LevelArchetype.BlockerMaze;
                case 2:
                    return levelNumber >= 26
                        ? LevelArchetype.TrickRoute
                        : LevelArchetype.CrumbOrder;
                case 3:
                    return LevelArchetype.Breather;
                case 4:
                    return levelNumber >= 26
                        ? LevelArchetype.DustBunnyDetour
                        : LevelArchetype.BlockerMaze;
                default:
                    return levelNumber >= 20
                        ? hash % 2 == 0
                            ? LevelArchetype.HazardAvoidance
                            : LevelArchetype.TightPath
                        : LevelArchetype.CrumbOrder;
            }
        }

        private static LevelArchetype SelectChallengeArchetype(uint hash)
        {
            LevelArchetype[] choices =
            {
                LevelArchetype.CrumbOrder,
                LevelArchetype.BlockerMaze,
                LevelArchetype.HazardAvoidance,
                LevelArchetype.DustBunnyDetour,
                LevelArchetype.TightPath,
                LevelArchetype.TrickRoute,
                LevelArchetype.ChallengeRoute
            };
            return choices[(int)(hash % (uint)choices.Length)];
        }
    }
}
