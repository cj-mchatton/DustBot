using System;
using System.Globalization;

namespace DustBot
{
    public sealed class LevelManifest
    {
        public const int MainJourneyLevelCount = 6000;
        public const int TutorialLevelCount = 3;
        public const int CurrentGenerationVersion = 7;

        public LevelManifestEntry GetMainEntry(int levelNumber)
        {
            levelNumber = Math.Max(1, Math.Min(MainJourneyLevelCount, levelNumber));
            DifficultyTier tier;
            int width;
            int height;

            LevelArchetype archetype = SelectMainArchetype(levelNumber);
            bool largeMaze = false;
            if (levelNumber <= TutorialLevelCount)
            {
                tier = DifficultyTier.Tutorial;
                width = levelNumber == TutorialLevelCount ? 5 : 4;
                height = width;
            }
            else if (levelNumber <= 20)
            {
                tier = DifficultyTier.Beginner;
                width = levelNumber % 4 == 0 ? 7 : 6;
                height = levelNumber % 5 == 0 ? 7 : 6;
            }
            else if (levelNumber <= 35)
            {
                tier = DifficultyTier.Easy;
                width = levelNumber % 3 == 0 ? 7 : 6;
                height = levelNumber % 4 == 0 ? 7 : 6;
            }
            else if (levelNumber <= 60)
            {
                tier = DifficultyTier.Medium;
                width = levelNumber % 3 == 0 ? 9 : 8;
                height = levelNumber % 4 == 0 ? 9 : 8;
                largeMaze = archetype == LevelArchetype.BlockerMaze ||
                            archetype == LevelArchetype.TrickRoute;
            }
            else if (levelNumber <= 100)
            {
                tier = DifficultyTier.Medium;
                width = levelNumber % 3 == 0 ? 10 : 9;
                height = levelNumber % 4 == 0 ? 10 : 9;
                largeMaze = archetype == LevelArchetype.BlockerMaze ||
                            archetype == LevelArchetype.TrickRoute ||
                            archetype == LevelArchetype.ChallengeRoute;
            }
            else if (levelNumber <= 250)
            {
                tier = DifficultyTier.Hard;
                width = levelNumber % 3 == 0 ? 13 : 12;
                height = levelNumber % 4 == 0 ? 13 : 12;
                largeMaze = IsLargeMazeArchetype(archetype);
            }
            else if (levelNumber <= 500)
            {
                tier = DifficultyTier.Hard;
                width = levelNumber % 3 == 0 ? 14 : 13;
                height = levelNumber % 4 == 0 ? 14 : 13;
                largeMaze = IsLargeMazeArchetype(archetype);
            }
            else if (levelNumber <= 1000)
            {
                tier = DifficultyTier.Hard;
                width = levelNumber % 3 == 0 ? 15 : 14;
                height = levelNumber % 5 == 0 ? 15 : 14;
                largeMaze = IsLargeMazeArchetype(archetype);
            }
            else if (levelNumber <= 4000)
            {
                tier = DifficultyTier.Expert;
                width = 15 + levelNumber % 4;
                height = 15 + (levelNumber / 3) % 4;
                largeMaze = IsLargeMazeArchetype(archetype);
            }
            else
            {
                tier = DifficultyTier.Master;
                width = 18 + levelNumber % 4;
                height = 18 + (levelNumber / 3) % 4;
                largeMaze = true;
            }

            if (tier >= DifficultyTier.Hard && !largeMaze)
            {
                width = tier >= DifficultyTier.Expert ? 10 : 9;
                height = tier >= DifficultyTier.Expert ? 10 : 9;
            }

            LevelManifestEntry entry = CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_Main_{0:0000}", levelNumber),
                tier,
                width,
                height,
                archetype,
                false);
            entry.useLargeMaze = largeMaze;
            return entry;
        }

        public LevelManifestEntry GetMasterEntry(int levelNumber)
        {
            levelNumber = Math.Max(1, levelNumber);
            int variant = (levelNumber - 1) % 5;
            LevelManifestEntry entry = CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_Master_{0:0000}", levelNumber),
                DifficultyTier.Master,
                18 + variant,
                18 + ((variant + 2) % 5),
                SelectChallengeArchetype(DeterministicRandom.StableHash("Master_" + levelNumber)),
                false);
            entry.useLargeMaze = true;
            return entry;
        }

        public LevelManifestEntry GetDailyEntry(DateTime date)
        {
            string dateKey = date.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            uint hash = DeterministicRandom.StableHash(dateKey);
            bool largeMaze = hash % 5 < 2;
            DifficultyTier tier = largeMaze
                ? (hash % 3 == 0 ? DifficultyTier.Expert : DifficultyTier.Hard)
                : DifficultyTier.Medium;
            LevelManifestEntry entry = CreateEntry(
                int.Parse(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                "DustBot_Daily_" + dateKey,
                tier,
                largeMaze ? 12 + (int)(hash % 5) : 8 + (int)(hash % 3),
                largeMaze ? 12 + (int)((hash >> 3) % 5) : 8 + (int)((hash >> 2) % 3),
                SelectChallengeArchetype(hash),
                true);
            entry.useLargeMaze = largeMaze;
            return entry;
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
                generationMode = GenerationMode.ProductionCampaign,
                generationVersion = CurrentGenerationVersion,
                difficultyTier = tier,
                boardWidth = width,
                boardHeight = height,
                archetype = archetype,
                useDailyChallengeProfile = dailyProfile,
                routeModifierStyle = RouteModifierStyle.Mixed,
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

        private static bool IsLargeMazeArchetype(LevelArchetype archetype)
        {
            return archetype == LevelArchetype.BlockerMaze ||
                   archetype == LevelArchetype.CrumbOrder ||
                   archetype == LevelArchetype.HazardAvoidance ||
                   archetype == LevelArchetype.DustBunnyDetour ||
                   archetype == LevelArchetype.TightPath ||
                   archetype == LevelArchetype.TrickRoute ||
                   archetype == LevelArchetype.ChallengeRoute;
        }
    }
}
