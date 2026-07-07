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

    public enum LevelCategory
    {
        None,
        Easy,
        Medium,
        Hard,
        Expert,
        CatChase
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

    public enum DevMazeArchetype
    {
        Baseline,
        DeadEndBranch,
        MultiRoom,
        DockReturn,
        DustBunnyDetour,
        OneWayCommitment,
        StickyShortcut,
        FragileCorridor,
        Loop,
        Chokepoint,
        ExpertLarge
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
        Toy,
        Sticky,
        Fragile,
        OneWayUp,
        OneWayRight,
        OneWayDown,
        OneWayLeft,
        Slippery
    }

    public enum CatBehavior
    {
        None,
        Sleepy,
        Curious,
        Pouncy
    }

    public enum CatPuzzleArchetype
    {
        None,
        HorizontalPriorityTrap,
        LoopAroundFurniture,
        CorridorDelay,
        ChokepointTiming,
        SafePocket,
        SplitRoom,
        DockPressure,
        DustBunnyRisk,
        CrumbOrderChase,
        NearCatch,
        CatAtChokepoint,
        LongRouteVsSafeRoute,
        CentralIsland,
        LureAwayFromCrumb,
        LureAwayFromDock,
        BacktrackBait,
        MultiCorridorPursuit,
        FurnitureDelay,
        MultiCrumbRoutePlanning
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
        CatPounce,
        FragileBreak
    }

    public static class CellContentUtility
    {
        public static bool IsWalkableFloor(CellContent content)
        {
            return content == CellContent.Empty ||
                   content == CellContent.Start ||
                   content == CellContent.Dock ||
                   content == CellContent.Crumb ||
                   content == CellContent.Sticky ||
                   content == CellContent.Fragile ||
                   content == CellContent.Slippery ||
                   IsOneWay(content);
        }

        public static bool IsDustBotBlocker(CellContent content)
        {
            return content == CellContent.Wall ||
                   content == CellContent.Toy ||
                   content == CellContent.Sock ||
                   content == CellContent.Cord ||
                   content == CellContent.WetSpot;
        }

        public static bool IsCatBlocker(CellContent content)
        {
            return content == CellContent.Wall ||
                   content == CellContent.Toy ||
                   content == CellContent.Sock ||
                   content == CellContent.Cord ||
                   content == CellContent.WetSpot;
        }

        public static bool IsRouteModifier(CellContent content)
        {
            return content == CellContent.Sticky ||
                   content == CellContent.Fragile ||
                   content == CellContent.Slippery ||
                   IsOneWay(content);
        }

        public static bool IsOneWay(CellContent content)
        {
            return content == CellContent.OneWayUp ||
                   content == CellContent.OneWayRight ||
                   content == CellContent.OneWayDown ||
                   content == CellContent.OneWayLeft;
        }

        public static Direction OneWayDirection(CellContent content)
        {
            switch (content)
            {
                case CellContent.OneWayUp: return Direction.Up;
                case CellContent.OneWayRight: return Direction.Right;
                case CellContent.OneWayDown: return Direction.Down;
                case CellContent.OneWayLeft: return Direction.Left;
                default: return Direction.None;
            }
        }

        public static bool AllowsDirection(CellContent fromContent, CellContent toContent, Direction direction)
        {
            if (direction == Direction.None)
            {
                return false;
            }

            if (IsOneWay(fromContent) && OneWayDirection(fromContent) != direction)
            {
                return false;
            }

            if (IsOneWay(toContent) && OneWayDirection(toContent) != direction)
            {
                return false;
            }

            return true;
        }

        public static int MoveCost(CellContent destinationContent)
        {
            return destinationContent == CellContent.Sticky ? 2 : 1;
        }
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
        public GenerationMode generationMode;
        public LevelCategory category;
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
        public int strategicDepthScore;
        public int catPressureScore;
        public CatPuzzleArchetype catPuzzleArchetype;
        public int catFreeParMoves;
        public bool largeMaze;
        public bool advancedDevMaze;
        public DevMazeArchetype devMazeArchetype;
        public int mazeComplexityScore;
        public string testArchetype = string.Empty;
        public string validationResult = string.Empty;
        public bool dailyChallengeStyle;
        public bool masterCleanStyle;
        public string themeId = "CozyHome";
        public string mechanicSet = "DrawPath";
        public string objectiveSet = "CleanAndDock";
        public string tutorialMessage;
        public string designPurpose;
        public string intendedStrategy;
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
        public GenerationMode generationMode;
        public LevelCategory category;
        public int generationVersion;
        public DifficultyTier difficultyTier;
        public int boardWidth;
        public int boardHeight;
        public string mechanicSet;
        public string objectiveSet;
        public string themeId;
        public LevelArchetype archetype;
        public bool useDailyChallengeProfile;
        public bool hasCatBehaviorOverride;
        public CatBehavior catBehaviorOverride;
        public bool useProceduralCatLayout;
        public CatPuzzleArchetype catPuzzleArchetype;
        public int catStartZone;
        public bool hasRouteModifierOverride;
        public RouteModifierStyle routeModifierStyle;
        public int routeModifierCountOverride;
        public string testArchetype;
        public bool dailyChallengeStyle;
        public bool masterCleanStyle;
        public bool useLargeMaze;
        public bool useAdvancedDevMaze;
        public DevMazeArchetype devMazeArchetype;
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
        public int minimumStrategicDepthScore;
        public int minimumCatPressureScore;
        public int moveLimitSlack;
        public bool hardPathLimit;
        public int minimumCrumbSpread;
        public int minimumRouteDecisions;
        public int minimumTemptingBranches;
        public int minimumBonusDetour;
        public int routeModifierCount;
        public CatBehavior catBehavior;
        public CatPuzzleArchetype catPuzzleArchetype;
        public int catStartZone;
        public RouteModifierStyle routeModifierStyle;
        public bool forcedRouteModifierStyle;
        public bool largeMaze;
        public int minimumMazeComplexityScore;
        public int minimumMazeBranches;
        public int minimumMazeDeadEnds;
        public int minimumMazeLoops;
    }

    [Serializable]
    public class LevelResult
    {
        public string levelId;
        public GameMode mode;
        public GenerationMode generationMode;
        public LevelCategory category;
        public bool catLevel;
        public bool dailyChallengeStyle;
        public bool masterCleanStyle;
        public int levelNumber;
        public int stars;
        public int coinsEarned;
        public int moves;
        public bool usedHint;
        public bool usedUndo;
        public bool collectedBonus;
        public bool bonusAvailable;
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
