using System;
using System.Collections.Generic;
using System.Globalization;

namespace DustBot
{
    public static class AdvancedDevMazeGenerator
    {
        private static readonly GridPosition[] Offsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        private sealed class Profile
        {
            public int crumbCount;
            public int requiredDetours;
            public int detourDepth;
            public int rooms;
            public int loops;
            public int modifierCount;
            public bool includeBonus;
            public int bonusDepth;
            public bool hardLimit;
            public RouteModifierStyle modifierStyle;
        }

        public static LevelDefinition Build(
            LevelManifestEntry entry,
            GameMode mode,
            int candidate)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            if (entry.generationMode == GenerationMode.ProductionCampaign ||
                !entry.useAdvancedDevMaze)
            {
                throw new InvalidOperationException(
                    "Advanced maze generation is restricted to explicit development profiles.");
            }

            Profile profile = CreateProfile(entry);
            DeterministicRandom random = new DeterministicRandom(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}_advanced_dev_maze_v1_{1}",
                    entry.seed,
                    candidate));
            bool[,] floor = CarveDepthFirstMaze(
                entry.boardWidth,
                entry.boardHeight,
                random);
            CarvePlayableEdgeCorridors(floor, random);
            List<GridPosition> solutionRoute = FindMazeDiameter(floor);
            HashSet<GridPosition> reservedDetourCells = new HashSet<GridPosition>();
            List<GridPosition> requiredDetourMarkers = new List<GridPosition>();
            int maximumDetourIndex = Math.Max(4, solutionRoute.Count * 3 / 4);
            for (int i = 0; i < profile.requiredDetours; i++)
            {
                GridPosition marker;
                if (TryInsertParallelDetour(
                        floor,
                        solutionRoute,
                        random,
                        profile.detourDepth,
                        3,
                        maximumDetourIndex,
                        reservedDetourCells,
                        out marker))
                {
                    requiredDetourMarkers.Add(marker);
                    maximumDetourIndex = Math.Max(4, solutionRoute.Count * 3 / 4);
                }
            }

            GridPosition bonusPosition = new GridPosition(-1, -1);
            bool hasBonus = profile.includeBonus &&
                            TryInsertParallelDetour(
                                floor,
                                solutionRoute,
                                random,
                                profile.bonusDepth,
                                Math.Max(4, solutionRoute.Count / 4),
                                Math.Max(5, solutionRoute.Count * 4 / 5),
                                reservedDetourCells,
                                out bonusPosition);

            CarveRooms(floor, profile.rooms, random);
            CarveLoops(floor, profile.loops, random);

            GridPosition stickyShortcut = new GridPosition(-1, -1);
            if (entry.devMazeArchetype == DevMazeArchetype.StickyShortcut ||
                entry.devMazeArchetype == DevMazeArchetype.ExpertLarge)
            {
                TryCarveStickyShortcut(
                    floor,
                    solutionRoute,
                    random,
                    out stickyShortcut);
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
                hardPathLimit = profile.hardLimit,
                archetype = entry.archetype,
                testArchetype = entry.testArchetype,
                dailyChallengeStyle = entry.dailyChallengeStyle,
                masterCleanStyle = entry.masterCleanStyle,
                largeMaze = true,
                advancedDevMaze = true,
                devMazeArchetype = entry.devMazeArchetype,
                themeId = entry.themeId,
                mechanicSet = "AdvancedDevMazePath",
                objectiveSet = "StrategicCrumbOrderAndDock",
                cat = new CatDefinition { behavior = CatBehavior.None }
            };
            level.objectives.noHintStar = true;
            level.objectives.noUndoStar = false;

            HashSet<GridPosition> occupied = new HashSet<GridPosition>();
            AddMazeWalls(level, floor, solutionRoute, random, occupied);
            AddCell(level, occupied, solutionRoute[0], CellContent.Start);
            AddCell(
                level,
                occupied,
                solutionRoute[solutionRoute.Count - 1],
                CellContent.Dock);

            if (hasBonus)
            {
                level.objectives.collectBonus = true;
                level.objectives.bonusRequiredForThreeStars = true;
                level.bonusPosition = bonusPosition;
            }

            List<int> crumbIndices = PickCrumbIndices(
                solutionRoute,
                requiredDetourMarkers,
                level.bonusPosition,
                profile.crumbCount,
                entry.devMazeArchetype);
            for (int i = 0; i < crumbIndices.Count; i++)
            {
                AddCell(
                    level,
                    occupied,
                    solutionRoute[crumbIndices[i]],
                    CellContent.Crumb);
            }

            if (level.IsInside(stickyShortcut))
            {
                AddCell(level, occupied, stickyShortcut, CellContent.Sticky);
            }

            PlaceRouteModifiers(
                level,
                solutionRoute,
                profile,
                random,
                occupied);
            for (int i = 0; i < solutionRoute.Count - 1; i++)
            {
                level.expectedSolution.Add(new SolutionStep(
                    solutionRoute[i],
                    DirectionUtility.Between(
                        solutionRoute[i],
                        solutionRoute[i + 1])));
            }

            ApplyStrictTargets(level, solutionRoute, profile);
            return level;
        }

        private static Profile CreateProfile(LevelManifestEntry entry)
        {
            int area = entry.boardWidth * entry.boardHeight;
            int tier = (int)entry.difficultyTier;
            Profile profile = new Profile
            {
                crumbCount = tier >= (int)DifficultyTier.Master
                    ? 9
                    : tier >= (int)DifficultyTier.Expert
                        ? 8
                        : tier >= (int)DifficultyTier.Hard ? 7 : 5,
                requiredDetours = tier >= (int)DifficultyTier.Master
                    ? 5
                    : tier >= (int)DifficultyTier.Expert ? 4 : 3,
                detourDepth = tier >= (int)DifficultyTier.Expert ? 2 : 1,
                rooms = Math.Max(1, area / 150),
                loops = Math.Max(3, area / 70),
                modifierCount = Math.Max(3, Math.Min(10, area / 55)),
                includeBonus = entry.devMazeArchetype == DevMazeArchetype.DustBunnyDetour ||
                               entry.devMazeArchetype == DevMazeArchetype.ExpertLarge ||
                               entry.levelNumber % 3 == 0,
                bonusDepth = tier >= (int)DifficultyTier.Expert ? 2 : 1,
                hardLimit = tier >= (int)DifficultyTier.Hard,
                modifierStyle = RouteModifierStyle.Mixed
            };

            switch (entry.devMazeArchetype)
            {
                case DevMazeArchetype.DeadEndBranch:
                    profile.rooms = 0;
                    profile.loops = Math.Max(2, area / 110);
                    profile.requiredDetours++;
                    break;
                case DevMazeArchetype.MultiRoom:
                    profile.rooms = Math.Max(3, area / 85);
                    profile.loops = Math.Max(3, area / 90);
                    break;
                case DevMazeArchetype.DockReturn:
                    profile.requiredDetours++;
                    profile.loops = Math.Max(3, area / 95);
                    break;
                case DevMazeArchetype.DustBunnyDetour:
                    profile.includeBonus = true;
                    profile.bonusDepth = tier >= (int)DifficultyTier.Expert ? 3 : 2;
                    break;
                case DevMazeArchetype.OneWayCommitment:
                    profile.modifierStyle = RouteModifierStyle.OneWay;
                    profile.modifierCount += 2;
                    break;
                case DevMazeArchetype.StickyShortcut:
                    profile.modifierStyle = RouteModifierStyle.Sticky;
                    profile.modifierCount += 2;
                    profile.loops += 2;
                    break;
                case DevMazeArchetype.FragileCorridor:
                    profile.modifierStyle = RouteModifierStyle.Fragile;
                    profile.modifierCount += 2;
                    profile.loops = Math.Max(2, area / 110);
                    break;
                case DevMazeArchetype.Loop:
                    profile.loops = Math.Max(6, area / 45);
                    break;
                case DevMazeArchetype.Chokepoint:
                    profile.rooms = Math.Max(2, area / 110);
                    profile.loops = Math.Max(2, area / 120);
                    break;
                case DevMazeArchetype.ExpertLarge:
                    profile.rooms = Math.Max(4, area / 80);
                    profile.loops = Math.Max(7, area / 48);
                    profile.requiredDetours++;
                    profile.includeBonus = true;
                    profile.modifierCount = Math.Max(profile.modifierCount, 8);
                    break;
            }

            return profile;
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
                Math.Min(width - 2, 1 + random.Range(0, nodeColumns) * 2),
                Math.Min(height - 2, 1 + random.Range(0, nodeRows) * 2));
            bool[,] visited = new bool[width, height];
            List<GridPosition> stack = new List<GridPosition> { start };
            floor[start.x, start.y] = true;
            visited[start.x, start.y] = true;
            while (stack.Count > 0)
            {
                GridPosition current = stack[stack.Count - 1];
                List<GridPosition> candidates = new List<GridPosition>();
                for (int i = 0; i < Offsets.Length; i++)
                {
                    GridPosition next = new GridPosition(
                        current.x + Offsets[i].x * 2,
                        current.y + Offsets[i].y * 2);
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
            GridPosition endA = FindFarthest(floor, first, out parents);
            GridPosition endB = FindFarthest(floor, endA, out parents);
            List<GridPosition> reversed = new List<GridPosition>();
            GridPosition current = endB;
            reversed.Add(current);
            while (current != endA)
            {
                current = parents[current.x, current.y];
                if (current.x < 0)
                {
                    break;
                }

                reversed.Add(current);
            }

            reversed.Reverse();
            return reversed;
        }

        private static void CarvePlayableEdgeCorridors(
            bool[,] floor,
            DeterministicRandom random)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            int rightSource = width - 2;
            int topSource = height - 2;
            if ((rightSource & 1) == 0) rightSource--;
            if ((topSource & 1) == 0) topSource--;
            List<GridPosition>[] sides =
            {
                new List<GridPosition>(),
                new List<GridPosition>(),
                new List<GridPosition>(),
                new List<GridPosition>()
            };

            for (int x = 1; x < width - 1; x++)
            {
                if (floor[x, 1]) sides[0].Add(new GridPosition(x, 0));
                if (floor[x, topSource]) sides[2].Add(new GridPosition(x, height - 1));
            }

            for (int y = 1; y < height - 1; y++)
            {
                if (floor[1, y]) sides[3].Add(new GridPosition(0, y));
                if (floor[rightSource, y]) sides[1].Add(new GridPosition(width - 1, y));
            }

            for (int side = 0; side < sides.Length; side++)
            {
                List<GridPosition> candidates = sides[side];
                random.Shuffle(candidates);
                int target = Math.Min(
                    candidates.Count,
                    Math.Max(2, candidates.Count / 3));
                int opened = 0;
                for (int i = 0; i < candidates.Count && opened < target; i++)
                {
                    GridPosition position = candidates[i];
                    if (HasAdjacentOpenEdgeCell(floor, position))
                    {
                        continue;
                    }

                    CarveEdgeConnection(floor, position);
                    opened++;
                }

                for (int i = 0; i < candidates.Count && opened < target; i++)
                {
                    GridPosition position = candidates[i];
                    if (floor[position.x, position.y])
                    {
                        continue;
                    }

                    CarveEdgeConnection(floor, position);
                    opened++;
                }
            }
        }

        private static void CarveEdgeConnection(
            bool[,] floor,
            GridPosition edge)
        {
            if (edge.x == 0)
            {
                floor[0, edge.y] = true;
                floor[1, edge.y] = true;
                return;
            }

            if (edge.x == floor.GetLength(0) - 1)
            {
                for (int x = edge.x; x >= Math.Max(0, edge.x - 2); x--)
                {
                    floor[x, edge.y] = true;
                }
                return;
            }

            if (edge.y == 0)
            {
                floor[edge.x, 0] = true;
                floor[edge.x, 1] = true;
                return;
            }

            for (int y = edge.y; y >= Math.Max(0, edge.y - 2); y--)
            {
                floor[edge.x, y] = true;
            }
        }

        private static bool HasAdjacentOpenEdgeCell(
            bool[,] floor,
            GridPosition position)
        {
            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition neighbor = position + Offsets[i];
                if (IsInside(floor, neighbor) &&
                    IsEdge(floor, neighbor) &&
                    floor[neighbor.x, neighbor.y])
                {
                    return true;
                }
            }

            return false;
        }

        private static GridPosition FindFarthest(
            bool[,] floor,
            GridPosition start,
            out GridPosition[,] parents)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            int[,] distance = new int[width, height];
            parents = new GridPosition[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    distance[x, y] = -1;
                    parents[x, y] = new GridPosition(-1, -1);
                }
            }

            Queue<GridPosition> queue = new Queue<GridPosition>();
            queue.Enqueue(start);
            distance[start.x, start.y] = 0;
            GridPosition farthest = start;
            while (queue.Count > 0)
            {
                GridPosition current = queue.Dequeue();
                if (distance[current.x, current.y] > distance[farthest.x, farthest.y])
                {
                    farthest = current;
                }

                for (int i = 0; i < Offsets.Length; i++)
                {
                    GridPosition next = current + Offsets[i];
                    if (!IsInside(floor, next) ||
                        !floor[next.x, next.y] ||
                        distance[next.x, next.y] >= 0)
                    {
                        continue;
                    }

                    distance[next.x, next.y] = distance[current.x, current.y] + 1;
                    parents[next.x, next.y] = current;
                    queue.Enqueue(next);
                }
            }

            return farthest;
        }

        private static bool TryInsertParallelDetour(
            bool[,] floor,
            List<GridPosition> route,
            DeterministicRandom random,
            int requestedDepth,
            int minimumIndex,
            int maximumIndex,
            HashSet<GridPosition> reserved,
            out GridPosition marker)
        {
            List<int> indices = new List<int>();
            int maximum = Math.Min(route.Count - 3, maximumIndex);
            for (int i = Math.Max(2, minimumIndex); i <= maximum; i++)
            {
                if (DirectionUtility.Between(route[i], route[i + 1]) != Direction.None)
                {
                    indices.Add(i);
                }
            }

            random.Shuffle(indices);
            for (int depth = Math.Max(1, requestedDepth); depth >= 1; depth--)
            {
                for (int candidate = 0; candidate < indices.Count; candidate++)
                {
                    int index = indices[candidate];
                    GridPosition from = route[index];
                    GridPosition to = route[index + 1];
                    Direction direction = DirectionUtility.Between(from, to);
                    GridPosition[] perpendiculars =
                        direction == Direction.Up || direction == Direction.Down
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
                        GridPosition perpendicular = perpendiculars[side];
                        List<GridPosition> cells = new List<GridPosition>();
                        for (int step = 1; step <= depth; step++)
                        {
                            cells.Add(new GridPosition(
                                from.x + perpendicular.x * step,
                                from.y + perpendicular.y * step));
                        }
                        for (int step = depth; step >= 1; step--)
                        {
                            cells.Add(new GridPosition(
                                to.x + perpendicular.x * step,
                                to.y + perpendicular.y * step));
                        }

                        bool available = true;
                        for (int i = 0; i < cells.Count; i++)
                        {
                            if (!IsInterior(floor, cells[i]) ||
                                floor[cells[i].x, cells[i].y] ||
                                route.Contains(cells[i]) ||
                                reserved.Contains(cells[i]))
                            {
                                available = false;
                                break;
                            }
                        }

                        if (!available)
                        {
                            continue;
                        }

                        for (int i = 0; i < cells.Count; i++)
                        {
                            floor[cells[i].x, cells[i].y] = true;
                            reserved.Add(cells[i]);
                        }

                        route.InsertRange(index + 1, cells);
                        marker = cells[cells.Count / 2];
                        return true;
                    }
                }
            }

            marker = new GridPosition(-1, -1);
            return false;
        }

        private static void CarveRooms(
            bool[,] floor,
            int count,
            DeterministicRandom random)
        {
            int width = floor.GetLength(0);
            int height = floor.GetLength(1);
            for (int room = 0; room < count; room++)
            {
                int roomWidth = Math.Min(width - 2, random.Range(2, 5));
                int roomHeight = Math.Min(height - 2, random.Range(2, 5));
                if (roomWidth < 2 || roomHeight < 2)
                {
                    continue;
                }

                int left = Math.Min(
                    width - roomWidth - 1,
                    random.Range(1, Math.Max(2, width - roomWidth)));
                int bottom = Math.Min(
                    height - roomHeight - 1,
                    random.Range(1, Math.Max(2, height - roomHeight)));
                for (int y = bottom; y < bottom + roomHeight; y++)
                {
                    for (int x = left; x < left + roomWidth; x++)
                    {
                        floor[x, y] = true;
                    }
                }
            }
        }

        private static void CarveLoops(
            bool[,] floor,
            int count,
            DeterministicRandom random)
        {
            List<GridPosition> candidates = new List<GridPosition>();
            for (int y = 1; y < floor.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < floor.GetLength(0) - 1; x++)
                {
                    if (floor[x, y])
                    {
                        continue;
                    }

                    bool horizontal = floor[x - 1, y] && floor[x + 1, y];
                    bool vertical = floor[x, y - 1] && floor[x, y + 1];
                    if (horizontal || vertical)
                    {
                        candidates.Add(new GridPosition(x, y));
                    }
                }
            }

            random.Shuffle(candidates);
            int carved = 0;
            for (int i = 0; i < candidates.Count && carved < count; i++)
            {
                GridPosition position = candidates[i];
                int openNeighbors = CountFloorNeighbors(floor, position);
                bool erasesDeadEnd = false;
                for (int offset = 0; offset < Offsets.Length; offset++)
                {
                    GridPosition neighbor = position + Offsets[offset];
                    if (floor[neighbor.x, neighbor.y] &&
                        CountFloorNeighbors(floor, neighbor) <= 1)
                    {
                        erasesDeadEnd = true;
                    }
                }

                if (openNeighbors == 2 && !erasesDeadEnd)
                {
                    floor[position.x, position.y] = true;
                    carved++;
                }
            }
        }

        private static bool TryCarveStickyShortcut(
            bool[,] floor,
            List<GridPosition> route,
            DeterministicRandom random,
            out GridPosition shortcut)
        {
            Dictionary<GridPosition, int> routeIndices = new Dictionary<GridPosition, int>();
            for (int i = 0; i < route.Count; i++)
            {
                routeIndices[route[i]] = i;
            }

            List<GridPosition> candidates = new List<GridPosition>();
            for (int y = 1; y < floor.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < floor.GetLength(0) - 1; x++)
                {
                    if (floor[x, y])
                    {
                        continue;
                    }

                    GridPosition position = new GridPosition(x, y);
                    List<int> neighborIndices = new List<int>();
                    for (int i = 0; i < Offsets.Length; i++)
                    {
                        int routeIndex;
                        if (routeIndices.TryGetValue(position + Offsets[i], out routeIndex))
                        {
                            neighborIndices.Add(routeIndex);
                        }
                    }

                    for (int left = 0; left < neighborIndices.Count; left++)
                    {
                        for (int right = left + 1; right < neighborIndices.Count; right++)
                        {
                            if (Math.Abs(neighborIndices[left] - neighborIndices[right]) >= 7)
                            {
                                candidates.Add(position);
                                left = neighborIndices.Count;
                                break;
                            }
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                shortcut = new GridPosition(-1, -1);
                return false;
            }

            random.Shuffle(candidates);
            shortcut = candidates[0];
            floor[shortcut.x, shortcut.y] = true;
            return true;
        }

        private static List<int> PickCrumbIndices(
            List<GridPosition> route,
            List<GridPosition> requiredMarkers,
            GridPosition bonusPosition,
            int count,
            DevMazeArchetype archetype)
        {
            List<int> chosen = new List<int>();
            int dockTail = archetype == DevMazeArchetype.DockReturn
                ? Math.Max(10, route.Count / 4)
                : Math.Max(7, route.Count / 8);
            int maximum = Math.Max(3, route.Count - dockTail - 1);
            int minimumGap = Math.Max(3, route.Count / Math.Max(12, count * 3));
            for (int i = 0; i < requiredMarkers.Count && chosen.Count < count; i++)
            {
                int index = route.IndexOf(requiredMarkers[i]);
                if (index >= 2 && index <= maximum && route[index] != bonusPosition)
                {
                    chosen.Add(index);
                }
            }

            int guard = count * 5;
            while (chosen.Count < count && guard-- > 0)
            {
                int target = ((chosen.Count + 1) * maximum) / (count + 1);
                int best = -1;
                int bestDistance = int.MaxValue;
                for (int index = 2; index <= maximum; index++)
                {
                    if (route[index] == bonusPosition || chosen.Contains(index))
                    {
                        continue;
                    }

                    bool spaced = true;
                    for (int i = 0; i < chosen.Count; i++)
                    {
                        if (Math.Abs(chosen[i] - index) < minimumGap)
                        {
                            spaced = false;
                            break;
                        }
                    }

                    if (spaced && Math.Abs(index - target) < bestDistance)
                    {
                        best = index;
                        bestDistance = Math.Abs(index - target);
                    }
                }

                if (best < 0)
                {
                    minimumGap = Math.Max(1, minimumGap - 1);
                    continue;
                }

                chosen.Add(best);
            }

            chosen.Sort();
            return chosen;
        }

        private static void AddMazeWalls(
            LevelDefinition level,
            bool[,] floor,
            List<GridPosition> route,
            DeterministicRandom random,
            HashSet<GridPosition> occupied)
        {
            HashSet<GridPosition> routeSet = new HashSet<GridPosition>(route);
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    if (floor[x, y])
                    {
                        continue;
                    }

                    GridPosition position = new GridPosition(x, y);
                    CellContent content = CellContent.Wall;
                    bool edge = IsEdge(floor, position);
                    int adjacentRoute = 0;
                    for (int i = 0; i < Offsets.Length; i++)
                    {
                        if (routeSet.Contains(position + Offsets[i]))
                        {
                            adjacentRoute++;
                        }
                    }

                    if (!edge && adjacentRoute > 0 && random.Chance(1, 30))
                    {
                        int hazard = random.Range(0, 3);
                        content = hazard == 0
                            ? CellContent.Sock
                            : hazard == 1 ? CellContent.Cord : CellContent.WetSpot;
                    }
                    else if (!edge && random.Chance(1, 15))
                    {
                        content = CellContent.Toy;
                    }

                    AddCell(level, occupied, position, content);
                }
            }
        }

        private static void PlaceRouteModifiers(
            LevelDefinition level,
            List<GridPosition> route,
            Profile profile,
            DeterministicRandom random,
            HashSet<GridPosition> occupied)
        {
            List<int> candidates = new List<int>();
            for (int i = 3; i < route.Count - 3; i++)
            {
                if (!occupied.Contains(route[i]) && route[i] != level.bonusPosition)
                {
                    candidates.Add(i);
                }
            }

            random.Shuffle(candidates);
            List<int> selected = new List<int>();
            for (int slotIndex = 0;
                 slotIndex < profile.modifierCount;
                 slotIndex++)
            {
                int target = ((slotIndex + 1) * (route.Count - 1)) /
                             (profile.modifierCount + 1);
                int routeIndex = -1;
                int bestDistance = int.MaxValue;
                for (int candidateIndex = 0;
                     candidateIndex < candidates.Count;
                     candidateIndex++)
                {
                    int candidate = candidates[candidateIndex];
                    bool spaced = true;
                    for (int i = 0; i < selected.Count; i++)
                    {
                        if (Math.Abs(selected[i] - candidate) < 3)
                        {
                            spaced = false;
                            break;
                        }
                    }

                    Direction candidateIncoming = DirectionUtility.Between(
                        route[candidate - 1],
                        route[candidate]);
                    Direction candidateOutgoing = DirectionUtility.Between(
                        route[candidate],
                        route[candidate + 1]);
                    if (!spaced ||
                        (profile.modifierStyle == RouteModifierStyle.OneWay &&
                         candidateIncoming != candidateOutgoing))
                    {
                        continue;
                    }

                    int distance = Math.Abs(candidate - target);
                    if (distance < bestDistance)
                    {
                        routeIndex = candidate;
                        bestDistance = distance;
                    }
                }

                if (routeIndex < 0)
                {
                    break;
                }

                Direction incoming = DirectionUtility.Between(
                    route[routeIndex - 1],
                    route[routeIndex]);
                Direction outgoing = DirectionUtility.Between(
                    route[routeIndex],
                    route[routeIndex + 1]);
                CellContent content;
                if (profile.modifierStyle == RouteModifierStyle.OneWay)
                {
                    if (incoming != outgoing)
                    {
                        continue;
                    }
                    content = OneWayFor(incoming);
                }
                else if (profile.modifierStyle == RouteModifierStyle.Sticky)
                {
                    content = CellContent.Sticky;
                }
                else if (profile.modifierStyle == RouteModifierStyle.Fragile)
                {
                    content = CellContent.Fragile;
                }
                else
                {
                    int slot = slotIndex % 4;
                    if (slot == 1 && incoming == outgoing)
                    {
                        content = OneWayFor(incoming);
                    }
                    else if (slot == 3 && incoming == outgoing)
                    {
                        content = CellContent.Slippery;
                    }
                    else
                    {
                        content = slot == 2
                            ? CellContent.Fragile
                            : CellContent.Sticky;
                    }
                }

                AddCell(level, occupied, route[routeIndex], content);
                selected.Add(routeIndex);
            }
        }

        private static void ApplyStrictTargets(
            LevelDefinition level,
            List<GridPosition> route,
            Profile profile)
        {
            int cost = 0;
            for (int i = 1; i < route.Count; i++)
            {
                cost += CellContentUtility.MoveCost(level.GetContent(route[i]));
            }

            level.parMoves = cost;
            level.threeStarMoveTarget = cost;
            int tier = (int)level.difficultyTier;
            int twoStarSlack = tier >= (int)DifficultyTier.Master
                ? 2
                : tier >= (int)DifficultyTier.Expert
                    ? Math.Max(2, cost / 40)
                    : tier >= (int)DifficultyTier.Hard
                        ? Math.Max(3, cost / 32)
                        : Math.Max(4, cost / 24);
            level.twoStarMoveTarget = cost + twoStarSlack;
            if (profile.hardLimit)
            {
                int maximumSlack = tier >= (int)DifficultyTier.Master
                    ? Math.Max(3, cost / 36)
                    : tier >= (int)DifficultyTier.Expert
                        ? Math.Max(4, cost / 30)
                        : Math.Max(5, cost / 24);
                level.moveLimit = cost + maximumSlack;
            }
            else
            {
                level.moveLimit = 0;
            }
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

        private static void AddCell(
            LevelDefinition level,
            HashSet<GridPosition> occupied,
            GridPosition position,
            CellContent content)
        {
            if (occupied.Add(position))
            {
                level.cells.Add(new GridCellDefinition(position, content));
            }
        }

        private static int CountFloorNeighbors(bool[,] floor, GridPosition position)
        {
            int count = 0;
            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition neighbor = position + Offsets[i];
                if (IsInside(floor, neighbor) && floor[neighbor.x, neighbor.y])
                {
                    count++;
                }
            }
            return count;
        }

        private static bool IsInside(bool[,] floor, GridPosition position)
        {
            return position.x >= 0 && position.y >= 0 &&
                   position.x < floor.GetLength(0) &&
                   position.y < floor.GetLength(1);
        }

        private static bool IsInterior(bool[,] floor, GridPosition position)
        {
            return position.x > 0 && position.y > 0 &&
                   position.x < floor.GetLength(0) - 1 &&
                   position.y < floor.GetLength(1) - 1;
        }

        private static bool IsEdge(bool[,] floor, GridPosition position)
        {
            return position.x == 0 || position.y == 0 ||
                   position.x == floor.GetLength(0) - 1 ||
                   position.y == floor.GetLength(1) - 1;
        }
    }
}
