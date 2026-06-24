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

            if (entry.generationVersion < 1 || entry.generationVersion > 4)
            {
                throw new NotSupportedException(
                    "Unsupported DustBot generation version " + entry.generationVersion);
            }

            if (string.IsNullOrEmpty(entry.seed) ||
                entry.boardWidth < 2 ||
                entry.boardHeight < 2 ||
                entry.boardWidth > 12 ||
                entry.boardHeight > 12)
            {
                throw new ArgumentException("Generation metadata contains an invalid seed or board size.", "entry");
            }

            LevelGenerationSettings settings = GetSettings(entry);
            string lastRejection = "No candidate was generated.";
            for (int candidate = 0; candidate < 24; candidate++)
            {
                List<GridPosition> path = GeneratePath(entry, settings, candidate * 128);
                LevelDefinition level = BuildLevel(entry, mode, settings, path, candidate);

                string validationMessage;
                if (!LevelValidator.TryValidate(level, out validationMessage))
                {
                    lastRejection = validationMessage;
                    continue;
                }

                LevelEngagementReport report;
                if (entry.generationVersion >= 3 &&
                    !LevelEngagementEvaluator.IsAccepted(level, settings, out report))
                {
                    lastRejection = string.Format(
                        CultureInfo.InvariantCulture,
                        "engagement {0}/{1}, turns {2}/{3}, detour {4}/{5}, spread {6}/{7}, decisions {8}/{9}, branches {10}/{11}, bunny detour {12}/{13}, nearby {14}, trivial {15}, dense {16}",
                        report.score,
                        settings.minimumEngagementScore,
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
                        report.bonusDetourCost,
                        settings.minimumBonusDetour,
                        report.nearbyBlockers + report.nearbyHazards,
                        report.tooTrivial,
                        report.tooDense);
                    continue;
                }

                level.engagementScore = entry.generationVersion >= 3
                    ? LevelEngagementEvaluator.Analyze(level).score
                    : 0;
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
                    moveLimitSlack = 2,
                    hardPathLimit = true,
                    minimumCrumbSpread = 3,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = 2
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
                    moveLimitSlack = 1,
                    hardPathLimit = true,
                    minimumCrumbSpread = 3,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = 2
                };
            }
            else if (level <= 20)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 9,
                    maximumPathLength = Math.Min(area - 3, 14),
                    crumbCount = level >= 19 ? 3 : 2,
                    blockerCount = 2,
                    hazardCount = level >= 18 ? 1 : 0,
                    includeBonus = level == 20 && entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 3,
                    minimumDetour = 2,
                    minimumEngagementScore = 16,
                    moveLimitSlack = 4,
                    hardPathLimit = false,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 1,
                    minimumTemptingBranches = 1
                };
            }
            else if (level <= 35)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 11,
                    maximumPathLength = Math.Min(area - 3, 18),
                    crumbCount = level >= 28 ? 3 : 2,
                    blockerCount = 3,
                    hazardCount = 1,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 4,
                    minimumDetour = 2,
                    minimumEngagementScore = 21,
                    moveLimitSlack = 3,
                    hardPathLimit = false,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 2,
                    minimumTemptingBranches = 1,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0
                };
            }
            else if (level <= 60)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 15,
                    maximumPathLength = Math.Min(area - 4, 23),
                    crumbCount = level % 6 == 0 ? 4 : 3,
                    blockerCount = 4,
                    hazardCount = 2,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour || level % 8 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 5,
                    minimumDetour = 3,
                    minimumEngagementScore = 27,
                    moveLimitSlack = 2,
                    hardPathLimit =
                        entry.archetype == LevelArchetype.TightPath ||
                        entry.archetype == LevelArchetype.ChallengeRoute ||
                        level % 6 == 0,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 2,
                    minimumTemptingBranches = 1,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0
                };
            }
            else if (level <= 100)
            {
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = 18,
                    maximumPathLength = Math.Min(area - 5, 28),
                    crumbCount = level % 5 == 0 ? 5 : 4,
                    blockerCount = 5,
                    hazardCount = 3,
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour || level % 5 == 0,
                    bonusRequiredForThreeStars = entry.archetype == LevelArchetype.DustBunnyDetour,
                    minimumTurns = 6,
                    minimumDetour = 4,
                    minimumEngagementScore = 32,
                    moveLimitSlack = 2,
                    hardPathLimit = entry.archetype != LevelArchetype.Breather && level % 3 != 1,
                    minimumCrumbSpread = 2,
                    minimumRouteDecisions = 3,
                    minimumTemptingBranches = 2,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0
                };
            }
            else
            {
                int tier = Math.Max(1, (int)entry.difficultyTier);
                settings = new LevelGenerationSettings
                {
                    minimumPathLength = Math.Min(area - 10, 19 + tier * 2),
                    maximumPathLength = Math.Min(area - 4, 28 + tier * 3),
                    crumbCount = Math.Min(5, 3 + tier / 2),
                    blockerCount = Math.Min(10, 4 + tier),
                    hazardCount = Math.Min(7, 2 + tier / 2),
                    includeBonus = entry.archetype == LevelArchetype.DustBunnyDetour ||
                                   entry.archetype == LevelArchetype.ChallengeRoute ||
                                   level % 4 == 0,
                    bonusRequiredForThreeStars =
                        entry.archetype == LevelArchetype.DustBunnyDetour ||
                        entry.archetype == LevelArchetype.ChallengeRoute,
                    minimumTurns = Math.Min(10, 5 + tier / 2),
                    minimumDetour = Math.Min(8, 3 + tier / 2),
                    minimumEngagementScore = Math.Min(48, 28 + tier * 3),
                    moveLimitSlack = tier >= (int)DifficultyTier.Expert ? 1 : 2,
                    hardPathLimit = entry.archetype != LevelArchetype.Breather && level % 4 != 0,
                    minimumCrumbSpread = level <= 250
                        ? 2
                        : Math.Min(4, 2 + tier / 3),
                    minimumRouteDecisions = 2,
                    minimumTemptingBranches = 1,
                    minimumBonusDetour = entry.archetype == LevelArchetype.DustBunnyDetour ? 2 : 0
                };
            }

            ApplyArchetype(settings, entry.archetype, area);
            if (entry.archetype == LevelArchetype.Breather)
            {
                settings.crumbCount = Math.Max(2, settings.crumbCount);
                settings.minimumCrumbSpread = Math.Min(settings.minimumCrumbSpread, 1);
                settings.minimumRouteDecisions = Math.Min(settings.minimumRouteDecisions, 1);
                settings.minimumTemptingBranches = 0;
                settings.hardPathLimit = false;
            }
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
                        entry.generationVersion,
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
            LevelDefinition level = new LevelDefinition
            {
                id = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_{1}",
                    mode,
                    entry.levelNumber),
                mode = mode,
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
                themeId = entry.themeId,
                mechanicSet = entry.mechanicSet,
                objectiveSet = entry.objectiveSet
            };

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

            if (entry.generationVersion == 1)
            {
                PlaceOffPathContentV1(level, path, settings, decorationSalt);
            }
            else
            {
                PlaceOffPathContentV2(level, path, settings, decorationSalt);
            }

            return level;
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
                    level.generationVersion + "_" + decorationSalt));
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
                    level.generationVersion + "_" + decorationSalt));
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
                    break;
                case LevelArchetype.BlockerMaze:
                    settings.blockerCount += 2;
                    settings.minimumEngagementScore += 2;
                    break;
                case LevelArchetype.HazardAvoidance:
                    settings.hazardCount += 2;
                    settings.minimumEngagementScore += 3;
                    break;
                case LevelArchetype.DustBunnyDetour:
                    settings.includeBonus = true;
                    settings.minimumDetour++;
                    settings.minimumEngagementScore += 3;
                    break;
                case LevelArchetype.TightPath:
                    settings.blockerCount += 2;
                    settings.hazardCount++;
                    settings.maximumPathLength = Math.Min(area - 2, settings.maximumPathLength + 2);
                    settings.minimumEngagementScore += 3;
                    break;
                case LevelArchetype.Breather:
                    settings.minimumPathLength = Math.Max(6, settings.minimumPathLength - 3);
                    settings.maximumPathLength = Math.Max(
                        settings.minimumPathLength + 2,
                        settings.maximumPathLength - 3);
                    settings.crumbCount = Math.Max(1, settings.crumbCount - 1);
                    settings.blockerCount = Math.Max(0, settings.blockerCount - 1);
                    settings.hazardCount = Math.Max(0, settings.hazardCount - 1);
                    settings.minimumTurns = Math.Max(2, settings.minimumTurns - 1);
                    settings.minimumDetour = Math.Max(0, settings.minimumDetour - 1);
                    settings.minimumEngagementScore = Math.Max(8, settings.minimumEngagementScore - 4);
                    break;
                case LevelArchetype.TrickRoute:
                    settings.minimumTurns++;
                    settings.minimumDetour += 2;
                    settings.minimumEngagementScore += 4;
                    break;
                case LevelArchetype.ChallengeRoute:
                    settings.crumbCount++;
                    settings.blockerCount++;
                    settings.hazardCount++;
                    settings.includeBonus = true;
                    settings.bonusRequiredForThreeStars = true;
                    settings.minimumTurns++;
                    settings.minimumDetour++;
                    settings.minimumEngagementScore += 5;
                    break;
            }
        }
    }
}
