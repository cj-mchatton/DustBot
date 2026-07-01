namespace DustBot
{
    public enum PathEditResult
    {
        None,
        Started,
        Resumed,
        Trimmed,
        Added,
        Backtracked,
        Invalid,
        LimitReached
    }

    public sealed class PlayerInputController
    {
        private readonly GameSession session;

        public PlayerInputController(GameSession session)
        {
            this.session = session;
        }

        public PathEditResult BeginPath(GridPosition position)
        {
            return session.BeginPath(position);
        }

        public PathEditResult ContinuePath(GridPosition position)
        {
            return session.TryExtendPath(position);
        }

        public void EndPath()
        {
            session.EndPath();
        }

        public bool Undo()
        {
            bool changed = session.Undo();
            if (changed)
            {
                AnalyticsStub.Track("undo_used");
            }

            return changed;
        }
    }
}
