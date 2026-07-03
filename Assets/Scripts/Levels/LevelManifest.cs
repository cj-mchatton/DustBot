using System;
using System.Globalization;

namespace DustBot
{
    public sealed class LevelManifest
    {
        // Kept as a compatibility alias for developer tooling. Production play
        // is category-based and contains 260 total levels.
        public const int MainJourneyLevelCount = LevelCategoryCatalog.TotalLevelCount;
        public const int TutorialLevelCount = 3;
        public const int CurrentGenerationVersion = 8;

        public LevelManifestEntry GetMainEntry(int levelNumber)
        {
            levelNumber = Math.Max(1, Math.Min(MainJourneyLevelCount, levelNumber));
            int remaining = levelNumber;
            foreach (LevelCategory category in LevelCategoryCatalog.All)
            {
                int count = LevelCategoryCatalog.Count(category);
                if (remaining <= count) return GetCategoryEntry(category, remaining);
                remaining -= count;
            }
            return GetCategoryEntry(LevelCategory.Easy, 1);
        }

        public LevelManifestEntry GetCategoryEntry(LevelCategory category, int levelNumber)
        {
            levelNumber = LevelCategoryCatalog.ClampLevel(category, levelNumber);
            DifficultyTier tier = LevelCategoryCatalog.Difficulty(category, levelNumber);
            bool cat = LevelCategoryCatalog.IsCatLevel(category, levelNumber);
            bool maze = category != LevelCategory.Easy && !cat;
            int width;
            int height;
            switch (category)
            {
                case LevelCategory.Easy:
                    width = levelNumber <= 3 ? 4 : 5;
                    height = width;
                    break;
                case LevelCategory.Medium:
                    width = cat ? 7 + levelNumber % 2 : 9 + levelNumber % 3;
                    height = cat ? 7 + (levelNumber / 2) % 2 : 9 + (levelNumber / 3) % 3;
                    break;
                case LevelCategory.Hard:
                    width = cat ? 9 : 12 + levelNumber % 4;
                    height = cat ? 9 : 12 + (levelNumber / 3) % 4;
                    break;
                case LevelCategory.Expert:
                    width = cat ? 10 + levelNumber % 3 : 16 + levelNumber % 5;
                    height = cat ? 10 + (levelNumber / 3) % 3 : 16 + (levelNumber / 4) % 5;
                    break;
                default:
                    width = tier <= DifficultyTier.Medium ? 7 + levelNumber % 2 : 9 + levelNumber % 3;
                    height = tier <= DifficultyTier.Medium ? 7 + (levelNumber / 2) % 2 : 9 + (levelNumber / 3) % 3;
                    break;
            }

            uint hash = DeterministicRandom.StableHash(category + "_Archetype_" + levelNumber);
            LevelManifestEntry entry = CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_{0}_{1:000}_v{2}", category, levelNumber, CurrentGenerationVersion),
                tier,
                width,
                height,
                category == LevelCategory.Easy ? SelectMainArchetype(levelNumber) : SelectChallengeArchetype(hash),
                false);
            entry.category = category;
            entry.useLargeMaze = maze;
            entry.hasCatBehaviorOverride = true;
            entry.catBehaviorOverride = cat ? CatBehavior.Curious : CatBehavior.None;
            entry.useProceduralCatLayout = cat;
            entry.catPuzzleArchetype = cat ? SelectCatPuzzleArchetype(levelNumber + (int)category * 101) : CatPuzzleArchetype.None;
            entry.catStartZone = cat ? SelectCatStartZone(levelNumber + (int)category * 101) : -1;
            entry.mechanicSet = cat ? "CatChaseTurns" : "DrawPath";
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
            bool dailyCat = !largeMaze &&
                            DeterministicRandom.StableHash(entry.seed + "_cat") % 10 < 7;
            entry.hasCatBehaviorOverride = true;
            entry.catBehaviorOverride = dailyCat
                ? CatBehavior.Curious
                : CatBehavior.None;
            if (dailyCat)
            {
                entry.useProceduralCatLayout = true;
                entry.catPuzzleArchetype = SelectCatPuzzleArchetype(entry.levelNumber);
                entry.catStartZone = SelectCatStartZone(entry.levelNumber);
            }
            return entry;
        }

        public LevelManifestEntry GetEndlessEntry(string runSeed, int levelNumber)
        {
            levelNumber = Math.Max(1, levelNumber);
            DifficultyTier tier = levelNumber < 5
                ? DifficultyTier.Easy
                : levelNumber < 15 ? DifficultyTier.Medium : DifficultyTier.Hard;
            LevelManifestEntry entry = CreateEntry(
                levelNumber,
                string.Format(CultureInfo.InvariantCulture, "DustBot_Endless_{0}_{1:0000}", runSeed, levelNumber),
                tier,
                levelNumber < 10 ? 6 : 7,
                levelNumber < 20 ? 7 : 8,
                SelectChallengeArchetype(DeterministicRandom.StableHash(runSeed + "_" + levelNumber)),
                false);
            bool useCat = levelNumber >= 8 &&
                          DeterministicRandom.StableHash(entry.seed + "_cat") % 10 < 7;
            entry.hasCatBehaviorOverride = true;
            entry.catBehaviorOverride = useCat
                ? CatBehavior.Curious
                : CatBehavior.None;
            entry.useProceduralCatLayout = useCat;
            entry.catPuzzleArchetype = useCat
                ? SelectCatPuzzleArchetype(levelNumber)
                : CatPuzzleArchetype.None;
            entry.catStartZone = useCat ? SelectCatStartZone(levelNumber) : -1;
            return entry;
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

        private static bool ShouldUseCampaignCat(int levelNumber)
        {
            if (levelNumber <= 35)
            {
                return levelNumber % 3 == 0 || levelNumber % 7 == 0;
            }

            // Large mazes are selected first. Of the remaining path levels,
            // this stable cadence keeps cats frequent without creating runs.
            return levelNumber % 4 != 0;
        }

        private static CatPuzzleArchetype SelectCatPuzzleArchetype(int levelNumber)
        {
            CatPuzzleArchetype[] choices =
            {
                CatPuzzleArchetype.HorizontalPriorityTrap,
                CatPuzzleArchetype.LoopAroundFurniture,
                CatPuzzleArchetype.CorridorDelay,
                CatPuzzleArchetype.ChokepointTiming,
                CatPuzzleArchetype.SafePocket,
                CatPuzzleArchetype.SplitRoom,
                CatPuzzleArchetype.DockPressure,
                CatPuzzleArchetype.DustBunnyRisk,
                CatPuzzleArchetype.CrumbOrderChase,
                CatPuzzleArchetype.NearCatch,
                CatPuzzleArchetype.CatAtChokepoint,
                CatPuzzleArchetype.LongRouteVsSafeRoute,
                CatPuzzleArchetype.CentralIsland,
                CatPuzzleArchetype.LureAwayFromCrumb,
                CatPuzzleArchetype.LureAwayFromDock,
                CatPuzzleArchetype.BacktrackBait,
                CatPuzzleArchetype.MultiCorridorPursuit,
                CatPuzzleArchetype.FurnitureDelay,
                CatPuzzleArchetype.MultiCrumbRoutePlanning
            };

            // A coprime stride walks every archetype before repeating. Since
            // campaign cat gaps are much shorter than the 19-level cycle,
            // the recent window cannot repeat an archetype.
            int index = Math.Abs((levelNumber - 22) * 7) % choices.Length;
            return choices[index];
        }

        private static int SelectCatStartZone(int levelNumber)
        {
            return Math.Abs((levelNumber - 22) * 3 + (levelNumber / 19)) % 5;
        }
    }
}
