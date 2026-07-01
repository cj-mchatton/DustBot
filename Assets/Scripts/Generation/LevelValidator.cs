using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DustBot
{
    public static class LevelValidator
    {
        public static bool TryValidate(LevelDefinition level, out string message)
        {
            if (level == null)
            {
                message = "Level definition is null.";
                return false;
            }

            bool allowsExtremeDevGrid = level.advancedDevMaze &&
                                        level.generationMode != GenerationMode.ProductionCampaign;
            int maximumBoardSize = allowsExtremeDevGrid ? 40 : 22;
            if (level.width < 2 || level.height < 2 ||
                level.width > maximumBoardSize || level.height > maximumBoardSize)
            {
                message = "Board dimensions are outside the supported range.";
                return false;
            }

            if (string.IsNullOrEmpty(level.id) ||
                string.IsNullOrEmpty(level.seed) ||
                level.generationVersion < 1)
            {
                message = "Level identity or generation version is invalid.";
                return false;
            }

            if (level.cells == null || level.expectedSolution == null || level.objectives == null)
            {
                message = "One or more serialized level collections are null.";
                return false;
            }

            if (level.cat == null)
            {
                message = "The moving-obstacle definition is null.";
                return false;
            }

            HashSet<GridPosition> occupied = new HashSet<GridPosition>();
            int startCount = 0;
            int dockCount = 0;
            for (int i = 0; i < level.cells.Count; i++)
            {
                GridCellDefinition cell = level.cells[i];
                if (cell == null || !level.IsInside(cell.position))
                {
                    message = "A cell is null or outside the board.";
                    return false;
                }

                if (!Enum.IsDefined(typeof(CellContent), cell.content))
                {
                    message = "A cell contains an unsupported content value.";
                    return false;
                }

                if (!occupied.Add(cell.position))
                {
                    message = "Multiple serialized cells occupy " + cell.position + ".";
                    return false;
                }

                if (cell.content == CellContent.Start) startCount++;
                if (cell.content == CellContent.Dock) dockCount++;
            }

            if (startCount != 1 || dockCount != 1)
            {
                message = "A level must contain exactly one start and one dock.";
                return false;
            }

            GridPosition start = level.Find(CellContent.Start);
            GridPosition dock = level.Find(CellContent.Dock);
            if (level.expectedSolution.Count == 0)
            {
                message = "Expected solution is empty.";
                return false;
            }

            if (level.objectives.bonusRequiredForThreeStars &&
                !level.objectives.collectBonus)
            {
                message = "A required three-star Dust Bunny is not enabled.";
                return false;
            }

            HashSet<GridPosition> visited = new HashSet<GridPosition> { start };
            HashSet<GridPosition> crumbs = new HashSet<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == CellContent.Crumb)
                {
                    crumbs.Add(level.cells[i].position);
                }
            }

            GridPosition current = start;
            int solutionMoveCost = 0;
            Direction previousDirection = Direction.None;
            crumbs.Remove(current);
            HashSet<GridPosition> fragileVisited = new HashSet<GridPosition>();
            for (int i = 0; i < level.expectedSolution.Count; i++)
            {
                SolutionStep step = level.expectedSolution[i];
                if (step == null ||
                    !Enum.IsDefined(typeof(Direction), step.direction) ||
                    step.position != current ||
                    step.direction == Direction.None)
                {
                    message = "Solution steps are discontinuous at index " + i;
                    return false;
                }

                GridPosition next = current + DirectionUtility.ToOffset(step.direction);
                if (!level.IsInside(next))
                {
                    message = "Solution leaves the board.";
                    return false;
                }

                CellContent fromContent = level.GetContent(current);
                CellContent content = level.GetContent(next);
                if (CellContentUtility.IsDustBotBlocker(content))
                {
                    message = "Solution crosses blocked or hazardous content.";
                    return false;
                }

                if (fromContent == CellContent.Slippery &&
                    previousDirection != Direction.None &&
                    previousDirection != step.direction)
                {
                    message = "Solution turns while crossing a slippery tile at index " + i + ".";
                    return false;
                }

                if (!CellContentUtility.AllowsDirection(fromContent, content, step.direction))
                {
                    message = "Solution violates a one-way tile at index " + i + ".";
                    return false;
                }

                current = next;
                previousDirection = step.direction;
                solutionMoveCost += CellContentUtility.MoveCost(content);
                if (content == CellContent.Fragile && !fragileVisited.Add(current))
                {
                    message = "Solution crosses a fragile tile more than once.";
                    return false;
                }

                if (!visited.Add(current) &&
                    current != dock &&
                    !level.cat.IsEnabled)
                {
                    message = "The expected route revisits a tile.";
                    return false;
                }

                crumbs.Remove(current);
            }

            if (level.parMoves != solutionMoveCost)
            {
                message = "Par moves must match the expected route movement cost.";
                return false;
            }

            if (level.moveLimit > 0 && level.moveLimit < level.parMoves)
            {
                message = "The hard move limit is lower than par.";
                return false;
            }

            if (level.hardPathLimit && level.moveLimit <= 0)
            {
                message = "Hard path limit is enabled without a maximum path length.";
                return false;
            }

            if ((level.twoStarMoveTarget > 0 && level.twoStarMoveTarget < level.parMoves) ||
                (level.threeStarMoveTarget > 0 && level.threeStarMoveTarget < level.parMoves) ||
                (level.twoStarMoveTarget > 0 &&
                 level.threeStarMoveTarget > level.twoStarMoveTarget))
            {
                message = "Star move targets are inconsistent with par.";
                return false;
            }

            if (current != dock)
            {
                message = "Solution does not end at the dock.";
                return false;
            }

            if (crumbs.Count > 0)
            {
                message = "One or more crumbs are not on the solution route.";
                return false;
            }

            if (level.objectives.collectBonus &&
                (!level.IsInside(level.bonusPosition) || !visited.Contains(level.bonusPosition)))
            {
                message = "Bonus objective is not reachable on the expected route.";
                return false;
            }

            if (!Enum.IsDefined(typeof(CatBehavior), level.cat.behavior))
            {
                message = "The cat uses an unsupported behavior.";
                return false;
            }

            if (level.cat.IsEnabled)
            {
                if (!level.IsInside(level.cat.startPosition))
                {
                    message = "The cat starts outside the board.";
                    return false;
                }

                if (level.GetContent(level.cat.startPosition) != CellContent.Empty ||
                    level.cat.startPosition == level.bonusPosition)
                {
                    message = "The cat must start on an unoccupied floor tile.";
                    return false;
                }

                if (!CatObstacleSimulator.SharesWalkableRegionWithDustBot(level))
                {
                    message = "The cat starts in a disconnected walkable region.";
                    return false;
                }

                List<GridPosition> expectedRoute = CatObstacleSimulator.BuildExpectedRoute(level);
                CatRoutePreview catPreview = CatObstacleSimulator.SimulateRoute(
                    level,
                    expectedRoute);
                if (catPreview.collided)
                {
                    message = "The expected route is caught by the cat at step " +
                              catPreview.collisionStep + ".";
                    return false;
                }

                CatRelevanceReport relevance = CatObstacleSimulator.AnalyzeRelevance(
                    level,
                    expectedRoute);
                if (!relevance.IsStrategicallyActive)
                {
                    message = string.Format(
                        "The cat is trapped or negligible: open {0}, reachable {1}, route {2}, botRegion {3}, routeDist {4}, movement {5}, unique {6}, closest {7}, pressure {8}.",
                        relevance.openStartNeighbors,
                        relevance.reachableTiles,
                        relevance.reachableRouteTiles,
                        relevance.sameConnectedRegion,
                        relevance.distanceToRoute,
                        relevance.movementTurns,
                        relevance.uniqueVisitedTiles,
                        relevance.closestDistance,
                        relevance.pressureTurns);
                    return false;
                }

                List<SolutionStep> verifiedSolution;
                int searchLimit = level.moveLimit > 0
                    ? level.moveLimit
                    : Math.Max(
                        level.expectedSolution.Count + 12,
                        level.width * level.height * 2);
                if (!CatLevelSolver.TrySolve(
                        level,
                        searchLimit,
                        out verifiedSolution))
                {
                    message = "The cat level has no valid turn-based cleaning solution.";
                    return false;
                }
            }

            if (level.largeMaze)
            {
                if (level.width < 8 || level.height < 8)
                {
                    message = "Large mazes must be at least 8×8.";
                    return false;
                }

                if (level.cat.IsEnabled)
                {
                    message = "Large path mazes cannot also use the turn-based cat rules.";
                    return false;
                }

                LargeMazeComplexityReport maze = LargeMazeEvaluator.Analyze(level);
                int area = level.width * level.height;
                int minimumScore = level.difficultyTier >= DifficultyTier.Master
                    ? 104
                    : level.difficultyTier >= DifficultyTier.Expert
                        ? 92
                        : level.difficultyTier >= DifficultyTier.Hard ? 80 : 62;
                if (!maze.allObjectivesReachable)
                {
                    message = "One or more large-maze objectives are unreachable.";
                    return false;
                }

                if (maze.tooOpen || maze.tooLinear || maze.tooTedious ||
                    maze.score < minimumScore ||
                    maze.branches < Math.Max(3, area / 65) ||
                    maze.deadEnds < Math.Max(3, area / 85) ||
                    maze.loops < Math.Max(2, area / 170))
                {
                    message = string.Format(
                        "Large maze is under-complex: score {0}/{1}, open {2}%, branches {3}, dead ends {4}, loops {5}, chokepoints {6}, decoys {7}.",
                        maze.score,
                        minimumScore,
                        maze.openPercent,
                        maze.branches,
                        maze.deadEnds,
                        maze.loops,
                        maze.chokepoints,
                        maze.decoyPaths);
                    return false;
                }

                if (level.objectives.collectBonus && maze.bonusDetourCost < 2)
                {
                    message = "The large-maze Dust Bunny must require a meaningful optional detour.";
                    return false;
                }

                if (level.advancedDevMaze)
                {
                    AdvancedDevMazeReport advanced;
                    string advancedReason;
                    if (!AdvancedDevMazeEvaluator.MeetsRequirements(
                            level,
                            maze,
                            out advanced,
                            out advancedReason))
                    {
                        message = "Advanced dev maze rejected: " + advancedReason;
                        return false;
                    }
                }
            }

            message = "Valid";
            return true;
        }

        public static string Signature(LevelDefinition level)
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append(level.id).Append('|')
                .Append((int)level.mode).Append('|')
                .Append(level.levelNumber.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.seed).Append("|v")
                .Append(level.generationVersion.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append((int)level.difficultyTier).Append('|')
                .Append(level.width.ToString(CultureInfo.InvariantCulture)).Append('x')
                .Append(level.height.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.parMoves.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.moveLimit.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.twoStarMoveTarget.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.threeStarMoveTarget.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.hardPathLimit ? '1' : '0').Append('|')
                .Append((int)level.archetype).Append('|')
                .Append(level.engagementScore.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.strategicDepthScore.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.catPressureScore.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.largeMaze ? '1' : '0').Append(':')
                .Append(level.mazeComplexityScore.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(level.themeId).Append('|')
                .Append(level.mechanicSet).Append('|')
                .Append(level.objectiveSet).Append('|')
                .Append(level.objectives.cleanAllCrumbs ? '1' : '0')
                .Append(level.objectives.returnToDock ? '1' : '0')
                .Append(level.objectives.collectBonus ? '1' : '0')
                .Append(level.objectives.noHintStar ? '1' : '0')
                .Append(level.objectives.noUndoStar ? '1' : '0')
                .Append(level.objectives.bonusRequiredForThreeStars ? '1' : '0').Append('|')
                .Append(level.bonusPosition.x.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(level.bonusPosition.y.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append((int)level.cat.behavior).Append(':')
                .Append(level.cat.startPosition.x.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(level.cat.startPosition.y.ToString(CultureInfo.InvariantCulture)).Append(':')
                .Append(level.cat.horizontalFirst ? 'H' : 'V');

            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    builder.Append('|').Append((int)level.GetContent(new GridPosition(x, y)));
                }
            }

            for (int i = 0; i < level.expectedSolution.Count; i++)
            {
                SolutionStep step = level.expectedSolution[i];
                builder.Append('>').Append(step.position.x.ToString(CultureInfo.InvariantCulture))
                    .Append(',').Append(step.position.y.ToString(CultureInfo.InvariantCulture))
                    .Append(':').Append((int)step.direction);
            }

            return builder.ToString();
        }
    }
}
