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
            int stepIndex = 0;
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
                    float duration = Mathf.Lerp(0.145f, 0.095f, Mathf.Clamp01(stepIndex / 10f));
                    yield return board.AnimateBotTo(
                        outcome.to,
                        outcome.direction,
                        duration,
                        delegate { return Paused; });
                }
                else
                {
                    float waitElapsed = 0f;
                    while (waitElapsed < 0.035f)
                    {
                        if (!Paused)
                        {
                            waitElapsed += Time.unscaledDeltaTime;
                        }

                        yield return null;
                    }
                }

                if (onStep != null)
                {
                    onStep(outcome);
                }

                if (session.State == GameSessionState.Failed)
                {
                    yield return board.AnimateFailure(outcome);
                }

                if (session.State == GameSessionState.Simulating)
                {
                    float waitElapsed = 0f;
                    while (waitElapsed < 0.025f)
                    {
                        if (!Paused)
                        {
                            waitElapsed += Time.unscaledDeltaTime;
                        }

                        yield return null;
                    }
                }

                stepIndex++;
            }

            if (onFinished != null)
            {
                onFinished();
            }
        }
    }
}
