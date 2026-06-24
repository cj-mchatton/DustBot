using System;
using System.Collections.Generic;

namespace DustBot
{
    public enum GameSessionState
    {
        Editing,
        Simulating,
        Won,
        Failed
    }

    public struct StepOutcome
    {
        public bool moved;
        public GridPosition from;
        public GridPosition to;
        public Direction direction;
        public bool cleanedCrumb;
        public bool collectedBonus;
        public bool won;
        public FailureReason failure;
    }

    public sealed class GameSession
    {
        private struct SimulationVisit : IEquatable<SimulationVisit>
        {
            public GridPosition position;
            public int crumbsRemaining;

            public bool Equals(SimulationVisit other)
            {
                return position == other.position && crumbsRemaining == other.crumbsRemaining;
            }

            public override bool Equals(object obj)
            {
                return obj is SimulationVisit other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (position.GetHashCode() * 397) ^ crumbsRemaining;
                }
            }
        }

        private readonly List<GridPosition> currentPathCells = new List<GridPosition>();
        private readonly Stack<List<GridPosition>> undoStack = new Stack<List<GridPosition>>();
        private readonly HashSet<GridPosition> remainingCrumbs = new HashSet<GridPosition>();
        private readonly HashSet<SimulationVisit> visits = new HashSet<SimulationVisit>();
        private int simulationPathIndex;
        private bool isDrawing;

        public LevelDefinition Level { get; private set; }
        public GridManager Grid { get; private set; }
        public GameSessionState State { get; private set; }
        public GridPosition BotPosition { get; private set; }
        public FailureReason FailureReason { get; private set; }
        public int Moves { get; private set; }
        public bool UsedHint { get; private set; }
        public bool UsedUndo { get; private set; }
        public bool CollectedBonus { get; private set; }
        public int SimulationAttempts { get; private set; }
        public int CrumbsRemaining { get { return remainingCrumbs.Count; } }
        public bool CanUndo { get { return State == GameSessionState.Editing && undoStack.Count > 0; } }
        public bool IsDrawing { get { return isDrawing; } }
        public IReadOnlyList<GridPosition> CurrentPathCells { get { return currentPathCells; } }
        public int CurrentPathLength { get { return Math.Max(0, currentPathCells.Count - 1); } }
        public int PlannedCrumbsRemaining
        {
            get
            {
                int remaining = 0;
                for (int i = 0; i < Grid.Crumbs.Count; i++)
                {
                    if (!currentPathCells.Contains(Grid.Crumbs[i]))
                    {
                        remaining++;
                    }
                }

                return remaining;
            }
        }
        public bool PlannedBonusCollected
        {
            get
            {
                return Level.objectives.collectBonus &&
                       currentPathCells.Contains(Level.bonusPosition);
            }
        }
        public bool CanStillEarnThreeStars
        {
            get
            {
                bool routeFinished = currentPathCells.Count > 1 &&
                                     currentPathCells[currentPathCells.Count - 1] == Grid.Dock;
                bool bonusPossible =
                    !Level.objectives.bonusRequiredForThreeStars ||
                    PlannedBonusCollected ||
                    !routeFinished;
                return CurrentPathLength <= Level.threeStarMoveTarget &&
                       (!Level.objectives.noHintStar || !UsedHint) &&
                       (!Level.objectives.noUndoStar || !UsedUndo) &&
                       bonusPossible;
            }
        }

        public GameSession(LevelDefinition level)
        {
            string validationMessage;
            if (!LevelValidator.TryValidate(level, out validationMessage))
            {
                throw new ArgumentException("Cannot start invalid level: " + validationMessage, "level");
            }

            Level = level;
            Grid = new GridManager(level);
            currentPathCells.Add(Grid.Start);
            ResetSimulation();
        }

        public bool HasCrumb(GridPosition position)
        {
            return remainingCrumbs.Contains(position);
        }

        public bool IsPathCell(GridPosition position)
        {
            return currentPathCells.Contains(position);
        }

        public int PathIndexOf(GridPosition position)
        {
            return currentPathCells.IndexOf(position);
        }

        public PathEditResult BeginPath(GridPosition position)
        {
            if (State != GameSessionState.Editing || position != Grid.Start)
            {
                return PathEditResult.Invalid;
            }

            if (currentPathCells.Count > 1)
            {
                SaveUndoSnapshot();
                currentPathCells.RemoveRange(1, currentPathCells.Count - 1);
            }

            isDrawing = true;
            return PathEditResult.Started;
        }

        public PathEditResult TryExtendPath(GridPosition position)
        {
            if (State != GameSessionState.Editing || !isDrawing || !Grid.IsInside(position))
            {
                return PathEditResult.Invalid;
            }

            GridPosition tail = currentPathCells[currentPathCells.Count - 1];
            if (position == tail)
            {
                return PathEditResult.None;
            }

            if (currentPathCells.Count > 1 &&
                position == currentPathCells[currentPathCells.Count - 2])
            {
                SaveUndoSnapshot();
                currentPathCells.RemoveAt(currentPathCells.Count - 1);
                return PathEditResult.Backtracked;
            }

            if (DirectionUtility.Between(tail, position) == Direction.None ||
                !Grid.CanDrawThrough(position) ||
                currentPathCells.Contains(position) ||
                Grid.GetContent(tail) == CellContent.Dock)
            {
                return PathEditResult.Invalid;
            }

            if (Level.hardPathLimit &&
                Level.moveLimit > 0 &&
                CurrentPathLength >= Level.moveLimit)
            {
                return PathEditResult.LimitReached;
            }

            SaveUndoSnapshot();
            currentPathCells.Add(position);
            return PathEditResult.Added;
        }

        public void EndPath()
        {
            isDrawing = false;
        }

        public bool Undo()
        {
            if (State != GameSessionState.Editing || undoStack.Count == 0)
            {
                return false;
            }

            RestorePath(undoStack.Pop());
            isDrawing = false;
            UsedUndo = true;
            return true;
        }

        public bool TryGetHintTarget(out GridPosition target)
        {
            List<GridPosition> expectedPath = BuildExpectedPath();
            int matchingCount = MatchingPrefixLength(expectedPath);
            if (matchingCount >= expectedPath.Count)
            {
                target = Grid.Start;
                return false;
            }

            target = expectedPath[matchingCount];
            return true;
        }

        public bool ApplyNextHint(out GridPosition target)
        {
            List<GridPosition> expectedPath = BuildExpectedPath();
            int matchingCount = MatchingPrefixLength(expectedPath);
            if (State != GameSessionState.Editing || matchingCount >= expectedPath.Count)
            {
                target = Grid.Start;
                return false;
            }

            SaveUndoSnapshot();
            if (currentPathCells.Count > matchingCount)
            {
                currentPathCells.RemoveRange(matchingCount, currentPathCells.Count - matchingCount);
            }

            target = expectedPath[matchingCount];
            currentPathCells.Add(target);
            isDrawing = false;
            UsedHint = true;
            return true;
        }

        public void ClearRoute()
        {
            if (State != GameSessionState.Editing)
            {
                return;
            }

            currentPathCells.Clear();
            currentPathCells.Add(Grid.Start);
            undoStack.Clear();
            isDrawing = false;
        }

        public bool TryGetRouteIssue(out string issue)
        {
            if (currentPathCells.Count < 2)
            {
                issue = "Drag from DustBot to draw a route.";
                return true;
            }

            if (currentPathCells[0] != Grid.Start)
            {
                issue = "The route must start at DustBot.";
                return true;
            }

            HashSet<GridPosition> visited = new HashSet<GridPosition>();
            for (int i = 0; i < currentPathCells.Count; i++)
            {
                GridPosition position = currentPathCells[i];
                if (!Grid.IsInside(position) || !Grid.CanDrawThrough(position))
                {
                    issue = "The route crosses a blocked tile.";
                    return true;
                }

                if (!visited.Add(position))
                {
                    issue = "The route cannot cross itself.";
                    return true;
                }

                if (i > 0 &&
                    DirectionUtility.Between(currentPathCells[i - 1], position) == Direction.None)
                {
                    issue = "The route must stay on neighboring tiles.";
                    return true;
                }
            }

            if (currentPathCells[currentPathCells.Count - 1] != Grid.Dock)
            {
                issue = "Route must end at the dock.";
                return true;
            }

            if (Level.hardPathLimit &&
                Level.moveLimit > 0 &&
                CurrentPathLength > Level.moveLimit)
            {
                issue = "Route exceeds the maximum path length of " + Level.moveLimit + ".";
                return true;
            }

            for (int i = 0; i < Grid.Crumbs.Count; i++)
            {
                if (!visited.Contains(Grid.Crumbs[i]))
                {
                    issue = "Route through every crumb before docking.";
                    return true;
                }
            }

            issue = string.Empty;
            return false;
        }

        public bool StartSimulation()
        {
            string issue;
            if (State != GameSessionState.Editing || TryGetRouteIssue(out issue))
            {
                return false;
            }

            ResetRuntimeState();
            SimulationAttempts++;
            simulationPathIndex = 0;
            isDrawing = false;
            State = GameSessionState.Simulating;
            visits.Add(new SimulationVisit
            {
                position = BotPosition,
                crumbsRemaining = remainingCrumbs.Count
            });
            return true;
        }

        public StepOutcome Advance()
        {
            StepOutcome outcome = new StepOutcome
            {
                from = BotPosition,
                to = BotPosition,
                failure = FailureReason.None
            };

            if (State != GameSessionState.Simulating)
            {
                return outcome;
            }

            if (simulationPathIndex + 1 >= currentPathCells.Count)
            {
                return Fail(outcome, FailureReason.GotStuck);
            }

            GridPosition next = currentPathCells[simulationPathIndex + 1];
            Direction direction = DirectionUtility.Between(BotPosition, next);
            if (direction == Direction.None)
            {
                return Fail(outcome, FailureReason.GotStuck);
            }

            outcome.direction = direction;
            outcome.to = next;
            Moves++;

            if (!Grid.IsInside(next))
            {
                return Fail(outcome, FailureReason.LeftBoard);
            }

            CellContent content = Grid.GetContent(next);
            if (content == CellContent.Wall || content == CellContent.Toy)
            {
                return Fail(outcome, FailureReason.WallBump);
            }

            BotPosition = next;
            simulationPathIndex++;
            outcome.moved = true;

            if (content == CellContent.Sock)
            {
                return Fail(outcome, FailureReason.SockJam);
            }

            if (content == CellContent.Cord)
            {
                return Fail(outcome, FailureReason.CordZap);
            }

            if (content == CellContent.WetSpot)
            {
                return Fail(outcome, FailureReason.WetSpotSlip);
            }

            if (remainingCrumbs.Remove(next))
            {
                outcome.cleanedCrumb = true;
            }

            if (Level.objectives.collectBonus && !CollectedBonus && next == Level.bonusPosition)
            {
                CollectedBonus = true;
                outcome.collectedBonus = true;
            }

            if (content == CellContent.Dock)
            {
                if (remainingCrumbs.Count == 0)
                {
                    State = GameSessionState.Won;
                    outcome.won = true;
                    return outcome;
                }

                return Fail(outcome, FailureReason.ReturnedTooEarly);
            }

            if (Level.hardPathLimit && Level.moveLimit > 0 && Moves >= Level.moveLimit)
            {
                return Fail(outcome, FailureReason.OutOfMoves);
            }

            SimulationVisit visit = new SimulationVisit
            {
                position = BotPosition,
                crumbsRemaining = remainingCrumbs.Count
            };
            if (!visits.Add(visit))
            {
                return Fail(outcome, FailureReason.LoopDetected);
            }

            return outcome;
        }

        public void ResetSimulation()
        {
            ResetRuntimeState();
            State = GameSessionState.Editing;
            isDrawing = false;
        }

        private void SaveUndoSnapshot()
        {
            undoStack.Push(new List<GridPosition>(currentPathCells));
        }

        private void RestorePath(List<GridPosition> path)
        {
            currentPathCells.Clear();
            currentPathCells.AddRange(path);
            if (currentPathCells.Count == 0 || currentPathCells[0] != Grid.Start)
            {
                currentPathCells.Clear();
                currentPathCells.Add(Grid.Start);
            }
        }

        private List<GridPosition> BuildExpectedPath()
        {
            List<GridPosition> path = new List<GridPosition>(Level.expectedSolution.Count + 1)
            {
                Grid.Start
            };

            GridPosition current = Grid.Start;
            for (int i = 0; i < Level.expectedSolution.Count; i++)
            {
                current += DirectionUtility.ToOffset(Level.expectedSolution[i].direction);
                path.Add(current);
            }

            return path;
        }

        private int MatchingPrefixLength(List<GridPosition> expectedPath)
        {
            int count = Math.Min(currentPathCells.Count, expectedPath.Count);
            int matching = 0;
            while (matching < count && currentPathCells[matching] == expectedPath[matching])
            {
                matching++;
            }

            return Math.Max(1, matching);
        }

        private void ResetRuntimeState()
        {
            BotPosition = Grid.Start;
            FailureReason = FailureReason.None;
            Moves = 0;
            CollectedBonus = false;
            remainingCrumbs.Clear();
            for (int i = 0; i < Grid.Crumbs.Count; i++)
            {
                remainingCrumbs.Add(Grid.Crumbs[i]);
            }

            visits.Clear();
        }

        private StepOutcome Fail(StepOutcome outcome, FailureReason reason)
        {
            FailureReason = reason;
            State = GameSessionState.Failed;
            outcome.failure = reason;
            return outcome;
        }
    }
}
