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
        IPointerUpHandler
    {
        private sealed class CellVisual
        {
            public RectTransform root;
            public Image background;
            public Image contentSprite;
            public Image bonusSprite;
            public Color baseColor;
            public Coroutine feedback;
        }

        private LevelDefinition level;
        private GameSession session;
        private PlayerInputController input;
        private CellVisual[,] cells;
        private RectTransform boardRect;
        private RectTransform routeLayer;
        private readonly List<Image> routeSegments = new List<Image>();
        private readonly List<Image> routeNodes = new List<Image>();
        private Image startGlow;
        private Image firstDirectionNub;
        private RectTransform bot;
        private Image botImage;
        private CosmeticSystem cosmetics;
        private System.Action<PathEditResult, GridPosition> onPathEdited;
        private bool drawing;
        private bool hasLastPointerLocal;
        private Vector2 lastPointerLocal;
        private bool hasLastProcessedCell;
        private GridPosition lastProcessedCell;

        private void Update()
        {
            if (session == null || bot == null || session.State != GameSessionState.Editing)
            {
                return;
            }

            float wave = Mathf.Sin(Time.unscaledTime * 2.4f);
            if (!drawing)
            {
                bot.localScale = Vector3.one * (0.92f + wave * 0.014f);
            }

            if (startGlow != null)
            {
                float alpha = 0.21f + (wave + 1f) * 0.035f;
                startGlow.color = new Color(
                    DustBotTheme.MintDark.r,
                    DustBotTheme.MintDark.g,
                    DustBotTheme.MintDark.b,
                    alpha);
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
            System.Action<PathEditResult, GridPosition> onPathEdited)
        {
            if (level == null) throw new System.ArgumentNullException("level");
            if (session == null) throw new System.ArgumentNullException("session");
            if (input == null) throw new System.ArgumentNullException("input");

            this.level = level;
            this.session = session;
            this.input = input;
            this.cosmetics = cosmetics;
            this.onPathEdited = onPathEdited;
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
            BuildBot();
            RefreshAll();
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
        }

        public void RefreshCell(GridPosition position)
        {
            CellVisual cell = cells[position.x, position.y];
            CellContent content = session.Grid.GetContent(position);
            cell.baseColor = TileColor(
                content,
                (position.x + position.y) % 2 == 0
                    ? cosmetics.ActiveTileA
                    : cosmetics.ActiveTileB);
            cell.background.color = session.IsPathCell(position)
                ? Color.Lerp(cell.baseColor, cosmetics.ActivePathColor, 0.2f)
                : cell.baseColor;

            Sprite contentSprite = DustBotSprites.ForCell(content);
            bool contentVisible = contentSprite != null &&
                                  (content != CellContent.Crumb || session.HasCrumb(position));
            cell.contentSprite.sprite = contentSprite;
            cell.contentSprite.gameObject.SetActive(contentVisible);
            cell.contentSprite.color = content == CellContent.Dock
                ? cosmetics.ActiveDockTint
                : Color.white;

            bool hasBonus = level.objectives.collectBonus &&
                            position == level.bonusPosition &&
                            !session.CollectedBonus;
            cell.bonusSprite.gameObject.SetActive(hasBonus);
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
            cell.feedback = StartCoroutine(PulseCellRoutine(cell, 0.24f, 0.16f));
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

            PathEditResult result = input.BeginPath(position);
            drawing = result == PathEditResult.Started;
            hasLastPointerLocal = drawing;
            lastPointerLocal = local;
            hasLastProcessedCell = true;
            lastProcessedCell = position;

            if (result == PathEditResult.Started)
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
            if (!drawing)
            {
                return;
            }

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
            if (drawing)
            {
                input.EndPath();
            }

            drawing = false;
            hasLastPointerLocal = false;
            hasLastProcessedCell = false;
        }

        public IEnumerator AnimateBotTo(
            GridPosition position,
            Direction direction,
            float duration,
            System.Func<bool> isPaused)
        {
            Vector2 start = bot.anchorMin;
            Vector2 target = AnchorFor(position);
            Quaternion startRotation = bot.localRotation;
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
                t = 1f - Mathf.Pow(1f - t, 3f);
                Vector2 anchor = Vector2.LerpUnclamped(start, target, t);
                bot.anchorMin = anchor;
                bot.anchorMax = anchor;
                bot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                float bounce = Mathf.Sin(t * Mathf.PI);
                bot.localScale = new Vector3(1f + bounce * 0.08f, 1f - bounce * 0.05f, 1f);
                yield return null;
            }

            bot.anchorMin = target;
            bot.anchorMax = target;
            bot.anchoredPosition = Vector2.zero;
            bot.localRotation = targetRotation;
            bot.localScale = Vector3.one;
        }

        public IEnumerator AnimateFailure(StepOutcome outcome)
        {
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
                botImage.color = Color.Lerp(originalColor, DustBotTheme.Coral, Mathf.Sin(t * Mathf.PI));
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
                    rootRect.offsetMin = new Vector2(5f, 5f);
                    rootRect.offsetMax = new Vector2(-5f, -5f);

                    Image background = root.AddComponent<Image>();
                    background.sprite = SpriteFactory.RoundedRect;
                    background.type = Image.Type.Sliced;
                    background.color = (x + y) % 2 == 0
                        ? cosmetics.ActiveTileA
                        : cosmetics.ActiveTileB;
                    background.raycastTarget = false;
                    Outline tileEdge = root.AddComponent<Outline>();
                    tileEdge.effectColor = new Color(1f, 1f, 1f, 0.44f);
                    tileEdge.effectDistance = new Vector2(1.5f, -1.5f);
                    tileEdge.useGraphicAlpha = true;

                    GameObject contentObject = UIFactory.CreateUIObject("Object Sprite", root.transform);
                    Image contentSprite = contentObject.AddComponent<Image>();
                    contentSprite.preserveAspect = true;
                    contentSprite.raycastTarget = false;
                    UIFactory.SetAnchors(
                        contentSprite.rectTransform,
                        new Vector2(0.1f, 0.1f),
                        new Vector2(0.9f, 0.9f),
                        Vector2.zero,
                        Vector2.zero);
                    Shadow objectShadow = contentObject.AddComponent<Shadow>();
                    objectShadow.effectColor = new Color(0.12f, 0.09f, 0.06f, 0.2f);
                    objectShadow.effectDistance = new Vector2(0f, -3f);
                    objectShadow.useGraphicAlpha = true;

                    GameObject bonusObject = UIFactory.CreateUIObject("Bonus Dust Bunny", root.transform);
                    Image bonusSprite = bonusObject.AddComponent<Image>();
                    bonusSprite.sprite = DustBotSprites.DustBunny;
                    bonusSprite.preserveAspect = true;
                    bonusSprite.raycastTarget = false;
                    UIFactory.SetAnchors(
                        bonusSprite.rectTransform,
                        new Vector2(0.56f, 0.55f),
                        new Vector2(0.98f, 0.98f),
                        Vector2.zero,
                        Vector2.zero);
                    Shadow bonusShadow = bonusObject.AddComponent<Shadow>();
                    bonusShadow.effectColor = new Color(0.15f, 0.1f, 0.2f, 0.2f);
                    bonusShadow.effectDistance = new Vector2(0f, -3f);
                    bonusShadow.useGraphicAlpha = true;

                    cells[x, y] = new CellVisual
                    {
                        root = rootRect,
                        background = background,
                        contentSprite = contentSprite,
                        bonusSprite = bonusSprite,
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
            float cellWidth = Mathf.Min(920f / level.width, 970f / level.height);
            bot.sizeDelta = Vector2.one * Mathf.Clamp(cellWidth * 0.78f, 58f, 128f);
            bot.pivot = new Vector2(0.5f, 0.5f);
        }

        private void RefreshPath()
        {
            IReadOnlyList<GridPosition> path = session.CurrentPathCells;
            float cellWidth = boardRect.rect.width / level.width;
            float cellHeight = boardRect.rect.height / level.height;
            float shortestCell = Mathf.Min(cellWidth, cellHeight);
            float thickness = Mathf.Clamp(shortestCell * 0.14f, 10f, 25f);
            Color routeColor = session.State == GameSessionState.Editing
                ? session.CanStillEarnThreeStars
                    ? cosmetics.ActivePathColor
                    : DustBotTheme.Coral
                : new Color(
                    cosmetics.ActivePathColor.r,
                    cosmetics.ActivePathColor.g,
                    cosmetics.ActivePathColor.b,
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
            PathEditResult result = input.ContinuePath(position);
            if (result == PathEditResult.Added ||
                result == PathEditResult.Backtracked)
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

        private void RefreshBotPresentation()
        {
            bool editing = session.State == GameSessionState.Editing;
            float cellHeight = boardRect.rect.height / level.height;
            bot.anchoredPosition = editing
                ? new Vector2(0f, Mathf.Clamp(cellHeight * 0.08f, 6f, 16f))
                : Vector2.zero;
            bot.localScale = editing ? Vector3.one * 0.92f : Vector3.one;
            botImage.color = editing
                ? new Color(
                    cosmetics.ActiveBotTint.r,
                    cosmetics.ActiveBotTint.g,
                    cosmetics.ActiveBotTint.b,
                    0.95f)
                : cosmetics.ActiveBotTint;
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

        private IEnumerator InvalidCellRoutine(CellVisual cell, GridPosition position)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector2 originalPosition = cell.root.anchoredPosition;
            Color originalColor = cell.background.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float shake = Mathf.Sin(t * Mathf.PI * 7f) * (1f - t);
                cell.root.anchoredPosition = originalPosition + new Vector2(shake * 7f, 0f);
                cell.background.color = Color.Lerp(originalColor, DustBotTheme.Coral, Mathf.Sin(t * Mathf.PI) * 0.65f);
                yield return null;
            }

            cell.root.anchoredPosition = originalPosition;
            cell.feedback = null;
            RefreshCell(position);
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
                default:
                    return floorColor;
            }
        }
    }
}
