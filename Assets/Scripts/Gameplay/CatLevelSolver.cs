using System;
using System.Collections.Generic;

namespace DustBot
{
    public static class CatLevelSolver
    {
        private struct SearchState : IEquatable<SearchState>
        {
            public GridPosition bot;
            public GridPosition cat;
            public int crumbsMask;
            public int fragileMask;
            public bool bonusCollected;

            public bool Equals(SearchState other)
            {
                return bot == other.bot &&
                       cat == other.cat &&
                       crumbsMask == other.crumbsMask &&
                       fragileMask == other.fragileMask &&
                       bonusCollected == other.bonusCollected;
            }

            public override bool Equals(object obj)
            {
                return obj is SearchState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = bot.GetHashCode();
                    hash = (hash * 397) ^ cat.GetHashCode();
                    hash = (hash * 397) ^ crumbsMask;
                    hash = (hash * 397) ^ fragileMask;
                    hash = (hash * 397) ^ (bonusCollected ? 1 : 0);
                    return hash;
                }
            }
        }

        private struct SearchNode
        {
            public SearchState state;
            public int moves;
        }

        private struct ParentLink
        {
            public SearchState parent;
            public Direction direction;
        }

        private static readonly Direction[] SearchOrder =
        {
            Direction.Up,
            Direction.Right,
            Direction.Down,
            Direction.Left
        };

        public static bool TrySolve(
            LevelDefinition level,
            int maximumMoves,
            out List<SolutionStep> solution)
        {
            solution = new List<SolutionStep>();
            if (level == null || level.cat == null || !level.cat.IsEnabled)
            {
                return false;
            }

            List<GridPosition> crumbs = GetCrumbs(level);
            List<GridPosition> fragileTiles = GetTiles(level, CellContent.Fragile);
            if (crumbs.Count > 20)
            {
                return false;
            }

            if (fragileTiles.Count > 20)
            {
                return false;
            }

            int allCrumbsMask = crumbs.Count == 0 ? 0 : (1 << crumbs.Count) - 1;
            SearchState start = new SearchState
            {
                bot = level.Find(CellContent.Start),
                cat = level.cat.startPosition,
                crumbsMask = CrumbMaskAt(crumbs, level.Find(CellContent.Start)),
                fragileMask = TileMaskAt(fragileTiles, level.Find(CellContent.Start)),
                bonusCollected =
                    level.objectives.collectBonus &&
                    level.Find(CellContent.Start) == level.bonusPosition
            };

            Queue<SearchNode> frontier = new Queue<SearchNode>();
            Dictionary<SearchState, ParentLink> parents =
                new Dictionary<SearchState, ParentLink>();
            HashSet<SearchState> visited = new HashSet<SearchState>();
            frontier.Enqueue(new SearchNode { state = start, moves = 0 });
            visited.Add(start);

            while (frontier.Count > 0)
            {
                SearchNode node = frontier.Dequeue();
                for (int i = 0; i < SearchOrder.Length; i++)
                {
                    Direction direction = SearchOrder[i];
                    GridPosition nextBot =
                        node.state.bot + DirectionUtility.ToOffset(direction);
                    if (!CanDustBotMove(level, node.state.bot, nextBot) ||
                        nextBot == node.state.cat)
                    {
                        continue;
                    }

                    int nextMask =
                        node.state.crumbsMask | CrumbMaskAt(crumbs, nextBot);
                    int nextFragileBit = TileMaskAt(fragileTiles, nextBot);
                    if (nextFragileBit != 0 &&
                        (node.state.fragileMask & nextFragileBit) != 0)
                    {
                        continue;
                    }

                    int nextFragileMask = node.state.fragileMask | nextFragileBit;
                    bool nextBonus =
                        node.state.bonusCollected ||
                        (level.objectives.collectBonus &&
                         nextBot == level.bonusPosition);
                    bool bonusGoal =
                        !level.objectives.bonusRequiredForThreeStars ||
                        nextBonus;
                    if (nextBot == level.Find(CellContent.Dock) &&
                        (nextMask != allCrumbsMask || !bonusGoal))
                    {
                        continue;
                    }

                    CatStepResult catStep = CatObstacleSimulator.Advance(
                        level,
                        node.state.cat,
                        nextBot,
                        node.moves + 1);
                    if (catStep.collided)
                    {
                        continue;
                    }

                    SearchState next = new SearchState
                    {
                        bot = nextBot,
                        cat = catStep.to,
                        crumbsMask = nextMask,
                        fragileMask = nextFragileMask,
                        bonusCollected = nextBonus
                    };
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    parents[next] = new ParentLink
                    {
                        parent = node.state,
                        direction = direction
                    };
                    int moveCount = node.moves + 1;
                    if (nextBot == level.Find(CellContent.Dock) &&
                        nextMask == allCrumbsMask &&
                        bonusGoal)
                    {
                        solution = Reconstruct(start, next, parents);
                        return true;
                    }

                    if (moveCount < maximumMoves)
                    {
                        frontier.Enqueue(new SearchNode
                        {
                            state = next,
                            moves = moveCount
                        });
                    }
                }
            }

            return false;
        }

        public static bool CanDustBotEnter(
            LevelDefinition level,
            GridPosition position)
        {
            if (!level.IsInside(position))
            {
                return false;
            }

            CellContent content = level.GetContent(position);
            return CellContentUtility.IsWalkableFloor(content);
        }

        private static bool CanDustBotMove(
            LevelDefinition level,
            GridPosition from,
            GridPosition to)
        {
            if (!CanDustBotEnter(level, to))
            {
                return false;
            }

            return CellContentUtility.AllowsDirection(
                level.GetContent(from),
                level.GetContent(to),
                DirectionUtility.Between(from, to));
        }

        public static List<GridPosition> BuildRoute(
            LevelDefinition level,
            IList<SolutionStep> solution)
        {
            List<GridPosition> route = new List<GridPosition>();
            GridPosition current = level.Find(CellContent.Start);
            route.Add(current);
            for (int i = 0; i < solution.Count; i++)
            {
                current += DirectionUtility.ToOffset(solution[i].direction);
                route.Add(current);
            }

            return route;
        }

        private static List<SolutionStep> Reconstruct(
            SearchState start,
            SearchState goal,
            Dictionary<SearchState, ParentLink> parents)
        {
            List<Direction> reversed = new List<Direction>();
            SearchState current = goal;
            while (!current.Equals(start))
            {
                ParentLink link = parents[current];
                reversed.Add(link.direction);
                current = link.parent;
            }

            reversed.Reverse();
            List<SolutionStep> solution = new List<SolutionStep>(reversed.Count);
            GridPosition position = start.bot;
            for (int i = 0; i < reversed.Count; i++)
            {
                solution.Add(new SolutionStep(position, reversed[i]));
                position += DirectionUtility.ToOffset(reversed[i]);
            }

            return solution;
        }

        private static List<GridPosition> GetCrumbs(LevelDefinition level)
        {
            return GetTiles(level, CellContent.Crumb);
        }

        private static List<GridPosition> GetTiles(LevelDefinition level, CellContent content)
        {
            List<GridPosition> tiles = new List<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == content)
                {
                    tiles.Add(level.cells[i].position);
                }
            }

            return tiles;
        }

        private static int CrumbMaskAt(
            List<GridPosition> crumbs,
            GridPosition position)
        {
            int mask = 0;
            for (int i = 0; i < crumbs.Count; i++)
            {
                if (crumbs[i] == position)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        private static int TileMaskAt(
            List<GridPosition> tiles,
            GridPosition position)
        {
            int mask = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == position)
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }
    }
}
