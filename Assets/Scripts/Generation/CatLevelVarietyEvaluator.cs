using System;
using System.Collections.Generic;

namespace DustBot
{
    public struct CatStrategyReport
    {
        public int pressureScore;
        public int nearCatchTurns;
        public int routeChangeMoves;
        public int backtrackMoves;
        public int horizontalCatMoves;
        public int verticalCatMoves;
        public int crumbPressure;
        public int dockPressure;
        public int bonusPressure;
        public int corridorCells;
        public int branches;
        public int chokepoints;
        public int loops;
        public int safePockets;
        public int nearbyFurniture;
        public int solutionPattern;
    }

    public static class CatLevelVarietyEvaluator
    {
        private static readonly GridPosition[] Offsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        public static CatStrategyReport Analyze(
            LevelDefinition level,
            IList<GridPosition> route)
        {
            CatStrategyReport report = new CatStrategyReport();
            if (level == null || level.cat == null || !level.cat.IsEnabled ||
                route == null || route.Count == 0)
            {
                return report;
            }

            int openCells = 0;
            int edges = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (!IsOpen(level, position)) continue;
                    openCells++;
                    int degree = OpenNeighborCount(level, position);
                    edges += degree;
                    if (degree == 2) report.corridorCells++;
                    if (degree >= 3) report.branches++;
                    if (degree == 1 ||
                        (degree == 2 && BlockedNeighborCount(level, position) >= 2))
                    {
                        report.safePockets++;
                    }
                }
            }

            edges /= 2;
            report.loops = Math.Max(0, edges - openCells + 1);
            report.chokepoints = CountArticulationPoints(level);
            report.routeChangeMoves = Math.Max(
                0,
                route.Count - 1 - Math.Max(0, level.catFreeParMoves));

            Direction previous = Direction.None;
            for (int i = 1; i < route.Count; i++)
            {
                Direction direction = DirectionUtility.Between(route[i - 1], route[i]);
                if (previous != Direction.None &&
                    DirectionUtility.ToOffset(previous) +
                    DirectionUtility.ToOffset(direction) == new GridPosition(0, 0))
                {
                    report.backtrackMoves++;
                }

                previous = direction;
            }

            CatRoutePreview preview = CatObstacleSimulator.SimulateRoute(level, route);
            for (int i = 1; i < preview.catPositions.Count; i++)
            {
                GridPosition before = preview.catPositions[i - 1];
                GridPosition after = preview.catPositions[i];
                if (before.x != after.x) report.horizontalCatMoves++;
                if (before.y != after.y) report.verticalCatMoves++;
                int routeIndex = Math.Min(i, route.Count - 1);
                int distance = Manhattan(after, route[routeIndex]);
                // Distance two is still a readable near-catch: the cat's next
                // horizontal-first turn can reach the bot unless the player
                // uses the layout immediately.
                if (distance <= 2) report.nearCatchTurns++;
                CellContent content = level.GetContent(route[routeIndex]);
                if (content == CellContent.Crumb && distance <= 3) report.crumbPressure++;
                if (content == CellContent.Dock && distance <= 3) report.dockPressure++;
                if (level.objectives.collectBonus &&
                    route[routeIndex] == level.bonusPosition && distance <= 3)
                {
                    report.bonusPressure++;
                }
            }

            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition neighbor = level.cat.startPosition + Offsets[i];
                if (level.IsInside(neighbor) &&
                    CellContentUtility.IsCatBlocker(level.GetContent(neighbor)))
                {
                    report.nearbyFurniture++;
                }
            }

            report.solutionPattern =
                Math.Min(3, report.backtrackMoves) |
                (Math.Min(3, report.nearCatchTurns) << 2) |
                (Math.Min(3, report.routeChangeMoves) << 4) |
                (Math.Min(3, report.crumbPressure + report.dockPressure) << 6);
            report.pressureScore = Math.Min(
                20,
                report.nearCatchTurns * 2 +
                Math.Min(5, report.routeChangeMoves) +
                Math.Min(4, report.backtrackMoves) +
                Math.Min(4, report.crumbPressure * 2) +
                Math.Min(4, report.dockPressure * 2) +
                Math.Min(3, report.bonusPressure * 2) +
                Math.Min(3, report.nearbyFurniture));
            return report;
        }

        public static bool MatchesArchetype(
            LevelDefinition level,
            CatPuzzleArchetype archetype,
            CatStrategyReport report)
        {
            switch (archetype)
            {
                case CatPuzzleArchetype.None:
                    return true;
                case CatPuzzleArchetype.HorizontalPriorityTrap:
                    return report.horizontalCatMoves >= report.verticalCatMoves;
                case CatPuzzleArchetype.LoopAroundFurniture:
                case CatPuzzleArchetype.CentralIsland:
                    return report.loops >= 1 && report.nearbyFurniture >= 1;
                case CatPuzzleArchetype.CorridorDelay:
                    return report.corridorCells >= 3;
                case CatPuzzleArchetype.ChokepointTiming:
                case CatPuzzleArchetype.CatAtChokepoint:
                    return report.chokepoints >= 1 || report.nearbyFurniture >= 2;
                case CatPuzzleArchetype.SafePocket:
                    return report.safePockets >= 1;
                case CatPuzzleArchetype.SplitRoom:
                    return report.chokepoints >= 1 || report.corridorCells >= 5;
                case CatPuzzleArchetype.DockPressure:
                case CatPuzzleArchetype.LureAwayFromDock:
                    return report.dockPressure >= 1 ||
                           Manhattan(level.cat.startPosition, level.Find(CellContent.Dock)) <= 5;
                case CatPuzzleArchetype.DustBunnyRisk:
                    return level.objectives.collectBonus && report.bonusPressure >= 1;
                case CatPuzzleArchetype.CrumbOrderChase:
                    return level.Count(CellContent.Crumb) >= 2 && report.crumbPressure >= 1;
                case CatPuzzleArchetype.NearCatch:
                    return report.nearCatchTurns >= 1;
                case CatPuzzleArchetype.LongRouteVsSafeRoute:
                    return report.routeChangeMoves >= 1 || report.backtrackMoves >= 1;
                case CatPuzzleArchetype.LureAwayFromCrumb:
                    return report.crumbPressure >= 1;
                case CatPuzzleArchetype.BacktrackBait:
                    return report.backtrackMoves >= 1;
                case CatPuzzleArchetype.MultiCorridorPursuit:
                    return report.corridorCells >= 6;
                case CatPuzzleArchetype.FurnitureDelay:
                    return report.nearbyFurniture >= 1;
                case CatPuzzleArchetype.MultiCrumbRoutePlanning:
                    return level.Count(CellContent.Crumb) >= 3 && report.crumbPressure >= 1;
                default:
                    return report.pressureScore >= 4;
            }
        }

        public static int Similarity(CatStrategyReport a, CatStrategyReport b)
        {
            int same = 0;
            if (Bucket(a.corridorCells) == Bucket(b.corridorCells)) same++;
            if (Bucket(a.chokepoints) == Bucket(b.chokepoints)) same++;
            if (Bucket(a.loops) == Bucket(b.loops)) same++;
            if (Bucket(a.safePockets) == Bucket(b.safePockets)) same++;
            if (Bucket(a.routeChangeMoves) == Bucket(b.routeChangeMoves)) same++;
            if (Bucket(a.backtrackMoves) == Bucket(b.backtrackMoves)) same++;
            if (a.solutionPattern == b.solutionPattern) same += 2;
            return same;
        }

        private static int Bucket(int value)
        {
            return value <= 0 ? 0 : value <= 2 ? 1 : value <= 5 ? 2 : 3;
        }

        private static int CountArticulationPoints(LevelDefinition level)
        {
            int[,] discovery = new int[level.width, level.height];
            int[,] low = new int[level.width, level.height];
            bool[,] articulation = new bool[level.width, level.height];
            int time = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (IsOpen(level, position) && discovery[x, y] == 0)
                    {
                        VisitArticulation(
                            level,
                            position,
                            new GridPosition(-1, -1),
                            discovery,
                            low,
                            articulation,
                            ref time);
                    }
                }
            }

            int count = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    if (articulation[x, y]) count++;
                }
            }

            return count;
        }

        private static void VisitArticulation(
            LevelDefinition level,
            GridPosition current,
            GridPosition parent,
            int[,] discovery,
            int[,] low,
            bool[,] articulation,
            ref int time)
        {
            discovery[current.x, current.y] = ++time;
            low[current.x, current.y] = discovery[current.x, current.y];
            int children = 0;
            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition next = current + Offsets[i];
                if (!IsOpen(level, next) || next == parent) continue;
                if (discovery[next.x, next.y] == 0)
                {
                    children++;
                    VisitArticulation(
                        level,
                        next,
                        current,
                        discovery,
                        low,
                        articulation,
                        ref time);
                    low[current.x, current.y] = Math.Min(
                        low[current.x, current.y],
                        low[next.x, next.y]);
                    if (parent.x < 0 && children > 1)
                    {
                        articulation[current.x, current.y] = true;
                    }
                    else if (parent.x >= 0 &&
                             low[next.x, next.y] >= discovery[current.x, current.y])
                    {
                        articulation[current.x, current.y] = true;
                    }
                }
                else
                {
                    low[current.x, current.y] = Math.Min(
                        low[current.x, current.y],
                        discovery[next.x, next.y]);
                }
            }
        }

        private static bool IsOpen(LevelDefinition level, GridPosition position)
        {
            return level.IsInside(position) &&
                   !CellContentUtility.IsCatBlocker(level.GetContent(position));
        }

        private static int OpenNeighborCount(LevelDefinition level, GridPosition position)
        {
            int count = 0;
            for (int i = 0; i < Offsets.Length; i++)
            {
                if (IsOpen(level, position + Offsets[i])) count++;
            }

            return count;
        }

        private static int BlockedNeighborCount(LevelDefinition level, GridPosition position)
        {
            int count = 0;
            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition neighbor = position + Offsets[i];
                if (!level.IsInside(neighbor) ||
                    CellContentUtility.IsCatBlocker(level.GetContent(neighbor))) count++;
            }

            return count;
        }

        private static int Manhattan(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }
    }
}
