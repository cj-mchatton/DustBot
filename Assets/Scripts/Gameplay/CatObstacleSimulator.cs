using System;
using System.Collections.Generic;

namespace DustBot
{
    public struct CatStepResult
    {
        public GridPosition from;
        public GridPosition first;
        public GridPosition to;
        public int moveCount;
        public bool collided;
    }

    public struct CatRoutePreview
    {
        public bool collided;
        public int collisionStep;
        public int closestDistance;
        public List<GridPosition> catPositions;
    }

    public struct CatRelevanceReport
    {
        public int openStartNeighbors;
        public int reachableTiles;
        public int reachableRouteTiles;
        public int movementTurns;
        public int uniqueVisitedTiles;
        public int closestDistance;
        public int pressureTurns;
        public int distanceToBotStart;
        public int distanceToRoute;
        public bool sameConnectedRegion;

        public bool IsStrategicallyActive
        {
            get
            {
                return openStartNeighbors >= 1 &&
                       sameConnectedRegion &&
                       reachableTiles >= 5 &&
                       reachableRouteTiles >= 3 &&
                       distanceToRoute <= 6 &&
                       movementTurns >= 2 &&
                       uniqueVisitedTiles >= 2 &&
                       closestDistance <= 4 &&
                       pressureTurns >= 1;
            }
        }
    }

    public static class CatObstacleSimulator
    {
        public static CatStepResult Advance(
            LevelDefinition level,
            GridPosition catPosition,
            GridPosition botPosition,
            int dustBotStep)
        {
            CatStepResult result = new CatStepResult
            {
                from = catPosition,
                first = catPosition,
                to = catPosition,
                moveCount = 0,
                collided = catPosition == botPosition
            };

            if (level == null ||
                level.cat == null ||
                !level.cat.IsEnabled ||
                result.collided)
            {
                return result;
            }

            int moves = MovesForTurn(level.cat.behavior, dustBotStep);
            GridPosition current = catPosition;
            for (int i = 0; i < moves; i++)
            {
                GridPosition next = ChooseNext(
                    level,
                    current,
                    botPosition,
                    true);
                if (next == current)
                {
                    break;
                }

                current = next;
                result.moveCount++;
                if (result.moveCount == 1)
                {
                    result.first = current;
                }

                result.to = current;
                if (current == botPosition)
                {
                    result.collided = true;
                    break;
                }
            }

            return result;
        }

        public static CatRoutePreview SimulateRoute(
            LevelDefinition level,
            IList<GridPosition> dustBotRoute)
        {
            CatRoutePreview preview = new CatRoutePreview
            {
                collided = false,
                collisionStep = -1,
                closestDistance = int.MaxValue,
                catPositions = new List<GridPosition>()
            };

            if (level == null ||
                level.cat == null ||
                !level.cat.IsEnabled ||
                dustBotRoute == null ||
                dustBotRoute.Count == 0)
            {
                preview.closestDistance = -1;
                return preview;
            }

            GridPosition catPosition = level.cat.startPosition;
            preview.catPositions.Add(catPosition);
            preview.closestDistance = Distance(catPosition, dustBotRoute[0]);
            for (int step = 1; step < dustBotRoute.Count; step++)
            {
                CatStepResult result = Advance(
                    level,
                    catPosition,
                    dustBotRoute[step],
                    step);
                catPosition = result.to;
                preview.catPositions.Add(catPosition);
                preview.closestDistance = Math.Min(
                    preview.closestDistance,
                    Distance(catPosition, dustBotRoute[step]));
                if (result.collided)
                {
                    preview.collided = true;
                    preview.collisionStep = step;
                    return preview;
                }
            }

            return preview;
        }

        public static List<GridPosition> BuildExpectedRoute(LevelDefinition level)
        {
            List<GridPosition> route = new List<GridPosition>();
            if (level == null)
            {
                return route;
            }

            GridPosition current = level.Find(CellContent.Start);
            route.Add(current);
            for (int i = 0; i < level.expectedSolution.Count; i++)
            {
                current += DirectionUtility.ToOffset(level.expectedSolution[i].direction);
                route.Add(current);
            }

            return route;
        }

        public static CatRelevanceReport AnalyzeRelevance(
            LevelDefinition level,
            IList<GridPosition> dustBotRoute)
        {
            CatRelevanceReport report = new CatRelevanceReport
            {
                closestDistance = -1,
                distanceToBotStart = int.MaxValue,
                distanceToRoute = int.MaxValue
            };
            if (level == null ||
                level.cat == null ||
                !level.cat.IsEnabled ||
                dustBotRoute == null ||
                dustBotRoute.Count == 0)
            {
                return report;
            }

            for (int i = 0; i < CardinalOffsets.Length; i++)
            {
                GridPosition neighbor = level.cat.startPosition + CardinalOffsets[i];
                if (CanCatEnter(level, neighbor))
                {
                    report.openStartNeighbors++;
                }
            }

            Dictionary<GridPosition, int> reachable =
                BuildReachableDistances(level, level.cat.startPosition);
            report.reachableTiles = reachable.Count;
            GridPosition botStart = level.Find(CellContent.Start);
            int botDistance;
            if (reachable.TryGetValue(botStart, out botDistance))
            {
                report.sameConnectedRegion = true;
                report.distanceToBotStart = botDistance;
            }

            for (int i = 0; i < dustBotRoute.Count; i++)
            {
                int routeDistance;
                if (reachable.TryGetValue(dustBotRoute[i], out routeDistance))
                {
                    report.reachableRouteTiles++;
                    report.distanceToRoute = Math.Min(report.distanceToRoute, routeDistance);
                }
            }

            CatRoutePreview preview = SimulateRoute(level, dustBotRoute);
            report.closestDistance = preview.closestDistance;
            HashSet<GridPosition> unique = new HashSet<GridPosition>();
            for (int i = 0; i < preview.catPositions.Count; i++)
            {
                GridPosition catPosition = preview.catPositions[i];
                unique.Add(catPosition);
                if (i > 0 && catPosition != preview.catPositions[i - 1])
                {
                    report.movementTurns++;
                }

                int routeIndex = Math.Min(i, dustBotRoute.Count - 1);
                if (Distance(catPosition, dustBotRoute[routeIndex]) <= 3)
                {
                    report.pressureTurns++;
                }
            }

            report.uniqueVisitedTiles = unique.Count;
            return report;
        }

        private static int MovesForTurn(CatBehavior behavior, int dustBotStep)
        {
            switch (behavior)
            {
                case CatBehavior.Sleepy:
                    return 1;
                case CatBehavior.Curious:
                    return 2;
                case CatBehavior.Pouncy:
                    return 2;
                default:
                    return 0;
            }
        }

        private static GridPosition ChooseNext(
            LevelDefinition level,
            GridPosition current,
            GridPosition target,
            bool horizontalFirst)
        {
            int dx = target.x - current.x;
            int dy = target.y - current.y;
            GridPosition horizontal = new GridPosition(Math.Sign(dx), 0);
            GridPosition vertical = new GridPosition(0, Math.Sign(dy));

            GridPosition next;
            if (TryAdvance(level, current, target, horizontal, out next))
            {
                return next;
            }

            if (TryAdvance(level, current, target, vertical, out next))
            {
                return next;
            }

            return current;
        }

        private static bool TryAdvance(
            LevelDefinition level,
            GridPosition current,
            GridPosition target,
            GridPosition offset,
            out GridPosition next)
        {
            next = current;
            if (offset.x == 0 && offset.y == 0)
            {
                return false;
            }

            GridPosition candidate = current + offset;
            if (!level.IsInside(candidate) ||
                IsFurniture(level.GetContent(candidate)) ||
                Distance(candidate, target) >= Distance(current, target))
            {
                return false;
            }

            next = candidate;
            return true;
        }

        private static readonly GridPosition[] CardinalOffsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        private static bool CanCatEnter(LevelDefinition level, GridPosition position)
        {
            return level.IsInside(position) && !IsFurniture(level.GetContent(position));
        }

        public static bool SharesWalkableRegionWithDustBot(LevelDefinition level)
        {
            if (level == null ||
                level.cat == null ||
                !level.cat.IsEnabled)
            {
                return false;
            }

            Dictionary<GridPosition, int> reachable =
                BuildReachableDistances(level, level.Find(CellContent.Start));
            return reachable.ContainsKey(level.cat.startPosition);
        }

        private static Dictionary<GridPosition, int> BuildReachableDistances(
            LevelDefinition level,
            GridPosition start)
        {
            Dictionary<GridPosition, int> distances =
                new Dictionary<GridPosition, int>();
            if (!CanCatEnter(level, start))
            {
                return distances;
            }

            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(start);
            distances[start] = 0;
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                for (int i = 0; i < CardinalOffsets.Length; i++)
                {
                    GridPosition next = current + CardinalOffsets[i];
                    if (CanCatEnter(level, next) && !distances.ContainsKey(next))
                    {
                        distances[next] = distances[current] + 1;
                        frontier.Enqueue(next);
                    }
                }
            }

            return distances;
        }

        private static bool IsFurniture(CellContent content)
        {
            return CellContentUtility.IsCatBlocker(content);
        }

        private static int Distance(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }
    }
}
