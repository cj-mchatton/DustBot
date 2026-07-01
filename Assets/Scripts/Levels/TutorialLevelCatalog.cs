using System;
using System.Collections.Generic;
using System.Globalization;

namespace DustBot
{
    public static class TutorialLevelCatalog
    {
        private struct PlacedCell
        {
            public GridPosition position;
            public CellContent content;

            public PlacedCell(int x, int y, CellContent content)
            {
                position = new GridPosition(x, y);
                this.content = content;
            }
        }

        public static LevelDefinition Get(int levelNumber)
        {
            switch (levelNumber)
            {
                case 1:
                    return Create(1, 3, 3, "Touch DustBot, then drag a path to the dock. Release and press Play.",
                        Path(0, 1, 1, 1, 2, 1), Indices(), Cells());
                case 2:
                    return Create(2, 3, 3, "Keep dragging through neighboring tiles to draw one continuous route.",
                        Path(0, 0, 0, 1, 1, 1, 2, 1), Indices(), Cells());
                case 3:
                    return Create(3, 3, 3, "Drag through the crumb before finishing at the dock.",
                        Path(0, 0, 1, 0, 2, 0, 2, 1), Indices(1), Cells());
                case 4:
                    return Create(4, 4, 3, "Clean first, dock second. Reaching the charger early fails the level.",
                        Path(0, 1, 0, 2, 1, 2, 2, 2, 3, 2), Indices(2), Cells(3, 1, CellContent.Wall));
                case 5:
                    return Create(5, 4, 4, "Every required crumb must be collected.",
                        Path(0, 0, 1, 0, 2, 0, 2, 1, 2, 2, 3, 2, 3, 3), Indices(1, 4), Cells());
                case 6:
                    return Create(6, 4, 4, "Furniture blocks drawing. Swipe around it to keep the route clear.",
                        Path(0, 0, 0, 1, 0, 2, 1, 2, 2, 2, 3, 2), Indices(2, 4),
                        Cells(1, 0, CellContent.Wall, 1, 1, CellContent.Wall));
                case 7:
                    return Create(7, 4, 4, "Hazards flash red and block the drawn route. Swipe around the sock.",
                        Path(0, 3, 0, 2, 1, 2, 2, 2, 2, 1, 3, 1), Indices(2, 4),
                        Cells(1, 3, CellContent.Sock, 1, 1, CellContent.Wall));
                case 8:
                    return Create(8, 4, 4, "Cords zap DustBot. Keep the route on clear floor.",
                        Path(0, 0, 1, 0, 1, 1, 2, 1, 3, 1, 3, 2), Indices(1, 3),
                        Cells(2, 0, CellContent.Cord, 2, 2, CellContent.Wall));
                case 9:
                    return Create(9, 4, 4, "Wet spots cause an undignified slide. Avoid them.",
                        Path(0, 2, 1, 2, 1, 3, 2, 3, 3, 3), Indices(1, 3),
                        Cells(2, 2, CellContent.WetSpot, 0, 3, CellContent.Wall));
                case 10:
                    return Create(10, 5, 5, "Drag backward onto the previous tile to erase the last path segment.",
                        Path(0, 0, 1, 0, 2, 0, 2, 1, 2, 2, 1, 2, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3),
                        Indices(2, 5, 8, 10), Cells(1, 1, CellContent.Wall, 3, 1, CellContent.Wall));
                case 11:
                    return Create(11, 5, 4, "Use Reset for an instant clean slate, then draw a direct route.",
                        Path(0, 0, 1, 0, 2, 0, 3, 0, 3, 1, 3, 2, 4, 2),
                        Indices(2, 5), Cells(2, 1, CellContent.Sock, 4, 1, CellContent.Wall), -1, 0);
                case 12:
                    return Create(12, 5, 4, "Drag backward or restart from an earlier route tile to redraw.",
                        Path(0, 3, 1, 3, 1, 2, 2, 2, 3, 2, 3, 1, 4, 1),
                        Indices(2, 4), Cells(2, 3, CellContent.Wall, 2, 1, CellContent.Cord));
                case 13:
                    return Create(13, 5, 5, "Hint previews the next correct route tile and repairs a wrong branch.",
                        Path(0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 3),
                        Indices(2, 4), Cells(2, 1, CellContent.Sock, 3, 2, CellContent.Wall));
                case 14:
                    return Create(14, 5, 5, "The bonus dust bunny is optional, but worth a little extra shine.",
                        Path(0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 2, 3, 3, 4, 3),
                        Indices(2, 5), Cells(2, 1, CellContent.Wall, 4, 2, CellContent.WetSpot), 4);
                default:
                    return Create(15, 5, 5, "Final training room: crumbs, hazards, a bonus, and a clean return home.",
                        Path(0, 4, 0, 3, 1, 3, 1, 2, 1, 1, 2, 1, 3, 1, 3, 2, 4, 2, 4, 1, 4, 0),
                        Indices(2, 5, 8), Cells(
                            1, 4, CellContent.Sock,
                            2, 3, CellContent.Wall,
                            2, 2, CellContent.Cord,
                            3, 3, CellContent.WetSpot,
                            3, 0, CellContent.Wall), 6);
            }
        }

        private static LevelDefinition Create(
            int levelNumber,
            int width,
            int height,
            string message,
            GridPosition[] path,
            int[] crumbIndices,
            PlacedCell[] placedCells,
            int bonusIndex = -1,
            int moveBuffer = 2)
        {
            LevelDefinition level = new LevelDefinition
            {
                id = string.Format(CultureInfo.InvariantCulture, "Main_{0}", levelNumber),
                mode = GameMode.MainJourney,
                levelNumber = levelNumber,
                seed = string.Format(CultureInfo.InvariantCulture, "DustBot_Tutorial_{0:00}", levelNumber),
                generationVersion = 1,
                difficultyTier = DifficultyTier.Tutorial,
                width = width,
                height = height,
                parMoves = path.Length - 1,
                moveLimit = 0,
                twoStarMoveTarget = path.Length - 1 + Math.Max(1, moveBuffer),
                threeStarMoveTarget = path.Length - 1,
                hardPathLimit = false,
                archetype = levelNumber <= 3
                    ? LevelArchetype.SimpleRoute
                    : levelNumber <= 7
                        ? LevelArchetype.CrumbOrder
                        : levelNumber <= 12
                            ? LevelArchetype.BlockerMaze
                            : LevelArchetype.TrickRoute,
                tutorialMessage = message
            };

            level.cells.Add(new GridCellDefinition(path[0], CellContent.Start));
            level.cells.Add(new GridCellDefinition(path[path.Length - 1], CellContent.Dock));

            for (int i = 0; i < crumbIndices.Length; i++)
            {
                level.cells.Add(new GridCellDefinition(path[crumbIndices[i]], CellContent.Crumb));
            }

            for (int i = 0; i < placedCells.Length; i++)
            {
                level.cells.Add(new GridCellDefinition(placedCells[i].position, placedCells[i].content));
            }

            for (int i = 0; i < path.Length - 1; i++)
            {
                level.expectedSolution.Add(new SolutionStep(path[i], DirectionUtility.Between(path[i], path[i + 1])));
            }

            if (bonusIndex >= 0)
            {
                level.objectives.collectBonus = true;
                level.bonusPosition = path[bonusIndex];
            }

            return level;
        }

        private static GridPosition[] Path(params int[] coordinates)
        {
            GridPosition[] result = new GridPosition[coordinates.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new GridPosition(coordinates[i * 2], coordinates[i * 2 + 1]);
            }

            return result;
        }

        private static int[] Indices(params int[] indices)
        {
            return indices;
        }

        private static PlacedCell[] Cells(params object[] values)
        {
            List<PlacedCell> cells = new List<PlacedCell>();
            for (int i = 0; i + 2 < values.Length; i += 3)
            {
                cells.Add(new PlacedCell((int)values[i], (int)values[i + 1], (CellContent)values[i + 2]));
            }

            return cells.ToArray();
        }
    }
}
