using System;
using System.Collections;
using UnityEngine;

namespace DustBot
{
    public sealed class DustBotController : MonoBehaviour
    {
        public bool Paused { get; set; }

        public IEnumerator Run(
            GameSession session,
            GameBoardView board,
            AudioManager audio,
            Action<StepOutcome> onStep,
            Action onFinished)
        {
            const float cruiseSecondsPerTile = 0.15f;
            while (session.State == GameSessionState.Simulating)
            {
                while (Paused)
                {
                    yield return null;
                }

                StepOutcome outcome = session.Advance();
                if (outcome.moved)
                {
                    audio.PlayMove();
                    if (outcome.catMoveCount > 0 || outcome.catCollision)
                    {
                        audio.PlayCatStep(outcome.catCollision);
                    }

                    yield return board.AnimateCruiseStep(
                        outcome,
                        cruiseSecondsPerTile,
                        delegate { return Paused; });
                }

                if (onStep != null)
                {
                    onStep(outcome);
                }

                if (session.State == GameSessionState.Failed)
                {
                    yield return board.AnimateFailure(outcome);
                }

            }

            if (onFinished != null)
            {
                onFinished();
            }
        }
    }
}
