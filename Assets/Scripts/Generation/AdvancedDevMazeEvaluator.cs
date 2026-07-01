using System;
using System.Collections.Generic;
using System.Globalization;

namespace DustBot
{
    public struct AdvancedDevMazeReport
    {
        public int score;
        public int meaningfulRouteChoices;
        public int temptingWrongPaths;
        public int shortcutChoices;
        public int routeCommitmentTiles;
        public int averageCrumbRouteDistance;
        public int finalCrumbToDock;
        public int hardLimitSlack;
        public LargeMazeComplexityReport topology;
    }

    public static class AdvancedDevMazeEvaluator
    {
        private static readonly GridPosition[] Offsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        public static AdvancedDevMazeReport Analyze(LevelDefinition level)
        {
            return Analyze(level, LargeMazeEvaluator.Analyze(level));
        }

        public static AdvancedDevMazeReport Analyze(
            LevelDefinition level,
            LargeMazeComplexityReport topology)
        {
            List<GridPosition> route = BuildExpectedRoute(level);
            Dictionary<GridPosition, int> routeIndices = new Dictionary<GridPosition, int>();
            for (int i = 0; i < route.Count; i++)
            {
                routeIndices[route[i]] = i;
            }

            int meaningfulChoices = 0;
            int shortcutEdges = 0;
            for (int i = 0; i < route.Count; i++)
            {
                bool hasOffRouteChoice = false;
                for (int offset = 0; offset < Offsets.Length; offset++)
                {
                    GridPosition neighbor = route[i] + Offsets[offset];
                    if (!level.IsInside(neighbor) || !IsWalkable(level, neighbor))
                    {
                        continue;
                    }

                    int neighborIndex;
                    if (!routeIndices.TryGetValue(neighbor, out neighborIndex))
                    {
                        hasOffRouteChoice = true;
                    }
                    else if (neighborIndex > i + 1)
                    {
                        shortcutEdges++;
                    }
                }

                if (hasOffRouteChoice)
                {
                    meaningfulChoices++;
                }
            }

            List<int> crumbIndices = new List<int>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content != CellContent.Crumb)
                {
                    continue;
                }

                int routeIndex;
                if (routeIndices.TryGetValue(level.cells[i].position, out routeIndex))
                {
                    crumbIndices.Add(routeIndex);
                }
            }
            crumbIndices.Sort();
            int crumbDistance = 0;
            for (int i = 1; i < crumbIndices.Count; i++)
            {
                crumbDistance += crumbIndices[i] - crumbIndices[i - 1];
            }
            int averageCrumbDistance = crumbIndices.Count > 1
                ? crumbDistance / (crumbIndices.Count - 1)
                : 0;
            int finalCrumbToDock = crumbIndices.Count > 0
                ? route.Count - 1 - crumbIndices[crumbIndices.Count - 1]
                : 0;
            int commitmentTiles =
                level.Count(CellContent.Fragile) +
                level.Count(CellContent.Slippery) +
                level.Count(CellContent.OneWayUp) +
                level.Count(CellContent.OneWayRight) +
                level.Count(CellContent.OneWayDown) +
                level.Count(CellContent.OneWayLeft);
            int hardSlack = level.hardPathLimit && level.moveLimit > 0
                ? level.moveLimit - level.parMoves
                : 0;
            int wrongPaths = topology.deadEnds + topology.decoyPaths + shortcutEdges;
            int score = topology.score +
                        Math.Min(28, meaningfulChoices) +
                        Math.Min(24, wrongPaths * 2) +
                        Math.Min(18, shortcutEdges * 3) +
                        Math.Min(12, commitmentTiles * 2) +
                        Math.Min(14, averageCrumbDistance / 2) +
                        Math.Min(14, finalCrumbToDock / 2) +
                        (level.hardPathLimit
                            ? Math.Min(12, Math.Max(0, 12 - hardSlack))
                            : 0);

            return new AdvancedDevMazeReport
            {
                score = score,
                meaningfulRouteChoices = meaningfulChoices,
                temptingWrongPaths = wrongPaths,
                shortcutChoices = shortcutEdges,
                routeCommitmentTiles = commitmentTiles,
                averageCrumbRouteDistance = averageCrumbDistance,
                finalCrumbToDock = finalCrumbToDock,
                hardLimitSlack = hardSlack,
                topology = topology
            };
        }

        public static bool MeetsRequirements(
            LevelDefinition level,
            out AdvancedDevMazeReport report,
            out string reason)
        {
            LargeMazeComplexityReport topology = LargeMazeEvaluator.Analyze(level);
            return MeetsRequirements(level, topology, out report, out reason);
        }

        public static bool MeetsRequirements(
            LevelDefinition level,
            LargeMazeComplexityReport topology,
            out AdvancedDevMazeReport report,
            out string reason)
        {
            report = Analyze(level, topology);
            if (!level.advancedDevMaze ||
                level.generationMode == GenerationMode.ProductionCampaign)
            {
                reason = "advanced maze validation is restricted to dev-only levels";
                return false;
            }

            int area = level.width * level.height;
            int minimumScore = level.difficultyTier >= DifficultyTier.Master
                ? 142
                : level.difficultyTier >= DifficultyTier.Expert
                    ? 130
                    : level.difficultyTier >= DifficultyTier.Hard ? 116 : 98;
            int minimumChoices = Math.Max(4, area / 105);
            int minimumWrongPaths = Math.Max(6, area / 90);
            int minimumShortcuts = level.difficultyTier >= DifficultyTier.Expert ? 3 : 2;
            int minimumCrumbDistance = Math.Max(5, (level.width + level.height) / 5);
            int minimumDockReturn = level.devMazeArchetype == DevMazeArchetype.DockReturn
                ? Math.Max(12, (level.width + level.height) / 2)
                : Math.Max(7, (level.width + level.height) / 4);
            int minimumPlayableEdgeCells = Math.Max(
                6,
                (level.width + level.height) / 4);
            if (report.score < minimumScore ||
                report.meaningfulRouteChoices < minimumChoices ||
                report.temptingWrongPaths < minimumWrongPaths ||
                report.shortcutChoices < minimumShortcuts ||
                report.averageCrumbRouteDistance < minimumCrumbDistance ||
                report.finalCrumbToDock < minimumDockReturn ||
                topology.fullBlockedPerimeter ||
                topology.playableEdgeCells < minimumPlayableEdgeCells)
            {
                reason = string.Format(
                    CultureInfo.InvariantCulture,
                    "advanced maze score {0}/{1}, choices {2}/{3}, wrong paths {4}/{5}, shortcuts {6}/{7}, crumb spread {8}/{9}, dock return {10}/{11}, playable edge {12}/{13}",
                    report.score,
                    minimumScore,
                    report.meaningfulRouteChoices,
                    minimumChoices,
                    report.temptingWrongPaths,
                    minimumWrongPaths,
                    report.shortcutChoices,
                    minimumShortcuts,
                    report.averageCrumbRouteDistance,
                    minimumCrumbDistance,
                    report.finalCrumbToDock,
                    minimumDockReturn,
                    topology.playableEdgeCells,
                    minimumPlayableEdgeCells);
                return false;
            }

            if (level.threeStarMoveTarget != level.parMoves ||
                level.twoStarMoveTarget <= level.threeStarMoveTarget)
            {
                reason = "advanced maze star targets are not strict and ordered";
                return false;
            }

            if (level.difficultyTier >= DifficultyTier.Hard &&
                (!level.hardPathLimit ||
                 level.moveLimit <= level.parMoves ||
                 report.hardLimitSlack > Math.Max(9, level.parMoves / 10)))
            {
                reason = "advanced maze hard maximum is missing or too generous";
                return false;
            }

            if (level.objectives.collectBonus &&
                report.topology.bonusDetourCost <
                (level.devMazeArchetype == DevMazeArchetype.DustBunnyDetour ? 4 : 2))
            {
                reason = "the dev maze Dust Bunny detour is not costly enough";
                return false;
            }

            if ((level.devMazeArchetype == DevMazeArchetype.OneWayCommitment ||
                 level.devMazeArchetype == DevMazeArchetype.FragileCorridor) &&
                report.routeCommitmentTiles < 4)
            {
                reason = "the commitment archetype does not contain enough commitment tiles";
                return false;
            }

            reason = "Valid";
            return true;
        }

        private static List<GridPosition> BuildExpectedRoute(LevelDefinition level)
        {
            List<GridPosition> route = new List<GridPosition>(level.expectedSolution.Count + 1);
            GridPosition current = level.Find(CellContent.Start);
            route.Add(current);
            for (int i = 0; i < level.expectedSolution.Count; i++)
            {
                current += DirectionUtility.ToOffset(level.expectedSolution[i].direction);
                route.Add(current);
            }
            return route;
        }

        private static bool IsWalkable(LevelDefinition level, GridPosition position)
        {
            return CellContentUtility.IsWalkableFloor(level.GetContent(position));
        }
    }
}
