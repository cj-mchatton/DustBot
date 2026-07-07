using System;
using System.Globalization;

namespace DustBot
{
    public sealed class LevelManifest
    {
        // Kept as a compatibility alias for developer tooling. Production play
        // is loaded from CuratedLevelCatalog and contains 255 fixed levels.
        public const int MainJourneyLevelCount = LevelCategoryCatalog.TotalLevelCount;
        public const int CurrentGenerationVersion = 8;

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
