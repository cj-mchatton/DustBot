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

            if (level.width < 2 || level.height < 2 || level.width > 12 || level.height > 12)
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

            if (level.parMoves != level.expectedSolution.Count)
            {
                message = "Par moves must match the expected solution length.";
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
            crumbs.Remove(current);
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

                current += DirectionUtility.ToOffset(step.direction);
                if (!level.IsInside(current))
                {
                    message = "Solution leaves the board.";
                    return false;
                }

                CellContent content = level.GetContent(current);
                if (content == CellContent.Wall || content == CellContent.Toy ||
                    content == CellContent.Sock || content == CellContent.Cord ||
                    content == CellContent.WetSpot)
                {
                    message = "Solution crosses blocked or hazardous content.";
                    return false;
                }

                if (!visited.Add(current) && current != dock)
                {
                    message = "The expected route revisits a tile.";
                    return false;
                }

                crumbs.Remove(current);
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
                .Append(level.bonusPosition.y.ToString(CultureInfo.InvariantCulture));

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
