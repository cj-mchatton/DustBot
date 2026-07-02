using System.Collections.Generic;

namespace DustBot
{
    public static class CatTutorialLevelCatalog
    {
        public static LevelDefinition GetIntroduction()
        {
            LevelDefinition level = new LevelDefinition
            {
                id = "MainJourney_21",
                mode = GameMode.MainJourney,
                levelNumber = 21,
                seed = "DustBot_Cat_Tutorial_21",
                generationVersion = LevelManifest.CurrentGenerationVersion,
                difficultyTier = DifficultyTier.Easy,
                width = 6,
                height = 5,
                parMoves = 11,
                moveLimit = 0,
                twoStarMoveTarget = 14,
                threeStarMoveTarget = 11,
                hardPathLimit = false,
                archetype = LevelArchetype.BlockerMaze,
                catPuzzleArchetype = CatPuzzleArchetype.HorizontalPriorityTrap,
                catFreeParMoves = 7,
                themeId = "CozyHome",
                mechanicSet = "CatChaseTurns",
                objectiveSet = "CleanAllAndDock",
                tutorialMessage =
                    "CAT CHASE • Swipe one tile. Then the cat moves twice, horizontal first. Use the corner, clean, and dock.",
                cat = new CatDefinition
                {
                    behavior = CatBehavior.Curious,
                    startPosition = new GridPosition(5, 0),
                    horizontalFirst = true
                }
            };

            level.cells.Add(new GridCellDefinition(new GridPosition(2, 3), CellContent.Start));
            level.cells.Add(new GridCellDefinition(new GridPosition(5, 1), CellContent.Dock));
            level.cells.Add(new GridCellDefinition(new GridPosition(4, 1), CellContent.Crumb));
            AddWalls(
                level,
                new GridPosition(0, 0),
                new GridPosition(1, 3),
                new GridPosition(2, 0),
                new GridPosition(2, 2),
                new GridPosition(3, 0),
                new GridPosition(5, 3),
                new GridPosition(5, 4));

            Direction[] directions =
            {
                Direction.Up,
                Direction.Down,
                Direction.Up,
                Direction.Left,
                Direction.Right,
                Direction.Right,
                Direction.Right,
                Direction.Down,
                Direction.Down,
                Direction.Down,
                Direction.Right
            };
            GridPosition current = new GridPosition(2, 3);
            for (int i = 0; i < directions.Length; i++)
            {
                level.expectedSolution.Add(new SolutionStep(current, directions[i]));
                current += DirectionUtility.ToOffset(directions[i]);
            }

            return level;
        }

        private static void AddWalls(
            LevelDefinition level,
            params GridPosition[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                level.cells.Add(new GridCellDefinition(
                    positions[i],
                    CellContent.Wall));
            }
        }
    }
}
