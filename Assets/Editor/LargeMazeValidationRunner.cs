using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace DustBot.Editor
{
    public static class LargeMazeValidationRunner
    {
        [MenuItem("DustBot/Validate Large Mazes")]
        public static void RunFromMenu()
        {
            RunBatch();
        }

        public static void RunBatch()
        {
            LevelGenerator generator = new LevelGenerator();
            DevelopmentLevelManifest development = new DevelopmentLevelManifest();
            LevelManifest production = new LevelManifest();
            List<LevelDefinition> levels = new List<LevelDefinition>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            int[] developmentLevels = { 9, 10, 11, 12, 13, 14, 15, 28, 29, 30 };
            for (int i = 0; i < developmentLevels.Length; i++)
            {
                int number = developmentLevels[i];
                levels.Add(generator.Generate(
                    development.GetEntry(GenerationMode.DevelopmentCampaign, number),
                    GameMode.MainJourney));
            }

            for (int number = 1;
                 number <= LevelGenerationConfig.MazeTestingLevelCount;
                 number++)
            {
                levels.Add(generator.Generate(
                    development.GetEntry(GenerationMode.MazeTesting, number),
                    GameMode.MainJourney));
            }

            levels.Add(generator.Generate(
                production.GetMasterEntry(2),
                GameMode.MasterClean));

            for (int i = 0; i < levels.Count; i++)
            {
                ValidateLevelAndInput(levels[i]);
                LargeMazeComplexityReport maze = LargeMazeEvaluator.Analyze(levels[i]);
                AdvancedDevMazeReport advanced = levels[i].advancedDevMaze
                    ? AdvancedDevMazeEvaluator.Analyze(levels[i], maze)
                    : new AdvancedDevMazeReport();
                UnityEngine.Debug.Log(string.Format(
                    "LARGE_MAZE_TEST {0} {1}x{2} score={3} route={4} open={5}% branches={6} deadEnds={7} loops={8} chokepoints={9} rooms={10} decoys={11} bunnyDetour={12} advancedScore={13} wrongPaths={14} shortcuts={15} dockReturn={16} playableEdge={17}",
                    levels[i].id,
                    levels[i].width,
                    levels[i].height,
                    maze.score,
                    levels[i].expectedSolution.Count,
                    maze.openPercent,
                    maze.branches,
                    maze.deadEnds,
                    maze.loops,
                    maze.chokepoints,
                    maze.rooms,
                    maze.decoyPaths,
                    maze.bonusDetourCost,
                    advanced.score,
                    advanced.temptingWrongPaths,
                    advanced.shortcutChoices,
                    advanced.finalCrumbToDock,
                    maze.playableEdgeCells));
            }

            stopwatch.Stop();
            UnityEngine.Debug.Log(
                "LARGE_MAZE_TEST PASS count=" + levels.Count +
                " elapsedMs=" + stopwatch.ElapsedMilliseconds);
        }

        private static void ValidateLevelAndInput(LevelDefinition level)
        {
            string validation = "level was not marked as a large maze";
            if (!level.largeMaze || !LevelValidator.TryValidate(level, out validation))
            {
                throw new InvalidOperationException(
                    "Large-maze validation failed for " + level.id + ": " + validation);
            }

            LargeMazeComplexityReport topology = LargeMazeEvaluator.Analyze(level);
            if (level.advancedDevMaze &&
                (topology.fullBlockedPerimeter || topology.playableEdgeCells < 6))
            {
                throw new InvalidOperationException(
                    "Dev maze did not expose meaningful playable edge space for " + level.id);
            }

            GameSession session = new GameSession(level);
            GridPosition current = session.Grid.Start;
            if (session.BeginPath(current) != PathEditResult.Started)
            {
                throw new InvalidOperationException("Could not begin expected route for " + level.id);
            }

            int firstPause = Math.Max(1, level.expectedSolution.Count / 3);
            int trimIndex = Math.Max(1, firstPause / 2);
            for (int i = 0; i < firstPause; i++)
            {
                current += DirectionUtility.ToOffset(level.expectedSolution[i].direction);
                PathEditResult result = session.TryExtendPath(current);
                if (result != PathEditResult.Added)
                {
                    throw new InvalidOperationException(
                        "Long-path input failed for " + level.id +
                        " at step " + i + ": " + result);
                }
            }

            session.EndPath();
            int pausedCost = session.CurrentPathMoveCost;
            GridPosition pausedTail = session.CurrentPathCells[session.CurrentPathCells.Count - 1];
            GridPosition randomCell = new GridPosition(-1, -1);
            for (int y = 0; y < level.height && randomCell.x < 0; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GridPosition candidate = new GridPosition(x, y);
                    if (!session.IsPathCell(candidate))
                    {
                        randomCell = candidate;
                        break;
                    }
                }
            }

            if (session.BeginPath(randomCell) != PathEditResult.Invalid ||
                session.CurrentPathMoveCost != pausedCost)
            {
                throw new InvalidOperationException(
                    "Path continuation started from a random tile for " + level.id);
            }

            if (session.BeginPath(pausedTail) != PathEditResult.Resumed ||
                session.CurrentPathMoveCost != pausedCost)
            {
                throw new InvalidOperationException(
                    "Path did not resume from its tail for " + level.id);
            }

            GridPosition jump = new GridPosition(
                pausedTail.x + 2 < level.width
                    ? pausedTail.x + 2
                    : pausedTail.x - 2,
                pausedTail.y);
            if (session.TryExtendPath(jump) != PathEditResult.Invalid)
            {
                throw new InvalidOperationException(
                    "Path continuation allowed a non-adjacent jump for " + level.id);
            }
            session.EndPath();

            GridPosition trimCell = session.CurrentPathCells[trimIndex];
            if (session.BeginPath(trimCell) != PathEditResult.Trimmed ||
                session.CurrentPathCells.Count != trimIndex + 1)
            {
                throw new InvalidOperationException(
                    "Path trimming failed for " + level.id);
            }

            current = trimCell;
            for (int i = trimIndex; i < level.expectedSolution.Count; i++)
            {
                current += DirectionUtility.ToOffset(level.expectedSolution[i].direction);
                PathEditResult result = session.TryExtendPath(current);
                if (result != PathEditResult.Added)
                {
                    throw new InvalidOperationException(
                        "Resumed long-path input failed for " + level.id +
                        " at step " + i + ": " + result);
                }
            }

            session.EndPath();
            string issue;
            if (session.TryGetRouteIssue(out issue))
            {
                throw new InvalidOperationException(
                    "Expected route was rejected for " + level.id + ": " + issue);
            }

            session.ClearRoute();
            if (session.CurrentPathCells.Count != 1 ||
                session.CurrentPathCells[0] != session.Grid.Start ||
                session.CurrentPathMoveCost != 0)
            {
                throw new InvalidOperationException(
                    "Reset did not clear the complete route for " + level.id);
            }
        }
    }
}
