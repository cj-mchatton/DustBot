using System;
using System.Globalization;

namespace DustBot
{
    public sealed class DevelopmentLevelManifest
    {
        private static readonly string[] CatArchetypes =
        {
            "Corridor Trap",
            "Horizontal Priority Puzzle",
            "Loop Around Furniture",
            "Split Room",
            "Chokepoint Timing",
            "Dust Bunny Risk",
            "Crumb Order Chase",
            "Near-Catch Puzzle",
            "Safe Pocket Puzzle",
            "Long Route vs Safe Route",
            "Cat Around the Table",
            "Cat at the Chokepoint",
            "Dock Pressure"
        };

        public LevelManifestEntry GetEntry(GenerationMode mode, int levelNumber)
        {
            int count = LevelGenerationConfig.LevelCount(mode);
            levelNumber = Math.Max(1, Math.Min(count, levelNumber));
            switch (mode)
            {
                case GenerationMode.DevelopmentCampaign:
                    return GetDevelopmentEntry(levelNumber);
                case GenerationMode.CatTesting:
                    return GetCatEntry(levelNumber);
                case GenerationMode.ObstacleTesting:
                    return GetObstacleEntry(levelNumber);
                case GenerationMode.MazeTesting:
                    return GetMazeTestingEntry(levelNumber);
                default:
                    throw new ArgumentException("The selected mode does not use generated test entries.", "mode");
            }
        }

        private static LevelManifestEntry GetDevelopmentEntry(int levelNumber)
        {
            DifficultyTier tier;
            int width;
            int height;
            LevelArchetype archetype;
            string label;

            if (levelNumber <= 6)
            {
                tier = DifficultyTier.Beginner;
                width = 5;
                height = 5;
                archetype = LevelArchetype.SimpleRoute;
                label = levelNumber <= 3 ? "Onboarding" : "Easy Path";
            }
            else if (levelNumber <= 9)
            {
                tier = DifficultyTier.Medium;
                width = levelNumber == 9 ? 12 : 7;
                height = levelNumber == 9 ? 12 : 7;
                archetype = levelNumber == 9 ? LevelArchetype.DustBunnyDetour : LevelArchetype.CrumbOrder;
                label = levelNumber == 9 ? "ADVANCED MAZE • MEDIUM 12×12" : "Medium Route Choices";
            }
            else if (levelNumber <= 12)
            {
                tier = levelNumber <= 11 ? DifficultyTier.Medium : DifficultyTier.Hard;
                width = levelNumber == 10 ? 14 : levelNumber == 11 ? 15 : 18;
                height = width;
                archetype = LevelArchetype.ChallengeRoute;
                label = levelNumber == 10
                    ? "ADVANCED MAZE • DEAD-END BRANCH 14×14"
                    : levelNumber == 11
                        ? "ADVANCED MAZE • LOOP 15×15"
                        : "ADVANCED MAZE • DOCK RETURN 18×18";
            }
            else if (levelNumber <= 15)
            {
                tier = DifficultyTier.Hard;
                width = levelNumber == 13 ? 20 : levelNumber == 14 ? 22 : 24;
                height = width;
                archetype = LevelArchetype.TightPath;
                label = levelNumber == 13
                    ? "ADVANCED MAZE • STICKY SHORTCUT 20×20"
                    : levelNumber == 14
                        ? "ADVANCED MAZE • ONE-WAY COMMITMENT 22×22"
                        : "ADVANCED MAZE • FRAGILE CORRIDOR 24×24";
            }
            else if (levelNumber <= 25)
            {
                tier = levelNumber <= 20 ? DifficultyTier.Medium : DifficultyTier.Hard;
                width = levelNumber <= 20 ? 6 : 7;
                height = levelNumber <= 20 ? 6 : 7;
                archetype = levelNumber % 3 == 0 ? LevelArchetype.BlockerMaze : LevelArchetype.CrumbOrder;
                label = CatArchetypes[(levelNumber - 16) % CatArchetypes.Length];
            }
            else if (levelNumber <= 28)
            {
                tier = DifficultyTier.Expert;
                width = levelNumber == 28 ? 28 : 9;
                height = levelNumber == 28 ? 28 : 9;
                archetype = LevelArchetype.ChallengeRoute;
                label = levelNumber == 28
                    ? "ADVANCED MAZE • EXPERT 28×28"
                    : CatArchetypes[(levelNumber - 16) % CatArchetypes.Length] + " • HARD";
            }
            else if (levelNumber == 29)
            {
                tier = DifficultyTier.Hard;
                width = 32;
                height = 32;
                archetype = LevelArchetype.ChallengeRoute;
                label = "ADVANCED DAILY MAZE • BUNNY DETOUR 32×32";
            }
            else
            {
                tier = DifficultyTier.Master;
                width = 36;
                height = 36;
                archetype = LevelArchetype.ChallengeRoute;
                label = "EXTREME MAZE • MASTER CLEAN 36×36";
            }

            LevelManifestEntry entry = CreateEntry(
                GenerationMode.DevelopmentCampaign,
                levelNumber,
                tier,
                width,
                height,
                archetype,
                label);
            entry.useLargeMaze = levelNumber == 9 ||
                                 (levelNumber >= 10 && levelNumber <= 15) ||
                                 levelNumber >= 28;
            entry.useAdvancedDevMaze = (levelNumber >= 9 && levelNumber <= 15) ||
                                       levelNumber >= 28;
            if (entry.useAdvancedDevMaze)
            {
                entry.devMazeArchetype = levelNumber == 9
                    ? DevMazeArchetype.Chokepoint
                    : levelNumber == 10
                    ? DevMazeArchetype.DeadEndBranch
                    : levelNumber == 11
                        ? DevMazeArchetype.Loop
                        : levelNumber == 12
                            ? DevMazeArchetype.DockReturn
                            : levelNumber == 13
                                ? DevMazeArchetype.StickyShortcut
                                : levelNumber == 14
                                    ? DevMazeArchetype.OneWayCommitment
                                    : levelNumber == 15
                                        ? DevMazeArchetype.FragileCorridor
                                        : levelNumber == 28
                                            ? DevMazeArchetype.MultiRoom
                                            : levelNumber == 29
                                                ? DevMazeArchetype.DustBunnyDetour
                                                : DevMazeArchetype.ExpertLarge;
            }
            if (levelNumber >= 13 && levelNumber <= 15)
            {
                entry.hasRouteModifierOverride = true;
                entry.routeModifierCountOverride = 3;
                entry.routeModifierStyle = levelNumber == 13
                    ? RouteModifierStyle.Sticky
                    : levelNumber == 14 ? RouteModifierStyle.OneWay : RouteModifierStyle.Fragile;
            }

            if (levelNumber >= 16 && levelNumber <= 27)
            {
                entry.hasCatBehaviorOverride = true;
                entry.catBehaviorOverride = CatBehavior.Curious;
                entry.useProceduralCatLayout = levelNumber > 18 && levelNumber % 3 != 0;
            }
            else if (levelNumber <= 15)
            {
                entry.hasCatBehaviorOverride = true;
                entry.catBehaviorOverride = CatBehavior.None;
            }

            if (levelNumber == 29)
            {
                entry.useDailyChallengeProfile = true;
                entry.dailyChallengeStyle = true;
                entry.hasCatBehaviorOverride = true;
                entry.catBehaviorOverride = CatBehavior.None;
            }
            else if (levelNumber == 30)
            {
                entry.masterCleanStyle = true;
                entry.hasCatBehaviorOverride = true;
                entry.catBehaviorOverride = CatBehavior.None;
                entry.useProceduralCatLayout = false;
            }

            return entry;
        }

        private static LevelManifestEntry GetMazeTestingEntry(int levelNumber)
        {
            int[] widths =
            {
                12, 13, 14, 15, 16, 18, 20, 22, 24, 26,
                28, 30, 32, 34, 36, 38, 24, 30, 34, 40
            };
            int[] heights =
            {
                12, 13, 14, 15, 16, 18, 20, 22, 24, 26,
                28, 30, 32, 34, 36, 38, 30, 24, 28, 36
            };
            DevMazeArchetype[] archetypes =
            {
                DevMazeArchetype.DeadEndBranch,
                DevMazeArchetype.Loop,
                DevMazeArchetype.DeadEndBranch,
                DevMazeArchetype.DockReturn,
                DevMazeArchetype.StickyShortcut,
                DevMazeArchetype.Chokepoint,
                DevMazeArchetype.MultiRoom,
                DevMazeArchetype.DustBunnyDetour,
                DevMazeArchetype.OneWayCommitment,
                DevMazeArchetype.FragileCorridor,
                DevMazeArchetype.Loop,
                DevMazeArchetype.ExpertLarge,
                DevMazeArchetype.DeadEndBranch,
                DevMazeArchetype.MultiRoom,
                DevMazeArchetype.DockReturn,
                DevMazeArchetype.ExpertLarge,
                DevMazeArchetype.StickyShortcut,
                DevMazeArchetype.OneWayCommitment,
                DevMazeArchetype.Chokepoint,
                DevMazeArchetype.DustBunnyDetour
            };
            string[] labels =
            {
                "MEDIUM 12x12 • DEAD-END BRANCH",
                "MEDIUM 13x13 • LOOP DIRECTION",
                "HARD 14x14 • DECOY BRANCHES",
                "HARD 15x15 • DOCK RETURN",
                "HARD 16x16 • STICKY SHORTCUT",
                "HARD 18x18 • CHOKEPOINT ORDER",
                "EXPERT 20x20 • MULTI-ROOM",
                "EXPERT 22x22 • DUST BUNNY DETOUR",
                "EXPERT 24x24 • ONE-WAY COMMITMENT",
                "EXPERT 26x26 • FRAGILE CORRIDORS",
                "EXPERT 28x28 • LOOP DIRECTION",
                "EXPERT 30x30 • EXPERT LARGE",
                "EXTREME 32x32 • DEAD-END PRESSURE",
                "EXTREME 34x34 • MULTI-ROOM",
                "EXTREME 36x36 • DOCK RETURN",
                "EXTREME 38x38 • EXPERT LARGE",
                "EXPERT 24x30 • STICKY SHORTCUT",
                "EXPERT 30x24 • ONE-WAY COMMITMENT",
                "EXTREME 34x28 • CHOKEPOINT ORDER",
                "EXTREME 40x36 • DUST BUNNY DETOUR"
            };

            DifficultyTier tier = levelNumber <= 2
                ? DifficultyTier.Medium
                : levelNumber <= 6
                    ? DifficultyTier.Hard
                    : levelNumber <= 11 || levelNumber == 17 || levelNumber == 18
                        ? DifficultyTier.Expert
                        : DifficultyTier.Master;
            LevelManifestEntry entry = CreateEntry(
                GenerationMode.MazeTesting,
                levelNumber,
                tier,
                widths[levelNumber - 1],
                heights[levelNumber - 1],
                LevelArchetype.ChallengeRoute,
                labels[levelNumber - 1]);
            entry.useLargeMaze = true;
            entry.useAdvancedDevMaze = true;
            entry.devMazeArchetype = archetypes[levelNumber - 1];
            entry.hasCatBehaviorOverride = true;
            entry.catBehaviorOverride = CatBehavior.None;
            return entry;
        }

        private static LevelManifestEntry GetCatEntry(int levelNumber)
        {
            DifficultyTier tier = levelNumber <= 4
                ? DifficultyTier.Beginner
                : levelNumber <= 12
                    ? DifficultyTier.Medium
                    : levelNumber <= 20 ? DifficultyTier.Hard : DifficultyTier.Expert;
            int size = levelNumber <= 4 ? 6 : levelNumber <= 12 ? 7 : 8;
            LevelManifestEntry entry = CreateEntry(
                GenerationMode.CatTesting,
                levelNumber,
                tier,
                size,
                levelNumber <= 4 ? 5 : size,
                levelNumber % 4 == 0 ? LevelArchetype.BlockerMaze : LevelArchetype.CrumbOrder,
                CatArchetypes[(levelNumber - 1) % CatArchetypes.Length]);
            entry.hasCatBehaviorOverride = true;
            entry.catBehaviorOverride = CatBehavior.Curious;
            entry.useProceduralCatLayout = levelNumber > 4 && levelNumber % 3 != 0;
            return entry;
        }

        private static LevelManifestEntry GetObstacleEntry(int levelNumber)
        {
            RouteModifierStyle style = levelNumber <= 6
                ? RouteModifierStyle.Sticky
                : levelNumber <= 12
                    ? RouteModifierStyle.OneWay
                    : levelNumber <= 16 ? RouteModifierStyle.Fragile : RouteModifierStyle.Mixed;
            DifficultyTier tier = levelNumber <= 3
                ? DifficultyTier.Easy
                : levelNumber <= 10 ? DifficultyTier.Medium : DifficultyTier.Hard;
            int size = levelNumber <= 3 ? 5 : levelNumber <= 10 ? 6 : 7;
            string label = style == RouteModifierStyle.Sticky
                ? "Sticky Path Cost"
                : style == RouteModifierStyle.OneWay
                    ? "One-Way Direction"
                    : style == RouteModifierStyle.Fragile ? "Fragile No-Backtrack" : "Combined Obstacles";
            LevelManifestEntry entry = CreateEntry(
                GenerationMode.ObstacleTesting,
                levelNumber,
                tier,
                size,
                size,
                levelNumber > 12 ? LevelArchetype.TightPath : LevelArchetype.TrickRoute,
                label);
            entry.hasCatBehaviorOverride = true;
            entry.catBehaviorOverride = CatBehavior.None;
            entry.hasRouteModifierOverride = true;
            entry.routeModifierStyle = style;
            entry.routeModifierCountOverride = levelNumber <= 3 ? 1 : levelNumber <= 10 ? 2 : 3;
            return entry;
        }

        private static LevelManifestEntry CreateEntry(
            GenerationMode mode,
            int levelNumber,
            DifficultyTier tier,
            int width,
            int height,
            LevelArchetype archetype,
            string testArchetype)
        {
            return new LevelManifestEntry
            {
                levelNumber = levelNumber,
                seed = string.Format(
                    CultureInfo.InvariantCulture,
                    "DustBot_{0}_v{1}_{2:00}",
                    mode,
                    LevelManifest.CurrentGenerationVersion,
                    levelNumber),
                generationMode = mode,
                generationVersion = LevelManifest.CurrentGenerationVersion,
                difficultyTier = tier,
                boardWidth = width,
                boardHeight = height,
                archetype = archetype,
                mechanicSet = "DrawPath",
                objectiveSet = "CleanAllAndDock",
                themeId = "CozyHome",
                routeModifierStyle = RouteModifierStyle.Mixed,
                testArchetype = testArchetype
            };
        }
    }
}
