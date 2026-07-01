using System;
using System.Collections.Generic;

namespace DustBot
{
    public struct LargeMazeComplexityReport
    {
        public int score;
        public int openCells;
        public int corridorCells;
        public int branches;
        public int deadEnds;
        public int loops;
        public int chokepoints;
        public int rooms;
        public int decoyPaths;
        public int routeTurns;
        public int averageCrumbDistance;
        public int finalCrumbToDock;
        public int optimalCompletionCost;
        public int threeStarRouteGap;
        public int bonusDetourCost;
        public int pathCostPressure;
        public int openPercent;
        public int playableEdgeCells;
        public int blockedEdgeCells;
        public bool fullBlockedPerimeter;
        public bool allObjectivesReachable;
        public bool tooOpen;
        public bool tooLinear;
        public bool tooTedious;
    }

    public static class LargeMazeEvaluator
    {
        private static readonly GridPosition[] Offsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        private struct HeapNode
        {
            public int state;
            public int cost;

            public HeapNode(int state, int cost)
            {
                this.state = state;
                this.cost = cost;
            }
        }

        private sealed class MinHeap
        {
            private readonly List<HeapNode> nodes = new List<HeapNode>();

            public int Count { get { return nodes.Count; } }

            public void Push(HeapNode node)
            {
                nodes.Add(node);
                int index = nodes.Count - 1;
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (nodes[parent].cost <= node.cost)
                    {
                        break;
                    }

                    nodes[index] = nodes[parent];
                    index = parent;
                }

                nodes[index] = node;
            }

            public HeapNode Pop()
            {
                HeapNode result = nodes[0];
                HeapNode tail = nodes[nodes.Count - 1];
                nodes.RemoveAt(nodes.Count - 1);
                if (nodes.Count == 0)
                {
                    return result;
                }

                int index = 0;
                while (true)
                {
                    int left = index * 2 + 1;
                    if (left >= nodes.Count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int child = right < nodes.Count && nodes[right].cost < nodes[left].cost
                        ? right
                        : left;
                    if (nodes[child].cost >= tail.cost)
                    {
                        break;
                    }

                    nodes[index] = nodes[child];
                    index = child;
                }

                nodes[index] = tail;
                return result;
            }
        }

        public static LargeMazeComplexityReport Analyze(LevelDefinition level)
        {
            int area = Math.Max(1, level.width * level.height);
            int[,] degrees = new int[level.width, level.height];
            int openCells = 0;
            int edges = 0;
            int corridorCells = 0;
            int branches = 0;
            int deadEnds = 0;
            int playableEdgeCells = 0;
            int blockedEdgeCells = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    if (!IsWalkable(level, position))
                    {
                        if (IsEdge(level, position)) blockedEdgeCells++;
                        continue;
                    }

                    openCells++;
                    if (IsEdge(level, position)) playableEdgeCells++;
                    int degree = CountWalkableNeighbors(level, position);
                    degrees[x, y] = degree;
                    edges += degree;
                    if (degree == 1) deadEnds++;
                    if (degree == 2) corridorCells++;
                    if (degree >= 3) branches++;
                }
            }

            edges /= 2;
            int components = CountComponents(level);
            int loops = Math.Max(0, edges - openCells + components);
            int chokepoints = CountArticulationPoints(level);
            int rooms = CountRoomSections(level, degrees);
            int decoys = CountDecoyPaths(level, degrees);
            List<GridPosition> route = BuildExpectedRoute(level);
            int routeTurns = CountTurns(route);
            int averageCrumbDistance = AverageCrumbDistance(level);
            int finalCrumbToDock = FinalCrumbToDock(level, route);
            bool reachable = ObjectivesReachable(level);
            int optimalCost = FindOptimalCompletionCost(level);
            int routeGap = optimalCost == int.MaxValue
                ? 0
                : Math.Max(0, level.parMoves - optimalCost);
            int bonusDetour = level.objectives.collectBonus ? routeGap : 0;
            int pathCostPressure = level.hardPathLimit && level.moveLimit > 0
                ? Math.Max(0, 12 - Math.Max(0, level.moveLimit - level.parMoves) * 2)
                : Math.Max(0, 5 - Math.Max(0, level.twoStarMoveTarget - level.parMoves));
            int openPercent = openCells * 100 / area;
            bool tooOpen = openPercent > 72;
            bool tooLinear = branches < Math.Max(3, area / 55) ||
                             deadEnds < Math.Max(3, area / 75) ||
                             loops < Math.Max(1, area / 130);
            bool tooTedious = route.Count > Math.Max(40, openCells * 9 / 10) ||
                              level.Count(CellContent.Crumb) > 9;

            int score =
                Math.Min(20, area / 12) +
                Math.Min(24, routeTurns / 2) +
                Math.Min(24, branches * 2) +
                Math.Min(18, deadEnds * 2) +
                Math.Min(24, loops * 4) +
                Math.Min(18, chokepoints) +
                Math.Min(12, rooms * 3) +
                Math.Min(18, decoys * 3) +
                Math.Min(12, averageCrumbDistance) +
                Math.Min(10, finalCrumbToDock / 2) +
                Math.Min(10, bonusDetour * 3) +
                Math.Min(12, pathCostPressure);

            return new LargeMazeComplexityReport
            {
                score = score,
                openCells = openCells,
                corridorCells = corridorCells,
                branches = branches,
                deadEnds = deadEnds,
                loops = loops,
                chokepoints = chokepoints,
                rooms = rooms,
                decoyPaths = decoys,
                routeTurns = routeTurns,
                averageCrumbDistance = averageCrumbDistance,
                finalCrumbToDock = finalCrumbToDock,
                optimalCompletionCost = optimalCost,
                threeStarRouteGap = routeGap,
                bonusDetourCost = bonusDetour,
                pathCostPressure = pathCostPressure,
                openPercent = openPercent,
                playableEdgeCells = playableEdgeCells,
                blockedEdgeCells = blockedEdgeCells,
                fullBlockedPerimeter = playableEdgeCells == 0,
                allObjectivesReachable = reachable && optimalCost != int.MaxValue,
                tooOpen = tooOpen,
                tooLinear = tooLinear,
                tooTedious = tooTedious
            };
        }

        public static bool MeetsRequirements(
            LevelDefinition level,
            LevelGenerationSettings settings,
            out LargeMazeComplexityReport report,
            out string reason)
        {
            report = Analyze(level);
            if (!report.allObjectivesReachable)
            {
                reason = "not all crumbs and the dock are reachable";
                return false;
            }

            if (report.tooOpen)
            {
                reason = "the maze is too open (" + report.openPercent + "% floor)";
                return false;
            }

            if (report.tooLinear)
            {
                reason = "the maze is too linear";
                return false;
            }

            if (report.tooTedious)
            {
                reason = "the required route is too long for the usable maze area";
                return false;
            }

            if (report.score < settings.minimumMazeComplexityScore ||
                report.branches < settings.minimumMazeBranches ||
                report.deadEnds < settings.minimumMazeDeadEnds ||
                report.loops < settings.minimumMazeLoops)
            {
                reason = string.Format(
                    "maze complexity {0}/{1}, branches {2}/{3}, dead ends {4}/{5}, loops {6}/{7}",
                    report.score,
                    settings.minimumMazeComplexityScore,
                    report.branches,
                    settings.minimumMazeBranches,
                    report.deadEnds,
                    settings.minimumMazeDeadEnds,
                    report.loops,
                    settings.minimumMazeLoops);
                return false;
            }

            if (level.objectives.collectBonus && report.bonusDetourCost < 2)
            {
                reason = "the Dust Bunny is not on a meaningful optional detour";
                return false;
            }

            reason = "Valid";
            return true;
        }

        public static bool ObjectivesReachable(LevelDefinition level)
        {
            GridPosition start = level.Find(CellContent.Start);
            if (!level.IsInside(start))
            {
                return false;
            }

            bool[,] visited = new bool[level.width, level.height];
            Queue<GridPosition> frontier = new Queue<GridPosition>();
            frontier.Enqueue(start);
            visited[start.x, start.y] = true;
            while (frontier.Count > 0)
            {
                GridPosition current = frontier.Dequeue();
                for (int i = 0; i < Offsets.Length; i++)
                {
                    GridPosition next = current + Offsets[i];
                    Direction direction = DirectionUtility.Between(current, next);
                    if (!level.IsInside(next) ||
                        visited[next.x, next.y] ||
                        !IsWalkable(level, next) ||
                        !CellContentUtility.AllowsDirection(
                            level.GetContent(current),
                            level.GetContent(next),
                            direction))
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    frontier.Enqueue(next);
                }
            }

            for (int i = 0; i < level.cells.Count; i++)
            {
                CellContent content = level.cells[i].content;
                if ((content == CellContent.Crumb || content == CellContent.Dock) &&
                    !visited[level.cells[i].position.x, level.cells[i].position.y])
                {
                    return false;
                }
            }

            return !level.objectives.collectBonus ||
                   (level.IsInside(level.bonusPosition) &&
                    visited[level.bonusPosition.x, level.bonusPosition.y]);
        }

        private static int FindOptimalCompletionCost(LevelDefinition level)
        {
            CellContent[,] contents = BuildContentMap(level);
            List<GridPosition> crumbs = new List<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == CellContent.Crumb)
                {
                    crumbs.Add(level.cells[i].position);
                }
            }

            if (crumbs.Count > 12)
            {
                return int.MaxValue;
            }

            int area = level.width * level.height;
            int maskCount = 1 << crumbs.Count;
            int[] distances = new int[area * maskCount];
            for (int i = 0; i < distances.Length; i++) distances[i] = int.MaxValue;
            int[,] crumbBits = new int[level.width, level.height];
            for (int i = 0; i < crumbs.Count; i++)
            {
                crumbBits[crumbs[i].x, crumbs[i].y] = 1 << i;
            }

            GridPosition start = level.Find(CellContent.Start);
            GridPosition dock = level.Find(CellContent.Dock);
            int startCell = start.y * level.width + start.x;
            int startMask = crumbBits[start.x, start.y];
            int startState = startMask * area + startCell;
            distances[startState] = 0;
            MinHeap heap = new MinHeap();
            heap.Push(new HeapNode(startState, 0));
            int allMask = maskCount - 1;
            while (heap.Count > 0)
            {
                HeapNode node = heap.Pop();
                if (node.cost != distances[node.state])
                {
                    continue;
                }

                int mask = node.state / area;
                int cell = node.state % area;
                GridPosition current = new GridPosition(cell % level.width, cell / level.width);
                if (mask == allMask && current == dock)
                {
                    return node.cost;
                }

                for (int i = 0; i < Offsets.Length; i++)
                {
                    GridPosition next = current + Offsets[i];
                    Direction direction = DirectionUtility.Between(current, next);
                    if (!level.IsInside(next) ||
                        !CellContentUtility.IsWalkableFloor(contents[next.x, next.y]) ||
                        !CellContentUtility.AllowsDirection(
                            contents[current.x, current.y],
                            contents[next.x, next.y],
                            direction))
                    {
                        continue;
                    }

                    int nextMask = mask | crumbBits[next.x, next.y];
                    int nextCell = next.y * level.width + next.x;
                    int nextState = nextMask * area + nextCell;
                    int nextCost = node.cost + CellContentUtility.MoveCost(contents[next.x, next.y]);
                    if (nextCost >= distances[nextState])
                    {
                        continue;
                    }

                    distances[nextState] = nextCost;
                    heap.Push(new HeapNode(nextState, nextCost));
                }
            }

            return int.MaxValue;
        }

        private static CellContent[,] BuildContentMap(LevelDefinition level)
        {
            CellContent[,] contents = new CellContent[level.width, level.height];
            for (int i = 0; i < level.cells.Count; i++)
            {
                GridCellDefinition cell = level.cells[i];
                if (cell != null && level.IsInside(cell.position))
                {
                    contents[cell.position.x, cell.position.y] = cell.content;
                }
            }

            return contents;
        }

        private static int CountComponents(LevelDefinition level)
        {
            bool[,] visited = new bool[level.width, level.height];
            int components = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition start = new GridPosition(x, y);
                    if (visited[x, y] || !IsWalkable(level, start)) continue;
                    components++;
                    Queue<GridPosition> queue = new Queue<GridPosition>();
                    queue.Enqueue(start);
                    visited[x, y] = true;
                    while (queue.Count > 0)
                    {
                        GridPosition current = queue.Dequeue();
                        for (int i = 0; i < Offsets.Length; i++)
                        {
                            GridPosition next = current + Offsets[i];
                            if (level.IsInside(next) &&
                                !visited[next.x, next.y] &&
                                IsWalkable(level, next))
                            {
                                visited[next.x, next.y] = true;
                                queue.Enqueue(next);
                            }
                        }
                    }
                }
            }

            return components;
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
                    if (discovery[x, y] == 0 && IsWalkable(level, position))
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
                if (!level.IsInside(next) || !IsWalkable(level, next)) continue;
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
                    if (parent.x < 0 && children > 1) articulation[current.x, current.y] = true;
                    if (parent.x >= 0 && low[next.x, next.y] >= discovery[current.x, current.y])
                        articulation[current.x, current.y] = true;
                }
                else if (next != parent)
                {
                    low[current.x, current.y] = Math.Min(
                        low[current.x, current.y],
                        discovery[next.x, next.y]);
                }
            }
        }

        private static int CountRoomSections(LevelDefinition level, int[,] degrees)
        {
            bool[,] visited = new bool[level.width, level.height];
            int rooms = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    if (visited[x, y] || degrees[x, y] < 3) continue;
                    int size = 0;
                    Queue<GridPosition> queue = new Queue<GridPosition>();
                    queue.Enqueue(new GridPosition(x, y));
                    visited[x, y] = true;
                    while (queue.Count > 0)
                    {
                        GridPosition current = queue.Dequeue();
                        size++;
                        for (int i = 0; i < Offsets.Length; i++)
                        {
                            GridPosition next = current + Offsets[i];
                            if (level.IsInside(next) &&
                                !visited[next.x, next.y] &&
                                degrees[next.x, next.y] >= 3)
                            {
                                visited[next.x, next.y] = true;
                                queue.Enqueue(next);
                            }
                        }
                    }

                    if (size >= 2) rooms++;
                }
            }

            return rooms;
        }

        private static int CountDecoyPaths(LevelDefinition level, int[,] degrees)
        {
            HashSet<GridPosition> landmarks = new HashSet<GridPosition>();
            landmarks.Add(level.Find(CellContent.Start));
            landmarks.Add(level.Find(CellContent.Dock));
            if (level.objectives.collectBonus) landmarks.Add(level.bonusPosition);
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == CellContent.Crumb)
                    landmarks.Add(level.cells[i].position);
            }

            int decoys = 0;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition end = new GridPosition(x, y);
                    if (degrees[x, y] != 1 || landmarks.Contains(end)) continue;
                    GridPosition current = end;
                    GridPosition previous = new GridPosition(-1, -1);
                    int length = 0;
                    while (length <= level.width + level.height)
                    {
                        length++;
                        GridPosition next = new GridPosition(-1, -1);
                        for (int i = 0; i < Offsets.Length; i++)
                        {
                            GridPosition candidate = current + Offsets[i];
                            if (level.IsInside(candidate) &&
                                candidate != previous &&
                                IsWalkable(level, candidate))
                            {
                                next = candidate;
                                break;
                            }
                        }

                        if (!level.IsInside(next) || degrees[next.x, next.y] != 2) break;
                        previous = current;
                        current = next;
                    }

                    if (length >= 2) decoys++;
                }
            }

            return decoys;
        }

        private static int AverageCrumbDistance(LevelDefinition level)
        {
            List<GridPosition> crumbs = new List<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == CellContent.Crumb)
                    crumbs.Add(level.cells[i].position);
            }

            if (crumbs.Count < 2) return 0;
            int total = 0;
            int pairs = 0;
            for (int i = 0; i < crumbs.Count; i++)
            {
                for (int j = i + 1; j < crumbs.Count; j++)
                {
                    int distance = ShortestDistance(level, crumbs[i], crumbs[j]);
                    if (distance < int.MaxValue)
                    {
                        total += distance;
                        pairs++;
                    }
                }
            }

            return pairs == 0 ? 0 : total / pairs;
        }

        private static int ShortestDistance(
            LevelDefinition level,
            GridPosition start,
            GridPosition target)
        {
            int[,] distances = new int[level.width, level.height];
            for (int y = 0; y < level.height; y++)
                for (int x = 0; x < level.width; x++) distances[x, y] = -1;
            Queue<GridPosition> queue = new Queue<GridPosition>();
            queue.Enqueue(start);
            distances[start.x, start.y] = 0;
            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();
                if (current == target) return distances[current.x, current.y];
                for (int i = 0; i < Offsets.Length; i++)
                {
                    GridPosition next = current + Offsets[i];
                    if (level.IsInside(next) &&
                        distances[next.x, next.y] < 0 &&
                        IsWalkable(level, next))
                    {
                        distances[next.x, next.y] = distances[current.x, current.y] + 1;
                        queue.Enqueue(next);
                    }
                }
            }

            return int.MaxValue;
        }

        private static int FinalCrumbToDock(
            LevelDefinition level,
            List<GridPosition> route)
        {
            HashSet<GridPosition> crumbs = new HashSet<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
                if (level.cells[i].content == CellContent.Crumb) crumbs.Add(level.cells[i].position);
            int finalIndex = 0;
            for (int i = 0; i < route.Count; i++)
                if (crumbs.Contains(route[i])) finalIndex = i;
            return Math.Max(0, route.Count - 1 - finalIndex);
        }

        private static List<GridPosition> BuildExpectedRoute(LevelDefinition level)
        {
            List<GridPosition> route = new List<GridPosition>();
            GridPosition current = level.Find(CellContent.Start);
            route.Add(current);
            for (int i = 0; i < level.expectedSolution.Count; i++)
            {
                current += DirectionUtility.ToOffset(level.expectedSolution[i].direction);
                route.Add(current);
            }

            return route;
        }

        private static int CountTurns(List<GridPosition> route)
        {
            int turns = 0;
            Direction previous = Direction.None;
            for (int i = 1; i < route.Count; i++)
            {
                Direction current = DirectionUtility.Between(route[i - 1], route[i]);
                if (previous != Direction.None && current != previous) turns++;
                previous = current;
            }

            return turns;
        }

        private static int CountWalkableNeighbors(LevelDefinition level, GridPosition position)
        {
            int count = 0;
            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition next = position + Offsets[i];
                if (level.IsInside(next) && IsWalkable(level, next)) count++;
            }

            return count;
        }

        private static bool IsWalkable(LevelDefinition level, GridPosition position)
        {
            return level.IsInside(position) &&
                   CellContentUtility.IsWalkableFloor(level.GetContent(position));
        }

        private static bool IsEdge(LevelDefinition level, GridPosition position)
        {
            return position.x == 0 || position.y == 0 ||
                   position.x == level.width - 1 ||
                   position.y == level.height - 1;
        }
    }
}
