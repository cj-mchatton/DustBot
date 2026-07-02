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
        private Button resetButton;
        private Button hintButton;
        private Coroutine simulation;
        private GameObject modal;
        private int dailyAttemptCount;
        private bool catAttemptRegistered;

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
            UIFactory.SetAnchors(
                coinText.rectTransform,
                LevelGenerationConfig.DeveloperToolsEnabled ? new Vector2(0.71f, 0.92f) : new Vector2(0.78f, 0.92f),
                LevelGenerationConfig.DeveloperToolsEnabled ? new Vector2(0.86f, 0.975f) : new Vector2(0.97f, 0.975f),
                Vector2.zero,
                Vector2.zero);
            if (LevelGenerationConfig.DeveloperToolsEnabled)
            {
                Button developer = UIFactory.CreateButton(
                    "Developer Panel",
                    transform,
                    "DEV",
                    app.UI.ShowDeveloperPanel,
                    DustBotTheme.Coral,
                    18);
                UIFactory.SetAnchors(
                    developer.GetComponent<RectTransform>(),
                    new Vector2(0.87f, 0.925f),
                    new Vector2(0.97f, 0.972f),
                    Vector2.zero,
                    Vector2.zero);
            }

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
            float boardHeight = level.largeMaze
                ? 970f
                : boardWidth * level.height / level.width;
            if (!level.largeMaze && boardHeight > 970f)
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

            GameObject viewportObject = UIFactory.CreateUIObject("Board Viewport", boardFrame.transform);
            RectTransform viewportRect = UIFactory.Stretch(viewportObject);
            viewportRect.offsetMin = Vector2.one * 15f;
            viewportRect.offsetMax = Vector2.one * -15f;
            viewportObject.AddComponent<RectMask2D>();

            GameObject boardObject = UIFactory.CreateUIObject("Board", viewportObject.transform);
            RectTransform boardRect = boardObject.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.anchoredPosition = Vector2.zero;
            float viewportWidth = boardWidth;
            float viewportHeight = boardHeight;
            float fitCell = Mathf.Min(
                viewportWidth / level.width,
                viewportHeight / level.height);
            float cellSize = level.largeMaze
                ? Mathf.Max(58f, fitCell)
                : fitCell;
            boardRect.sizeDelta = new Vector2(
                cellSize * level.width,
                cellSize * level.height);
            board = boardObject.AddComponent<GameBoardView>();
            board.Initialize(
                level,
                session,
                input,
                app.Cosmetics,
                OnPathEdited,
                OnCatSwipe,
                viewportRect);

            if (level.largeMaze)
            {
                CreateBoardCameraButton(
                    boardFrame.transform,
                    "Zoom Out",
                    "−",
                    new Vector2(1.005f, 0.91f),
                    new Vector2(1.065f, 0.985f),
                    delegate { board.AdjustZoom(-0.18f); });
                CreateBoardCameraButton(
                    boardFrame.transform,
                    "Center Maze",
                    "◎",
                    new Vector2(1.005f, 0.825f),
                    new Vector2(1.065f, 0.9f),
                    board.ResetCamera);
                CreateBoardCameraButton(
                    boardFrame.transform,
                    "Zoom In",
                    "+",
                    new Vector2(1.005f, 0.74f),
                    new Vector2(1.065f, 0.815f),
                    delegate { board.AdjustZoom(0.18f); });
            }

            Image actionBar = UIFactory.CreatePanel(
                "Action Bar",
                transform,
                new Color(1f, 0.99f, 0.96f, 0.98f));
            UIFactory.SetAnchors(
                actionBar.rectTransform,
                new Vector2(0.025f, 0.032f),
                new Vector2(0.975f, 0.142f),
                Vector2.zero,
                Vector2.zero);
            Shadow actionShadow = actionBar.gameObject.AddComponent<Shadow>();
            actionShadow.effectColor = new Color(0.08f, 0.14f, 0.11f, 0.14f);
            actionShadow.effectDistance = new Vector2(0f, -5f);
            Outline actionRim = actionBar.gameObject.AddComponent<Outline>();
            actionRim.effectColor = new Color(
                DustBotTheme.MintDark.r,
                DustBotTheme.MintDark.g,
                DustBotTheme.MintDark.b,
                0.12f);
            actionRim.effectDistance = new Vector2(2f, -2f);

            playButton = CreateBottomButton(
                actionBar.transform,
                session.HasCat ? "SWIPE TO MOVE" : "PLAY",
                session.HasCat ? null : (UnityEngine.Events.UnityAction)OnPlay,
                DustBotTheme.Mint,
                0.012f,
                0.545f,
                session.HasCat ? 25 : 32);
            resetButton = CreateBottomButton(
                actionBar.transform,
                session.HasCat ? "RESTART" : "RESET",
                OnReset,
                DustBotTheme.Blue,
                0.565f,
                0.76f,
                24);
            hintButton = CreateBottomButton(
                actionBar.transform,
                "HINT",
                OnHint,
                DustBotTheme.Coral,
                0.78f,
                0.988f,
                24);
            if (session.HasCat)
            {
                playButton.interactable = false;
            }
        }

        private Button CreateBottomButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action,
            Color color,
            float left,
            float right,
            int fontSize)
        {
            Button button = UIFactory.CreateButton(label, parent, label, action, color, fontSize);
            UIFactory.SetAnchors(
                button.GetComponent<RectTransform>(),
                new Vector2(left, 0.12f),
                new Vector2(right, 0.88f),
                Vector2.zero,
                Vector2.zero);
            return button;
        }

        private static void CreateBoardCameraButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityEngine.Events.UnityAction action)
        {
            Button button = UIFactory.CreateButton(
                name,
                parent,
                label,
                action,
                new Color(0.12f, 0.2f, 0.18f, 0.88f),
                26);
            UIFactory.SetAnchors(
                button.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax,
                Vector2.zero,
                Vector2.zero);
            button.transform.SetAsLastSibling();
        }

        private void OnPathEdited(PathEditResult result, GridPosition position)
        {
            if (result == PathEditResult.Invalid)
            {
                app.Audio.PlayInvalidPath(false);
                app.Haptics.Error();
                StartCoroutine(FlashStatus(
                    "BLOCKED • The red × and shake mark an invalid tile."));
            }
            else if (result == PathEditResult.LimitReached)
            {
                app.Audio.PlayInvalidPath(true);
                app.Haptics.Error();
                StartCoroutine(FlashStatus(
                    "MAX COST " + level.moveLimit + " — drag backward or trim the route."));
            }
            else
            {
                bool routeReady =
                    session.PlannedCrumbsRemaining == 0 &&
                    session.CurrentPathCells.Count > 1 &&
                    session.CurrentPathCells[session.CurrentPathCells.Count - 1] == session.Grid.Dock;
                app.Audio.PlayPathEdit(result, routeReady);
                app.Haptics.LightTap();
            }

            UpdateHud();
        }

        private void OnPlay()
        {
            string issue;
            if (session.TryGetRouteIssue(out issue))
            {
                app.Audio.PlayInvalidPath(issue.IndexOf("cost", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          issue.IndexOf("move", System.StringComparison.OrdinalIgnoreCase) >= 0);
                app.Haptics.Error();
                StartCoroutine(FlashStatus(issue));
                return;
            }

            if (!session.StartSimulation())
            {
                return;
            }

            app.Audio.PlayBotStart();

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

        private void OnCatSwipe(Direction direction)
        {
            if (!session.HasCat ||
                session.State != GameSessionState.CatTurn ||
                simulation != null ||
                modal != null ||
                botController.Paused)
            {
                return;
            }

            StepOutcome outcome;
            if (!session.TryCatTurn(direction, out outcome))
            {
                GridPosition blocked =
                    session.BotPosition + DirectionUtility.ToOffset(direction);
                board.PlayInvalidFeedback(
                    session.Grid.IsInside(blocked)
                        ? blocked
                        : session.BotPosition);
                app.Audio.PlayInvalidPath(false);
                app.Haptics.Error();
                StartCoroutine(FlashStatus(
                    "BLOCKED • Swipe toward an open neighboring tile."));
                return;
            }

            if (level.mode == GameMode.DailyChallenge && !catAttemptRegistered)
            {
                dailyAttemptCount = app.RegisterDailyAttempt();
                catAttemptRegistered = true;
            }

            simulation = StartCoroutine(PlayCatTurn(outcome));
        }

        private IEnumerator PlayCatTurn(StepOutcome outcome)
        {
            app.Audio.PlayMove();
            if (outcome.catMoveCount > 0 || outcome.catCollision)
            {
                app.Audio.PlayCatStep(outcome.catCollision);
                if (!outcome.catCollision)
                {
                    app.Audio.PlayCatPositionCue(outcome.to, outcome.catTo);
                }
            }

            yield return board.AnimateCatTurn(
                outcome,
                delegate { return botController.Paused; });
            OnSimulationStep(outcome);

            if (session.State == GameSessionState.Failed)
            {
                yield return board.AnimateFailure(outcome);
            }

            simulation = null;
            board.RefreshAll();
            UpdateHud();
            if (session.State == GameSessionState.Won)
            {
                HandleWin();
            }
            else if (session.State == GameSessionState.Failed)
            {
                HandleFailure();
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
            catAttemptRegistered = false;
            CloseModal();
            board.RefreshAll();
            SetEditingButtons(true);
            UpdateHud();
        }

        private void OnHint()
        {
            GridPosition target;
            if (!session.TryGetHintTarget(out target))
            {
                StartCoroutine(FlashStatus("Route already matches the solution."));
                return;
            }

            int cost = HintSystem.CostFor(level);
            if (cost <= 0)
            {
                ApplyConfirmedHint();
                return;
            }

            if (app.Economy.Coins < cost)
            {
                app.Audio.PlayNotEnoughCoins();
                ShowHintNotice(
                    "Not Enough Coins",
                    "Not enough Dust Coins.\nA hint costs " + cost + " Dust Coins.");
                return;
            }

            ShowHintConfirmation(cost);
        }

        private void ApplyConfirmedHint()
        {
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
                    app.Economy.Coins < HintSystem.CostFor(level)
                        ? "Not enough Dust Coins."
                        : "Route already matches the solution."));
            }
        }

        private void ShowHintConfirmation(int cost)
        {
            CloseModal();
            app.Audio.PlayHintConfirmationOpened();
            modal = UIFactory.CreateUIObject("Hint Confirmation", transform);
            UIFactory.Stretch(modal);
            Image shade = modal.AddComponent<Image>();
            shade.color = DustBotTheme.Overlay;

            Image panel = UIFactory.CreatePanel("Panel", modal.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(
                panel.rectTransform,
                new Vector2(0.1f, 0.34f),
                new Vector2(0.9f, 0.66f),
                Vector2.zero,
                Vector2.zero);
            panel.rectTransform.localScale = Vector3.one * 0.9f;

            Text heading = UIFactory.CreateText(
                "Heading",
                panel.transform,
                "Use Hint?",
                44,
                DustBotTheme.MintDark);
            UIFactory.SetAnchors(
                heading.rectTransform,
                new Vector2(0.08f, 0.68f),
                new Vector2(0.92f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            Text body = UIFactory.CreateText(
                "Details",
                panel.transform,
                "Spend " + cost + " Dust Coins for a hint?",
                29,
                DustBotTheme.Ink);
            UIFactory.SetAnchors(
                body.rectTransform,
                new Vector2(0.08f, 0.4f),
                new Vector2(0.92f, 0.69f),
                Vector2.zero,
                Vector2.zero);

            Button cancel = UIFactory.CreateButton(
                "Cancel",
                panel.transform,
                "CANCEL",
                CloseModal,
                DustBotTheme.MutedInk,
                27);
            UIFactory.SetAnchors(
                cancel.GetComponent<RectTransform>(),
                new Vector2(0.08f, 0.1f),
                new Vector2(0.46f, 0.34f),
                Vector2.zero,
                Vector2.zero);
            Button confirm = UIFactory.CreateButton(
                "Confirm Hint",
                panel.transform,
                "USE HINT",
                delegate
                {
                    CloseModal();
                    ApplyConfirmedHint();
                },
                DustBotTheme.Coral,
                27);
            UIFactory.SetAnchors(
                confirm.GetComponent<RectTransform>(),
                new Vector2(0.54f, 0.1f),
                new Vector2(0.92f, 0.34f),
                Vector2.zero,
                Vector2.zero);
            StartCoroutine(AnimateModalIn(panel.rectTransform));
        }

        private void ShowHintNotice(string title, string details)
        {
            CloseModal();
            modal = UIFactory.CreateUIObject("Hint Notice", transform);
            UIFactory.Stretch(modal);
            Image shade = modal.AddComponent<Image>();
            shade.color = DustBotTheme.Overlay;

            Image panel = UIFactory.CreatePanel("Panel", modal.transform, DustBotTheme.Panel);
            UIFactory.SetAnchors(
                panel.rectTransform,
                new Vector2(0.1f, 0.35f),
                new Vector2(0.9f, 0.65f),
                Vector2.zero,
                Vector2.zero);
            panel.rectTransform.localScale = Vector3.one * 0.9f;
            Text heading = UIFactory.CreateText(
                "Heading",
                panel.transform,
                title,
                42,
                DustBotTheme.Coral);
            UIFactory.SetAnchors(
                heading.rectTransform,
                new Vector2(0.08f, 0.68f),
                new Vector2(0.92f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            Text body = UIFactory.CreateText(
                "Details",
                panel.transform,
                details,
                28,
                DustBotTheme.Ink);
            UIFactory.SetAnchors(
                body.rectTransform,
                new Vector2(0.08f, 0.38f),
                new Vector2(0.92f, 0.68f),
                Vector2.zero,
                Vector2.zero);
            Button okay = UIFactory.CreateButton(
                "Okay",
                panel.transform,
                "OK",
                CloseModal,
                DustBotTheme.Mint,
                29);
            UIFactory.SetAnchors(
                okay.GetComponent<RectTransform>(),
                new Vector2(0.22f, 0.1f),
                new Vector2(0.78f, 0.34f),
                Vector2.zero,
                Vector2.zero);
            StartCoroutine(AnimateModalIn(panel.rectTransform));
        }

        private void OnSimulationStep(StepOutcome outcome)
        {
            if (outcome.cleanedCrumb)
            {
                app.Audio.PlayCrumbClean();
                board.PlayCrumbFeedback(outcome.to);
            }

            if (outcome.collectedBonus)
            {
                app.Audio.PlayDustBunnyCollected();
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
            app.Haptics.Success();
            AnalyticsStub.Track(LevelCompleteEvent(level.mode));
            StartCoroutine(ShowWinSequence(result));
        }

        private IEnumerator ShowWinSequence(LevelResult result)
        {
            yield return board.AnimateDockArrival();
            app.Audio.PlayWin();
            yield return board.AnimateWin();
            if (result.stars >= 3)
            {
                app.Audio.PlayPerfectClean();
            }
            else
            {
                app.Audio.PlayStarEarned();
            }
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
            app.Audio.PlayFailure(session.FailureReason);
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
            UIFactory.GetButtonText(restart).text = session.HasCat ? "RESTART CHASE" : "EDIT ROUTE";
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
            if (level.mode == GameMode.MainJourney &&
                level.generationMode != GenerationMode.ProductionCampaign)
            {
                if (level.levelNumber < app.Levels.CampaignLevelCount)
                {
                    app.StartMainLevel(level.levelNumber + 1);
                }
                else
                {
                    app.UI.ShowDeveloperPanel();
                }
                return;
            }

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
            if (session.HasCat)
            {
                playButton.interactable = false;
                hintButton.interactable =
                    session.State == GameSessionState.CatTurn;
                resetButton.interactable = true;
                UIFactory.GetButtonText(resetButton).text = "RESTART";
                return;
            }

            playButton.interactable = editing;
            hintButton.interactable = editing;
            resetButton.interactable = true;
            UIFactory.GetButtonText(resetButton).text = editing ? "RESET" : "EDIT";
        }

        private void UpdateHud()
        {
            if (session.HasCat)
            {
                string bonusState = level.objectives.collectBonus
                    ? "  •  BUNNY " +
                      (session.CollectedBonus ? "COLLECTED" : "WAITING")
                    : string.Empty;
                string limit = level.hardPathLimit
                    ? "  •  MAX " + level.moveLimit
                    : string.Empty;
                string moveLabel = HasCostPressure(level) ? "COST" : "MOVES";
                string perfectState = session.CanStillEarnThreeStars
                    ? "3★ CLEAN AVAILABLE"
                    : "REPLAY FOR 3★";
                statusText.text = string.Format(
                    "CRUMBS {0}{1}\n{2} {3}  •  IDEAL {4}{5}  •  {6}",
                    session.CrumbsRemaining,
                    bonusState,
                    moveLabel,
                    session.Moves,
                    level.threeStarMoveTarget,
                    limit,
                    perfectState);
                statusText.color = session.CanStillEarnThreeStars
                    ? DustBotTheme.Ink
                    : DustBotTheme.Warning;
                coinText.text = string.Format("{0}\nCOINS", app.Economy.Coins);
                return;
            }

            int displayedMoves = session.State == GameSessionState.Editing
                ? session.CurrentPathMoveCost
                : session.Moves;
            string metricLabel = HasCostPressure(level) ? "COST" : "PATH";
            string stickyDelta = session.State == GameSessionState.Editing &&
                                 session.CurrentPathMoveCost > session.CurrentPathLength
                ? "  •  STICKY +" + (session.CurrentPathMoveCost - session.CurrentPathLength)
                : string.Empty;
            string moveText = level.hardPathLimit
                ? string.Format(
                    "{0} {1}  •  PERFECT {2}  •  MAX {3}",
                    metricLabel,
                    displayedMoves,
                    level.threeStarMoveTarget,
                    level.moveLimit)
                : string.Format(
                    "{0} {1}  •  PERFECT {2}  •  2★ {3}",
                    metricLabel,
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
            int catCollisionStep = -1;
            bool catDanger = session.State == GameSessionState.Editing &&
                             session.TryGetCatCollisionStep(out catCollisionStep);
            string catStatus = session.HasCat
                ? string.Format(
                    "  •  CAT {0}",
                    catDanger ? "DANGER " + catCollisionStep : "SAFE")
                : string.Empty;
            string perfect = session.CanStillEarnThreeStars
                ? "PERFECT CLEAN AVAILABLE"
                : "REPLAY FOR PERFECT CLEAN";
            statusText.text = string.Format(
                "CRUMBS {0}{1}{2}{3}\n{4}  •  {5}",
                session.State == GameSessionState.Editing
                    ? session.PlannedCrumbsRemaining
                    : session.CrumbsRemaining,
                bonus,
                catStatus,
                stickyDelta,
                moveText,
                perfect);
            statusText.color = catDanger
                ? DustBotTheme.Error
                : session.CanStillEarnThreeStars
                    ? DustBotTheme.Ink
                    : DustBotTheme.Warning;
            coinText.text = string.Format("{0}\nCOINS", app.Economy.Coins);
        }

        private IEnumerator FlashStatus(string message)
        {
            string previous = statusText.text;
            Color previousColor = statusText.color;
            statusText.text = message;
            statusText.color = DustBotTheme.Error;
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
            if (definition.generationMode != GenerationMode.ProductionCampaign)
            {
                if (definition.dailyChallengeStyle) return "DEV DAILY CHALLENGE";
                if (definition.masterCleanStyle) return "DEV MASTER CLEAN";
                if (definition.generationMode == GenerationMode.MazeTesting)
                    return "MAZE TEST " + definition.levelNumber;
                string prefix = definition.cat != null && definition.cat.IsEnabled ? "CAT TEST" : "DEV LEVEL";
                return prefix + " " + definition.levelNumber;
            }

            if (definition.cat != null && definition.cat.IsEnabled)
            {
                switch (definition.mode)
                {
                    case GameMode.DailyChallenge: return "DAILY CAT CHASE";
                    case GameMode.MasterClean: return "MASTER CAT CHASE " + definition.levelNumber;
                    case GameMode.EndlessClean: return "CAT CHASE " + definition.levelNumber;
                    default: return "CAT CHASE " + definition.levelNumber;
                }
            }

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

            if (definition.generationMode != GenerationMode.ProductionCampaign &&
                !string.IsNullOrEmpty(definition.testArchetype))
            {
                return definition.testArchetype.ToUpperInvariant() + " • " +
                       (definition.advancedDevMaze
                           ? "Pinch to zoom, drag open board space to pan, and plan every branch before drawing. "
                           : string.Empty) +
                       (definition.cat != null && definition.cat.IsEnabled
                           ? "Turn-based cat pressure and route safety."
                           : "Draw, clean, dock, and verify the intended mechanic affects the solve.") +
                       ObstacleBriefing(definition);
            }

            if (definition.mode == GameMode.DailyChallenge)
            {
                return string.Format(
                    "SPECIAL {0}{1}{2} • Plan every turn, collect the Dust Bunny, and avoid hints for a perfect daily clean.",
                    ArchetypeLabel(definition.archetype),
                    definition.cat != null && definition.cat.IsEnabled
                        ? " • " + CatLabel(definition.cat.behavior)
                        : string.Empty,
                    ObstacleBriefing(definition));
            }

            if (definition.cat != null && definition.cat.IsEnabled)
            {
                return "SWIPE ONE TILE • Cat moves twice, horizontal first. Paw marks preview danger. Use furniture, clean every crumb, then dock." +
                       ObstacleBriefing(definition);
            }

            return string.Format(
                "{0}{1}{2} • Drag through every crumb, watch route cost, then finish at the dock.",
                ArchetypeLabel(definition.archetype),
                definition.cat != null && definition.cat.IsEnabled
                    ? " • " + CatLabel(definition.cat.behavior)
                    : string.Empty,
                ObstacleBriefing(definition));
        }

        private static string ObstacleBriefing(LevelDefinition definition)
        {
            if (definition == null || definition.cells == null)
            {
                return string.Empty;
            }

            bool sticky = HasContent(definition, CellContent.Sticky);
            bool fragile = HasContent(definition, CellContent.Fragile);
            bool slippery = HasContent(definition, CellContent.Slippery);
            bool oneWay = HasOneWay(definition);
            if (!sticky && !fragile && !slippery && !oneWay)
            {
                return string.Empty;
            }

            string text = " • ";
            bool wrote = false;
            if (sticky)
            {
                text += "Sticky costs +1";
                wrote = true;
            }

            if (oneWay)
            {
                text += wrote ? "; arrows force direction" : "Arrows force direction";
                wrote = true;
            }

            if (fragile)
            {
                text += wrote ? "; diamonds crack once" : "Diamonds crack once";
                wrote = true;
            }

            if (slippery)
            {
                text += wrote ? "; waves keep momentum straight" : "Waves keep momentum straight";
            }

            return text;
        }

        private static string CatLabel(CatBehavior behavior)
        {
            return "CURIOUS CAT: TWO MOVES, HORIZONTAL FIRST";
        }

        private static bool HasCostPressure(LevelDefinition definition)
        {
            return definition != null &&
                   (definition.hardPathLimit ||
                    HasContent(definition, CellContent.Sticky) ||
                    HasContent(definition, CellContent.Fragile) ||
                    HasContent(definition, CellContent.Slippery) ||
                    HasOneWay(definition));
        }

        private static bool HasContent(LevelDefinition definition, CellContent content)
        {
            if (definition == null || definition.cells == null)
            {
                return false;
            }

            for (int i = 0; i < definition.cells.Count; i++)
            {
                if (definition.cells[i] != null &&
                    definition.cells[i].content == content)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasOneWay(LevelDefinition definition)
        {
            if (definition == null || definition.cells == null)
            {
                return false;
            }

            for (int i = 0; i < definition.cells.Count; i++)
            {
                if (definition.cells[i] != null &&
                    CellContentUtility.IsOneWay(definition.cells[i].content))
                {
                    return true;
                }
            }

            return false;
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
                case FailureReason.OutOfMoves: return "Maximum route cost reached. Avoid sticky detours or redraw a cleaner route.";
                case FailureReason.LoopDetected: return "DustBot found an infinite cleaning career.";
                case FailureReason.CatPounce: return "Cat pounce! Use the paw preview and furniture to keep whiskers at a safe distance.";
                case FailureReason.FragileBreak: return "Fragile tile cracked. Cross each diamond tile only once.";
                default: return "DustBot got stuck. Redraw the next part of the route.";
            }
        }

        private static string StarLine(int stars)
        {
            if (stars == 3) return "★  ★  ★";
            if (stars == 2) return "★  ★  ☆";
            return "★  ☆  ☆";
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
            int campaignCount = level.generationMode == GenerationMode.ProductionCampaign
                ? LevelManifest.MainJourneyLevelCount
                : app.Levels.CampaignLevelCount;
            if (level.mode == GameMode.MainJourney && level.levelNumber < campaignCount)
            {
                return string.Format("\nLevel {0} is ready.", level.levelNumber + 1);
            }

            if (level.mode == GameMode.DailyChallenge)
            {
                return "\nDaily challenge complete.";
            }

            if (level.generationMode != GenerationMode.ProductionCampaign)
            {
                return "\nTesting playlist complete.";
            }

            return "\nThe next room is ready.";
        }

        private string NextButtonLabel()
        {
            if (level.mode == GameMode.DailyChallenge)
            {
                return "DONE";
            }

            if (level.generationMode != GenerationMode.ProductionCampaign &&
                level.levelNumber >= app.Levels.CampaignLevelCount)
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
            if (session.HasCat)
            {
                routeSteps = Mathf.Min(routeSteps, 1);
                for (int i = 0;
                     i < routeSteps &&
                     session.State == GameSessionState.CatTurn;
                     i++)
                {
                    StepOutcome outcome;
                    session.TryCatTurn(
                        level.expectedSolution[i].direction,
                        out outcome);
                }

                board.RefreshAll();
                UpdateHud();
                return;
            }

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

        public void PrepareLargeMazeScreenshot(int routeSteps)
        {
            PrepareForStoreScreenshot(routeSteps);
            if (level.largeMaze && board != null)
            {
                board.AdjustZoom(-1f);
            }
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
