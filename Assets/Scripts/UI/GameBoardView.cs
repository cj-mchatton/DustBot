using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustBot
{
    public sealed class GameBoardView : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        IScrollHandler
    {
        private sealed class CellVisual
        {
            public RectTransform root;
            public Image background;
            public Image contentSprite;
            public Text contentLabel;
            public Image bonusSprite;
            public Text invalidMarker;
            public Text catDangerMarker;
            public Color baseColor;
            public Coroutine feedback;
        }

        private LevelDefinition level;
        private GameSession session;
        private PlayerInputController input;
        private CellVisual[,] cells;
        private RectTransform boardRect;
        private RectTransform viewportRect;
        private RectTransform routeLayer;
        private readonly List<Image> routeSegments = new List<Image>();
        private readonly List<Image> routeNodes = new List<Image>();
        private readonly List<RectTransform> pawMarkers = new List<RectTransform>();
        private Image startGlow;
        private Image firstDirectionNub;
        private RectTransform cat;
        private Image catImage;
        private Image catDangerGlow;
        private RectTransform bot;
        private Image botImage;
        private CosmeticSystem cosmetics;
        private System.Action<PathEditResult, GridPosition> onPathEdited;
        private System.Action<Direction> onCatSwipe;
        private bool drawing;
        private bool catSwipeStarted;
        private Vector2 catSwipeStart;
        private bool hasLastPointerLocal;
        private Vector2 lastPointerLocal;
        private bool hasLastProcessedCell;
        private GridPosition lastProcessedCell;
        private bool panning;
        private bool pinchActive;
        private Vector2 lastPanLocal;
        private float zoom = 1f;
        private float minimumZoom = 1f;
        private float overviewZoom = 1f;
        private const float MaximumZoom = 1.75f;

        private void Update()
        {
            if (session == null)
            {
                return;
            }

            HandlePinchAndTwoFingerPan();
            if (level.largeMaze &&
                session.State == GameSessionState.Simulating &&
                !pinchActive)
            {
                FocusOnBot(7f * Time.unscaledDeltaTime);
            }

            float wave = Mathf.Sin(Time.unscaledTime * 2.4f);
            bool editing = session.State == GameSessionState.Editing ||
                           session.State == GameSessionState.CatTurn;
            if (editing && bot != null && !drawing)
            {
                float personality = cosmetics.ActiveBotSkinId == "bot_retro" ? 0.009f : 0.018f;
                bot.localScale = new Vector3(
                    0.92f + wave * personality,
                    0.92f - wave * personality * 0.55f,
                    1f);
                if (cosmetics.ActiveBotSkinId == "bot_arcade")
                    bot.localRotation = Quaternion.Euler(0f, 0f, wave * 2.2f);
            }

            if (editing && startGlow != null)
            {
                float alpha = 0.21f + (wave + 1f) * 0.035f;
                startGlow.color = new Color(
                    DustBotTheme.MintDark.r,
                    DustBotTheme.MintDark.g,
                    DustBotTheme.MintDark.b,
                    alpha);
            }

            if (cat != null && cat.gameObject.activeSelf && editing)
            {
                cat.anchoredPosition = new Vector2(0f, 3f + Mathf.Sin(Time.unscaledTime * 3.2f) * 3f);
                cat.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Sin(Time.unscaledTime * 2.1f) * 2.5f);
                if (catDangerGlow != null)
                {
                    float dangerAlpha = 0.13f + (wave + 1f) * 0.05f;
                    catDangerGlow.color = new Color(
                        DustBotTheme.Warning.r,
                        DustBotTheme.Warning.g,
                        DustBotTheme.Warning.b,
                        dangerAlpha);
                }
            }

            if (routeNodes.Count > 0 &&
                (cosmetics.ActivePathTrailId == "path_neon" ||
                 cosmetics.ActivePathTrailId == "path_rainbow" ||
                 cosmetics.ActivePathTrailId == "path_bubble"))
            {
                Color animated = PathColorAtTime();
                for (int i = 0; i < routeNodes.Count; i++)
                {
                    if (!routeNodes[i].gameObject.activeSelf) continue;
                    routeNodes[i].rectTransform.localScale = Vector3.one *
                        (1f + Mathf.Sin(Time.unscaledTime * 3.5f + i * 0.7f) * 0.08f);
                    if (cosmetics.ActivePathTrailId == "path_rainbow")
                        routeNodes[i].color = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.12f + i * 0.08f, 1f), 0.65f, 1f);
                    else if (cosmetics.ActivePathTrailId == "path_neon")
                        routeNodes[i].color = animated;
                }
            }

            if (cells == null)
            {
                return;
            }

            float bonusTilt = Mathf.Sin(Time.unscaledTime * 2f) * 4f;
            float bonusScale = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.035f;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    Image bonus = cells[x, y].bonusSprite;
                    if (bonus.gameObject.activeSelf)
                    {
                        bonus.rectTransform.localRotation = Quaternion.Euler(0f, 0f, bonusTilt);
                        bonus.rectTransform.localScale = Vector3.one * bonusScale;
                    }
                }
            }
        }

        public void Initialize(
            LevelDefinition level,
            GameSession session,
            PlayerInputController input,
            CosmeticSystem cosmetics,
            System.Action<PathEditResult, GridPosition> onPathEdited,
            System.Action<Direction> onCatSwipe,
            RectTransform viewportRect = null)
        {
            if (level == null) throw new System.ArgumentNullException("level");
            if (session == null) throw new System.ArgumentNullException("session");
            if (input == null) throw new System.ArgumentNullException("input");

            this.level = level;
            this.session = session;
            this.input = input;
            this.cosmetics = cosmetics;
            this.onPathEdited = onPathEdited;
            this.onCatSwipe = onCatSwipe;
            this.viewportRect = viewportRect;
            boardRect = (RectTransform)transform;
            cells = new CellVisual[level.width, level.height];

            Image inputSurface = gameObject.GetComponent<Image>();
            if (inputSurface == null)
            {
                inputSurface = gameObject.AddComponent<Image>();
            }

            inputSurface.color = new Color(0f, 0f, 0f, 0f);
            inputSurface.raycastTarget = true;

            BuildCells();
            BuildRouteLayer();
            BuildCat();
            BuildBot();
            RefreshAll();
            ConfigureCamera();
        }

        public void AdjustZoom(float delta)
        {
            if (!level.largeMaze)
            {
                return;
            }

            SetZoom(zoom + delta);
        }

        public void ResetCamera()
        {
            if (!level.largeMaze)
            {
                return;
            }

            SetZoom(overviewZoom);
            CenterOnCell(session != null ? session.BotPosition : level.Find(CellContent.Start));
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (level.largeMaze)
            {
                SetZoom(zoom + Mathf.Sign(eventData.scrollDelta.y) * 0.12f);
            }
        }

        public void RefreshAll()
        {
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    RefreshCell(new GridPosition(x, y));
                }
            }

            RefreshPath();
            PositionBot(session.BotPosition);
            bot.localRotation = Quaternion.identity;
            RefreshBotPresentation();
            PositionCat(session.CatPosition);
            RefreshCatPresentation();
        }

        public void RefreshCell(GridPosition position)
        {
            CellVisual cell = cells[position.x, position.y];
            CellContent content = session.Grid.GetContent(position);
            bool alternate = (position.x + position.y) % 2 != 0;
            cell.background.sprite = CosmeticSpriteLibrary.Tile(cosmetics.ActiveTileThemeId, alternate);
            cell.background.type = Image.Type.Simple;
            cell.baseColor = TileColor(content, Color.white);
            cell.background.color = session.IsPathCell(position)
                ? Color.Lerp(cell.baseColor, PathColorAtTime(), 0.2f)
                : cell.baseColor;

            Sprite contentSprite = DustBotSprites.ForCell(content);
            if (content == CellContent.Crumb)
                contentSprite = CosmeticSpriteLibrary.Crumb(cosmetics.ActiveCrumbStyleId);
            else if (content == CellContent.Dock)
                contentSprite = CosmeticSpriteLibrary.Dock(cosmetics.ActiveDockDesignId);
            bool contentVisible = contentSprite != null &&
                                  (content != CellContent.Crumb || session.HasCrumb(position));
            cell.contentSprite.sprite = contentSprite;
            cell.contentSprite.gameObject.SetActive(contentVisible);
            cell.contentSprite.color = Color.white;

            string label = ContentLabel(content);
            cell.contentLabel.text = label;
            cell.contentLabel.color = ContentLabelColor(content);
            cell.contentLabel.gameObject.SetActive(!contentVisible && !string.IsNullOrEmpty(label));

            bool hasBonus = level.objectives.collectBonus &&
                            position == level.bonusPosition &&
                            !session.CollectedBonus;
            cell.bonusSprite.gameObject.SetActive(hasBonus);
            cell.invalidMarker.gameObject.SetActive(false);
            cell.catDangerMarker.gameObject.SetActive(false);
        }

        public void PlayTileFeedback(GridPosition position)
        {
            CellVisual cell = cells[position.x, position.y];
            PrepareCellFeedback(cell);
            cell.feedback = StartCoroutine(PulseCellRoutine(cell, 0.18f, 0.1f));
        }

        public void PlayCrumbFeedback(GridPosition position)
        {
            CellVisual cell = cells[position.x, position.y];
            PrepareCellFeedback(cell);
            cell.feedback = StartCoroutine(CrumbCleanRoutine(cell, position));
        }

        public void PlayInvalidFeedback(GridPosition position)
        {
            if (!session.Grid.IsInside(position))
            {
                return;
            }

            CellVisual cell = cells[position.x, position.y];
            PrepareCellFeedback(cell);
            cell.feedback = StartCoroutine(InvalidCellRoutine(cell, position));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (level.largeMaze && Input.touchCount >= 2)
            {
                return;
            }

            if (session.HasCat &&
                session.State == GameSessionState.CatTurn)
            {
                Vector2 catLocal;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        boardRect,
                        eventData.position,
                        eventData.pressEventCamera,
                        out catLocal))
                {
                    catSwipeStarted = true;
                    catSwipeStart = catLocal;
                }

                return;
            }

            if (session.State != GameSessionState.Editing)
            {
                return;
            }

            GridPosition position;
            Vector2 local;
            if (!TryGetCell(eventData.position, eventData.pressEventCamera, out position, out local))
            {
                return;
            }

            if (level.largeMaze && !session.IsPathCell(position))
            {
                Vector2 viewportLocal;
                if (TryGetViewportLocal(
                        eventData.position,
                        eventData.pressEventCamera,
                        out viewportLocal))
                {
                    panning = true;
                    lastPanLocal = viewportLocal;
                }

                return;
            }

            PathEditResult result = input.BeginPath(position);
            drawing = result == PathEditResult.Started ||
                      result == PathEditResult.Resumed ||
                      result == PathEditResult.Trimmed;
            hasLastPointerLocal = drawing;
            lastPointerLocal = local;
            hasLastProcessedCell = true;
            lastProcessedCell = position;

            if (drawing)
            {
                RefreshAll();
                PlayTileFeedback(position);
            }
            else if (result == PathEditResult.Invalid)
            {
                PlayInvalidFeedback(position);
            }
            else if (result == PathEditResult.LimitReached)
            {
                PlayInvalidFeedback(lastProcessedCell);
            }

            NotifyPathEdited(result, position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panning)
            {
                Vector2 viewportLocal;
                if (TryGetViewportLocal(
                        eventData.position,
                        eventData.pressEventCamera,
                        out viewportLocal))
                {
                    PanBy(viewportLocal - lastPanLocal);
                    lastPanLocal = viewportLocal;
                }

                return;
            }

            if (session.HasCat)
            {
                return;
            }

            if (!drawing)
            {
                return;
            }

            AutoPanNearViewportEdge(eventData.position, eventData.pressEventCamera);

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    boardRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out local))
            {
                return;
            }

            if (!hasLastPointerLocal)
            {
                lastPointerLocal = local;
                hasLastPointerLocal = true;
            }

            float cellSize = Mathf.Min(
                boardRect.rect.width / level.width,
                boardRect.rect.height / level.height);
            int samples = Mathf.Max(
                1,
                Mathf.CeilToInt(Vector2.Distance(lastPointerLocal, local) / Mathf.Max(8f, cellSize * 0.32f)));

            for (int i = 1; i <= samples; i++)
            {
                Vector2 sample = Vector2.Lerp(lastPointerLocal, local, (float)i / samples);
                GridPosition position;
                if (TryGetCell(sample, out position))
                {
                    ProcessDraggedCell(position);
                }
            }

            lastPointerLocal = local;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (panning)
            {
                panning = false;
                return;
            }

            if (catSwipeStarted)
            {
                catSwipeStarted = false;
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        boardRect,
                        eventData.position,
                        eventData.pressEventCamera,
                        out local))
                {
                    Vector2 delta = local - catSwipeStart;
                    float threshold = Mathf.Max(
                        24f,
                        Mathf.Min(
                            boardRect.rect.width / level.width,
                            boardRect.rect.height / level.height) * 0.18f);
                    if (delta.magnitude >= threshold && onCatSwipe != null)
                    {
                        Direction direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                            ? delta.x > 0f ? Direction.Right : Direction.Left
                            : delta.y > 0f ? Direction.Up : Direction.Down;
                        onCatSwipe(direction);
                    }
                }

                return;
            }

            if (drawing)
            {
                input.EndPath();
            }

            drawing = false;
            hasLastPointerLocal = false;
            hasLastProcessedCell = false;
        }

        public IEnumerator AnimateCatTurn(
            StepOutcome outcome,
            System.Func<bool> isPaused)
        {
            yield return AnimateBotSlide(
                outcome.from,
                outcome.to,
                outcome.direction,
                0.115f,
                isPaused);

            float pause = 0f;
            while (pause < 0.035f)
            {
                if (isPaused == null || !isPaused())
                {
                    pause += Time.unscaledDeltaTime;
                }

                yield return null;
            }

            if (outcome.catMoveCount > 0)
            {
                yield return AnimateCatSegment(
                    outcome.catFrom,
                    outcome.catFirst,
                    0.085f,
                    isPaused);
                if (outcome.catMoveCount > 1)
                {
                    yield return AnimateCatSegment(
                        outcome.catFirst,
                        outcome.catTo,
                        0.085f,
                        isPaused);
                }
            }
        }

        public IEnumerator AnimateCruiseStep(
            StepOutcome outcome,
            float duration,
            System.Func<bool> isPaused)
        {
            Vector2 start = bot.anchorMin;
            Vector2 target = AnchorFor(outcome.to);
            Quaternion startRotation = bot.localRotation;
            Quaternion targetRotation = Quaternion.Euler(
                0f,
                0f,
                RotationFor(outcome.direction));
            bool animateCat = cat != null &&
                              cat.gameObject.activeSelf &&
                              outcome.catMoveCount > 0;
            Vector2 catStart = animateCat ? AnchorFor(outcome.catFrom) : Vector2.zero;
            Vector2 catFirst = animateCat ? AnchorFor(outcome.catFirst) : Vector2.zero;
            Vector2 catTarget = animateCat ? AnchorFor(outcome.catTo) : Vector2.zero;
            Quaternion catStartRotation = animateCat
                ? cat.localRotation
                : Quaternion.identity;
            Direction catDirection = animateCat
                ? DirectionUtility.Between(
                    outcome.catMoveCount > 1 ? outcome.catFirst : outcome.catFrom,
                    outcome.catTo)
                : Direction.None;
            Quaternion catTargetRotation = Quaternion.Euler(
                0f,
                0f,
                RotationFor(catDirection));

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (isPaused != null && isPaused())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector2 anchor = Vector2.LerpUnclamped(start, target, t);
                bot.anchorMin = anchor;
                bot.anchorMax = anchor;
                bot.anchoredPosition = Vector2.zero;
                bot.localScale = Vector3.one;
                bot.localRotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    SmootherStep(Mathf.Min(1f, t * 1.8f)));

                if (animateCat)
                {
                    float catT = Mathf.Clamp01((t - 0.28f) / 0.72f);
                    Vector2 catAnchor;
                    if (outcome.catMoveCount > 1)
                    {
                        catAnchor = catT < 0.5f
                            ? Vector2.LerpUnclamped(catStart, catFirst, SmootherStep(catT * 2f))
                            : Vector2.LerpUnclamped(catFirst, catTarget, SmootherStep((catT - 0.5f) * 2f));
                    }
                    else
                    {
                        catAnchor = Vector2.LerpUnclamped(
                            catStart,
                            catTarget,
                            SmootherStep(catT));
                    }

                    SetCatAnchor(catAnchor);
                    cat.anchoredPosition = Vector2.zero;
                    cat.localRotation = Quaternion.Slerp(
                        catStartRotation,
                        catTargetRotation,
                        SmootherStep(catT));
                    float catPounce = outcome.catCollision
                        ? Mathf.Sin(catT * Mathf.PI) * 0.18f
                        : 0f;
                    cat.localScale = Vector3.one * (1f + catPounce);
                }

                yield return null;
            }

            bot.anchorMin = target;
            bot.anchorMax = target;
            bot.anchoredPosition = Vector2.zero;
            bot.localRotation = targetRotation;
            bot.localScale = Vector3.one;
            if (animateCat)
            {
                SetCatAnchor(catTarget);
                cat.anchoredPosition = Vector2.zero;
                cat.localRotation = catTargetRotation;
                cat.localScale = Vector3.one;
            }
        }

        public IEnumerator AnimateCatMovement(
            StepOutcome outcome,
            System.Func<bool> isPaused)
        {
            if (cat == null || !cat.gameObject.activeSelf)
            {
                yield break;
            }

            if (outcome.catMoveCount == 0)
            {
                if (outcome.catCollision)
                {
                    yield return AnimateCatPounce(isPaused);
                }

                yield break;
            }

            yield return AnimateCatSegment(
                outcome.catFrom,
                outcome.catFirst,
                0.12f,
                isPaused);
            if (outcome.catMoveCount > 1)
            {
                yield return AnimateCatSegment(
                    outcome.catFirst,
                    outcome.catTo,
                    0.105f,
                    isPaused);
            }

            if (outcome.catCollision)
            {
                yield return AnimateCatPounce(isPaused);
            }
        }

        public IEnumerator AnimateDockArrival()
        {
            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float settle = Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t);
                bot.localScale = new Vector3(
                    1f + settle * 0.08f,
                    1f - settle * 0.055f,
                    1f);
                bot.localRotation = Quaternion.Euler(0f, 0f, settle * 8f);
                yield return null;
            }

            bot.localScale = Vector3.one;
            bot.localRotation = Quaternion.identity;
        }

        public IEnumerator AnimateFailure(StepOutcome outcome)
        {
            if (outcome.failure == FailureReason.CatPounce)
            {
                yield return AnimateCatCollisionFailure();
                yield break;
            }

            Vector2 offset = DirectionUtility.ToOffset(outcome.direction).x != 0
                ? new Vector2(DirectionUtility.ToOffset(outcome.direction).x * 24f, 0f)
                : new Vector2(0f, DirectionUtility.ToOffset(outcome.direction).y * 24f);
            Vector2 original = bot.anchoredPosition;
            Color originalColor = botImage.color;
            string animation = cosmetics.ActiveFailureAnimationId;
            float duration = animation == "fail_sad" ? 0.48f : 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float shake = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t);
                bot.anchoredPosition = original + offset * shake;
                float rotation = shake * 12f;
                if (animation == "fail_dizzy") rotation = t * 540f;
                if (animation == "fail_faceplant") rotation = Mathf.Lerp(0f, 82f, t);
                if (animation == "fail_slide")
                    bot.anchoredPosition = original + new Vector2(Mathf.Sin(t * Mathf.PI) * 55f, -t * 12f);
                if (animation == "fail_explosion")
                    bot.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.35f);
                bot.localRotation = Quaternion.Euler(0f, 0f, rotation);
                botImage.color = Color.Lerp(originalColor, DustBotTheme.Error, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            bot.anchoredPosition = original;
            bot.localRotation = Quaternion.identity;
            bot.localScale = Vector3.one;
            botImage.color = originalColor;
        }

        public IEnumerator AnimateWin()
        {
            Color originalColor = botImage.color;
            string animation = cosmetics.ActiveWinAnimationId;
            float duration = animation == "win_dance" ? 0.75f : 0.52f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI * 3f);
                bot.localScale = Vector3.one * (1f + Mathf.Abs(wave) * 0.14f);
                float rotation = t * 360f;
                Color celebration = DustBotTheme.Yellow;
                if (animation == "win_dance") rotation = wave * 28f;
                if (animation == "win_confetti") celebration = DustBotTheme.Coral;
                if (animation == "win_bubbles") celebration = DustBotTheme.Blue;
                if (animation == "win_fireworks") celebration = new Color32(174, 112, 238, 255);
                bot.localRotation = Quaternion.Euler(0f, 0f, rotation);
                botImage.color = Color.Lerp(originalColor, celebration, Mathf.Abs(wave) * 0.45f);
                yield return null;
            }

            bot.localScale = Vector3.one;
            bot.localRotation = Quaternion.identity;
            botImage.color = originalColor;
        }

        private void ConfigureCamera()
        {
            if (!level.largeMaze || viewportRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            float fitX = viewportRect.rect.width / Mathf.Max(1f, boardRect.rect.width);
            float fitY = viewportRect.rect.height / Mathf.Max(1f, boardRect.rect.height);
            minimumZoom = Mathf.Clamp(Mathf.Min(1f, fitX, fitY), 0.55f, 1f);
            overviewZoom = minimumZoom;
            zoom = overviewZoom;
            boardRect.localScale = Vector3.one * zoom;
            CenterOnCell(session.BotPosition);
        }

        private void HandlePinchAndTwoFingerPan()
        {
            if (!level.largeMaze || viewportRect == null || Input.touchCount < 2)
            {
                pinchActive = false;
                return;
            }

            Touch first = Input.GetTouch(0);
            Touch second = Input.GetTouch(1);
            if (first.phase == TouchPhase.Ended || second.phase == TouchPhase.Ended ||
                first.phase == TouchPhase.Canceled || second.phase == TouchPhase.Canceled)
            {
                pinchActive = false;
                return;
            }

            if (drawing)
            {
                input.EndPath();
                drawing = false;
                hasLastPointerLocal = false;
                hasLastProcessedCell = false;
            }

            panning = false;
            pinchActive = true;
            Vector2 firstPrevious = first.position - first.deltaPosition;
            Vector2 secondPrevious = second.position - second.deltaPosition;
            float previousDistance = Vector2.Distance(firstPrevious, secondPrevious);
            float currentDistance = Vector2.Distance(first.position, second.position);
            Vector2 currentMidpoint = (first.position + second.position) * 0.5f;
            Vector2 previousMidpoint = (firstPrevious + secondPrevious) * 0.5f;
            Vector2 currentLocal;
            Vector2 previousLocal;
            if (TryGetViewportLocal(currentMidpoint, null, out currentLocal) &&
                TryGetViewportLocal(previousMidpoint, null, out previousLocal))
            {
                PanBy(currentLocal - previousLocal);
            }

            if (previousDistance > 1f)
            {
                SetZoom(zoom * currentDistance / previousDistance);
            }
        }

        private void AutoPanNearViewportEdge(Vector2 screenPosition, Camera eventCamera)
        {
            if (!level.largeMaze || viewportRect == null)
            {
                return;
            }

            Vector2 local;
            if (!TryGetViewportLocal(screenPosition, eventCamera, out local))
            {
                return;
            }

            Rect rect = viewportRect.rect;
            float edge = Mathf.Min(76f, Mathf.Min(rect.width, rect.height) * 0.14f);
            Vector2 delta = Vector2.zero;
            if (local.x > rect.xMax - edge) delta.x = -18f;
            else if (local.x < rect.xMin + edge) delta.x = 18f;
            if (local.y > rect.yMax - edge) delta.y = -18f;
            else if (local.y < rect.yMin + edge) delta.y = 18f;
            if (delta.sqrMagnitude > 0f)
            {
                PanBy(delta);
            }
        }

        private bool TryGetViewportLocal(
            Vector2 screenPosition,
            Camera eventCamera,
            out Vector2 local)
        {
            local = Vector2.zero;
            return viewportRect != null &&
                   RectTransformUtility.ScreenPointToLocalPointInRectangle(
                       viewportRect,
                       screenPosition,
                       eventCamera,
                       out local);
        }

        private void SetZoom(float value)
        {
            if (!level.largeMaze || viewportRect == null)
            {
                return;
            }

            zoom = Mathf.Clamp(value, minimumZoom, MaximumZoom);
            boardRect.localScale = Vector3.one * zoom;
            ClampBoardPosition();
        }

        private void PanBy(Vector2 delta)
        {
            if (!level.largeMaze || viewportRect == null)
            {
                return;
            }

            boardRect.anchoredPosition += delta;
            ClampBoardPosition();
        }

        private void CenterOnCell(GridPosition position)
        {
            if (!level.largeMaze || viewportRect == null || !level.IsInside(position))
            {
                return;
            }

            boardRect.anchoredPosition = -LocalCenterFor(position) * zoom;
            ClampBoardPosition();
        }

        private void FocusOnBot(float interpolation)
        {
            if (viewportRect == null || bot == null)
            {
                return;
            }

            Rect rect = boardRect.rect;
            Vector2 botLocal = new Vector2(
                rect.xMin + bot.anchorMin.x * rect.width,
                rect.yMin + bot.anchorMin.y * rect.height);
            Vector2 target = -botLocal * zoom;
            boardRect.anchoredPosition = Vector2.Lerp(
                boardRect.anchoredPosition,
                target,
                Mathf.Clamp01(interpolation));
            ClampBoardPosition();
        }

        private void ClampBoardPosition()
        {
            if (viewportRect == null)
            {
                return;
            }

            float excessX = Mathf.Max(
                0f,
                (boardRect.rect.width * zoom - viewportRect.rect.width) * 0.5f);
            float excessY = Mathf.Max(
                0f,
                (boardRect.rect.height * zoom - viewportRect.rect.height) * 0.5f);
            Vector2 position = boardRect.anchoredPosition;
            position.x = excessX <= 0f ? 0f : Mathf.Clamp(position.x, -excessX, excessX);
            position.y = excessY <= 0f ? 0f : Mathf.Clamp(position.y, -excessY, excessY);
            boardRect.anchoredPosition = position;
        }

        private void BuildCells()
        {
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    GameObject root = UIFactory.CreateUIObject(string.Format("Cell {0},{1}", x, y), transform);
                    RectTransform rootRect = root.GetComponent<RectTransform>();
                    rootRect.anchorMin = new Vector2((float)x / level.width, (float)y / level.height);
                    rootRect.anchorMax = new Vector2((float)(x + 1) / level.width, (float)(y + 1) / level.height);
                    float gutter = level.advancedDevMaze
                        ? level.width >= 22 || level.height >= 22 ? 2.25f : 1.9f
                        : level.largeMaze ? 1.75f : 5f;
                    rootRect.offsetMin = new Vector2(gutter, gutter);
                    rootRect.offsetMax = new Vector2(-gutter, -gutter);

                    Image background = root.AddComponent<Image>();
                    background.sprite = SpriteFactory.RoundedRect;
                    background.type = Image.Type.Sliced;
                    background.color = (x + y) % 2 == 0
                        ? cosmetics.ActiveTileA
                        : cosmetics.ActiveTileB;
                    background.raycastTarget = false;
                    if (!level.largeMaze)
                    {
                        Outline tileEdge = root.AddComponent<Outline>();
                        tileEdge.effectColor = new Color(1f, 1f, 1f, 0.44f);
                        tileEdge.effectDistance = new Vector2(1.5f, -1.5f);
                        tileEdge.useGraphicAlpha = true;
                    }

                    GameObject contentObject = UIFactory.CreateUIObject("Object Sprite", root.transform);
                    Image contentSprite = contentObject.AddComponent<Image>();
                    contentSprite.preserveAspect = true;
                    contentSprite.raycastTarget = false;
                    UIFactory.SetAnchors(
                        contentSprite.rectTransform,
                        level.largeMaze ? new Vector2(0.04f, 0.04f) : new Vector2(0.1f, 0.1f),
                        level.largeMaze ? new Vector2(0.96f, 0.96f) : new Vector2(0.9f, 0.9f),
                        Vector2.zero,
                        Vector2.zero);
                    if (!level.largeMaze)
                    {
                        Shadow objectShadow = contentObject.AddComponent<Shadow>();
                        objectShadow.effectColor = new Color(0.12f, 0.09f, 0.06f, 0.2f);
                        objectShadow.effectDistance = new Vector2(0f, -3f);
                        objectShadow.useGraphicAlpha = true;
                    }

                    Text contentLabel = UIFactory.CreateText(
                        "Tile Symbol",
                        root.transform,
                        string.Empty,
                        46,
                        DustBotTheme.Ink);
                    contentLabel.fontStyle = FontStyle.Bold;
                    contentLabel.resizeTextForBestFit = true;
                    contentLabel.resizeTextMinSize = 22;
                    contentLabel.resizeTextMaxSize = 54;
                    UIFactory.SetAnchors(
                        contentLabel.rectTransform,
                        new Vector2(0.13f, 0.12f),
                        new Vector2(0.87f, 0.88f),
                        Vector2.zero,
                        Vector2.zero);
                    Outline symbolOutline = contentLabel.gameObject.AddComponent<Outline>();
                    symbolOutline.effectColor = new Color(1f, 1f, 1f, 0.82f);
                    symbolOutline.effectDistance = new Vector2(2f, -2f);
                    contentLabel.gameObject.SetActive(false);

                    GameObject bonusObject = UIFactory.CreateUIObject("Bonus Dust Bunny", root.transform);
                    Image bonusSprite = bonusObject.AddComponent<Image>();
                    bonusSprite.sprite = DustBotSprites.DustBunny;
                    bonusSprite.preserveAspect = true;
                    bonusSprite.raycastTarget = false;
                    UIFactory.SetAnchors(
                        bonusSprite.rectTransform,
                        level.largeMaze ? new Vector2(0.44f, 0.44f) : new Vector2(0.56f, 0.55f),
                        new Vector2(0.98f, 0.98f),
                        Vector2.zero,
                        Vector2.zero);
                    if (!level.largeMaze)
                    {
                        Shadow bonusShadow = bonusObject.AddComponent<Shadow>();
                        bonusShadow.effectColor = new Color(0.15f, 0.1f, 0.2f, 0.2f);
                        bonusShadow.effectDistance = new Vector2(0f, -3f);
                        bonusShadow.useGraphicAlpha = true;
                    }

                    Text invalidMarker = UIFactory.CreateText(
                        "Invalid X",
                        root.transform,
                        "×",
                        58,
                        DustBotTheme.Error);
                    invalidMarker.fontStyle = FontStyle.Bold;
                    invalidMarker.resizeTextForBestFit = true;
                    invalidMarker.resizeTextMinSize = 24;
                    invalidMarker.resizeTextMaxSize = 68;
                    UIFactory.SetAnchors(
                        invalidMarker.rectTransform,
                        new Vector2(0.18f, 0.12f),
                        new Vector2(0.82f, 0.88f),
                        Vector2.zero,
                        Vector2.zero);
                    Outline invalidOutline = invalidMarker.gameObject.AddComponent<Outline>();
                    invalidOutline.effectColor = new Color(1f, 1f, 1f, 0.95f);
                    invalidOutline.effectDistance = new Vector2(3f, -3f);
                    invalidMarker.gameObject.SetActive(false);

                    Text catDangerMarker = UIFactory.CreateText(
                        "Cat Danger",
                        root.transform,
                        "!",
                        34,
                        DustBotTheme.Error);
                    catDangerMarker.fontStyle = FontStyle.Bold;
                    UIFactory.SetAnchors(
                        catDangerMarker.rectTransform,
                        new Vector2(0.65f, 0.62f),
                        new Vector2(0.96f, 0.96f),
                        Vector2.zero,
                        Vector2.zero);
                    Outline dangerOutline =
                        catDangerMarker.gameObject.AddComponent<Outline>();
                    dangerOutline.effectColor = Color.white;
                    dangerOutline.effectDistance = new Vector2(2f, -2f);
                    catDangerMarker.transform.SetAsLastSibling();
                    catDangerMarker.gameObject.SetActive(false);

                    cells[x, y] = new CellVisual
                    {
                        root = rootRect,
                        background = background,
                        contentSprite = contentSprite,
                        contentLabel = contentLabel,
                        bonusSprite = bonusSprite,
                        invalidMarker = invalidMarker,
                        catDangerMarker = catDangerMarker,
                        baseColor = background.color
                    };
                }
            }
        }

        private void BuildRouteLayer()
        {
            GameObject layerObject = UIFactory.CreateUIObject("Drawn Route", transform);
            routeLayer = UIFactory.Stretch(layerObject);

            startGlow = CreateRouteImage("Start Glow", SpriteFactory.Circle);
            startGlow.color = new Color(DustBotTheme.MintDark.r, DustBotTheme.MintDark.g, DustBotTheme.MintDark.b, 0.24f);

            firstDirectionNub = CreateRouteImage("First Direction Nub", SpriteFactory.Circle);
            firstDirectionNub.color = DustBotTheme.Yellow;
        }

        private void BuildBot()
        {
            GameObject botObject = UIFactory.CreateUIObject("DustBot", transform);
            botImage = botObject.AddComponent<Image>();
            botImage.sprite = DustBotSprites.Player;
            botImage.color = Color.white;
            botImage.preserveAspect = true;
            botImage.raycastTarget = false;
            Shadow shadow = botObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.05f, 0.12f, 0.1f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -6f);
            bot = botObject.GetComponent<RectTransform>();
            float cellWidth = Mathf.Min(
                boardRect.rect.width / level.width,
                boardRect.rect.height / level.height);
            bot.sizeDelta = Vector2.one * Mathf.Clamp(
                cellWidth * 0.78f,
                level.largeMaze ? 42f : 58f,
                128f);
            bot.pivot = new Vector2(0.5f, 0.5f);
        }

        private void BuildCat()
        {
            if (level.cat == null || !level.cat.IsEnabled)
            {
                return;
            }

            GameObject glowObject = UIFactory.CreateUIObject("Cat Danger Glow", transform);
            catDangerGlow = glowObject.AddComponent<Image>();
            catDangerGlow.sprite = SpriteFactory.Circle;
            catDangerGlow.raycastTarget = false;
            catDangerGlow.color = new Color(
                DustBotTheme.Warning.r,
                DustBotTheme.Warning.g,
                DustBotTheme.Warning.b,
                0.18f);
            RectTransform glowRect = catDangerGlow.rectTransform;
            float cellWidth = Mathf.Min(920f / level.width, 970f / level.height);
            glowRect.sizeDelta = Vector2.one * Mathf.Clamp(cellWidth * 0.9f, 66f, 142f);

            GameObject catObject = UIFactory.CreateUIObject("Pet Cat", transform);
            catImage = catObject.AddComponent<Image>();
            catImage.sprite = DustBotSprites.Cat;
            catImage.color = Color.white;
            catImage.preserveAspect = true;
            catImage.raycastTarget = false;
            Shadow shadow = catObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.18f, 0.1f, 0.05f, 0.24f);
            shadow.effectDistance = new Vector2(0f, -5f);
            cat = catObject.GetComponent<RectTransform>();
            cat.sizeDelta = Vector2.one * Mathf.Clamp(cellWidth * 0.72f, 54f, 116f);
            cat.pivot = new Vector2(0.5f, 0.5f);
        }

        private void RefreshPath()
        {
            if (session.HasCat)
            {
                DisableUnused(routeSegments, 0);
                DisableUnused(routeNodes, 0);
                startGlow.gameObject.SetActive(false);
                firstDirectionNub.gameObject.SetActive(false);
                RefreshCatPrediction();
                return;
            }

            IReadOnlyList<GridPosition> path = session.CurrentPathCells;
            float cellWidth = boardRect.rect.width / level.width;
            float cellHeight = boardRect.rect.height / level.height;
            float shortestCell = Mathf.Min(cellWidth, cellHeight);
            float thickness = level.advancedDevMaze
                ? Mathf.Clamp(shortestCell * 0.18f, 14f, 30f)
                : Mathf.Clamp(shortestCell * 0.14f, 10f, 25f);
            Color routeColor = session.State == GameSessionState.Editing
                ? session.CanStillEarnThreeStars
                    ? PathColorAtTime()
                    : DustBotTheme.Warning
                : new Color(
                    PathColorAtTime().r,
                    PathColorAtTime().g,
                    PathColorAtTime().b,
                    0.48f);

            int segmentCount = Mathf.Max(0, path.Count - 1);
            for (int i = 0; i < segmentCount; i++)
            {
                Image segment = GetRouteImage(routeSegments, i, "Route Segment", null);
                Vector2 from = LocalCenterFor(path[i]);
                Vector2 to = LocalCenterFor(path[i + 1]);
                Vector2 delta = to - from;
                RectTransform rect = segment.rectTransform;
                rect.anchoredPosition = (from + to) * 0.5f;
                rect.sizeDelta = new Vector2(delta.magnitude + thickness * 0.2f, thickness);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                segment.color = routeColor;
                segment.gameObject.SetActive(true);
            }

            DisableUnused(routeSegments, segmentCount);

            for (int i = 0; i < path.Count; i++)
            {
                Image node = GetRouteImage(routeNodes, i, "Route Node", SpriteFactory.Circle);
                node.sprite = CosmeticSpriteLibrary.PathNode(cosmetics.ActivePathTrailId);
                node.rectTransform.anchoredPosition = LocalCenterFor(path[i]);
                float nodeSize = i == 0 ? thickness * 1.75f : thickness * 1.35f;
                node.rectTransform.sizeDelta = Vector2.one * nodeSize;
                bool isEnd = i == path.Count - 1 && path[i] == session.Grid.Dock;
                node.color = isEnd ? DustBotTheme.Yellow : routeColor;
                node.gameObject.SetActive(true);
                node.transform.SetAsLastSibling();
            }

            DisableUnused(routeNodes, path.Count);

            bool editing = session.State == GameSessionState.Editing;
            startGlow.gameObject.SetActive(editing);
            startGlow.rectTransform.anchoredPosition = LocalCenterFor(session.Grid.Start);
            startGlow.rectTransform.sizeDelta = Vector2.one * (shortestCell * 0.76f);

            bool showNub = editing && path.Count > 1;
            firstDirectionNub.gameObject.SetActive(showNub);
            if (showNub)
            {
                Vector2 start = LocalCenterFor(path[0]);
                Vector2 next = LocalCenterFor(path[1]);
                firstDirectionNub.rectTransform.anchoredPosition = Vector2.Lerp(start, next, 0.58f);
                firstDirectionNub.rectTransform.sizeDelta = Vector2.one * (thickness * 1.45f);
                firstDirectionNub.transform.SetAsLastSibling();
            }

            RefreshCatPrediction();
        }

        private Image CreateRouteImage(string name, Sprite sprite)
        {
            GameObject gameObject = UIFactory.CreateUIObject(name, routeLayer);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        private Image GetRouteImage(
            List<Image> images,
            int index,
            string name,
            Sprite sprite)
        {
            while (images.Count <= index)
            {
                images.Add(CreateRouteImage(name + " " + images.Count, sprite));
            }

            return images[index];
        }

        private static void DisableUnused(List<Image> images, int usedCount)
        {
            for (int i = usedCount; i < images.Count; i++)
            {
                images[i].gameObject.SetActive(false);
            }
        }

        private void ProcessDraggedCell(GridPosition position)
        {
            if (hasLastProcessedCell && position == lastProcessedCell)
            {
                return;
            }

            hasLastProcessedCell = true;
            lastProcessedCell = position;
            GridPosition previousTail = session.CurrentPathCells.Count > 0
                ? session.CurrentPathCells[session.CurrentPathCells.Count - 1]
                : position;
            PathEditResult result = input.ContinuePath(position);
            if (result == PathEditResult.Added ||
                result == PathEditResult.Backtracked)
            {
                if (level.largeMaze)
                {
                    RefreshCell(previousTail);
                    RefreshCell(position);
                    RefreshPath();
                }
                else
                {
                    RefreshAll();
                }
                PlayTileFeedback(position);
            }
            else if (result == PathEditResult.Invalid)
            {
                PlayInvalidFeedback(position);
            }
            else if (result == PathEditResult.LimitReached)
            {
                PlayInvalidFeedback(position);
            }

            NotifyPathEdited(result, position);
        }

        private void NotifyPathEdited(PathEditResult result, GridPosition position)
        {
            if (result != PathEditResult.None && onPathEdited != null)
            {
                onPathEdited(result, position);
            }
        }

        private bool TryGetCell(
            Vector2 screenPosition,
            Camera eventCamera,
            out GridPosition position,
            out Vector2 local)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    boardRect,
                    screenPosition,
                    eventCamera,
                    out local))
            {
                position = new GridPosition(-1, -1);
                return false;
            }

            return TryGetCell(local, out position);
        }

        private bool TryGetCell(Vector2 local, out GridPosition position)
        {
            Rect rect = boardRect.rect;
            if (!rect.Contains(local))
            {
                position = new GridPosition(-1, -1);
                return false;
            }

            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * level.width), 0, level.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(normalizedY * level.height), 0, level.height - 1);
            position = new GridPosition(x, y);
            return true;
        }

        private Vector2 LocalCenterFor(GridPosition position)
        {
            Rect rect = boardRect.rect;
            return new Vector2(
                rect.xMin + (position.x + 0.5f) * rect.width / level.width,
                rect.yMin + (position.y + 0.5f) * rect.height / level.height);
        }

        private void PositionBot(GridPosition position)
        {
            Vector2 anchor = AnchorFor(position);
            bot.anchorMin = anchor;
            bot.anchorMax = anchor;
            bot.anchoredPosition = Vector2.zero;
        }

        private void PositionCat(GridPosition position)
        {
            if (cat == null || !level.IsInside(position))
            {
                return;
            }

            SetCatAnchor(AnchorFor(position));
            cat.anchoredPosition = Vector2.zero;
            cat.localScale = Vector3.one;
            cat.localRotation = Quaternion.identity;
        }

        private void RefreshCatPresentation()
        {
            if (cat == null)
            {
                return;
            }

            bool active = session.HasCat;
            cat.gameObject.SetActive(active);
            catImage.sprite = CosmeticSpriteLibrary.Cat(cosmetics.ActiveCatSkinId);
            catImage.color = Color.white;
            if (catDangerGlow != null)
            {
                catDangerGlow.gameObject.SetActive(active);
            }
        }

        private void RefreshCatPrediction()
        {
            bool show = session != null &&
                        session.State == GameSessionState.CatTurn &&
                        session.HasCat;
            if (!show)
            {
                HideCatDangerMarkers();
                DisablePawMarkers(0);
                return;
            }

            HideCatDangerMarkers();
            int shown = 0;
            Direction[] directions =
            {
                Direction.Up,
                Direction.Right,
                Direction.Down,
                Direction.Left
            };
            for (int directionIndex = 0;
                 directionIndex < directions.Length;
                 directionIndex++)
            {
                GridPosition destination;
                CatStepResult catStep;
                FailureReason danger;
                if (!session.TryPreviewCatTurn(
                        directions[directionIndex],
                        out destination,
                        out catStep,
                        out danger))
                {
                    continue;
                }

                if (danger != FailureReason.None &&
                    session.Grid.IsInside(destination))
                {
                    cells[destination.x, destination.y]
                        .catDangerMarker.gameObject.SetActive(true);
                }

                GridPosition[] catPositions =
                {
                    catStep.first,
                    catStep.to
                };
                int count = catStep.moveCount == 0 ? 0 : catStep.moveCount;
                for (int step = 0; step < count; step++)
                {
                    RectTransform marker = GetPawMarker(shown);
                    marker.anchoredPosition = LocalCenterFor(catPositions[step]);
                    marker.localScale = Vector3.one * (step == 0 ? 0.68f : 0.8f);
                    Color color = danger == FailureReason.CatPounce
                        ? DustBotTheme.Error
                        : DustBotTheme.Warning;
                    Image[] images = marker.GetComponentsInChildren<Image>(true);
                    for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
                    {
                        images[imageIndex].color = new Color(
                            color.r,
                            color.g,
                            color.b,
                            danger == FailureReason.CatPounce ? 0.76f : 0.24f);
                    }

                    marker.gameObject.SetActive(true);
                    marker.SetAsLastSibling();
                    shown++;
                }
            }

            DisablePawMarkers(shown);
        }

        private void HideCatDangerMarkers()
        {
            if (cells == null)
            {
                return;
            }

            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    cells[x, y].catDangerMarker.gameObject.SetActive(false);
                }
            }
        }

        private RectTransform GetPawMarker(int index)
        {
            while (pawMarkers.Count <= index)
            {
                GameObject root = UIFactory.CreateUIObject(
                    "Predicted Paw " + pawMarkers.Count,
                    routeLayer);
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(35f, 35f);

                CreatePawPart(root.transform, "Pad", new Vector2(0f, -5f), new Vector2(18f, 15f));
                CreatePawPart(root.transform, "Toe Left", new Vector2(-8f, 8f), new Vector2(8f, 9f));
                CreatePawPart(root.transform, "Toe Center", new Vector2(0f, 11f), new Vector2(8f, 9f));
                CreatePawPart(root.transform, "Toe Right", new Vector2(8f, 8f), new Vector2(8f, 9f));
                pawMarkers.Add(rect);
            }

            return pawMarkers[index];
        }

        private static void CreatePawPart(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject part = UIFactory.CreateUIObject(name, parent);
            Image image = part.AddComponent<Image>();
            image.sprite = SpriteFactory.Circle;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
        }

        private void DisablePawMarkers(int used)
        {
            for (int i = used; i < pawMarkers.Count; i++)
            {
                pawMarkers[i].gameObject.SetActive(false);
            }
        }

        private void RefreshBotPresentation()
        {
            bool editing = session.State == GameSessionState.Editing;
            float cellHeight = boardRect.rect.height / level.height;
            bot.anchoredPosition = editing
                ? new Vector2(0f, Mathf.Clamp(cellHeight * 0.08f, 6f, 16f))
                : Vector2.zero;
            bot.localScale = editing ? Vector3.one * 0.92f : Vector3.one;
            botImage.sprite = CosmeticSpriteLibrary.DustBot(cosmetics.ActiveBotSkinId);
            botImage.color = editing ? new Color(1f, 1f, 1f, 0.96f) : Color.white;
        }

        private Color PathColorAtTime()
        {
            if (cosmetics.ActivePathTrailId == "path_rainbow")
                return Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.12f, 1f), 0.68f, 0.94f);
            if (cosmetics.ActivePathTrailId == "path_neon")
                return Color.Lerp(cosmetics.ActivePathColor, Color.white, (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.12f);
            return cosmetics.ActivePathColor;
        }

        private Vector2 AnchorFor(GridPosition position)
        {
            return new Vector2(
                (position.x + 0.5f) / level.width,
                (position.y + 0.5f) / level.height);
        }

        private void PrepareCellFeedback(CellVisual cell)
        {
            if (cell.feedback != null)
            {
                StopCoroutine(cell.feedback);
                cell.feedback = null;
            }

            cell.root.localScale = Vector3.one;
            cell.root.anchoredPosition = Vector2.zero;
        }

        private IEnumerator PulseCellRoutine(CellVisual cell, float duration, float amount)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                cell.root.localScale = Vector3.one * (1f + wave * amount);
                yield return null;
            }

            cell.root.localScale = Vector3.one;
            cell.feedback = null;
        }

        private IEnumerator CrumbCleanRoutine(CellVisual cell, GridPosition position)
        {
            Image crumb = cell.contentSprite;
            crumb.gameObject.SetActive(true);
            Color originalColor = crumb.color;
            Vector3 originalScale = crumb.rectTransform.localScale;
            Quaternion originalRotation = crumb.rectTransform.localRotation;
            float elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pop = Mathf.Sin(t * Mathf.PI);
                cell.root.localScale = Vector3.one * (1f + pop * 0.11f);
                crumb.rectTransform.localScale = originalScale * (1f + pop * 0.42f);
                crumb.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 115f);
                crumb.color = new Color(
                    originalColor.r,
                    originalColor.g,
                    originalColor.b,
                    1f - SmootherStep(t));
                yield return null;
            }

            cell.root.localScale = Vector3.one;
            crumb.rectTransform.localScale = originalScale;
            crumb.rectTransform.localRotation = originalRotation;
            crumb.color = originalColor;
            cell.feedback = null;
            RefreshCell(position);
        }

        private IEnumerator InvalidCellRoutine(CellVisual cell, GridPosition position)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector2 originalPosition = cell.root.anchoredPosition;
            Color originalColor = cell.background.color;
            cell.invalidMarker.gameObject.SetActive(true);
            cell.invalidMarker.rectTransform.localScale = Vector3.one * 0.7f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float shake = Mathf.Sin(t * Mathf.PI * 7f) * (1f - t);
                cell.root.anchoredPosition = originalPosition + new Vector2(shake * 7f, 0f);
                cell.background.color = Color.Lerp(originalColor, DustBotTheme.Error, Mathf.Sin(t * Mathf.PI) * 0.72f);
                cell.invalidMarker.rectTransform.localScale =
                    Vector3.one * Mathf.Lerp(0.7f, 1.12f, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            cell.root.anchoredPosition = originalPosition;
            cell.invalidMarker.gameObject.SetActive(false);
            cell.feedback = null;
            RefreshCell(position);
        }

        private IEnumerator AnimateCatSegment(
            GridPosition from,
            GridPosition to,
            float duration,
            System.Func<bool> isPaused)
        {
            Vector2 start = AnchorFor(from);
            Vector2 target = AnchorFor(to);
            Direction direction = DirectionUtility.Between(from, to);
            Quaternion startRotation = cat.localRotation;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, RotationFor(direction));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (isPaused != null && isPaused())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = SmootherStep(t);
                SetCatAnchor(Vector2.LerpUnclamped(start, target, eased));
                cat.anchoredPosition = Vector2.zero;
                cat.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
                cat.localScale = Vector3.one;
                yield return null;
            }

            SetCatAnchor(target);
            cat.anchoredPosition = Vector2.zero;
            cat.localRotation = targetRotation;
            cat.localScale = Vector3.one;
        }

        private IEnumerator AnimateBotSlide(
            GridPosition from,
            GridPosition to,
            Direction direction,
            float duration,
            System.Func<bool> isPaused)
        {
            Vector2 start = AnchorFor(from);
            Vector2 target = AnchorFor(to);
            Quaternion startRotation = bot.localRotation;
            Quaternion targetRotation = Quaternion.Euler(
                0f,
                0f,
                RotationFor(direction));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (isPaused != null && isPaused())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector2 anchor = Vector2.LerpUnclamped(start, target, SmootherStep(t));
                bot.anchorMin = anchor;
                bot.anchorMax = anchor;
                bot.anchoredPosition = Vector2.zero;
                bot.localRotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    SmootherStep(t));
                bot.localScale = Vector3.one;
                yield return null;
            }

            PositionBot(to);
            bot.localRotation = targetRotation;
            bot.localScale = Vector3.one;
        }

        private IEnumerator AnimateCatPounce(System.Func<bool> isPaused)
        {
            float elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                if (isPaused != null && isPaused())
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pounce = Mathf.Sin(t * Mathf.PI);
                cat.localScale = Vector3.one * (1f + pounce * 0.28f);
                cat.localRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 5f);
                yield return null;
            }

            cat.localScale = Vector3.one;
        }

        private IEnumerator AnimateCatCollisionFailure()
        {
            Color originalColor = botImage.color;
            float elapsed = 0f;
            const float duration = 0.46f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float impact = Mathf.Sin(Mathf.Min(1f, t * 2f) * Mathf.PI);
                float wobble = Mathf.Sin(t * Mathf.PI * 7f) * (1f - t);
                bot.localScale = new Vector3(
                    1f + impact * 0.18f,
                    1f - impact * 0.28f,
                    1f);
                bot.localRotation = Quaternion.Euler(0f, 0f, wobble * 18f);
                botImage.color = Color.Lerp(originalColor, DustBotTheme.Error, impact * 0.72f);
                if (cat != null)
                {
                    cat.localScale = Vector3.one * (1f + impact * 0.2f);
                }

                yield return null;
            }

            bot.localScale = Vector3.one;
            bot.localRotation = Quaternion.identity;
            botImage.color = originalColor;
            if (cat != null)
            {
                cat.localScale = Vector3.one;
            }
        }

        private void SetCatAnchor(Vector2 anchor)
        {
            cat.anchorMin = anchor;
            cat.anchorMax = anchor;
            if (catDangerGlow != null)
            {
                catDangerGlow.rectTransform.anchorMin = anchor;
                catDangerGlow.rectTransform.anchorMax = anchor;
                catDangerGlow.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private static float SmootherStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float RotationFor(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right: return -90f;
                case Direction.Down: return 180f;
                case Direction.Left: return 90f;
                default: return 0f;
            }
        }

        private static string ContentLabel(CellContent content)
        {
            switch (content)
            {
                case CellContent.Sticky: return "2";
                case CellContent.Fragile: return "◇";
                case CellContent.Slippery: return "≈";
                case CellContent.OneWayUp: return "↑";
                case CellContent.OneWayRight: return "→";
                case CellContent.OneWayDown: return "↓";
                case CellContent.OneWayLeft: return "←";
                default: return string.Empty;
            }
        }

        private static Color ContentLabelColor(CellContent content)
        {
            switch (content)
            {
                case CellContent.Sticky: return DustBotTheme.MintDark;
                case CellContent.Fragile: return DustBotTheme.Blue;
                case CellContent.Slippery: return DustBotTheme.Blue;
                case CellContent.OneWayUp:
                case CellContent.OneWayRight:
                case CellContent.OneWayDown:
                case CellContent.OneWayLeft:
                    return DustBotTheme.Warning;
                default: return DustBotTheme.Ink;
            }
        }

        private static Color TileColor(CellContent content, Color floorColor)
        {
            switch (content)
            {
                case CellContent.Start:
                    return Color.Lerp(floorColor, DustBotTheme.Mint, 0.18f);
                case CellContent.Dock:
                    return Color.Lerp(floorColor, DustBotTheme.Yellow, 0.28f);
                case CellContent.Wall:
                case CellContent.Toy:
                    return Color.Lerp(floorColor, DustBotTheme.Blocker, 0.42f);
                case CellContent.Sock:
                    return Color.Lerp(floorColor, DustBotTheme.Coral, 0.24f);
                case CellContent.Cord:
                    return Color.Lerp(floorColor, DustBotTheme.Yellow, 0.26f);
                case CellContent.WetSpot:
                    return Color.Lerp(floorColor, DustBotTheme.Blue, 0.24f);
                case CellContent.Sticky:
                    return Color.Lerp(floorColor, DustBotTheme.Mint, 0.2f);
                case CellContent.Fragile:
                    return Color.Lerp(floorColor, DustBotTheme.Blue, 0.18f);
                case CellContent.Slippery:
                    return Color.Lerp(floorColor, DustBotTheme.Blue, 0.28f);
                case CellContent.OneWayUp:
                case CellContent.OneWayRight:
                case CellContent.OneWayDown:
                case CellContent.OneWayLeft:
                    return Color.Lerp(floorColor, DustBotTheme.Yellow, 0.2f);
                default:
                    return floorColor;
            }
        }
    }
}
