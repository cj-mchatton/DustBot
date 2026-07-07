using System.Collections.Generic;
using System.Text;

namespace DustBot
{
    public static class LevelMetadata
    {
        public static string Format(LevelDefinition level)
        {
            if (level == null)
            {
                return "No level loaded.";
            }

            string validation;
            bool valid = LevelValidator.TryValidate(level, out validation);
            LevelEngagementReport report = LevelEngagementEvaluator.Analyze(level);
            StringBuilder builder = new StringBuilder(480);
            builder.Append(level.category != LevelCategory.None
                    ? "Content source: fixed curated catalog\n"
                    : "Content source: generated non-campaign mode\n")
                .Append("Generation mode: ").Append(level.generationMode).Append('\n')
                .Append("Level number: ").Append(level.levelNumber).Append('\n');
            if (level.category != LevelCategory.None)
                builder.Append("Category: ").Append(LevelCategoryCatalog.Name(level.category)).Append('\n');
            if (level.mode == GameMode.MainJourney)
            {
                builder.Append("Playlist size: ")
                    .Append(level.category == LevelCategory.None
                        ? LevelGenerationConfig.LevelCount(level.generationMode)
                        : LevelCategoryCatalog.Count(level.category)).Append('\n');
            }
            builder
                .Append("Seed: ").Append(level.seed).Append('\n')
                .Append("Generation version: ").Append(level.generationVersion).Append('\n')
                .Append("Level type: ").Append(level.cat.IsEnabled ? "Cat turn-based" : "Path-drawing").Append('\n')
                .Append("Difficulty: ").Append(level.difficultyTier).Append('\n')
                .Append("Archetype: ").Append(string.IsNullOrEmpty(level.testArchetype) ? level.archetype.ToString() : level.testArchetype).Append('\n')
                .Append("Obstacles: ").Append(ObstacleList(level)).Append('\n')
                .Append("Difficulty score: ").Append(report.score).Append('\n')
                .Append("Strategic depth score: ").Append(report.strategicDepthScore).Append('\n')
                .Append("Cat pressure: ").Append(report.catPressureScore).Append('\n')
                .Append("Large maze: ").Append(level.largeMaze ? "yes" : "no").Append('\n');
            if (level.cat != null && level.cat.IsEnabled)
            {
                CatStrategyReport cat = CatLevelVarietyEvaluator.Analyze(
                    level,
                    CatObstacleSimulator.BuildExpectedRoute(level));
                builder
                    .Append("Cat archetype: ").Append(level.catPuzzleArchetype).Append('\n')
                    .Append("Cat strategy: pressure ").Append(cat.pressureScore)
                    .Append(", reroute ").Append(cat.routeChangeMoves)
                    .Append(", backtracks ").Append(cat.backtrackMoves)
                    .Append(", near-catches ").Append(cat.nearCatchTurns)
                    .Append(", chokepoints ").Append(cat.chokepoints)
                    .Append(", loops ").Append(cat.loops)
                    .Append(", safe pockets ").Append(cat.safePockets)
                    .Append('\n');
            }
            if (level.largeMaze)
            {
                LargeMazeComplexityReport maze = LargeMazeEvaluator.Analyze(level);
                builder
                    .Append("Maze complexity: ").Append(maze.score).Append('\n')
                    .Append("Maze topology: ")
                    .Append(maze.branches).Append(" branches, ")
                    .Append(maze.deadEnds).Append(" dead ends, ")
                    .Append(maze.loops).Append(" loops, ")
                    .Append(maze.chokepoints).Append(" chokepoints, ")
                    .Append(maze.decoyPaths).Append(" decoys, ")
                    .Append(maze.playableEdgeCells).Append(" playable edge tiles")
                    .Append('\n');
                if (level.advancedDevMaze)
                {
                    AdvancedDevMazeReport advanced =
                        AdvancedDevMazeEvaluator.Analyze(level, maze);
                    builder
                        .Append("Dev maze archetype: ")
                        .Append(level.devMazeArchetype).Append('\n')
                        .Append("Dev route pressure: ")
                        .Append(advanced.meaningfulRouteChoices).Append(" choices, ")
                        .Append(advanced.temptingWrongPaths).Append(" wrong paths, ")
                        .Append(advanced.shortcutChoices).Append(" shortcuts, ")
                        .Append(advanced.finalCrumbToDock).Append(" dock-return cost")
                        .Append('\n');
                }
            }
            builder
                .Append("Solver / validator: ").Append(valid ? "Valid" : validation).Append('\n')
                .Append("Ideal cost: ").Append(level.parMoves).Append('\n')
                .Append("Hard max: ").Append(level.hardPathLimit ? level.moveLimit.ToString() : "none");
            if (!string.IsNullOrEmpty(level.designPurpose))
                builder.Append('\n').Append("Design purpose: ").Append(level.designPurpose);
            if (!string.IsNullOrEmpty(level.intendedStrategy))
                builder.Append('\n').Append("Intended strategy: ").Append(level.intendedStrategy);
            return builder.ToString();
        }

        public static string ObstacleList(LevelDefinition level)
        {
            List<string> names = new List<string>();
            AddIfPresent(level, names, CellContent.Wall, "Wall/Furniture");
            AddIfPresent(level, names, CellContent.Sock, "Sock");
            AddIfPresent(level, names, CellContent.Cord, "Cord");
            AddIfPresent(level, names, CellContent.WetSpot, "Wet Spot");
            AddIfPresent(level, names, CellContent.Toy, "Cat Toy/Furniture");
            AddIfPresent(level, names, CellContent.Sticky, "Sticky");
            AddIfPresent(level, names, CellContent.Fragile, "Fragile");
            AddIfPresent(level, names, CellContent.Slippery, "Slippery Momentum");
            if (level.Count(CellContent.OneWayUp) +
                level.Count(CellContent.OneWayRight) +
                level.Count(CellContent.OneWayDown) +
                level.Count(CellContent.OneWayLeft) > 0)
            {
                names.Add("One-Way");
            }

            return names.Count == 0 ? "None" : string.Join(", ", names.ToArray());
        }

        private static void AddIfPresent(
            LevelDefinition level,
            List<string> names,
            CellContent content,
            string name)
        {
            if (level.Count(content) > 0)
            {
                names.Add(name);
            }
        }
    }
}
