using System.Collections.Generic;

namespace DustBot
{
    public sealed class GridManager
    {
        private readonly CellContent[,] contents;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public GridPosition Start { get; private set; }
        public GridPosition Dock { get; private set; }
        public IReadOnlyList<GridPosition> Crumbs { get; private set; }

        public GridManager(LevelDefinition level)
        {
            if (level == null)
            {
                throw new System.ArgumentNullException("level");
            }

            Width = level.width;
            Height = level.height;
            contents = new CellContent[Width, Height];
            List<GridPosition> crumbs = new List<GridPosition>();

            for (int i = 0; i < level.cells.Count; i++)
            {
                GridCellDefinition cell = level.cells[i];
                contents[cell.position.x, cell.position.y] = cell.content;
                if (cell.content == CellContent.Start)
                {
                    Start = cell.position;
                }
                else if (cell.content == CellContent.Dock)
                {
                    Dock = cell.position;
                }
                else if (cell.content == CellContent.Crumb)
                {
                    crumbs.Add(cell.position);
                }
            }

            Crumbs = crumbs;
        }

        public bool IsInside(GridPosition position)
        {
            return position.x >= 0 && position.y >= 0 && position.x < Width && position.y < Height;
        }

        public CellContent GetContent(GridPosition position)
        {
            return IsInside(position) ? contents[position.x, position.y] : CellContent.Empty;
        }

        public bool CanDrawThrough(GridPosition position)
        {
            CellContent content = GetContent(position);
            return CellContentUtility.IsWalkableFloor(content);
        }

        public bool CanDrawMove(GridPosition from, GridPosition to)
        {
            if (!IsInside(from) || !IsInside(to) || !CanDrawThrough(to))
            {
                return false;
            }

            Direction direction = DirectionUtility.Between(from, to);
            return CellContentUtility.AllowsDirection(
                GetContent(from),
                GetContent(to),
                direction);
        }

        public int MoveCost(GridPosition destination)
        {
            return CellContentUtility.MoveCost(GetContent(destination));
        }
    }
}
