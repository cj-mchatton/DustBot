namespace DustBot
{
    public sealed class HintSystem
    {
        private readonly EconomySystem economy;

        public HintSystem(EconomySystem economy)
        {
            this.economy = economy;
        }

        public bool TryUseHint(GameSession session, System.Action<GridPosition> onApplied)
        {
            GridPosition target;
            if (!session.TryGetHintTarget(out target))
            {
                return false;
            }

            int cost = CostFor(session.Level);
            if (economy.TrySpend(cost))
            {
                Apply(session, onApplied);
                return true;
            }

            return false;
        }

        public static int CostFor(LevelDefinition level)
        {
            if (level.mode == GameMode.MainJourney &&
                level.levelNumber <= EconomyConfig.FreeTutorialHintThroughLevel)
            {
                return 0;
            }

            return level.mode == GameMode.DailyChallenge
                ? EconomyConfig.DailyHintCost
                : EconomyConfig.NormalHintCost;
        }

        private static void Apply(
            GameSession session,
            System.Action<GridPosition> onApplied)
        {
            GridPosition target;
            if (!session.ApplyNextHint(out target))
            {
                return;
            }

            AnalyticsStub.Track("hint_used");
            if (onApplied != null)
            {
                onApplied(target);
            }
        }
    }
}
