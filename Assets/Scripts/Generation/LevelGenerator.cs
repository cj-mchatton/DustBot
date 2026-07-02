using System;
using System.Collections.Generic;
using System.Globalization;

namespace DustBot
{
    public sealed class LevelGenerator
    {
        private static readonly GridPosition[] CardinalOffsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        public LevelDefinition Generate(LevelManifestEntry entry, GameMode mode)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (entry.generationVersion < 1 || entry.generationVersion > 8)
            {
                throw new NotSupportedException(
                    "Unsupported DustBot generation version " + entry.generationVersion);
            }

            bool advancedDevMaze = entry.useAdvancedDevMaze &&
                                   entry.generationMode != GenerationMode.ProductionCampaign;
            int maximumBoardSize = advancedDevMaze ? 40 : 22;
            if (string.IsNullOrEmpty(entry.seed) ||
                entry.boardWidth < 2 ||
                entry.boardHeight < 2 ||
                entry.boardWidth > maximumBoardSize ||
                entry.boardHeight > maximumBoardSize)
            {
                throw new ArgumentException("Generation metadata contains an invalid seed or board size.", "entry");
            }

            LevelGenerationSettings settings = GetSettings(entry);
            string lastRejection = "No candidate was generated.";
            for (int candidate = 0; candidate < 96; candidate++)
            {
                LevelDefinition level;
                if (advancedDevMaze)
                {
                    level = AdvancedDevMazeGenerator.Build(entry, mode, candidate);
                }
                else if (settings.largeMaze)
                {
                    level = BuildLargeMazeLevel(entry, mode, settings, candidate);
                }
                else
                {
                    List<GridPosition> path = GeneratePath(entry, settings, candidate * 128);
                    level = BuildLevel(entry, mode, settings, path, candidate);
                }
                if (settings.catBehavior != CatBehavior.None &&
                    (level.cat == null || !level.cat.IsEnabled))
                {
                    lastRejection = "The required cat profile could not place a fair, strategically active cat start.";
                    continue;
                }

                string validationMessage;
                if (!LevelValidator.TryValidate(level, out validationMessage))
                {
                    lastRejection = validationMessage;
                    continue;
                }

                LevelEngagementReport report;
                if (advancedDevMaze)
                {
                    AdvancedDevMazeReport advancedReport;
                    string advancedReason;
                    if (!AdvancedDevMazeEvaluator.MeetsRequirements(
                            level,
                            out advancedReport,
                            out advancedReason))
                    {
                        lastRejection = advancedReason;
                        continue;
                    }

                    level.mazeComplexityScore = advancedReport.score;
                }
                else if (settings.largeMaze)
                {
                    LargeMazeComplexityReport mazeReport;
                    string mazeReason;
                    if (!LargeMazeEvaluator.MeetsRequirements(
                            level,
                            settings,
                            out mazeReport,
                            out mazeReason))
                    {
                        lastRejection = mazeReason;
                        continue;
                    }

                    level.mazeComplexityScore = mazeReport.score;
                }
                else if (entry.generationVersion >= 3 &&
                    !LevelEngagementEvaluator.IsAccepted(level, settings, out report))
                {
                    lastRejection = string.Format(
                        CultureInfo.InvariantCulture,
                        "engagement {0}/{1}, strategic {2}/{3}, cat pressure {4}/{5}, turns {6}/{7}, detour {8}/{9}, spread {10}/{11}, decisions {12}/{13}, branches {14}/{15}, chokepoints {16}, obstacle {17}, bunny detour {18}/{19}, nearby {20}, trivial {21}, dense {22}",
                        report.score,
                        settings.minimumEngagementScore,
                        report.strategicDepthScore,
                        settings.minimumStrategicDepthScore,
                        report.catPressureScore,
                        settings.minimumCatPressureScore,
                        report.turns,
                        settings.minimumTurns,
                        report.endpointDetour,
                        settings.minimumDetour,
                        report.crumbSpread,
                        settings.minimumCrumbSpread,
                        report.routeDecisions,
                        settings.minimumRouteDecisions,
                        report.temptingBranches,
                        settings.minimumTemptingBranches,
                        report.chokepoints,
                        report.obstacleDecisionScore,
                        report.bonusDetourCost,
                        settings.minimumBonusDetour,
                        report.nearbyBlockers + report.nearbyHazards,
                        report.tooTrivial,
                        report.tooDense);
                    continue;
                }

                if (entry.generationVersion >= 3)
                {
                    LevelEngagementReport acceptedReport = LevelEngagementEvaluator.Analyze(level);
                    level.engagementScore = acceptedReport.score;
                    level.strategicDepthScore = acceptedReport.strategicDepthScore;
                    level.catPressureScore = acceptedReport.catPressureScore;
                }
                else
                {
                    level.engagementScore = 0;
                    level.strategicDepthScore = 0;
                    level.catPressureScore = 0;
                }
                return level;
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Unable to produce engaging deterministic level {0}: {1}",
                    entry.seed,
                    lastRejection));
        }

        private static LevelGenerationSettings GetSettings(LevelManifestEntry entry)
        {
            int area = entry.boardWidth * entry.boardHeight;
            if (entry.generationVersion <= 2)
            {
                int legacyTier = (int)entry.difficultyTier;
                int legacyMinimum = Math.Min(area - 3, 5 + legacyTier * 2);
                int legacyMaximum = Math.Min(area - 1, legacyMinimum + 4 + legacyTier);
                return new LevelGenerationSettings
                {
                    minimumPathLength = Math.Max(4, legacyMinimum),
                    maximumPathLength = Math.Max(legacyMinimum, legacyMaximum),
                    crumbCount = Math.Max(1, Math.Min(2 + legacyTier / 2, legacyMaximum / 3)),
                    blockerCount = Math.Min(area / 5, 1 + legacyTier),
                    hazardCount = entry.difficultyTier <= DifficultyTier.Beginner
                        ? 1
                        : Math.Min(area / 6, legacyTier),
                    includeBonus = entry.difficultyTier >= DifficultyTier.Medium,
                    minimumTurns = 0,
                    minimumDetour = 0,
                    minimumEngagementScore = 0,
                    moveLimitSlack = Math.Max(2, 5 - legacyTier / 2)
                };
            }

            if (entry.generationVersion == 3)
            {
                return GetVersion3Settings(entry, area);
            }

            LevelGenerationSettings settings;
            int level = entry.levelNumber;
            if (entry.useDailyChallengeProfile)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 8, 24),
                    maximumPathLength = Math.Min(area - 4, 34),
                    crumbCount = 5,
                    blockerCount = 6,
                    hazardCount = 5,
                    includeBonus = true,
                    bonusRequiredForThreeStars = true,
                    minimumTurns = 8,
                    minimumDetour = 5,
                    minimumEngagementScore = 38,
                    minimumStrategicDepthScore = 44,
                    minimumCatPressureScore = 8,
                    moveLimitSlack = 2,
                    hardPathLimit = true,
                    minimumCrumbSpread = 3,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = 2,
                    routeModifierCount = 3
                };
            }
            else if (entry.difficultyTier == DifficultyTier.Master)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 10, 28),
                    maximumPathLength = Math.Min(area - 4, 42),
                    crumbCount = 5,
                    blockerCount = 8,
                    hazardCount = 6,
                    includeBonus = true,
                    bonusRequiredForThreeStars = true,
                    minimumTurns = 9,
                    minimumDetour = 6,
                    minimumEngagementScore = 42,
                    minimumStrategicDepthScore = 48,
                    minimumCatPressureScore = 9,
                    moveLimitSlack = 1,
                    hardPathLimit = true,
                    minimumCrumbSpread = 3,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = 2,
                    routeModifierCount = 3
                };
            }
            else if (level <= 7)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 7,
                    maximumPathLength = Math.Min(area - 3, 11),
                    crumbCount = 2,
                    blockerCount = 2,
                    hazardCount = 0,
                    includeBonus = false,
                    minimumTurns = 2,
                    minimumDetour = 1,
                    minimumEngagementScore = 13,
                    minimumStrategicDepthScore = 12,
                    moveLimitSlack = 4,
                    hardPathLimit = false,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 1,
                    minimumTemptingBranches = 1,
                    routeModifierCount = level >= 6 ? 1 : 0
                };
            }
            else if (level <= 12)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 9,
                    maximumPathLength = Math.Min(area - 3, 15),
                    crumbCount = 2,
                    blockerCount = 4,
                    hazardCount = level >= 10 ? 1 : 0,
                    includeBonus = false,
                    minimumTurns = 4,
                    minimumDetour = 2,
                    minimumEngagementScore = 22,
                    minimumStrategicDepthScore = 20,
                    moveLimitSlack = 3,
                    hardPathLimit = level >= 10,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 2,
                    minimumTemptingBranches = 1,
                    routeModifierCount = 1
                };
            }
            else if (level <= 20)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 12,
                    maximumPathLength = Math.Min(area - 3, 18),
                    crumbCount = level >= 16 ? 3 : 2,
                    blockerCount = 4,
                    hazardCount = 1,
                    includeBonus = level >= 18 && entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 5,
                    minimumDetour = 3,
                    minimumEngagementScore = 28,
                    minimumStrategicDepthScore = 28,
                    moveLimitSlack = 2,
                    hardPathLimit = level >= 15 &&
                                    (entry.archetype == LevelArchetype.TightPath ||
                                     entry.archetype == LevelArchetype.TrickRoute ||
                                     entry.archetype == LevelArchetype.ChallengeRoute),
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 2,
                    minimumTemptingBranches = 1,
                    routeModifierCount = level >= 15 ? 2 : 1
                };
            }
            else if (level <= 35)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 14,
                    maximumPathLength = Math.Min(area - 3, 22),
                    crumbCount = level >= 24 ? 3 : 2,
                    blockerCount = 5,
                    hazardCount = 2,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 6,
                    minimumDetour = 4,
                    minimumEngagementScore = 34,
                    minimumStrategicDepthScore = 34,
                    minimumCatPressureScore = 6,
                    moveLimitSlack = 2,
                    hardPathLimit = entry.archetype == LevelArchetype.TightPath ||
                                    entry.archetype == LevelArchetype.ChallengeRoute ||
                                    entry.archetype == LevelArchetype.TrickRoute ||
                                    level % 5 == 0,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 2,
                    minimumTemptingBranches = 1,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0,
                    routeModifierCount = level >= 26 ? 2 : 1
                };
            }
            else if (level <= 60)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 17,
                    maximumPathLength = Math.Min(area - 4, 26),
                    crumbCount = level % 6 == 0 ? 4 : 3,
                    blockerCount = 6,
                    hazardCount = 3,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour || level % 8 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 6,
                    minimumDetour = 4,
                    minimumEngagementScore = 38,
                    minimumStrategicDepthScore = 40,
                    minimumCatPressureScore = 7,
                    moveLimitSlack = 2,
                    hardPathLimit =
                        entry.archetype == LevelArchetype.TightPath ||
                        entry.archetype == LevelArchetype.ChallengeRoute ||
                        entry.archetype == LevelArchetype.TrickRoute ||
                        level % 6 == 0,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0,
                    routeModifierCount = level % 5 == 0 ? 3 : 2
                };
            }
            else if (level <= 100)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 20,
                    maximumPathLength = Math.Min(area - 5, 31),
                    crumbCount = level % 5 == 0 ? 5 : 4,
                    blockerCount = 7,
                    hazardCount = 4,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour || level % 5 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 7,
                    minimumDetour = 5,
                    minimumEngagementScore = 43,
                    minimumStrategicDepthScore = 46,
                    minimumCatPressureScore = 8,
                    moveLimitSlack = 1,
                    hardPathLimit = entry.archetype != LevelArchetype.Breather && level % 3 != 1,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0,
                    routeModifierCount = level % 4 == 0 ? 4 : 3
                };
            }
            else
            {
                int tier = Math.Max(1, (int)entry.difficultyTier);
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 10, 21 + tier * 2),
                    maximumPathLength = Math.Min(area - 4, 30 + tier * 3),
                    crumbCount = Math.Min(5, 3 + tier / 2),
                    blockerCount = Math.Min(10, 5 + tier),
                    hazardCount = Math.Min(7, 3 + tier / 2),
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour ||
                                   entry.archetype == LevelArchetype.ChallengeRoute ||
                                   level % 4 == 0,
                    bonusRequiredForThreeStars =
                        entry.archetype == LevelArchetype.DustBunnyDetour ||
                        entry.archetype == LevelArchetype.ChallengeRoute,
                    minimumTurns = Math.Min(11, 6 + tier / 2),
                    minimumDetour = Math.Min(9, 4 + tier / 2),
                    minimumEngagementScore = Math.Min(58, 36 + tier * 4),
                    minimumStrategicDepthScore = Math.Min(56, 32 + tier * 4),
                    minimumCatPressureScore = tier >= (int)DifficultyTier.Expert ? 9 : 8,
                    moveLimitSlack = tier >= (int)DifficultyTier.Hard ? 1 : 2,
                    hardPathLimit = entry.archetype != LevelArchetype.Breather && level % 4 != 0,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0,
                    routeModifierCount = Math.Min(5, 2 + tier / 2)
                };
            }

            ApplyArchetype(settings, entry.archetype, area);
            if (entry.useLargeMaze && entry.generationVersion >= 7)
            {
                int tier = Math.Max((int)DifficultyTier.Medium, (int)entry.difficultyTier);
                settings.largeMaze = true;
                settings.minimumPathLength = Math.Max(18, area / 4);
                settings.maximumPathLength = Math.Max(settings.minimumPathLength + 8, area * 3 / 4);
                settings.crumbCount = tier >= (int)DifficultyTier.Master
                    ? 8
                    : tier >= (int)DifficultyTier.Expert ? 7 : tier >= (int)DifficultyTier.Hard ? 6 : 4;
                settings.blockerCount = 0;
                settings.hazardCount = 0;
                settings.includeBonus = true;
                settings.bonusRequiredForThreeStars = true;
                settings.hardPathLimit = tier >= (int)DifficultyTier.Hard;
                settings.moveLimitSlack = tier >= (int)DifficultyTier.Expert ? 2 : 4;
                settings.routeModifierCount = Math.Min(7, Math.Max(2, area / 75));
                settings.routeModifierStyle = RouteModifierStyle.Sticky;
                settings.forcedRouteModifierStyle = true;
                settings.minimumMazeBranches = Math.Max(3, area / 60);
                settings.minimumMazeDeadEnds = Math.Max(3, area / 80);
                settings.minimumMazeLoops = Math.Max(2, area / 150);
                settings.minimumMazeComplexityScore = tier >= (int)DifficultyTier.Master
                    ? 112
                    : tier >= (int)DifficultyTier.Expert ? 100 : tier >= (int)DifficultyTier.Hard ? 88 : 68;
                settings.minimumEngagementScore = 0;
                settings.minimumStrategicDepthScore = 0;
                settings.minimumCatPressureScore = 0;
            }
            if (entry.archetype == LevelArchetype.Breather)
            {
                settings.crumbCount = Math.Max(2, settings.crumbCount);
                settings.minimumCrumbSpread = Math.Min(settings.minimumCrumbSpread, 1);
                settings.minimumRouteDecisions = Math.Min(settings.minimumRouteDecisions, 1);
                settings.minimumTemptingBranches = 0;
                settings.routeModifierCount = Math.Min(settings.routeModifierCount, 1);
                settings.minimumStrategicDepthScore = Math.Min(settings.minimumStrategicDepthScore, 22);
                settings.hardPathLimit = false;
            }
            if (entry.generationVersion >= 5 &&
                !entry.useDailyChallengeProfile &&
                entry.levelNumber == 21)
            {
                settings.minimumPathLength = 9;
                settings.maximumPathLength = Math.Min(area - 5, 14);
                settings.crumbCount = 1;
                settings.blockerCount = 7;
                settings.hazardCount = 0;
                settings.includeBonus = false;
                settings.bonusRequiredForThreeStars = false;
                settings.minimumTurns = 3;
                settings.minimumDetour = 2;
                settings.minimumEngagementScore = 18;
                settings.minimumStrategicDepthScore = 18;
                settings.minimumCatPressureScore = 5;
                settings.hardPathLimit = false;
                settings.minimumCrumbSpread = 1;
                settings.minimumRouteDecisions = 1;
                settings.minimumTemptingBranches = 0;
                settings.routeModifierCount = 0;
            }
            settings.minimumPathLength = Math.Max(5, Math.Min(area - 3, settings.minimumPathLength));
            settings.maximumPathLength = Math.Max(
                settings.minimumPathLength,
                Math.Min(area - 1, settings.maximumPathLength));
            settings.crumbCount = Math.Max(
                1,
                Math.Min(settings.largeMaze ? 9 : 5,
                    Math.Min(settings.crumbCount, Math.Max(1, settings.minimumPathLength / 3))));
            settings.blockerCount = Math.Max(0, Math.Min(area / 4, settings.blockerCount));
            settings.hazardCount = Math.Max(0, Math.Min(area / 5, settings.hazardCount));
            settings.routeModifierCount = Math.Max(
                0,
                Math.Min(Math.Max(0, settings.minimumPathLength / 5), settings.routeModifierCount));
            if (entry.generationVersion >= 5)
            {
                settings.catBehavior = settings.largeMaze
                    ? CatBehavior.None
                    : entry.hasCatBehaviorOverride
                    ? entry.catBehaviorOverride
                    : SelectCatBehavior(entry);
                if (settings.catBehavior != CatBehavior.None)
                {
                    settings.blockerCount = Math.Max(3, settings.blockerCount - 1);
                    settings.hazardCount = Math.Max(0, settings.hazardCount - 1);
                    settings.blockerCount = Math.Min(
                        settings.blockerCount,
                        entry.levelNumber < 36
                            ? 3
                            : area <= 36 ? 4 : 6);
                    settings.hazardCount = Math.Min(
                        settings.hazardCount,
                        entry.levelNumber < 36 ? 1 : 2);
                    settings.routeModifierCount = 0;
                    settings.minimumTemptingBranches = Math.Max(1, settings.minimumTemptingBranches - 1);
                    settings.minimumCatPressureScore = Math.Max(5, settings.minimumCatPressureScore);
                    settings.minimumTurns = Math.Min(settings.minimumTurns, 6);
                    settings.minimumDetour = Math.Min(settings.minimumDetour, 6);
                    settings.minimumCrumbSpread = 1;
                    settings.minimumBonusDetour = 0;
                    if (entry.seed.IndexOf("DustBot_Main_", StringComparison.Ordinal) >= 0)
                    {
                        settings.includeBonus = false;
                        settings.bonusRequiredForThreeStars = false;
                    }
                    if (!entry.useDailyChallengeProfile &&
                        entry.difficultyTier != DifficultyTier.Master &&
                        entry.levelNumber < 36)
                    {
                        settings.minimumPathLength = Math.Min(settings.minimumPathLength, 11);
                        settings.maximumPathLength = Math.Min(settings.maximumPathLength, Math.Max(13, area - 5));
                        settings.crumbCount = Math.Min(settings.crumbCount, 2);
                        settings.blockerCount = Math.Min(settings.blockerCount, 3);
                        settings.hazardCount = Math.Min(settings.hazardCount, 1);
                        settings.hardPathLimit = false;
                        settings.minimumTurns = Math.Min(settings.minimumTurns, 4);
                        settings.minimumDetour = Math.Min(settings.minimumDetour, 2);
                        settings.minimumEngagementScore = Math.Min(settings.minimumEngagementScore, 28);
                        settings.minimumStrategicDepthScore = Math.Min(settings.minimumStrategicDepthScore, 30);
                        settings.minimumRouteDecisions = Math.Min(settings.minimumRouteDecisions, 2);
                        settings.minimumTemptingBranches = Math.Min(settings.minimumTemptingBranches, 1);
                    }
                    else if (!entry.useDailyChallengeProfile &&
                             entry.difficultyTier != DifficultyTier.Master)
                    {
                        int catMinimum = area <= 36 ? 15 : 18;
                        int catMaximum = area <= 36 ? 23 : 28;
                        settings.minimumPathLength = Math.Min(settings.minimumPathLength, catMinimum);
                        settings.maximumPathLength = Math.Min(
                            settings.maximumPathLength,
                            Math.Max(settings.minimumPathLength + 4, catMaximum));
                        settings.crumbCount = Math.Min(settings.crumbCount, area <= 49 ? 2 : 3);
                        settings.minimumTurns = Math.Min(settings.minimumTurns, area <= 36 ? 6 : 7);
                        settings.minimumDetour = Math.Min(settings.minimumDetour, area <= 36 ? 4 : 5);
                        settings.minimumEngagementScore = Math.Min(settings.minimumEngagementScore, area <= 36 ? 36 : 42);
                        settings.minimumStrategicDepthScore = Math.Min(settings.minimumStrategicDepthScore, area <= 36 ? 38 : 44);
                        settings.minimumRouteDecisions = Math.Min(settings.minimumRouteDecisions, 3);
                        settings.minimumTemptingBranches = Math.Min(settings.minimumTemptingBranches, 1);
                    }
                    else
                    {
                        int catMinimum = area <= 49 ? 20 : 24;
                        int catMaximum = area <= 49 ? 30 : 36;
                        settings.minimumPathLength = Math.Min(settings.minimumPathLength, catMinimum);
                        settings.maximumPathLength = Math.Min(
                            settings.maximumPathLength,
                            Math.Max(settings.minimumPathLength + 6, catMaximum));
                        settings.crumbCount = Math.Min(settings.crumbCount, 4);
                        settings.minimumTurns = Math.Min(settings.minimumTurns, 8);
                        settings.minimumDetour = Math.Min(settings.minimumDetour, 5);
                        settings.minimumEngagementScore = Math.Min(settings.minimumEngagementScore, 44);
                        settings.minimumStrategicDepthScore = Math.Min(settings.minimumStrategicDepthScore, 50);
                        settings.minimumRouteDecisions = Math.Min(settings.minimumRouteDecisions, 3);
                        settings.minimumTemptingBranches = Math.Min(settings.minimumTemptingBranches, 2);
                    }
                }
            }
            if (entry.hasRouteModifierOverride)
            {
                settings.routeModifierCount = Math.Max(0, entry.routeModifierCountOverride);
                settings.routeModifierStyle = entry.routeModifierStyle;
                settings.forcedRouteModifierStyle = true;
            }
            settings.catPuzzleArchetype = entry.catPuzzleArchetype;
            settings.catStartZone = entry.catStartZone;
            if (settings.catBehavior != CatBehavior.None && entry.generationVersion >= 8)
            {
                ApplyCatArchetype(settings, entry.catPuzzleArchetype, area);
            }
            return settings;
        }

        private static LevelGenerationSettings GetVersion3Settings(
            LevelManifestEntry entry,
            int area)
        {
            LevelGenerationSettings settings;
            int level = entry.levelNumber;
            if (entry.useDailyChallengeProfile)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 6, 20),
                    maximumPathLength = Math.Min(area - 3, 30),
                    crumbCount = 4 + level % 2,
                    blockerCount = 5,
                    hazardCount = 4,
                    includeBonus = true,
                    bonusRequiredForThreeStars = true,
                    minimumTurns = 6,
                    minimumDetour = 4,
                    minimumEngagementScore = 28,
                    moveLimitSlack = 3
                };
            }
            else if (entry.difficultyTier == DifficultyTier.Master)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 8, 24),
                    maximumPathLength = Math.Min(area - 4, 36),
                    crumbCount = 5,
                    blockerCount = 7,
                    hazardCount = 5,
                    includeBonus = true,
                    bonusRequiredForThreeStars = true,
                    minimumTurns = 7,
                    minimumDetour = 5,
                    minimumEngagementScore = 32,
                    moveLimitSlack = 2
                };
            }
            else if (level <= 25)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 7,
                    maximumPathLength = Math.Min(area - 3, 11),
                    crumbCount = level >= 20 ? 2 : 1,
                    blockerCount = level >= 16 ? 2 : 1,
                    hazardCount = level >= 20 ? 1 : 0,
                    includeBonus = false,
                    minimumTurns = 2,
                    minimumDetour = 0,
                    minimumEngagementScore = 9,
                    moveLimitSlack = 4
                };
            }
            else if (level <= 50)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 9,
                    maximumPathLength = Math.Min(area - 3, 15),
                    crumbCount = level >= 36 ? 3 : 2,
                    blockerCount = 2,
                    hazardCount = 1,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 3,
                    minimumDetour = 2,
                    minimumEngagementScore = 15,
                    moveLimitSlack = 4
                };
            }
            else if (level <= 100)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 13,
                    maximumPathLength = Math.Min(area - 4, 20),
                    crumbCount = level % 10 == 0 ? 4 : 3,
                    blockerCount = 3,
                    hazardCount = 2,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour || level % 9 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 4,
                    minimumDetour = 3,
                    minimumEngagementScore = 21,
                    moveLimitSlack = 3
                };
            }
            else if (level <= 200)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 15,
                    maximumPathLength = Math.Min(area - 5, 24),
                    crumbCount = level % 4 == 0 ? 4 : 3,
                    blockerCount = 4,
                    hazardCount = 2,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour || level % 6 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 4,
                    minimumDetour = 3,
                    minimumEngagementScore = 23,
                    moveLimitSlack = 3
                };
            }
            else
            {
                int tier = Math.Max(1, (int)entry.difficultyTier);
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 8, 16 + tier),
                    maximumPathLength = Math.Min(area - 4, 23 + tier * 2),
                    crumbCount = Math.Min(5, 3 + tier / 2),
                    blockerCount = Math.Min(8, 3 + tier),
                    hazardCount = Math.Min(6, 1 + tier / 2),
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour ||
                                   entry.archetype == LevelArchetype.ChallengeRoute ||
                                   level % 5 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = Math.Min(7, 4 + tier / 2),
                    minimumDetour = Math.Min(6, 3 + tier / 3),
                    minimumEngagementScore = Math.Min(35, 22 + tier * 2),
                    moveLimitSlack = 2
                };
            }

            ApplyArchetype(settings, entry.archetype, area);
            settings.minimumPathLength = Math.Max(5, Math.Min(area - 3, settings.minimumPathLength));
            settings.maximumPathLength = Math.Max(
                settings.minimumPathLength,
                Math.Min(area - 1, settings.maximumPathLength));
            settings.crumbCount = Math.Max(
                1,
                Math.Min(5, Math.Min(settings.crumbCount, Math.Max(1, settings.minimumPathLength / 3))));
            settings.blockerCount = Math.Max(0, Math.Min(area / 4, settings.blockerCount));
            settings.hazardCount = Math.Max(0, Math.Min(area / 5, settings.hazardCount));
            return settings;
        }

        private static LevelDefinition BuildLargeMazeLevel(
            LevelManifestEntry entry,
            GameMode mode,
            LevelGenerationSettings settings,
            int candidate)
        {
            DeterministicRandom random = new DeterministicRandom(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_v8_large_maze_{1}",
                    entry.seed,
                    candidate));
            bool[,] floor = CarveDepthFirstMaze(
                entry.boardWidth,
                entry.boardHeight,
                random);
            int roomCount = Math.Max(1, entry.boardWidth * entry.boardHeight / 120);
            CarveMazeRooms(floor, roomCount, random);
            int loopCount = Math.Max(
                settings.minimumMazeLoops + 2,
                entry.boardWidth * entry.boardHeight / 48);
            CarveMazeLoops(floor, loopCount, random);
            CarvePlayableMazeEdges(
                floor,
                Math.Max(6, (entry.boardWidth + entry.boardHeight) / 5),
                random);

            List<GridPosition> mainRoute = FindMazeDiameter(floor);
            if (mainRoute.Count < Math.Max(
                    8,
                    (entry.boardWidth + entry.boardHeight) * 3 / 4))
            {
                return BuildInvalidLargeMazePlaceholder(entry, mode);
            }

            List<GridPosition> solutionRoute = new List<GridPosition>(mainRoute);
            HashSet<GridPosition> bonusDetourCells = new HashSet<GridPosition>();
            GridPosition bonusPosition = new GridPosition(-1, -1);
            bool hasBonus = settings.includeBonus &&
                            TryCarveBonusDetour(
                                floor,
                                solutionRoute,
                                random,
                                bonusDetourCells,
                                out bonusPosition);

            LevelDefinition level = new LevelDefinition
            {
                id = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}",
                    mode,
                    entry.levelNumber),
                mode = mode,
                generationMode = entry.generationMode,
                levelNumber = entry.levelNumber,
                seed = entry.seed,
                generationVersion = entry.generationVersion,
                difficultyTier = entry.difficultyTier,
                width = entry.boardWidth,
                height = entry.boardHeight,
                hardPathLimit = settings.hardPathLimit,
                archetype = entry.archetype,
                catPuzzleArchetype = entry.catPuzzleArchetype,
                testArchetype = entry.testArchetype,
                dailyChallengeStyle = entry.dailyChallengeStyle,
                masterCleanStyle = entry.masterCleanStyle,
                largeMaze = true,
                themeId = entry.themeId,
                mechanicSet = "LargeMazePath",
                objectiveSet = entry.objectiveSet,
                cat = new CatDefinition { behavior = CatBehavior.None }
            };

            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    if (floor[x, y])
                    {
                        continue;
                    }

                    GridPosition wall = new GridPosition(x, y);
                    CellContent content = random.Chance(1, 12)
                        ? CellContent.Toy
                        : CellContent.Wall;
                    level.cells.Add(new GridCellDefinition(wall, content));
                }
            }

            level.cells.Add(new GridCellDefinition(solutionRoute[0], CellContent.Start));
            level.cells.Add(new GridCellDefinition(
                solutionRoute[solutionRoute.Count - 1],
                CellContent.Dock));
            if (hasBonus)
            {
                level.objectives.collectBonus = true;
                level.objectives.bonusRequiredForThreeStars = true;
                level.bonusPosition = bonusPosition;
            }

            List<int> crumbIndices = PickLargeMazeCrumbIndices(
                solutionRoute,
                settings.crumbCount,
                bonusDetourCells);
            for (int i = 0; i < crumbIndices.Count; i++)
            {
                level.cells.Add(new GridCellDefinition(
                    solutionRoute[crumbIndices[i]],
                    CellContent.Crumb));
            }

            for (int i = 0; i < solutionRoute.Count - 1; i++)
            {
                level.expectedSolution.Add(new SolutionStep(
                    solutionRoute[i],
                    DirectionUtility.Between(solutionRoute[i], solutionRoute[i + 1])));
            }

            PlaceRouteModifiers(level, solutionRoute, settings, candidate);
            RefreshMoveTargets(level, settings, entry.levelNumber);
            level.twoStarMoveTarget = level.parMoves + Math.Max(4, level.parMoves / 10);
            level.threeStarMoveTarget = level.parMoves;
            level.moveLimit = level.hardPathLimit
                ? level.parMoves + Math.Max(settings.moveLimitSlack, level.parMoves / 22)
                : 0;
            return level;
        }

        private static LevelDefinition BuildInvalidLargeMazePlaceholder(
            LevelManifestEntry entry,
            GameMode mode)
        {
            LevelDefinition level = new LevelDefinition
            {
                id = mode + "_" + entry.levelNumber,
                mode = mode,
                generationMode = entry.generationMode,
                levelNumber = entry.levelNumber,
                seed = entry.seed,
                generationVersion = entry.generationVersion,
                difficultyTier = entry.difficultyTier,
                width = entry.boardWidth,
                height = entry.boardHeight,
                largeMaze = true
            };
            level.cells.Add(new GridCellDefinition(new GridPosition(0, 0), CellContent.Start));
            level.cells.Add(new GridCellDefinition(new GridPosition(1, 0), CellContent.Dock));
            level.expectedSolution.Add(new SolutionStep(
                new GridPosition(0, 0),
                Direction.Right));
            level.parMoves = 1;
            level.twoStarMoveTarget = 1;
            level.threeStarMoveTarget = 1;
            return level;
        }

        private static bool[,] CarveDepthFirstMaze(
            int width,
            int height,
            DeterministicRandom random)
        {
            bool[,] floor = new bool[width, height];
            int nodeColumns = Math.Max(1, (width - 1) / 2);
            int nodeRows = Math.Max(1, (height - 1) / 2);
            GridPosition start = new GridPosition(
                1 + random.Range(0, nodeColumns) * 2,
                1 + random.Range(0, nodeRows) * 2);
            start.x = Math.Min(width - 2, start.x);
            start.y = Math.Min(height - 2, start.y);
            bool[,] visited = new bool[width, height];
            List<GridPosition> stack = new List<GridPosition>();
            stack.Add(start);
            floor[start.x, start.y] = true;
            visited[start.x, start.y] = true;
            while (stack.Count > 0)
            {
                GridPosition current = stack[stack.Count - 1];
                List<GridPosition> candidates = new List<GridPosition>();
                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    GridPosition next = new GridPosition(
                        current.x + CardinalOffsets[i].x * 2,
                        current.y + CardinalOffsets[i].y * 2);
                    if (next.x > 0 && next.y > 0 &&
                        next.x < width - 1 && next.y < height - 1 &&
                        !visited[next.x, next.y])
                    {
                        candidates.Add(next);
                    }
                }

                if (candidates.Count == 0)
                {
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }

                random.Shuffle(candidates);
                GridPosition selected = candidates[0];
                GridPosition between = new GridPosition(
                    (current.x + selected.x) / 2,
                    (current.y + selected.y) / 2);
                floor[between.x, between.y] = true;
                floor[selected.x, selected.y] = true;
                visited[selected.x, selected.y] = true;
                stack.Add(selected);
            }

            return floor;
        }

        private static List<GridPosition> FindMazeDiameter(bool[,] floor)
        {
            GridPosition first = new GridPosition(-1, -1);
            for (int y = 0; y < floor.GetLength(1) && first.x < 0; y++)
            {
                for (int x = 0; x < floor.GetLength(0); x++)
                {
                    if (floor[x, y])
                    {
                        first = new GridPosition(x, y);
                        break;
                    }
                }
            }

            GridPosition[,] parents;
            int[,] distances;
            GridPosition endA = FindFarthestMazeCell(
                floor,
                first,
                out parents,
                out distances);
            GridPosition endB = FindFarthestMazeCell(
                floor,
                endA,
                out parents,
                out distances);
            List<GridPosition> reversed = new List<GridPosition>();
            GridPosition current = endB;
            reversed.Add(current);
            while (current != endA)
            {
                GridPosition parent = parents[current.x, current.y];
                if (parent.x < 0)
                {
                    break;
                }

                current = parent;
                reversed.Add(current);
            }

            reversed.Reverse();
            return reversed;
        }

        private static GridPosition FindFarthestMazeCell(
            bool[,] floor,
            GridPosition start,
            out GridPosition[,] parents,
            out int[,] distances)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            parents = new GridPosition[width, height];
            distances = new int[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    parents[x, y] = new GridPosition(-1, -1);
                    distances[x, y] = -1;
                }
            }

            Queue<GridPosition> queue = new Queue<GridPosition>();
            queue.Enqueue(start);
            distances[start.x, start.y] = 0;
            GridPosition farthest = start;
            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();
                if (distances[current.x, current.y] > distances[farthest.x, farthest.y])
                {
                    farthest = current;
                }

                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    GridPosition next = current + CardinalOffsets[i];
                    if (next.x < 0 || next.y < 0 ||
                        next.x >= width || next.y >= height ||
                        !floor[next.x, next.y] ||
                        distances[next.x, next.y] >= 0)
                    {
                        continue;
                    }

                    distances[next.x, next.y] = distances[current.x, current.y] + 1;
                    parents[next.x, next.y] = current;
                    queue.Enqueue(next);
                }
            }

            return farthest;
        }

        private static bool TryCarveBonusDetour(
            bool[,] floor,
            List<GridPosition> route,
            DeterministicRandom random,
            HashSet<GridPosition> detourCells,
            out GridPosition bonusPosition)
        {
            List<int> candidates = new List<int>();
            for (int i = 1; i < route.Count - 2; i++)
            {
                Direction direction = DirectionUtility.Between(route[i], route[i + 1]);
                if (direction != Direction.None)
                {
                    candidates.Add(i);
                }
            }

            random.Shuffle(candidates);
            for (int i = 0; i < candidates.Count; i++)
            {
                int index = candidates[i];
                GridPosition from = route[index];
                GridPosition to = route[index + 1];
                GridPosition[] perpendiculars = DirectionUtility.Between(from, to) == Direction.Up ||
                                                DirectionUtility.Between(from, to) == Direction.Down
                    ? new[] { new GridPosition(1, 0), new GridPosition(-1, 0) }
                    : new[] { new GridPosition(0, 1), new GridPosition(0, -1) };
                if (random.Chance(1, 2))
                {
                    GridPosition swap = perpendiculars[0];
                    perpendiculars[0] = perpendiculars[1];
                    perpendiculars[1] = swap;
                }

                for (int side = 0; side < perpendiculars.Length; side++)
                {
                    GridPosition first = from + perpendiculars[side];
                    GridPosition second = to + perpendiculars[side];
                    if (!IsInteriorMazeCell(floor, first) ||
                        !IsInteriorMazeCell(floor, second) ||
                        floor[first.x, first.y] ||
                        floor[second.x, second.y])
                    {
                        continue;
                    }

                    floor[first.x, first.y] = true;
                    floor[second.x, second.y] = true;
                    route.Insert(index + 1, first);
                    route.Insert(index + 2, second);
                    detourCells.Add(first);
                    detourCells.Add(second);
                    bonusPosition = second;
                    return true;
                }
            }

            bonusPosition = new GridPosition(-1, -1);
            return false;
        }

        private static void CarveMazeRooms(
            bool[,] floor,
            int count,
            DeterministicRandom random)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            for (int room = 0; room < count; room++)
            {
                int roomWidth = Math.Min(width - 2, random.Range(2, 4));
                int roomHeight = Math.Min(height - 2, random.Range(2, 4));
                if (roomWidth < 2 || roomHeight < 2) continue;
                int left = random.Range(1, Math.Max(2, width - roomWidth));
                int bottom = random.Range(1, Math.Max(2, height - roomHeight));
                left = Math.Min(left, width - roomWidth - 1);
                bottom = Math.Min(bottom, height - roomHeight - 1);
                for (int y = bottom; y < bottom + roomHeight; y++)
                {
                    for (int x = left; x < left + roomWidth; x++)
                    {
                        floor[x, y] = true;
                    }
                }
            }
        }

        private static void CarveMazeLoops(
            bool[,] floor,
            int count,
            DeterministicRandom random)
        {
            List<GridPosition> candidates = new List<GridPosition>();
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (floor[x, y]) continue;
                    bool horizontal = floor[x - 1, y] && floor[x + 1, y];
                    bool vertical = floor[x, y - 1] && floor[x, y + 1];
                    if (horizontal || vertical) candidates.Add(new GridPosition(x, y));
                }
            }

            random.Shuffle(candidates);
            int carved = 0;
            for (int i = 0; i < candidates.Count && carved < count; i++)
            {
                GridPosition candidate = candidates[i];
                int nearbyOpen = 0;
                bool wouldEraseDeadEnd = false;
                for (int offset = 0; offset < CardinalOffsets.Length; offset++)
                {
                    GridPosition neighbor = candidate + CardinalOffsets[offset];
                    if (floor[neighbor.x, neighbor.y])
                    {
                        nearbyOpen++;
                        if (CountFloorNeighbors(floor, neighbor) <= 1)
                        {
                            wouldEraseDeadEnd = true;
                        }
                    }
                }

                if (nearbyOpen == 2 && !wouldEraseDeadEnd)
                {
                    floor[candidate.x, candidate.y] = true;
                    carved++;
                }
            }
        }

        private static int CountFloorNeighbors(bool[,] floor, GridPosition position)
        {
            int count = 0;
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                GridPosition neighbor = position + CardinalOffsets[i];
                if (neighbor.x >= 0 && neighbor.y >= 0 &&
                    neighbor.x < floor.GetLength(0) &&
                    neighbor.y < floor.GetLength(1) &&
                    floor[neighbor.x, neighbor.y])
                {
                    count++;
                }
            }

            return count;
        }

        private static List<int> PickLargeMazeCrumbIndices(
            List<GridPosition> route,
            int count,
            HashSet<GridPosition> excluded)
        {
            List<int> chosen = new List<int>();
            int minimumIndexGap = Math.Max(3, route.Count / Math.Max(8, count * 2));
            for (int selection = 0; selection < count; selection++)
            {
                int target = ((selection + 1) * (route.Count - 1)) / (count + 1);
                int best = -1;
                int bestDistance = int.MaxValue;
                for (int index = 2; index < route.Count - 2; index++)
                {
                    if (excluded.Contains(route[index])) continue;
                    bool spaced = true;
                    for (int i = 0; i < chosen.Count; i++)
                    {
                        if (Math.Abs(chosen[i] - index) < minimumIndexGap)
                        {
                            spaced = false;
                            break;
                        }
                    }

                    if (!spaced) continue;
                    int distance = Math.Abs(index - target);
                    if (distance < bestDistance)
                    {
                        best = index;
                        bestDistance = distance;
                    }
                }

                if (best >= 0) chosen.Add(best);
            }

            chosen.Sort();
            return chosen;
        }

        private static bool IsInteriorMazeCell(bool[,] floor, GridPosition position)
        {
            return position.x > 0 && position.y > 0 &&
                   position.x < floor.GetLength(0) - 1 &&
                   position.y < floor.GetLength(1) - 1;
        }

        private static void CarvePlayableMazeEdges(
            bool[,] floor,
            int targetCount,
            DeterministicRandom random)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            List<GridPosition> candidates = new List<GridPosition>();
            for (int x = 1; x < width - 1; x++)
            {
                if (floor[x, 1]) candidates.Add(new GridPosition(x, 0));
                if (floor[x, height - 2]) candidates.Add(new GridPosition(x, height - 1));
            }

            for (int y = 1; y < height - 1; y++)
            {
                if (floor[1, y]) candidates.Add(new GridPosition(0, y));
                if (floor[width - 2, y]) candidates.Add(new GridPosition(width - 1, y));
            }

            random.Shuffle(candidates);
            List<GridPosition> carved = new List<GridPosition>();
            for (int i = 0; i < candidates.Count && carved.Count < targetCount; i++)
            {
                GridPosition candidate = candidates[i];
                bool tooClose = false;
                for (int j = 0; j < carved.Count; j++)
                {
                    if (Manhattan(candidate, carved[j]) <= 1)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    floor[candidate.x, candidate.y] = true;
                    carved.Add(candidate);
                }
            }
        }

        private static List<GridPosition> GeneratePath(
            LevelManifestEntry entry,
            LevelGenerationSettings settings,
            int attemptOffset)
        {
            for (int attempt = 0; attempt < 128; attempt++)
            {
                int canonicalAttempt = attemptOffset + attempt;
                DeterministicRandom random = new DeterministicRandom(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}_v{1}_path_{2}",
                        entry.seed,
                        SeedVersion(entry.generationVersion),
                        canonicalAttempt));
                int targetLength = random.Range(settings.minimumPathLength, settings.maximumPathLength + 1);
                GridPosition start = RandomEdgePosition(random, entry.boardWidth, entry.boardHeight);
                List<GridPosition> path = new List<GridPosition>(targetLength) { start };
                HashSet<GridPosition> visited = new HashSet<GridPosition> { start };

                if (GrowPath(path, visited, targetLength, entry.boardWidth, entry.boardHeight, random))
                {
                    GridPosition end = path[path.Count - 1];
                    bool validEndpoints = !AreAdjacent(start, end) || path.Count <= 5;
                    bool qualityAccepted =
                        entry.generationVersion == 1 ||
                        IsInterestingPath(path, settings);
                    if (validEndpoints && qualityAccepted)
                    {
                        return path;
                    }
                }
            }

            throw new InvalidOperationException("Unable to produce deterministic solution path for " + entry.seed);
        }

        private static bool GrowPath(
            List<GridPosition> path,
            HashSet<GridPosition> visited,
            int targetLength,
            int width,
            int height,
            DeterministicRandom random)
        {
            if (path.Count >= targetLength)
            {
                return true;
            }

            List<GridPosition> offsets = new List<GridPosition>(CardinalOffsets);
            random.Shuffle(offsets);
            GridPosition current = path[path.Count - 1];

            for (int i = 0; i < offsets.Count; i++)
            {
                GridPosition next = current + offsets[i];
                if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);
                path.Add(next);
                if (GrowPath(path, visited, targetLength, width, height, random))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
                visited.Remove(next);
            }

            return false;
        }

        private static LevelDefinition BuildLevel(
            LevelManifestEntry entry,
            GameMode mode,
            LevelGenerationSettings settings,
            List<GridPosition> path,
            int decorationSalt)
        {
            if (settings.catBehavior != CatBehavior.None && !entry.useProceduralCatLayout)
            {
                LevelDefinition catTemplate =
                    BuildCatTemplateLevel(entry, mode, settings, decorationSalt);
                if (catTemplate != null)
                {
                    return catTemplate;
                }
            }

            LevelDefinition level = new LevelDefinition
            {
                id = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}",
                    mode,
                    entry.levelNumber),
                mode = mode,
                generationMode = entry.generationMode,
                levelNumber = entry.levelNumber,
                seed = entry.seed,
                generationVersion = entry.generationVersion,
                difficultyTier = entry.difficultyTier,
                width = entry.boardWidth,
                height = entry.boardHeight,
                parMoves = path.Count - 1,
                moveLimit = settings.hardPathLimit
                    ? path.Count - 1 + settings.moveLimitSlack
                    : 0,
                twoStarMoveTarget = path.Count - 1 +
                                    (entry.levelNumber <= 20 ? 3 :
                                     entry.levelNumber <= 60 ? 2 : 1),
                threeStarMoveTarget = path.Count - 1,
                hardPathLimit = settings.hardPathLimit,
                archetype = entry.archetype,
                catPuzzleArchetype = entry.catPuzzleArchetype,
                testArchetype = entry.testArchetype,
                dailyChallengeStyle = entry.dailyChallengeStyle,
                masterCleanStyle = entry.masterCleanStyle,
                themeId = entry.themeId,
                mechanicSet = entry.mechanicSet,
                objectiveSet = entry.objectiveSet
            };

            if (settings.catBehavior != CatBehavior.None)
            {
                level.catPuzzleArchetype = settings.catPuzzleArchetype;
                level.catFreeParMoves = path.Count - 1;
                level.mechanicSet = "CatChaseTurns";
                if (mode == GameMode.MainJourney && entry.levelNumber == 21)
                {
                    level.tutorialMessage =
                        "CAT CHASE • Swipe one tile. Then the cat moves twice, horizontal first. Use furniture, clean, and dock.";
                }
            }

            level.cells.Add(new GridCellDefinition(path[0], CellContent.Start));
            level.cells.Add(new GridCellDefinition(path[path.Count - 1], CellContent.Dock));

            int excludedStart = -1;
            int excludedEnd = -1;
            int bonusIndex = -1;
            if (settings.includeBonus && path.Count > 6)
            {
                if (TryFindBonusDetour(path, out excludedStart, out excludedEnd, out bonusIndex))
                {
                    level.objectives.collectBonus = true;
                    level.bonusPosition = path[bonusIndex];
                    level.objectives.bonusRequiredForThreeStars = true;
                }
                else
                {
                    bonusIndex = Math.Max(2, path.Count - 3);
                    level.objectives.collectBonus = true;
                    level.bonusPosition = path[bonusIndex];
                    level.objectives.bonusRequiredForThreeStars = settings.bonusRequiredForThreeStars;
                }
            }

            List<int> crumbIndices = PickEvenlySpacedIndices(
                path,
                settings.crumbCount,
                excludedStart,
                excludedEnd);
            for (int i = 0; i < crumbIndices.Count; i++)
            {
                level.cells.Add(new GridCellDefinition(path[crumbIndices[i]], CellContent.Crumb));
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                level.expectedSolution.Add(
                    new SolutionStep(path[i], DirectionUtility.Between(path[i], path[i + 1])));
            }

            PlaceRouteModifiers(level, path, settings, decorationSalt);
            RefreshMoveTargets(level, settings, entry.levelNumber);

            if (entry.generationVersion == 1)
            {
                PlaceOffPathContentV1(level, path, settings, decorationSalt);
            }
            else
            {
                PlaceOffPathContentV2(level, path, settings, decorationSalt);
            }

            if (settings.catBehavior != CatBehavior.None)
            {
                if (!TryPlaceCat(level, path, settings, decorationSalt))
                {
                    RelaxCatArena(level, path, decorationSalt);
                    TryPlaceCat(level, path, settings, decorationSalt + 101);
                }
            }

            return level;
        }

        private static LevelDefinition BuildCatTemplateLevel(
            LevelManifestEntry entry,
            GameMode mode,
            LevelGenerationSettings settings,
            int decorationSalt)
        {
            const int templateWidth = 6;
            const int templateHeight = 5;
            int variant = (int)(DeterministicRandom.StableHash(
                entry.seed + "_cat_template_" + decorationSalt) % 4);
            LevelDefinition level = new LevelDefinition
            {
                id = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}",
                    mode,
                    entry.levelNumber),
                mode = mode,
                generationMode = entry.generationMode,
                levelNumber = entry.levelNumber,
                seed = entry.seed,
                generationVersion = entry.generationVersion,
                difficultyTier = entry.difficultyTier,
                width = templateWidth,
                height = templateHeight,
                hardPathLimit = settings.hardPathLimit,
                archetype = entry.archetype,
                testArchetype = entry.testArchetype,
                dailyChallengeStyle = entry.dailyChallengeStyle,
                masterCleanStyle = entry.masterCleanStyle,
                themeId = entry.themeId,
                mechanicSet = "CatChaseTurns",
                objectiveSet = entry.objectiveSet,
                cat = new CatDefinition
                {
                    behavior = settings.catBehavior,
                    startPosition = TransformCatTemplatePosition(
                        new GridPosition(5, 0),
                        variant),
                    horizontalFirst = true
                }
            };

            level.cells.Add(new GridCellDefinition(
                TransformCatTemplatePosition(new GridPosition(2, 3), variant),
                CellContent.Start));
            level.cells.Add(new GridCellDefinition(
                TransformCatTemplatePosition(new GridPosition(5, 1), variant),
                CellContent.Dock));
            level.cells.Add(new GridCellDefinition(
                TransformCatTemplatePosition(new GridPosition(4, 1), variant),
                CellContent.Crumb));

            GridPosition[] walls =
            {
                new GridPosition(0, 0),
                new GridPosition(1, 3),
                new GridPosition(2, 0),
                new GridPosition(2, 2),
                new GridPosition(3, 0),
                new GridPosition(5, 3),
                new GridPosition(5, 4)
            };
            for (int i = 0; i < walls.Length; i++)
            {
                level.cells.Add(new GridCellDefinition(
                    TransformCatTemplatePosition(walls[i], variant),
                    CellContent.Wall));
            }

            List<SolutionStep> solution;
            int searchLimit = templateWidth * templateHeight * 4;
            if (!CatLevelSolver.TrySolve(level, searchLimit, out solution))
            {
                return null;
            }

            level.expectedSolution.AddRange(solution);
            if (entry.useDailyChallengeProfile)
            {
                List<GridPosition> route = CatLevelSolver.BuildRoute(level, solution);
                if (route.Count > 2)
                {
                    level.objectives.collectBonus = true;
                    level.objectives.bonusRequiredForThreeStars = true;
                    level.bonusPosition = route[Math.Max(1, route.Count / 2)];
                }
            }

            RefreshMoveTargets(level, settings, entry.levelNumber);
            level.moveLimit = level.hardPathLimit
                ? level.parMoves + Math.Max(2, settings.moveLimitSlack)
                : 0;
            level.twoStarMoveTarget = level.parMoves + 3;
            level.threeStarMoveTarget = level.parMoves;

            if (entry.levelNumber == 21 && mode == GameMode.MainJourney)
            {
                level.tutorialMessage =
                    "CAT CHASE • Swipe one tile. Then the cat moves twice, horizontal first. Use furniture, clean, and dock.";
            }

            return level;
        }

        private static GridPosition TransformCatTemplatePosition(
            GridPosition position,
            int variant)
        {
            int x = position.x;
            int y = position.y;
            if ((variant & 1) != 0)
            {
                x = 5 - x;
            }

            if ((variant & 2) != 0)
            {
                y = 4 - y;
            }

            return new GridPosition(x, y);
        }

        private static void RelaxCatArena(
            LevelDefinition level,
            List<GridPosition> path,
            int decorationSalt)
        {
            HashSet<GridPosition> route = new HashSet<GridPosition>(path);
            for (int i = level.cells.Count - 1; i >= 0; i--)
            {
                GridCellDefinition cell = level.cells[i];
                if (cell == null || route.Contains(cell.position))
                {
                    continue;
                }

                if (cell.content == CellContent.Sock ||
                    cell.content == CellContent.Cord ||
                    cell.content == CellContent.WetSpot)
                {
                    level.cells.RemoveAt(i);
                    continue;
                }
            }
        }

        private static void PlaceOffPathContentV1(
            LevelDefinition level,
            List<GridPosition> path,
            LevelGenerationSettings settings,
            int decorationSalt)
        {
            HashSet<GridPosition> occupied = new HashSet<GridPosition>(path);
            List<GridPosition> available = new List<GridPosition>();
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (!occupied.Contains(position))
                    {
                        available.Add(position);
                    }
                }
            }

            DeterministicRandom random = new DeterministicRandom(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_v{1}_decor",
                    level.seed,
                    SeedVersion(level.generationVersion) + "_" + decorationSalt));
            random.Shuffle(available);
            int cursor = 0;

            for (int i = 0; i < settings.blockerCount && cursor < available.Count; i++, cursor++)
            {
                level.cells.Add(new GridCellDefinition(
                    available[cursor],
                    random.Chance(1, 4) ? CellContent.Toy : CellContent.Wall));
            }

            CellContent[] hazards = { CellContent.Sock, CellContent.Cord, CellContent.WetSpot };
            for (int i = 0; i < settings.hazardCount && cursor < available.Count; i++, cursor++)
            {
                level.cells.Add(new GridCellDefinition(available[cursor], hazards[random.Range(0, hazards.Length)]));
            }
        }

        private static void PlaceOffPathContentV2(
            LevelDefinition level,
            List<GridPosition> path,
            LevelGenerationSettings settings,
            int decorationSalt)
        {
            HashSet<GridPosition> route = new HashSet<GridPosition>(path);
            List<GridPosition> available = new List<GridPosition>();
            List<GridPosition> hazardCandidates = new List<GridPosition>();
            List<GridPosition> blockerCandidates = new List<GridPosition>();

            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (route.Contains(position))
                    {
                        continue;
                    }

                    available.Add(position);
                    if (IsAdjacentToRoute(position, route))
                    {
                        hazardCandidates.Add(position);
                        blockerCandidates.Add(position);
                    }
                }
            }

            DeterministicRandom random = new DeterministicRandom(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_v{1}_decor",
                    level.seed,
                    SeedVersion(level.generationVersion) + "_" + decorationSalt));
            random.Shuffle(available);
            random.Shuffle(hazardCandidates);
            random.Shuffle(blockerCandidates);
            HashSet<GridPosition> used = new HashSet<GridPosition>();
            CellContent[] hazards = { CellContent.Sock, CellContent.Cord, CellContent.WetSpot };

            int placedHazards = 0;
            for (int i = 0;
                 i < hazardCandidates.Count && placedHazards < settings.hazardCount;
                 i++)
            {
                GridPosition position = hazardCandidates[i];
                used.Add(position);
                level.cells.Add(new GridCellDefinition(
                    position,
                    hazards[random.Range(0, hazards.Length)]));
                placedHazards++;
            }

            for (int i = 0; i < available.Count && placedHazards < settings.hazardCount; i++)
            {
                GridPosition position = available[i];
                if (!used.Add(position))
                {
                    continue;
                }

                level.cells.Add(new GridCellDefinition(
                    position,
                    hazards[random.Range(0, hazards.Length)]));
                placedHazards++;
            }

            int placedBlockers = 0;
            for (int i = 0; i < blockerCandidates.Count && placedBlockers < settings.blockerCount; i++)
            {
                GridPosition position = blockerCandidates[i];
                if (!used.Add(position))
                {
                    continue;
                }

                level.cells.Add(new GridCellDefinition(
                    position,
                    random.Chance(1, 4) ? CellContent.Toy : CellContent.Wall));
                placedBlockers++;
            }

            for (int i = 0; i < available.Count && placedBlockers < settings.blockerCount; i++)
            {
                GridPosition position = available[i];
                if (!used.Add(position))
                {
                    continue;
                }

                level.cells.Add(new GridCellDefinition(
                    position,
                    random.Chance(1, 4) ? CellContent.Toy : CellContent.Wall));
                placedBlockers++;
            }
        }

        private static void PlaceRouteModifiers(
            LevelDefinition level,
            List<GridPosition> path,
            LevelGenerationSettings settings,
            int decorationSalt)
        {
            if (settings.routeModifierCount <= 0 ||
                path.Count < 6)
            {
                return;
            }

            HashSet<GridPosition> routeSet = new HashSet<GridPosition>(path);
            List<int> candidates = new List<int>();
            for (int i = 1; i < path.Count - 1; i++)
            {
                if (!HasSerializedCell(level, path[i]) &&
                    path[i] != level.bonusPosition)
                {
                    candidates.Add(i);
                }
            }

            DeterministicRandom random = new DeterministicRandom(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_v{1}_route_mod_{2}",
                    level.seed,
                    SeedVersion(level.generationVersion),
                    decorationSalt));
            Dictionary<int, int> candidateScores = new Dictionary<int, int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                int pathIndex = candidates[i];
                candidateScores[pathIndex] =
                    ScoreRouteModifierCandidate(level, path, routeSet, pathIndex) +
                    random.Range(0, 5);
            }

            candidates.Sort(delegate(int left, int right)
            {
                int scoreCompare = candidateScores[right].CompareTo(candidateScores[left]);
                return scoreCompare != 0 ? scoreCompare : left.CompareTo(right);
            });

            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < settings.routeModifierCount; i++)
            {
                int pathIndex = candidates[i];
                Direction incoming = DirectionUtility.Between(path[pathIndex - 1], path[pathIndex]);
                Direction outgoing = DirectionUtility.Between(path[pathIndex], path[pathIndex + 1]);
                bool straight = incoming != Direction.None && incoming == outgoing;
                bool branchy = HasOffRouteWalkableNeighbor(level, path[pathIndex], routeSet);
                bool chokepoint = CountOpenNeighbors(level, path[pathIndex]) <= 2;
                CellContent content;
                if (settings.forcedRouteModifierStyle &&
                    settings.routeModifierStyle == RouteModifierStyle.Mixed)
                {
                    int forcedSlot = placed % 3;
                    content = forcedSlot == 0
                        ? CellContent.Sticky
                        : forcedSlot == 1 ? OneWayFor(incoming) : CellContent.Fragile;
                }
                else if (settings.routeModifierStyle == RouteModifierStyle.Sticky)
                {
                    content = CellContent.Sticky;
                }
                else if (settings.routeModifierStyle == RouteModifierStyle.OneWay)
                {
                    content = OneWayFor(incoming);
                }
                else if (settings.routeModifierStyle == RouteModifierStyle.Fragile)
                {
                    content = CellContent.Fragile;
                }
                else
                {
                    int roll = random.Range(0, 100);
                    if (level.levelNumber >= 8 &&
                        straight &&
                        (branchy || chokepoint || level.hardPathLimit) &&
                        roll < 50)
                    {
                        content = OneWayFor(incoming);
                    }
                    else if (level.levelNumber >= 21 &&
                             (chokepoint || !straight || level.cat.IsEnabled) &&
                             roll >= 72)
                    {
                        content = CellContent.Fragile;
                    }
                    else
                    {
                        content = CellContent.Sticky;
                    }
                }

                level.cells.Add(new GridCellDefinition(path[pathIndex], content));
                placed++;
            }
        }

        private static int ScoreRouteModifierCandidate(
            LevelDefinition level,
            List<GridPosition> path,
            HashSet<GridPosition> routeSet,
            int pathIndex)
        {
            GridPosition position = path[pathIndex];
            Direction incoming = DirectionUtility.Between(path[pathIndex - 1], position);
            Direction outgoing = DirectionUtility.Between(position, path[pathIndex + 1]);
            bool straight = incoming != Direction.None && incoming == outgoing;
            int score = 0;
            if (HasOffRouteWalkableNeighbor(level, position, routeSet))
            {
                score += 9;
            }

            int openNeighbors = CountOpenNeighbors(level, position);
            if (openNeighbors <= 2)
            {
                score += 8;
            }

            if (straight)
            {
                score += 4;
            }

            if (level.hardPathLimit)
            {
                score += 5;
            }

            if (level.objectives.collectBonus &&
                Manhattan(position, level.bonusPosition) <= 2)
            {
                score += 7;
            }

            if (DistanceToNearestContent(level, position, CellContent.Crumb) <= 2)
            {
                score += 6;
            }

            GridPosition dock = level.Find(CellContent.Dock);
            if (Manhattan(position, dock) <= 3 && pathIndex > path.Count / 2)
            {
                score += 5;
            }

            int middleDistance = Math.Abs(pathIndex - path.Count / 2);
            score += Math.Max(0, 6 - middleDistance);
            return score;
        }

        private static bool HasOffRouteWalkableNeighbor(
            LevelDefinition level,
            GridPosition position,
            HashSet<GridPosition> routeSet)
        {
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                GridPosition neighbor = position + CardinalOffsets[i];
                if (level.IsInside(neighbor) &&
                    !routeSet.Contains(neighbor) &&
                    level.GetContent(neighbor) == CellContent.Empty)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountOpenNeighbors(LevelDefinition level, GridPosition position)
        {
            int count = 0;
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                GridPosition neighbor = position + CardinalOffsets[i];
                if (level.IsInside(neighbor) &&
                    CellContentUtility.IsWalkableFloor(level.GetContent(neighbor)))
                {
                    count++;
                }
            }

            return count;
        }

        private static int DistanceToNearestContent(
            LevelDefinition level,
            GridPosition position,
            CellContent content)
        {
            int distance = int.MaxValue;
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == content)
                {
                    distance = Math.Min(distance, Manhattan(position, level.cells[i].position));
                }
            }

            return distance == int.MaxValue ? 99 : distance;
        }

        private static bool HasSerializedCell(LevelDefinition level, GridPosition position)
        {
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].position == position)
                {
                    return true;
                }
            }

            return false;
        }

        private static CellContent OneWayFor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return CellContent.OneWayUp;
                case Direction.Right: return CellContent.OneWayRight;
                case Direction.Down: return CellContent.OneWayDown;
                case Direction.Left: return CellContent.OneWayLeft;
                default: return CellContent.Sticky;
            }
        }

        private static void RefreshMoveTargets(
            LevelDefinition level,
            LevelGenerationSettings settings,
            int levelNumber)
        {
            int moveCost = CalculateSolutionMoveCost(level, level.expectedSolution);
            level.parMoves = moveCost;
            level.threeStarMoveTarget = moveCost;
            level.twoStarMoveTarget = moveCost +
                                      (levelNumber <= 20 ? 3 :
                                       levelNumber <= 60 ? 2 : 1);
            level.moveLimit = level.hardPathLimit
                ? moveCost + settings.moveLimitSlack
                : 0;
        }

        private static int CalculateSolutionMoveCost(
            LevelDefinition level,
            IList<SolutionStep> solution)
        {
            int cost = 0;
            GridPosition current = level.Find(CellContent.Start);
            for (int i = 0; i < solution.Count; i++)
            {
                current += DirectionUtility.ToOffset(solution[i].direction);
                cost += CellContentUtility.MoveCost(level.GetContent(current));
            }

            return cost;
        }

        private static List<int> PickEvenlySpacedIndices(
            List<GridPosition> path,
            int count,
            int excludedStart,
            int excludedEnd)
        {
            List<int> candidates = new List<int>();
            for (int i = 1; i < path.Count - 1; i++)
            {
                if (excludedStart >= 0 && i > excludedStart && i < excludedEnd)
                {
                    continue;
                }

                candidates.Add(i);
            }

            List<int> indices = new List<int>();
            count = Math.Min(count, candidates.Count);
            for (int selection = 0; selection < count; selection++)
            {
                int targetIndex = ((selection + 1) * (path.Count - 1)) / (count + 1);
                int bestCandidate = -1;
                int bestScore = int.MinValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    int candidate = candidates[i];
                    if (indices.Contains(candidate))
                    {
                        continue;
                    }

                    int minimumSpatialDistance = int.MaxValue;
                    int minimumPathDistance = int.MaxValue;
                    for (int j = 0; j < indices.Count; j++)
                    {
                        minimumSpatialDistance = Math.Min(
                            minimumSpatialDistance,
                            Math.Abs(path[candidate].x - path[indices[j]].x) +
                            Math.Abs(path[candidate].y - path[indices[j]].y));
                        minimumPathDistance = Math.Min(
                            minimumPathDistance,
                            Math.Abs(candidate - indices[j]));
                    }

                    if (indices.Count == 0)
                    {
                        minimumSpatialDistance = 3;
                        minimumPathDistance = path.Count;
                    }

                    int score =
                        minimumSpatialDistance * 100 +
                        Math.Min(20, minimumPathDistance) * 5 -
                        Math.Abs(candidate - targetIndex);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate >= 0)
                {
                    indices.Add(bestCandidate);
                }
            }

            indices.Sort();
            return indices;
        }

        private static bool TryFindBonusDetour(
            List<GridPosition> path,
            out int detourStart,
            out int detourEnd,
            out int bonusIndex)
        {
            int bestCost = 0;
            detourStart = -1;
            detourEnd = -1;
            bonusIndex = -1;
            for (int start = 1; start < path.Count - 4; start++)
            {
                for (int end = start + 3; end < path.Count - 1; end++)
                {
                    if (!AreAdjacent(path[start], path[end]))
                    {
                        continue;
                    }

                    int cost = end - start - 1;
                    if (cost > bestCost)
                    {
                        bestCost = cost;
                        detourStart = start;
                        detourEnd = end;
                        bonusIndex = start + Math.Max(1, (end - start) / 2);
                    }
                }
            }

            return bestCost >= 2;
        }

        private static GridPosition RandomEdgePosition(DeterministicRandom random, int width, int height)
        {
            int edge = random.Range(0, 4);
            switch (edge)
            {
                case 0: return new GridPosition(random.Range(0, width), 0);
                case 1: return new GridPosition(width - 1, random.Range(0, height));
                case 2: return new GridPosition(random.Range(0, width), height - 1);
                default: return new GridPosition(0, random.Range(0, height));
            }
        }

        private static bool AreAdjacent(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y) == 1;
        }

        private static int SeedVersion(int generationVersion)
        {
            return generationVersion >= 6
                ? 6
                : Math.Min(generationVersion, 4);
        }

        private static bool IsAdjacentToRoute(
            GridPosition position,
            HashSet<GridPosition> route)
        {
            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                if (route.Contains(position + CardinalOffsets[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInterestingPath(
            List<GridPosition> path,
            LevelGenerationSettings settings)
        {
            int turns = 0;
            Direction previous = Direction.None;
            for (int i = 0; i < path.Count - 1; i++)
            {
                Direction current = DirectionUtility.Between(path[i], path[i + 1]);
                if (previous != Direction.None && current != previous)
                {
                    turns++;
                }

                previous = current;
            }

            GridPosition start = path[0];
            GridPosition dock = path[path.Count - 1];
            int manhattan = Math.Abs(start.x - dock.x) + Math.Abs(start.y - dock.y);
            int detour = path.Count - 1 - manhattan;
            return turns >= settings.minimumTurns && detour >= settings.minimumDetour;
        }

        private static CatBehavior SelectCatBehavior(LevelManifestEntry entry)
        {
            uint hash = DeterministicRandom.StableHash(entry.seed + "_cat");
            bool daily = entry.useDailyChallengeProfile;
            bool master = entry.difficultyTier == DifficultyTier.Master &&
                          entry.seed.IndexOf("Master", StringComparison.Ordinal) >= 0;
            bool endless = entry.seed.IndexOf("Endless", StringComparison.Ordinal) >= 0;

            if (daily)
            {
                return hash % 10 < 7 ? CatBehavior.Curious : CatBehavior.None;
            }

            if (master)
            {
                return hash % 10 < 8 ? CatBehavior.Curious : CatBehavior.None;
            }

            if (endless)
            {
                if (entry.levelNumber < 8)
                {
                    return CatBehavior.None;
                }

                return hash % 10 < 7 ? CatBehavior.Curious : CatBehavior.None;
            }

            int level = entry.levelNumber;
            if (level <= 20)
            {
                return CatBehavior.None;
            }

            if (level == 21)
            {
                return CatBehavior.Curious;
            }

            if (level <= 35)
            {
                return hash % 10 < 4 ? CatBehavior.Curious : CatBehavior.None;
            }

            if (level <= 60)
            {
                return hash % 10 < 7 ? CatBehavior.Curious : CatBehavior.None;
            }

            if (level <= 100)
            {
                return hash % 10 < 7 ? CatBehavior.Curious : CatBehavior.None;
            }

            return hash % 10 < 7 ? CatBehavior.Curious : CatBehavior.None;
        }

        private static bool TryPlaceCat(
            LevelDefinition level,
            List<GridPosition> route,
            LevelGenerationSettings settings,
            int decorationSalt)
        {
            HashSet<GridPosition> occupied = new HashSet<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                occupied.Add(level.cells[i].position);
            }

            List<GridPosition> candidates = new List<GridPosition>();
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (!occupied.Contains(position) &&
                        Manhattan(position, route[0]) >= 2 &&
                        DistanceToRoute(position, route) <= 6)
                    {
                        candidates.Add(position);
                    }
                }
            }

            DeterministicRandom random = new DeterministicRandom(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_v{1}_cat_{2}",
                    level.seed,
                    SeedVersion(level.generationVersion),
                    decorationSalt));
            random.Shuffle(candidates);
            GridPosition best = new GridPosition(-1, -1);
            List<SolutionStep> bestSolution = null;
            int bestScore = int.MinValue;
            int candidateLimit = Math.Min(72, candidates.Count);
            for (int i = 0; i < candidateLimit; i++)
            {
                int routeIndex = NearestRouteIndex(candidates[i], route);
                level.cat = new CatDefinition
                {
                    behavior = settings.catBehavior,
                    startPosition = candidates[i],
                    horizontalFirst = true
                };

                if (!CatObstacleSimulator.SharesWalkableRegionWithDustBot(level))
                {
                    continue;
                }

                List<SolutionStep> solution;
                int area = level.width * level.height;
                int searchLimit = Math.Min(
                    area * 4,
                    Math.Max(route.Count + 36, area * 2));
                if (!CatLevelSolver.TrySolve(level, searchLimit, out solution))
                {
                    continue;
                }

                List<GridPosition> solvedRoute =
                    CatLevelSolver.BuildRoute(level, solution);
                CatRoutePreview preview =
                    CatObstacleSimulator.SimulateRoute(level, solvedRoute);
                CatRelevanceReport relevance =
                    CatObstacleSimulator.AnalyzeRelevance(level, solvedRoute);
                if (preview.collided || !relevance.IsStrategicallyActive)
                {
                    continue;
                }

                CatStrategyReport strategy = CatLevelVarietyEvaluator.Analyze(level, solvedRoute);
                if (!CatLevelVarietyEvaluator.MatchesArchetype(
                        level,
                        settings.catPuzzleArchetype,
                        strategy))
                {
                    continue;
                }

                int score =
                    Math.Min(10, relevance.pressureTurns) * 8 +
                    Math.Min(10, relevance.movementTurns) * 3 +
                    Math.Min(12, relevance.uniqueVisitedTiles) * 2 +
                    Math.Min(12, relevance.reachableTiles) -
                    solution.Count +
                    ScoreCatLayoutProfile(
                        level,
                        route,
                        candidates[i],
                        routeIndex,
                        settings.catPuzzleArchetype) +
                    ScoreCatStartZone(level, candidates[i], settings.catStartZone) +
                    strategy.pressureScore * 4;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidates[i];
                    bestSolution = solution;
                }
            }

            if (!level.IsInside(best) || bestSolution == null)
            {
                level.cat = new CatDefinition();
                return false;
            }

            level.cat = new CatDefinition
            {
                behavior = settings.catBehavior,
                startPosition = best,
                horizontalFirst = true
            };
            level.expectedSolution.Clear();
            level.expectedSolution.AddRange(bestSolution);
            int moveCost = CalculateSolutionMoveCost(level, bestSolution);
            level.parMoves = moveCost;
            level.threeStarMoveTarget = moveCost;
            level.twoStarMoveTarget = moveCost + 3;
            level.moveLimit = level.hardPathLimit
                ? moveCost + 5
                : 0;

            return true;
        }

        private static int ScoreCatLayoutProfile(
            LevelDefinition level,
            List<GridPosition> route,
            GridPosition candidate,
            int routeIndex,
            CatPuzzleArchetype archetype)
        {
            int routeDistance = DistanceToRoute(candidate, route);
            int startDistance = Manhattan(candidate, route[0]);
            int dockDistance = Manhattan(candidate, route[route.Count - 1]);
            int crumbDistance = DistanceToNearestContent(level, candidate, CellContent.Crumb);
            int openNeighbors = CountOpenNeighbors(level, candidate);
            bool nearTurn = IsNearRouteTurn(route, routeIndex, 2);
            bool middleRoute = routeIndex > route.Count / 4 &&
                               routeIndex < route.Count - route.Count / 4;
            int score = Math.Max(0, 8 - routeDistance) +
                        Math.Max(0, 6 - Math.Abs(startDistance - 4));

            switch (archetype)
            {
                case CatPuzzleArchetype.HorizontalPriorityTrap:
                    score += Math.Abs(candidate.y - route[Math.Max(0, routeIndex)].y) <= 1 ? 10 : 0;
                    score += Math.Abs(candidate.x - route[0].x) >= 3 ? 4 : 0;
                    break;
                case CatPuzzleArchetype.LoopAroundFurniture:
                case CatPuzzleArchetype.CentralIsland:
                    score += nearTurn ? 12 : 0;
                    score += openNeighbors >= 2 ? 4 : 0;
                    break;
                case CatPuzzleArchetype.CorridorDelay:
                case CatPuzzleArchetype.MultiCorridorPursuit:
                    score += openNeighbors <= 2 ? 12 : 0;
                    score += middleRoute ? 5 : 0;
                    break;
                case CatPuzzleArchetype.SplitRoom:
                    score += routeDistance >= 2 && routeDistance <= 4 ? 10 : 0;
                    score += middleRoute ? 4 : 0;
                    break;
                case CatPuzzleArchetype.ChokepointTiming:
                case CatPuzzleArchetype.CatAtChokepoint:
                    score += openNeighbors <= 2 ? 10 : 0;
                    score += routeDistance <= 2 ? 5 : 0;
                    break;
                case CatPuzzleArchetype.DustBunnyRisk:
                    score += level.objectives.collectBonus &&
                             Manhattan(candidate, level.bonusPosition) <= 4 ? 14 : 0;
                    break;
                case CatPuzzleArchetype.FurnitureDelay:
                    score += DistanceToNearestContent(level, candidate, CellContent.Toy) <= 3 ? 10 : 0;
                    score += openNeighbors <= 3 ? 3 : 0;
                    break;
                case CatPuzzleArchetype.CrumbOrderChase:
                case CatPuzzleArchetype.MultiCrumbRoutePlanning:
                case CatPuzzleArchetype.LureAwayFromCrumb:
                    score += crumbDistance <= 3 ? 12 : 0;
                    score += middleRoute ? 3 : 0;
                    break;
                case CatPuzzleArchetype.NearCatch:
                    score += startDistance <= 5 && routeDistance <= 2 ? 12 : 0;
                    break;
                case CatPuzzleArchetype.SafePocket:
                    score += openNeighbors == 1 || openNeighbors == 2 ? 9 : 0;
                    score += nearTurn ? 4 : 0;
                    break;
                case CatPuzzleArchetype.LongRouteVsSafeRoute:
                case CatPuzzleArchetype.BacktrackBait:
                    score += routeDistance >= 3 ? 9 : 0;
                    score += dockDistance >= 5 ? 4 : 0;
                    break;
                case CatPuzzleArchetype.DockPressure:
                case CatPuzzleArchetype.LureAwayFromDock:
                    score += dockDistance <= 5 && routeIndex > route.Count / 2 ? 13 : 0;
                    break;
            }

            return score;
        }

        private static int ScoreCatStartZone(
            LevelDefinition level,
            GridPosition position,
            int targetZone)
        {
            int horizontal = position.x * 3 < level.width
                ? 0
                : position.x * 3 >= level.width * 2 ? 2 : 1;
            int vertical = position.y * 2 < level.height ? 0 : 1;
            int zone = horizontal == 1
                ? 4
                : vertical * 2 + (horizontal == 2 ? 1 : 0);
            return zone == targetZone ? 14 : 0;
        }

        private static int NearestRouteIndex(GridPosition position, List<GridPosition> route)
        {
            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < route.Count; i++)
            {
                int distance = Manhattan(position, route[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static bool IsNearRouteTurn(
            List<GridPosition> route,
            int routeIndex,
            int radius)
        {
            int start = Math.Max(1, routeIndex - radius);
            int end = Math.Min(route.Count - 2, routeIndex + radius);
            for (int i = start; i <= end; i++)
            {
                Direction incoming = DirectionUtility.Between(route[i - 1], route[i]);
                Direction outgoing = DirectionUtility.Between(route[i], route[i + 1]);
                if (incoming != Direction.None &&
                    outgoing != Direction.None &&
                    incoming != outgoing)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountRouteModifiersNear(
            LevelDefinition level,
            List<GridPosition> route,
            int routeIndex,
            int radius)
        {
            int count = 0;
            int start = Math.Max(0, routeIndex - radius);
            int end = Math.Min(route.Count - 1, routeIndex + radius);
            for (int i = start; i <= end; i++)
            {
                if (CellContentUtility.IsRouteModifier(level.GetContent(route[i])))
                {
                    count++;
                }
            }

            return count;
        }

        private static int DistanceToRoute(
            GridPosition position,
            List<GridPosition> route)
        {
            int distance = int.MaxValue;
            for (int i = 0; i < route.Count; i++)
            {
                distance = Math.Min(distance, Manhattan(position, route[i]));
            }

            return distance;
        }

        private static int Manhattan(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        private static void ApplyCatArchetype(
            LevelGenerationSettings settings,
            CatPuzzleArchetype archetype,
            int area)
        {
            int structuralBlockers = Math.Max(5, area / 7);
            settings.blockerCount = Math.Max(settings.blockerCount, structuralBlockers);
            settings.hazardCount = Math.Min(settings.hazardCount, 1);
            settings.minimumCatPressureScore = Math.Max(
                settings.minimumCatPressureScore,
                7);
            settings.minimumPathLength = Math.Max(
                settings.minimumPathLength,
                Math.Min(area - 5, Math.Max(11, area / 4)));
            settings.maximumPathLength = Math.Max(
                settings.maximumPathLength,
                Math.Min(area - 3, settings.minimumPathLength + Math.Max(5, area / 10)));

            switch (archetype)
            {
                case CatPuzzleArchetype.DustBunnyRisk:
                    settings.includeBonus = true;
                    settings.bonusRequiredForThreeStars = true;
                    settings.crumbCount = Math.Max(2, settings.crumbCount);
                    break;
                case CatPuzzleArchetype.CrumbOrderChase:
                    settings.crumbCount = Math.Max(2, settings.crumbCount);
                    settings.minimumCrumbSpread = Math.Max(2, settings.minimumCrumbSpread);
                    break;
                case CatPuzzleArchetype.MultiCrumbRoutePlanning:
                    settings.crumbCount = Math.Max(area >= 64 ? 4 : 3, settings.crumbCount);
                    settings.minimumCrumbSpread = Math.Max(2, settings.minimumCrumbSpread);
                    break;
                case CatPuzzleArchetype.CorridorDelay:
                case CatPuzzleArchetype.ChokepointTiming:
                case CatPuzzleArchetype.SafePocket:
                case CatPuzzleArchetype.SplitRoom:
                case CatPuzzleArchetype.CatAtChokepoint:
                case CatPuzzleArchetype.MultiCorridorPursuit:
                    settings.blockerCount = Math.Max(settings.blockerCount, area / 5);
                    break;
                case CatPuzzleArchetype.LoopAroundFurniture:
                case CatPuzzleArchetype.CentralIsland:
                case CatPuzzleArchetype.FurnitureDelay:
                    settings.blockerCount = Math.Max(settings.blockerCount, area / 6);
                    break;
                case CatPuzzleArchetype.DockPressure:
                case CatPuzzleArchetype.LureAwayFromDock:
                    settings.minimumPathLength = Math.Max(
                        settings.minimumPathLength,
                        Math.Min(area - 5, area / 3));
                    break;
                case CatPuzzleArchetype.BacktrackBait:
                case CatPuzzleArchetype.LongRouteVsSafeRoute:
                    settings.moveLimitSlack = Math.Max(3, settings.moveLimitSlack);
                    break;
            }

            settings.blockerCount = Math.Min(area / 4, settings.blockerCount);
            settings.crumbCount = Math.Min(area >= 64 ? 4 : 3, settings.crumbCount);
        }

        private static void ApplyArchetype(
            LevelGenerationSettings settings,
            LevelArchetype archetype,
            int area)
        {
            switch (archetype)
            {
                case LevelArchetype.CrumbOrder:
                    settings.crumbCount++;
                    settings.minimumTurns++;
                    settings.minimumEngagementScore += 2;
                    settings.minimumStrategicDepthScore += 3;
                    break;
                case LevelArchetype.BlockerMaze:
                    settings.blockerCount += 2;
                    settings.minimumEngagementScore += 2;
                    settings.minimumStrategicDepthScore += 2;
                    settings.minimumRouteDecisions++;
                    break;
                case LevelArchetype.HazardAvoidance:
                    settings.hazardCount += 2;
                    settings.minimumEngagementScore += 3;
                    settings.minimumStrategicDepthScore += 3;
                    break;
                case LevelArchetype.DustBunnyDetour:
                    settings.includeBonus = true;
                    settings.minimumDetour++;
                    settings.minimumEngagementScore += 3;
                    settings.minimumStrategicDepthScore += 4;
                    break;
                case LevelArchetype.TightPath:
                    settings.blockerCount += 2;
                    settings.hazardCount++;
                    settings.routeModifierCount++;
                    settings.maximumPathLength = Math.Min(area - 2, settings.maximumPathLength + 2);
                    settings.minimumEngagementScore += 3;
                    settings.minimumStrategicDepthScore += 4;
                    settings.minimumTemptingBranches++;
                    break;
                case LevelArchetype.Breather:
                    settings.minimumPathLength = Math.Max(6, settings.minimumPathLength - 3);
                    settings.maximumPathLength = Math.Max(
                        settings.minimumPathLength + 2,
                        settings.maximumPathLength - 3);
                    settings.crumbCount = Math.Max(1, settings.crumbCount - 1);
                    settings.blockerCount = Math.Max(0, settings.blockerCount - 1);
                    settings.hazardCount = Math.Max(0, settings.hazardCount - 1);
                    settings.routeModifierCount = Math.Max(0, settings.routeModifierCount - 1);
                    settings.minimumTurns = Math.Max(2, settings.minimumTurns - 1);
                    settings.minimumDetour = Math.Max(0, settings.minimumDetour - 1);
                    settings.minimumEngagementScore = Math.Max(8, settings.minimumEngagementScore - 4);
                    settings.minimumStrategicDepthScore = Math.Max(10, settings.minimumStrategicDepthScore - 6);
                    break;
                case LevelArchetype.TrickRoute:
                    settings.minimumTurns++;
                    settings.minimumDetour += 2;
                    settings.routeModifierCount++;
                    settings.minimumEngagementScore += 4;
                    settings.minimumStrategicDepthScore += 6;
                    settings.minimumTemptingBranches++;
                    break;
                case LevelArchetype.ChallengeRoute:
                    settings.crumbCount++;
                    settings.blockerCount++;
                    settings.hazardCount++;
                    settings.routeModifierCount++;
                    settings.includeBonus = true;
                    settings.bonusRequiredForThreeStars = true;
                    settings.minimumTurns++;
                    settings.minimumDetour++;
                    settings.minimumEngagementScore += 5;
                    settings.minimumStrategicDepthScore += 8;
                    settings.minimumRouteDecisions++;
                    settings.minimumTemptingBranches++;
                    break;
            }
        }
    }
}
