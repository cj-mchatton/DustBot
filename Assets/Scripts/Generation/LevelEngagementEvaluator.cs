using System;
using System.Collections.Generic;

namespace DustBot
{
    public struct LevelEngagementReport
    {
        public int score;
        public int turns;
        public int endpointDetour;
        public int routeDecisions;
        public int nearbyBlockers;
        public int nearbyHazards;
        public int crumbSpread;
        public int orderComplexity;
        public int temptingBranches;
        public int bonusDetourCost;
        public int catPressure;
        public int catPressureScore;
        public int routeModifierCount;
        public int strategicDepthScore;
        public int chokepoints;
        public int obstacleDecisionScore;
        public int stickyCost;
        public int oneWayCommitments;
        public int fragileCommitments;
        public int possibleRoutes;
        public int pathCostPressure;
        public bool tooTrivial;
        public bool tooDense;
    }

    public static class LevelEngagementEvaluator
    {
        private static readonly GridPosition[] Offsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        public static LevelEngagementReport Analyze(LevelDefinition level)
        {
            List<GridPosition> route = BuildRoute(level);
            HashSet<GridPosition> routeSet = new HashSet<GridPosition>(route);
            int turns = CountTurns(route);
            int manhattan = Distance(route[0], route[route.Count - 1]);
            int detour = Math.Max(0, route.Count - 1 - manhattan);
            int decisions = CountRouteDecisions(level, route, routeSet);
            int temptingBranches = CountTemptingBranches(level, route, routeSet);
            int crumbSpread = CalculateCrumbSpread(level);
            int orderComplexity = CalculateOrderComplexity(level, route);
            int bonusDetourCost = CalculateBonusDetourCost(level, route);
            int routeModifierCount = CountRouteModifiers(level, routeSet);
            int routeMoveCost = CalculateRouteMoveCost(level, route);
            int stickyCost = Math.Max(0, routeMoveCost - Math.Max(0, route.Count - 1));
            int oneWayCommitments = CountRouteContent(
                level,
                routeSet,
                CellContentUtility.IsOneWay);
            int fragileCommitments = CountRouteContent(
                level,
                routeSet,
                delegate(CellContent content) { return content == CellContent.Fragile; });
            int chokepoints = CountChokepoints(level, route, routeSet);
            int possibleRoutes = Math.Min(
                4,
                1 + Math.Min(3, decisions / 2) + Math.Min(2, temptingBranches / 2));
            int pathCostPressure = CalculatePathCostPressure(
                level,
                stickyCost,
                routeMoveCost);
            int obstacleDecisionScore =
                Math.Min(12, stickyCost * 2) +
                Math.Min(10, oneWayCommitments * 3) +
                Math.Min(8, fragileCommitments * 2) +
                Math.Min(8, routeModifierCount * 2) +
                Math.Min(6, pathCostPressure);
            int catPressure = 0;
            int catPressureScore = 0;
            if (level.cat != null && level.cat.IsEnabled)
            {
                CatRelevanceReport relevance =
                    CatObstacleSimulator.AnalyzeRelevance(level, route);
                CatStrategyReport strategy =
                    CatLevelVarietyEvaluator.Analyze(level, route);
                catPressureScore = relevance.IsStrategicallyActive
                    ? Math.Min(
                        20,
                        relevance.pressureTurns +
                        Math.Max(0, 5 - relevance.closestDistance) +
                        Math.Min(3, relevance.reachableRouteTiles / 2) +
                        Math.Min(3, relevance.uniqueVisitedTiles / 2) +
                        Math.Min(4, strategy.nearCatchTurns) +
                        Math.Min(3, strategy.routeChangeMoves) +
                        Math.Min(2, strategy.backtrackMoves) +
                        Math.Min(3, strategy.crumbPressure + strategy.dockPressure) +
                        Math.Min(2, strategy.bonusPressure))
                    : 0;
                catPressure = Math.Min(8, catPressureScore);
            }
            int nearbyBlockers = 0;
            int nearbyHazards = 0;
            int blockingCells = 0;

            for (int i = 0; i < level.cells.Count; i++)
            {
                CellContent content = level.cells[i].content;
                bool blocker = content == CellContent.Wall || content == CellContent.Toy;
                bool hazard = content == CellContent.Sock ||
                              content == CellContent.Cord ||
                              content == CellContent.WetSpot;
                if (!blocker && !hazard)
                {
                    continue;
                }

                blockingCells++;
                if (IsAdjacentTo(level.cells[i].position, routeSet))
                {
                    if (blocker) nearbyBlockers++;
                    if (hazard) nearbyHazards++;
                }
            }

            int strategicDepthScore =
                Math.Min(12, decisions * 2) +
                Math.Min(10, temptingBranches * 2) +
                Math.Min(10, chokepoints * 2) +
                Math.Min(10, orderComplexity * 2) +
                Math.Min(12, obstacleDecisionScore) +
                Math.Min(8, bonusDetourCost * 2) +
                Math.Min(10, pathCostPressure * 2) +
                Math.Min(16, catPressureScore) +
                Math.Min(8, possibleRoutes * 2) +
                Math.Min(6, detour);

            int score =
                Math.Min(8, (route.Count - 1) / 3) +
                turns * 2 +
                Math.Min(8, level.Count(CellContent.Crumb) * 2) +
                Math.Min(5, decisions) +
                Math.Min(5, detour / 2) +
                Math.Min(5, nearbyBlockers) +
                Math.Min(6, nearbyHazards * 2) +
                Math.Min(6, crumbSpread) +
                Math.Min(7, orderComplexity) +
                Math.Min(6, temptingBranches * 2) +
                Math.Min(6, bonusDetourCost * 2) +
                Math.Min(6, routeModifierCount * 2) +
                Math.Min(8, catPressure * 2) +
                Math.Min(14, strategicDepthScore / 3) +
                (level.hardPathLimit ? 4 : 0) +
                (level.objectives.collectBonus ? 2 : 0);

            bool strongStrategicPressure =
                strategicDepthScore >= 35 ||
                catPressureScore >= 5 ||
                obstacleDecisionScore >= 10;
            bool tooTrivial =
                (level.levelNumber > 7 && turns < 2) ||
                (level.levelNumber > 10 &&
                 decisions == 0 &&
                 nearbyBlockers + nearbyHazards + routeModifierCount == 0 &&
                 !level.hardPathLimit) ||
                (level.levelNumber > 15 &&
                 !strongStrategicPressure &&
                 ((route.Count - 1 < 9) ||
                  level.Count(CellContent.Crumb) < 2 ||
                  crumbSpread < 2)) ||
                (level.levelNumber > 25 &&
                 !strongStrategicPressure &&
                 ((turns < 4 && detour < 3) ||
                  nearbyBlockers + nearbyHazards < 2 ||
                  (temptingBranches == 0 &&
                   nearbyHazards == 0 &&
                   routeModifierCount == 0 &&
                   catPressure == 0 &&
                   !level.hardPathLimit))) ||
                (level.levelNumber > 12 &&
                 strategicDepthScore < 18) ||
                (level.levelNumber > 20 &&
                 strategicDepthScore < 26);
            bool tooDense = level.largeMaze
                ? blockingCells > level.width * level.height * 72 / 100
                : blockingCells > level.width * level.height / 2;

            return new LevelEngagementReport
            {
                score = score,
                turns = turns,
                endpointDetour = detour,
                routeDecisions = decisions,
                nearbyBlockers = nearbyBlockers,
                nearbyHazards = nearbyHazards,
                crumbSpread = crumbSpread,
                orderComplexity = orderComplexity,
                temptingBranches = temptingBranches,
                bonusDetourCost = bonusDetourCost,
                catPressure = catPressure,
                catPressureScore = catPressureScore,
                routeModifierCount = routeModifierCount,
                strategicDepthScore = strategicDepthScore,
                chokepoints = chokepoints,
                obstacleDecisionScore = obstacleDecisionScore,
                stickyCost = stickyCost,
                oneWayCommitments = oneWayCommitments,
                fragileCommitments = fragileCommitments,
                possibleRoutes = possibleRoutes,
                pathCostPressure = pathCostPressure,
                tooTrivial = tooTrivial,
                tooDense = tooDense
            };
        }

        public static bool IsAccepted(
            LevelDefinition level,
            LevelGenerationSettings settings,
            out LevelEngagementReport report)
        {
            report = Analyze(level);
            int effectiveDecisions =
                report.routeDecisions +
                Math.Min(3, report.nearbyBlockers + report.nearbyHazards) +
                Math.Min(3, report.routeModifierCount) +
                Math.Min(2, report.catPressure) +
                (level.hardPathLimit ? 1 : 0) +
                Math.Min(2, report.orderComplexity / 3);
            int effectiveTemptations =
                report.temptingBranches +
                Math.Min(2, report.nearbyHazards) +
                Math.Min(2, report.routeModifierCount) +
                (report.catPressure > 0 ? 1 : 0) +
                (level.hardPathLimit ? 1 : 0);
            int requiredStrategicDepth = settings.minimumStrategicDepthScore > 0
                ? settings.minimumStrategicDepthScore
                : Math.Max(0, settings.minimumEngagementScore / 2);
            if (report.score >= settings.minimumEngagementScore + 18 ||
                (report.chokepoints >= 5 && report.obstacleDecisionScore >= 10))
            {
                requiredStrategicDepth = Math.Max(0, requiredStrategicDepth - 4);
            }

            if (report.tooDense ||
                report.turns < settings.minimumTurns ||
                report.endpointDetour < settings.minimumDetour ||
                report.crumbSpread < settings.minimumCrumbSpread ||
                effectiveDecisions < settings.minimumRouteDecisions ||
                effectiveTemptations < settings.minimumTemptingBranches ||
                report.bonusDetourCost < settings.minimumBonusDetour ||
                report.strategicDepthScore < requiredStrategicDepth ||
                report.score < settings.minimumEngagementScore)
            {
                return false;
            }

            if (settings.routeModifierCount > 0 &&
                report.routeModifierCount < Math.Min(settings.routeModifierCount, 2))
            {
                return false;
            }

            if (level.cat != null &&
                level.cat.IsEnabled &&
                settings.minimumCatPressureScore > 0 &&
                report.catPressureScore < settings.minimumCatPressureScore)
            {
                return false;
            }

            return level.levelNumber <= 7 || !report.tooTrivial;
        }

        private static List<GridPosition> BuildRoute(LevelDefinition level)
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

        private static int CountTurns(List<GridPosition> route)
        {
            int turns = 0;
            Direction previous = Direction.None;
            for (int i = 1; i < route.Count; i++)
            {
                Direction current = DirectionUtility.Between(route[i - 1], route[i]);
                if (previous != Direction.None && previous != current)
                {
                    turns++;
                }

                previous = current;
            }

            return turns;
        }

        private static int CountRouteDecisions(
            LevelDefinition level,
            List<GridPosition> route,
            HashSet<GridPosition> routeSet)
        {
            int decisions = 0;
            for (int i = 1; i < route.Count - 1; i++)
            {
                int alternateOpenNeighbors = 0;
                for (int j = 0; j < Offsets.Length; j++)
                {
                    GridPosition neighbor = route[i] + Offsets[j];
                    if (!level.IsInside(neighbor) || routeSet.Contains(neighbor))
                    {
                        continue;
                    }

                    CellContent content = level.GetContent(neighbor);
                    if (content == CellContent.Empty)
                    {
                        alternateOpenNeighbors++;
                    }
                }

                if (alternateOpenNeighbors > 0)
                {
                    decisions++;
                }
            }

            return decisions;
        }

        private static int CountTemptingBranches(
            LevelDefinition level,
            List<GridPosition> route,
            HashSet<GridPosition> routeSet)
        {
            int branches = 0;
            for (int i = 1; i < route.Count - 1; i++)
            {
                for (int j = 0; j < Offsets.Length; j++)
                {
                    GridPosition branch = route[i] + Offsets[j];
                    if (!level.IsInside(branch) ||
                        routeSet.Contains(branch) ||
                        level.GetContent(branch) != CellContent.Empty)
                    {
                        continue;
                    }

                    int continuationCount = 0;
                    for (int k = 0; k < Offsets.Length; k++)
                    {
                        GridPosition continuation = branch + Offsets[k];
                        if (level.IsInside(continuation) &&
                            !routeSet.Contains(continuation) &&
                            level.GetContent(continuation) == CellContent.Empty)
                        {
                            continuationCount++;
                        }
                    }

                    if (continuationCount > 0)
                    {
                        branches++;
                        break;
                    }
                }
            }

            return branches;
        }

        private static int CalculateCrumbSpread(LevelDefinition level)
        {
            List<GridPosition> crumbs = new List<GridPosition>();
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (level.cells[i].content == CellContent.Crumb)
                {
                    crumbs.Add(level.cells[i].position);
                }
            }

            if (crumbs.Count < 2)
            {
                return crumbs.Count;
            }

            int minimum = int.MaxValue;
            for (int i = 0; i < crumbs.Count; i++)
            {
                for (int j = i + 1; j < crumbs.Count; j++)
                {
                    minimum = Math.Min(minimum, Distance(crumbs[i], crumbs[j]));
                }
            }

            return minimum == int.MaxValue ? 0 : minimum;
        }

        private static int CalculateOrderComplexity(
            LevelDefinition level,
            List<GridPosition> route)
        {
            List<int> crumbIndices = new List<int>();
            for (int i = 0; i < route.Count; i++)
            {
                if (level.GetContent(route[i]) == CellContent.Crumb)
                {
                    crumbIndices.Add(i);
                }
            }

            if (crumbIndices.Count < 2)
            {
                return crumbIndices.Count;
            }

            int complexity = 0;
            for (int i = 1; i < crumbIndices.Count; i++)
            {
                int gap = crumbIndices[i] - crumbIndices[i - 1];
                complexity += gap >= 4 ? 2 : 1;
            }

            return complexity + Math.Max(0, crumbIndices.Count - 2);
        }

        private static int CalculateBonusDetourCost(
            LevelDefinition level,
            List<GridPosition> route)
        {
            if (!level.objectives.collectBonus)
            {
                return 0;
            }

            int bonusIndex = route.IndexOf(level.bonusPosition);
            if (bonusIndex < 0)
            {
                return 0;
            }

            int best = 0;
            for (int start = 0; start < bonusIndex; start++)
            {
                for (int end = bonusIndex + 1; end < route.Count; end++)
                {
                    if (Distance(route[start], route[end]) == 1)
                    {
                        best = Math.Max(best, end - start - 1);
                    }
                }
            }

            return best;
        }

        private static int CountRouteModifiers(
            LevelDefinition level,
            HashSet<GridPosition> routeSet)
        {
            int count = 0;
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (routeSet.Contains(level.cells[i].position) &&
                    CellContentUtility.IsRouteModifier(level.cells[i].content))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountRouteContent(
            LevelDefinition level,
            HashSet<GridPosition> routeSet,
            Predicate<CellContent> predicate)
        {
            int count = 0;
            for (int i = 0; i < level.cells.Count; i++)
            {
                if (routeSet.Contains(level.cells[i].position) &&
                    predicate(level.cells[i].content))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountChokepoints(
            LevelDefinition level,
            List<GridPosition> route,
            HashSet<GridPosition> routeSet)
        {
            int chokepoints = 0;
            for (int i = 1; i < route.Count - 1; i++)
            {
                GridPosition position = route[i];
                int walkableNeighbors = 0;
                bool adjacentPressure = false;
                for (int j = 0; j < Offsets.Length; j++)
                {
                    GridPosition neighbor = position + Offsets[j];
                    if (!level.IsInside(neighbor))
                    {
                        adjacentPressure = true;
                        continue;
                    }

                    CellContent content = level.GetContent(neighbor);
                    if (CellContentUtility.IsWalkableFloor(content))
                    {
                        walkableNeighbors++;
                    }
                    else
                    {
                        adjacentPressure = true;
                    }
                }

                CellContent routeContent = level.GetContent(position);
                if ((walkableNeighbors <= 2 && adjacentPressure) ||
                    CellContentUtility.IsRouteModifier(routeContent) ||
                    HasAdjacentTemptation(level, position, routeSet))
                {
                    chokepoints++;
                }
            }

            return chokepoints;
        }

        private static bool HasAdjacentTemptation(
            LevelDefinition level,
            GridPosition position,
            HashSet<GridPosition> routeSet)
        {
            for (int i = 0; i < Offsets.Length; i++)
            {
                GridPosition neighbor = position + Offsets[i];
                if (level.IsInside(neighbor) &&
                    !routeSet.Contains(neighbor) &&
                    level.GetContent(neighbor) == CellContent.Empty)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateRouteMoveCost(
            LevelDefinition level,
            List<GridPosition> route)
        {
            int cost = 0;
            for (int i = 1; i < route.Count; i++)
            {
                cost += CellContentUtility.MoveCost(level.GetContent(route[i]));
            }

            return cost;
        }

        private static int CalculatePathCostPressure(
            LevelDefinition level,
            int stickyCost,
            int routeMoveCost)
        {
            int pressure = Math.Min(5, stickyCost);
            if (level.hardPathLimit && level.moveLimit > 0)
            {
                pressure += Math.Max(1, 5 - Math.Max(0, level.moveLimit - routeMoveCost));
            }

            if (level.twoStarMoveTarget > 0)
            {
                pressure += Math.Max(0, 3 - Math.Max(0, level.twoStarMoveTarget - routeMoveCost));
            }

            if (level.objectives.bonusRequiredForThreeStars)
            {
                pressure += 2;
            }

            if (level.objectives.noHintStar)
            {
                pressure += 1;
            }

            return pressure;
        }

        private static bool IsAdjacentTo(
            GridPosition position,
            HashSet<GridPosition> route)
        {
            for (int i = 0; i < Offsets.Length; i++)
            {
                if (route.Contains(position + Offsets[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int Distance(GridPosition a, GridPosition b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }
    }
}
