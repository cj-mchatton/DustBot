using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    public enum GameMode
    {
        MainJourney,
        DailyChallenge,
        MasterClean,
        EndlessClean
    }

    public enum DifficultyTier
    {
        Tutorial,
        Beginner,
        Easy,
        Medium,
        Hard,
        Expert,
        Master
    }

    public enum LevelArchetype
    {
        SimpleRoute,
        CrumbOrder,
        BlockerMaze,
        HazardAvoidance,
        DustBunnyDetour,
        TightPath,
        Breather,
        TrickRoute,
        ChallengeRoute
    }

    public enum Direction
    {
        None,
        Up,
        Right,
        Down,
        Left
    }

    public enum CellContent
    {
        Empty,
        Start,
        Dock,
        Crumb,
        Wall,
        Sock,
        Cord,
        WetSpot,
        Toy
    }

    public enum CatBehavior
    {
        None,
        Sleepy,
        Curious,
        Pouncy
    }

    public enum FailureReason
    {
        None,
        SockJam,
        CordZap,
        WetSpotSlip,
        WallBump,
        LeftBoard,
        GotStuck,
        ReturnedTooEarly,
        OutOfMoves,
        LoopDetected,
        CatPounce
    }

    [Serializable]
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int x;
        public int y;

        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(GridPosition other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }

        public static GridPosition operator +(GridPosition left, GridPosition right)
        {
            return new GridPosition(left.x + right.x, left.y + right.y);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", x, y);
        }
    }

    public static class DirectionUtility
    {
        public static Direction Next(Direction direction)
        {
            switch (direction)
            {
                case Direction.None: return Direction.Up;
                case Direction.Up: return Direction.Right;
                case Direction.Right: return Direction.Down;
                case Direction.Down: return Direction.Left;
                default: return Direction.None;
            }
        }

        public static GridPosition ToOffset(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new GridPosition(0, 1);
                case Direction.Right: return new GridPosition(1, 0);
                case Direction.Down: return new GridPosition(0, -1);
                case Direction.Left: return new GridPosition(-1, 0);
                default: return new GridPosition(0, 0);
            }
        }

        public static Direction Between(GridPosition from, GridPosition to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            if (dx == 1 && dy == 0) return Direction.Right;
            if (dx == -1 && dy == 0) return Direction.Left;
            if (dx == 0 && dy == 1) return Direction.Up;
            if (dx == 0 && dy == -1) return Direction.Down;
            return Direction.None;
        }
    }

    [Serializable]
    public class GridCellDefinition
    {
        public GridPosition position;
        public CellContent content;

        public GridCellDefinition(GridPosition position, CellContent content)
        {
            this.position = position;
            this.content = content;
        }
    }

    [Serializable]
    public class SolutionStep
    {
        public GridPosition position;
        public Direction direction;

        public SolutionStep(GridPosition position, Direction direction)
        {
            this.position = position;
            this.direction = direction;
        }
    }

    [Serializable]
    public class LevelObjective
    {
        public bool cleanAllCrumbs = true;
        public bool returnToDock = true;
        public bool collectBonus;
        public bool noHintStar = true;
        public bool noUndoStar = true;
        public bool bonusRequiredForThreeStars;
    }

    [Serializable]
    public class CatDefinition
    {
        public CatBehavior behavior;
        public GridPosition startPosition = new GridPosition(-1, -1);
        public bool horizontalFirst = true;

        public bool IsEnabled
        {
            get { return behavior != CatBehavior.None; }
        }
    }

    [Serializable]
    public class LevelDefinition
    {
        public string id;
        public GameMode mode;
        public int levelNumber;
        public string seed;
        public int generationVersion;
        public DifficultyTier difficultyTier;
        public int width;
        public int height;
        public int parMoves;
        public int moveLimit;
        public int twoStarMoveTarget;
        public int threeStarMoveTarget;
        public bool hardPathLimit;
        public LevelArchetype archetype;
        public int engagementScore;
        public string themeId = "CozyHome";
        public string mechanicSet = "DrawPath";
        public string objectiveSet = "CleanAndDock";
        public string tutorialMessage;
        public List<GridCellDefinition> cells = new List<GridCellDefinition>();
        public List<SolutionStep> expectedSolution = new List<SolutionStep>();
        public LevelObjective objectives = new LevelObjective();
        public GridPosition bonusPosition = new GridPosition(-1, -1);
        public CatDefinition cat = new CatDefinition();

        public CellContent GetContent(GridPosition position)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].position == position)
                {
                    return cells[i].content;
                }
            }

            return CellContent.Empty;
        }

        public GridPosition Find(CellContent content)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].content == content)
                {
                    return cells[i].position;
                }
            }

            return new GridPosition(-1, -1);
        }

        public bool IsInside(GridPosition position)
        {
            return position.x >= 0 && position.y >= 0 && position.x < width && position.y < height;
        }

        public int Count(CellContent content)
        {
            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].content == content)
                {
                    count++;
                }
            }

            return count;
        }
    }

    [Serializable]
    public class LevelManifestEntry
    {
        public int levelNumber;
        public string seed;
        public int generationVersion;
        public DifficultyTier difficultyTier;
        public int boardWidth;
        public int boardHeight;
        public string mechanicSet;
        public string objectiveSet;
        public string themeId;
        public LevelArchetype archetype;
        public bool useDailyChallengeProfile;
    }

    [Serializable]
    public class LevelGenerationSettings
    {
        public int minimumPathLength;
        public int maximumPathLength;
        public int crumbCount;
        public int blockerCount;
        public int hazardCount;
        public bool includeBonus;
        public bool bonusRequiredForThreeStars;
        public int minimumTurns;
        public int minimumDetour;
        public int minimumEngagementScore;
        public int moveLimitSlack;
        public bool hardPathLimit;
        public int minimumCrumbSpread;
        public int minimumRouteDecisions;
        public int minimumTemptingBranches;
        public int minimumBonusDetour;
        public CatBehavior catBehavior;
    }

    [Serializable]
    public class LevelResult
    {
        public string levelId;
        public GameMode mode;
        public int levelNumber;
        public int stars;
        public int coinsEarned;
        public int moves;
        public bool usedHint;
        public bool usedUndo;
        public bool collectedBonus;
        public bool firstAttempt;
        public int baseCoins;
        public int starBonusCoins;
        public int bunnyBonusCoins;
        public int noHintBonusCoins;
        public int firstAttemptBonusCoins;
        public int streakBonusCoins;
        public int milestoneBonusCoins;
        public int dailyStreak;
        public string cosmeticUnlocked;
    }
}
