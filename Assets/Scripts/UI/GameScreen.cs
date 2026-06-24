using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DustBot
{
    public sealed class GameScreen : MonoBehaviour
    {
        private DustBotApp app;
        private LevelDefinition level;
        private GameSession session;
        private PlayerInputController input;
        private HintSystem hints;
        private GameBoardView board;
        private DustBotController botController;
        private Text statusText;
        private Text coinText;
        private Button playButton;
        private Button undoButton;
        private Button resetButton;
        private Button hintButton;
        private Coroutine simulation;
        private GameObject modal;
        private int dailyAttemptCount;

        public void Initialize(DustBotApp application, LevelDefinition definition)
        {
            if (application == null) throw new System.ArgumentNullException("application");
            if (definition == null) throw new System.ArgumentNullException("definition");

            app = application;
            level = definition;
            session = new GameSession(level);
            input = new PlayerInputController(session);
            hints = new HintSystem(app.Economy);
            botController = gameObject.AddComponent<DustBotController>();

            Build();
            UpdateHud();
            AnalyticsStub.Track(LevelStartEvent(level.mode));
        }

        private void Build()
        {
            Image headerCard = UIFactory.CreatePanel("Header Card", transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(
                headerCard.rectTransform,
                new Vector2(0.025f, 0.905f),
                new Vector2(0.975f, 0.99f),
                Vector2.zero,
                Vector2.zero);
            Shadow headerShadow = headerCard.gameObject.AddComponent<Shadow>();
            headerShadow.effectColor = new Color(0.08f, 0.14f, 0.11f, 0.12f);
            headerShadow.effectDistance = new Vector2(0f, -4f);

            Image briefingCard = UIFactory.CreatePanel("Briefing Card", transform, DustBotTheme.PanelSoft);
            UIFactory.SetAnchors(
                briefingCard.rectTransform,
                new Vector2(0.045f, 0.745f),
                new Vector2(0.955f, 0.902f),
                Vector2.zero,
                Vector2.zero);

            Text modeText = UIFactory.CreateText(
                "Level Title",
                transform,
                LevelTitle(level),
                48,
                DustBotTheme.Ink);
            UIFactory.SetAnchors(modeText.rectTransform, new Vector2(0.22f, 0.91f), new Vector2(0.78f, 0.985f), Vector2.zero, Vector2.zero);

            Button pause = UIFactory.CreateButton(
                "Pause",
                transform,
                "PAUSE",
                ShowPause,
                DustBotTheme.MutedInk,
                24);
            UIFactory.SetAnchors(pause.GetComponent<RectTransform>(), new Vector2(0.035f, 0.92f), new Vector2(0.21f, 0.975f), Vector2.zero, Vector2.zero);

            coinText = UIFactory.CreateText(
                "Coins",
                transform,
                string.Empty,
                24,
                DustBotTheme.MintDark);
            UIFactory.SetAnchors(coinText.rectTransform, new Vector2(0.78f, 0.92f), new Vector2(0.97f, 0.975f), Vector2.zero, Vector2.zero);

            Text tutorial = UIFactory.CreateText(
                "Tutorial",
                transform,
                LevelBriefing(level),
                level.difficultyTier == DifficultyTier.Tutorial ? 27 : 24,
                DustBotTheme.MutedInk);
            UIFactory.SetAnchors(tutorial.rectTransform, new Vector2(0.07f, 0.82f), new Vector2(0.93f, 0.91f), new Vector2(0f, 2f), new Vector2(0f, -2f));

            statusText = UIFactory.CreateText(
                "Status",
                transform,
                string.Empty,
                22,
                DustBotTheme.Ink);
            UIFactory.SetAnchors(statusText.rectTransform, new Vector2(0.06f, 0.755f), new Vector2(0.94f, 0.82f), Vector2.zero, Vector2.zero);

            float boardWidth = 920f;
            float boardHeight = boardWidth * level.height / level.width;
            if (boardHeight > 970f)
            {
                boardHeight = 970f;
                boardWidth = boardHeight * level.width / level.height;
            }

            Image boardFrame = UIFactory.CreatePanel("Board Frame", transform, DustBotTheme.Panel);
            RectTransform frameRect = boardFrame.rectTransform;
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = new Vector2(0f, -55f);
            frameRect.sizeDelta = new Vector2(boardWidth + 30f, boardHeight + 30f);
            Shadow boardShadow = boardFrame.gameObject.AddComponent<Shadow>();
            boardShadow.effectColor = new Color(0.08f, 0.16f, 0.12f, 0.18f);
            boardShadow.effectDistance = new Vector2(0f, -8f);
            Outline boardRim = boardFrame.gameObject.AddComponent<Outline>();
            boardRim.effectColor = new Color(DustBotTheme.MintDark.r, DustBotTheme.MintDark.g, DustBotTheme.MintDark.b, 0.18f);
            boardRim.effectDistance = new Vector2(3f, -3f);

            GameObject boardObject = UIFactory.CreateUIObject("Board", boardFrame.transform);
            RectTransform boardRect = UIFactory.Stretch(boardObject);
            boardRect.offsetMin = Vector2.one * 15f;
            boardRect.offsetMax = Vector2.one * -15f;
            board = boardObject.AddComponent<GameBoardView>();
            board.Initialize(level, session, input, app.Cosmetics, OnPathEdited);

            playButton = CreateBottomButton("PLAY", OnPlay, DustBotTheme.Mint, 0.035f, 0.37f, 31);
            undoButton = CreateBottomButton("UNDO", OnUndo, DustBotTheme.Blue, 0.39f, 0.585f, 25);
            resetButton = CreateBottomButton("RESET", OnReset, DustBotTheme.MutedInk, 0.605f, 0.79f, 25);
            hintButton = CreateBottomButton(HintButtonLabel(), OnHint, DustBotTheme.Coral, 0.81f, 0.965f, 25);
        }

        private Button CreateBottomButton(
            string label,
            UnityEngine.Events.UnityAction action,
            Color color,
            float left,
            float right,
            int fontSize)
        {
            Button button = UIFactory.CreateButton(label, transform, label, action, color, fontSize);
            UIFactory.SetAnchors(
                button.GetComponent<RectTransform>(),
                new Vector2(left, 0.045f),
                new Vector2(right, 0.125f),
                Vector2.zero,
                Vector2.zero);
            return button;
        }

        private void OnPathEdited(PathEditResult result, GridPosition position)
        {
            if (result == PathEditResult.Invalid)
            {
                app.Haptics.Error();
            }
            else if (result == PathEditResult.LimitReached)
            {
                app.Haptics.Error();
                StartCoroutine(FlashStatus(
                    "MAX PATH " + level.moveLimit + " — drag backward or Undo."));
            }
            else
            {
                app.Audio.PlayPathEdit();
                app.Haptics.LightTap();
            }

            UpdateHud();
        }

        private void OnPlay()
        {
            string issue;
            if (session.TryGetRouteIssue(out issue))
            {
                app.Haptics.Error();
                StartCoroutine(FlashStatus(issue));
                return;
            }

            if (!session.StartSimulation())
            {
                return;
            }

            if (level.mode == GameMode.DailyChallenge)
            {
                dailyAttemptCount = app.RegisterDailyAttempt();
                if (dailyAttemptCount > 1)
                {
                    StartCoroutine(FlashStatus("Daily attempt " + dailyAttemptCount + " — plan the clean."));
                }
            }

            SetEditingButtons(false);
            board.RefreshAll();
            UpdateHud();
            simulation = StartCoroutine(botController.Run(
                session,
                board,
                app.Audio,
                OnSimulationStep,
                OnSimulationFinished));
        }

        private void OnUndo()
        {
            if (input.Undo())
            {
                board.RefreshAll();
                UpdateHud();
            }
        }

        private void OnReset()
        {
            ResetAttempt(session.State == GameSessionState.Editing);
        }

        private void RetryAttempt()
        {
            ResetAttempt(false);
        }

        private void ResetAttempt(bool clearRoute)
        {
            botController.Paused = false;
            if (simulation != null)
            {
                StopCoroutine(simulation);
                simulation = null;
            }

            if (clearRoute)
            {
                session.ClearRoute();
            }

            session.ResetSimulation();
            CloseModal();
            board.RefreshAll();
            SetEditingButtons(true);
            UpdateHud();
        }

        private void OnHint()
        {
            GridPosition target;
            bool routeNeedsHint = session.TryGetHintTarget(out target);
            bool applied = hints.TryUseHint(session, delegate(GridPosition position)
            {
                app.Audio.PlayHint();
                app.Haptics.LightTap();
                board.RefreshAll();
                board.PlayTileFeedback(position);
                app.SaveNow();
                UpdateHud();
            });

            if (!applied)
            {
                StartCoroutine(FlashStatus(
                    routeNeedsHint
                        ? HintSystem.CostFor(level) == 0
                            ? "This tutorial hint is free."
                            : string.Format(
                                "Hints cost {0} coins. Daily hints remove the no-hint bonus.",
                                HintSystem.CostFor(level))
                        : "Route already matches the solution."));
            }
        }

        private void OnSimulationStep(StepOutcome outcome)
        {
            if (outcome.cleanedCrumb)
            {
                app.Audio.PlayCrumbClean();
                board.RefreshCell(outcome.to);
                board.PlayCrumbFeedback(outcome.to);
            }

            if (outcome.collectedBonus)
            {
                app.Audio.PlayReward();
                board.RefreshCell(outcome.to);
            }

            UpdateHud();
        }

        private void OnSimulationFinished()
        {
            simulation = null;
            if (session.State == GameSessionState.Won)
            {
                HandleWin();
            }
            else
            {
                HandleFailure();
            }
        }

        private void HandleWin()
        {
            LevelResult result = ObjectiveSystem.BuildResult(session);
            if (level.mode == GameMode.DailyChallenge)
            {
                result.firstAttempt = dailyAttemptCount <= 1;
            }
            app.CommitResult(result);
            UpdateHud();

            app.Audio.PlayDock();
            app.Audio.PlayWin();
            app.Haptics.Success();
            AnalyticsStub.Track(LevelCompleteEvent(level.mode));
            StartCoroutine(ShowWinSequence(result));
        }

        private IEnumerator ShowWinSequence(LevelResult result)
        {
            yield return board.AnimateWin();
            string nextText = NextLevelText();
            string details = string.Format(
                "{0}\n{1}\n+{2} Dust Coins{3}{4}{5}",
                StarLine(result.stars),
                PerformanceMessage(result),
                result.coinsEarned,
                result.collectedBonus
                    ? string.Format("\nDust Bunny +{0}", result.bunnyBonusCoins)
                    : level.objectives.collectBonus ? "\nDust Bunny missed — replay to collect it." : string.Empty,
                RewardDetail(result),
                nextText);
            ShowModal("ROOM SPARKLING!", details, true);
        }

        private void HandleFailure()
        {
            app.Audio.PlayFail();
            app.Haptics.Failure();
            AnalyticsStub.Track("level_fail");
            ShowModal("CLEANING INTERRUPTED", FailureText(session.FailureReason), false);
        }

        private void ShowPause()
        {
            if (modal != null ||
                session.State == GameSessionState.Won ||
                session.State == GameSessionState.Failed)
            {
                return;
            }

            botController.Paused = true;

            modal = UIFactory.CreateUIObject("Pause Modal", transform);
            UIFactory.Stretch(modal);
            Image shade = modal.AddComponent<Image>();
            shade.color = DustBotTheme.Overlay;

            Image panel = UIFactory.CreatePanel("Panel", modal.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(panel.rectTransform, new Vector2(0.12f, 0.31f), new Vector2(0.88f, 0.69f), Vector2.zero, Vector2.zero);
            panel.rectTransform.localScale = Vector3.one * 0.9f;

            Text heading = UIFactory.CreateText("Heading", panel.transform, "PAUSED", 52, DustBotTheme.Ink);
            UIFactory.SetAnchors(heading.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

            Button resume = UIFactory.CreateButton("Resume", panel.transform, "RESUME", ResumeFromPause, DustBotTheme.Mint, 32);
            UIFactory.SetAnchors(resume.GetComponent<RectTransform>(), new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);
            Button restart = UIFactory.CreateButton("Restart", panel.transform, "EDIT ROUTE", RetryAttempt, DustBotTheme.Blue, 27);
            UIFactory.SetAnchors(restart.GetComponent<RectTransform>(), new Vector2(0.08f, 0.2f), new Vector2(0.46f, 0.38f), Vector2.zero, Vector2.zero);
            Button home = UIFactory.CreateButton("Home", panel.transform, "HOME", app.UI.ShowMainMenu, DustBotTheme.MutedInk, 27);
            UIFactory.SetAnchors(home.GetComponent<RectTransform>(), new Vector2(0.54f, 0.2f), new Vector2(0.92f, 0.38f), Vector2.zero, Vector2.zero);
            StartCoroutine(AnimateModalIn(panel.rectTransform));
        }

        private void ResumeFromPause()
        {
            CloseModal();
            botController.Paused = false;
        }

        private void ShowModal(string title, string details, bool won)
        {
            CloseModal();
            modal = UIFactory.CreateUIObject("Result Modal", transform);
            UIFactory.Stretch(modal);
            Image shade = modal.AddComponent<Image>();
            shade.color = DustBotTheme.Overlay;

            Image panel = UIFactory.CreatePanel("Panel", modal.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(panel.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);
            panel.rectTransform.localScale = Vector3.one * 0.9f;

            Text heading = UIFactory.CreateText("Heading", panel.transform, title, 48, won ? DustBotTheme.MintDark : DustBotTheme.Coral);
            UIFactory.SetAnchors(heading.rectTransform, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.93f), Vector2.zero, Vector2.zero);
            Text body = UIFactory.CreateText("Details", panel.transform, details, 29, DustBotTheme.Ink);
            UIFactory.SetAnchors(body.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.73f), Vector2.zero, Vector2.zero);

            if (won)
            {
                Button next = UIFactory.CreateButton("Next", panel.transform, NextButtonLabel(), OnNext, DustBotTheme.Mint, 34);
                UIFactory.SetAnchors(next.GetComponent<RectTransform>(), new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.35f), Vector2.zero, Vector2.zero);
                Button replay = UIFactory.CreateButton("Replay", panel.transform, "REPLAY", RetryAttempt, DustBotTheme.Blue, 24);
                UIFactory.SetAnchors(replay.GetComponent<RectTransform>(), new Vector2(0.08f, 0.045f), new Vector2(0.46f, 0.14f), Vector2.zero, Vector2.zero);
                Button home = UIFactory.CreateButton("Home", panel.transform, "HOME", app.UI.ShowMainMenu, DustBotTheme.MutedInk, 31);
                UIFactory.GetButtonText(home).fontSize = 24;
                UIFactory.SetAnchors(home.GetComponent<RectTransform>(), new Vector2(0.54f, 0.045f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero);
            }
            else
            {
                Button retry = UIFactory.CreateButton("Retry", panel.transform, "RETRY", RetryAttempt, DustBotTheme.Mint, 31);
                UIFactory.SetAnchors(retry.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.46f, 0.31f), Vector2.zero, Vector2.zero);
                Button hint = UIFactory.CreateButton("Hint", panel.transform, "HINT", delegate
                {
                    RetryAttempt();
                    OnHint();
                }, DustBotTheme.Coral, 31);
                UIFactory.SetAnchors(hint.GetComponent<RectTransform>(), new Vector2(0.54f, 0.12f), new Vector2(0.92f, 0.31f), Vector2.zero, Vector2.zero);
            }

            StartCoroutine(AnimateModalIn(panel.rectTransform));
        }

        private void OnNext()
        {
            switch (level.mode)
            {
                case GameMode.MainJourney:
                    if (level.levelNumber >= LevelManifest.MainJourneyLevelCount)
                    {
                        app.StartMaster();
                    }
                    else
                    {
                        app.StartMainLevel(level.levelNumber + 1);
                    }
                    break;
                case GameMode.MasterClean:
                    app.UI.ShowGame(app.Levels.LoadMaster(level.levelNumber + 1));
                    break;
                case GameMode.EndlessClean:
                    app.UI.ShowGame(app.Levels.LoadEndless(
                        app.Progression.Data.endlessRunSeed,
                        level.levelNumber + 1));
                    break;
                default:
                    app.UI.ShowMainMenu();
                    break;
            }
        }

        private void SetEditingButtons(bool editing)
        {
            playButton.interactable = editing;
            undoButton.interactable = editing && session.CanUndo;
            hintButton.interactable = editing;
            resetButton.interactable = true;
            UIFactory.GetButtonText(resetButton).text = editing ? "RESET" : "EDIT";
        }

        private void UpdateHud()
        {
            int displayedMoves = session.State == GameSessionState.Editing
                ? Mathf.Max(0, session.CurrentPathCells.Count - 1)
                : session.Moves;
            string moveText = level.hardPathLimit
                ? string.Format(
                    "PATH {0}  •  PERFECT {1}  •  MAX {2}",
                    displayedMoves,
                    level.threeStarMoveTarget,
                    level.moveLimit)
                : string.Format(
                    "PATH {0}  •  PERFECT {1}  •  2★ {2}",
                    displayedMoves,
                    level.threeStarMoveTarget,
                    level.twoStarMoveTarget);
            string bonus = level.objectives.collectBonus
                ? string.Format(
                    "  •  BUNNY {0}",
                    (session.State == GameSessionState.Editing
                        ? session.PlannedBonusCollected
                        : session.CollectedBonus)
                        ? "ROUTED"
                        : "WAITING")
                : string.Empty;
            string perfect = session.CanStillEarnThreeStars
                ? "PERFECT CLEAN AVAILABLE"
                : "REPLAY FOR PERFECT CLEAN";
            statusText.text = string.Format(
                "CRUMBS {0}{1}\n{2}  •  {3}",
                session.State == GameSessionState.Editing
                    ? session.PlannedCrumbsRemaining
                    : session.CrumbsRemaining,
                bonus,
                moveText,
                perfect);
            statusText.color = session.CanStillEarnThreeStars
                ? DustBotTheme.Ink
                : DustBotTheme.Coral;
            coinText.text = string.Format("{0}\nCOINS", app.Economy.Coins);
            if (session.State == GameSessionState.Editing)
            {
                undoButton.interactable = session.CanUndo;
            }
        }

        private IEnumerator FlashStatus(string message)
        {
            string previous = statusText.text;
            Color previousColor = statusText.color;
            statusText.text = message;
            statusText.color = DustBotTheme.Coral;
            yield return new WaitForSecondsRealtime(1.25f);
            statusText.color = previousColor;
            statusText.text = previous;
        }

        private static IEnumerator AnimateModalIn(RectTransform panel)
        {
            float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration && panel != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                panel.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
                yield return null;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }
        }

        private void CloseModal()
        {
            if (modal != null)
            {
                Destroy(modal);
                modal = null;
            }
        }

        private static string LevelTitle(LevelDefinition definition)
        {
            switch (definition.mode)
            {
                case GameMode.DailyChallenge: return "DAILY CHALLENGE";
                case GameMode.MasterClean: return "MASTER CLEAN " + definition.levelNumber;
                case GameMode.EndlessClean: return "ENDLESS " + definition.levelNumber;
                default: return "LEVEL " + definition.levelNumber;
            }
        }

        private static string LevelBriefing(LevelDefinition definition)
        {
            if (!string.IsNullOrEmpty(definition.tutorialMessage))
            {
                return definition.tutorialMessage;
            }

            if (definition.mode == GameMode.DailyChallenge)
            {
                return string.Format(
                    "SPECIAL {0} • Plan every turn, collect the Dust Bunny, and avoid hints for a perfect daily clean.",
                    ArchetypeLabel(definition.archetype));
            }

            return string.Format(
                "{0} • Drag through every crumb, then finish at the dock.",
                ArchetypeLabel(definition.archetype));
        }

        private static string ArchetypeLabel(LevelArchetype archetype)
        {
            switch (archetype)
            {
                case LevelArchetype.CrumbOrder: return "CRUMB ORDER";
                case LevelArchetype.BlockerMaze: return "BLOCKER MAZE";
                case LevelArchetype.HazardAvoidance: return "HAZARD ROUTE";
                case LevelArchetype.DustBunnyDetour: return "BUNNY DETOUR";
                case LevelArchetype.TightPath: return "TIGHT PATH";
                case LevelArchetype.Breather: return "BREATHER";
                case LevelArchetype.TrickRoute: return "TRICK ROUTE";
                case LevelArchetype.ChallengeRoute: return "CHALLENGE ROUTE";
                default: return "SIMPLE ROUTE";
            }
        }

        private static string FailureText(FailureReason reason)
        {
            switch (reason)
            {
                case FailureReason.SockJam: return "Sock jam. DustBot has become a very small laundry basket.";
                case FailureReason.CordZap: return "Cord zap! Route around loose cables.";
                case FailureReason.WetSpotSlip: return "Wet spot slip. Graceful? No. Recoverable? Absolutely.";
                case FailureReason.WallBump: return "Bonk. Furniture remains undefeated.";
                case FailureReason.LeftBoard: return "DustBot left the room. Keep the route inside the grid.";
                case FailureReason.ReturnedTooEarly: return "Back at the dock, but crumbs are still plotting.";
                case FailureReason.OutOfMoves: return "Maximum path length reached. Redraw a shorter, cleaner route.";
                case FailureReason.LoopDetected: return "DustBot found an infinite cleaning career.";
                default: return "DustBot got stuck. Redraw the next part of the route.";
            }
        }

        private static string StarLine(int stars)
        {
            if (stars == 3) return "★  ★  ★";
            if (stars == 2) return "★  ★  ☆";
            return "★  ☆  ☆";
        }

        private string HintButtonLabel()
        {
            int cost = HintSystem.CostFor(level);
            return cost == 0 ? "FREE HINT" : "HINT " + cost;
        }

        private static string RewardDetail(LevelResult result)
        {
            string detail = string.Empty;
            if (result.milestoneBonusCoins > 0)
                detail += "\nMilestone +" + result.milestoneBonusCoins;
            if (result.noHintBonusCoins > 0)
                detail += "\nNo-Hint Bonus +" + result.noHintBonusCoins;
            if (result.firstAttemptBonusCoins > 0)
                detail += "\nFirst-Attempt Bonus +" + result.firstAttemptBonusCoins;
            if (result.streakBonusCoins > 0)
                detail += "\nDay " + result.dailyStreak + " Streak +" + result.streakBonusCoins;
            if (!string.IsNullOrEmpty(result.cosmeticUnlocked))
                detail += "\nUnlocked: " + result.cosmeticUnlocked;
            return detail;
        }

        private string PerformanceMessage(LevelResult result)
        {
            if (result.stars >= 3)
            {
                return result.collectedBonus && level.objectives.collectBonus
                    ? "PERFECT CLEAN • SHORTEST ROUTE • DUST BUNNY!"
                    : "PERFECT CLEAN • SHORTEST ROUTE BONUS!";
            }

            if (level.objectives.collectBonus && !result.collectedBonus)
            {
                return "DUST BUNNY MISSED • REPLAY FOR 3 STARS";
            }

            if (result.usedHint)
            {
                return "NO-HINT BONUS MISSED • REPLAY FOR PERFECT CLEAN";
            }

            return result.stars == 2
                ? "GOOD CLEAN • FIND THE SHORTEST ROUTE FOR 3 STARS"
                : "ROUTE TOO LONG • REPLAY FOR 3 STARS";
        }

        private string NextLevelText()
        {
            if (level.mode == GameMode.MainJourney && level.levelNumber < LevelManifest.MainJourneyLevelCount)
            {
                return string.Format("\nLevel {0} is ready.", level.levelNumber + 1);
            }

            if (level.mode == GameMode.DailyChallenge)
            {
                return "\nDaily challenge complete.";
            }

            return "\nThe next room is ready.";
        }

        private string NextButtonLabel()
        {
            if (level.mode == GameMode.DailyChallenge)
            {
                return "DONE";
            }

            if (level.mode == GameMode.MainJourney &&
                level.levelNumber >= LevelManifest.MainJourneyLevelCount)
            {
                return "MASTER CLEAN";
            }

            return "NEXT ROOM";
        }

        public void PrepareForStoreScreenshot(int routeSteps)
        {
            routeSteps = Mathf.Clamp(routeSteps, 0, level.expectedSolution.Count);
            session.BeginPath(session.Grid.Start);
            for (int i = 0; i < routeSteps; i++)
            {
                SolutionStep step = level.expectedSolution[i];
                GridPosition next = step.position + DirectionUtility.ToOffset(step.direction);
                if (session.TryExtendPath(next) != PathEditResult.Added)
                {
                    break;
                }
            }
            session.EndPath();

            board.RefreshAll();
            UpdateHud();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus &&
                modal == null &&
                session != null &&
                session.State != GameSessionState.Won &&
                session.State != GameSessionState.Failed)
            {
                ShowPause();
            }
        }

        private static string LevelStartEvent(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.DailyChallenge: return "daily_start";
                case GameMode.MasterClean: return "master_level_start";
                default: return "level_start";
            }
        }

        private static string LevelCompleteEvent(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.DailyChallenge: return "daily_complete";
                case GameMode.MasterClean: return "master_level_complete";
                case GameMode.EndlessClean: return "endless_level_reached";
                default: return "level_complete";
            }
        }
    }
}
