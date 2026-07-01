namespace DustBot
{
    public static class ObjectiveSystem
    {
        public static LevelResult BuildResult(GameSession session)
        {
            int stars = 1;
            int twoStarTarget = session.Level.twoStarMoveTarget > 0
                ? session.Level.twoStarMoveTarget
                : session.Level.parMoves + 2;
            int threeStarTarget = session.Level.threeStarMoveTarget > 0
                ? session.Level.threeStarMoveTarget
                : session.Level.parMoves;

            if (session.Level.parMoves <= 0 || session.Moves <= twoStarTarget)
            {
                stars++;
            }

            bool hintGoalMet =
                !session.Level.objectives.noHintStar ||
                !session.UsedHint;
            bool undoGoalMet =
                !session.Level.objectives.noUndoStar ||
                !session.UsedUndo;
            bool moveGoalMet = session.Level.parMoves <= 0 || session.Moves <= threeStarTarget;
            bool bonusGoalMet =
                !session.Level.objectives.bonusRequiredForThreeStars ||
                session.CollectedBonus;
            if (stars >= 2 && moveGoalMet && hintGoalMet && undoGoalMet && bonusGoalMet)
            {
                stars++;
            }

            bool daily = session.Level.mode == GameMode.DailyChallenge ||
                         session.Level.dailyChallengeStyle;
            int coins =
                (daily ? EconomyConfig.DailyBaseCompletionCoins : EconomyConfig.BaseLevelCompletionCoins) +
                EconomyConfig.StarBonusFor(stars, daily) +
                (session.CollectedBonus
                    ? daily
                        ? EconomyConfig.DailyDustBunnyBonusCoins
                        : EconomyConfig.DustBunnyBonusCoins
                    : 0);

            return new LevelResult
            {
                levelId = session.Level.id,
                mode = session.Level.mode,
                generationMode = session.Level.generationMode,
                dailyChallengeStyle = session.Level.dailyChallengeStyle,
                masterCleanStyle = session.Level.masterCleanStyle,
                levelNumber = session.Level.levelNumber,
                stars = stars,
                coinsEarned = coins,
                moves = session.Moves,
                usedHint = session.UsedHint,
                usedUndo = session.UsedUndo,
                collectedBonus = session.CollectedBonus,
                firstAttempt = session.SimulationAttempts <= 1
            };
        }
    }
}
