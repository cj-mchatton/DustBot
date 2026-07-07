using UnityEngine;
using UnityEngine.UI;

namespace DustBot
{
    public struct StoreGridMetrics
    {
        public int columns;
        public int rows;
        public float cardWidth;
        public float preferredHeight;
    }

    [RequireComponent(typeof(GridLayoutGroup), typeof(LayoutElement))]
    public sealed class ResponsiveStoreGrid : MonoBehaviour
    {
        public const float DefaultHorizontalPadding = 12f;
        public const float DefaultVerticalPadding = 4f;
        public const float DefaultColumnSpacing = 22f;
        public const float DefaultRowSpacing = 22f;
        public const float DefaultCardHeight = 350f;
        public const float DefaultMinimumTwoColumnCardWidth = 300f;

        private RectTransform rectTransform;
        private GridLayoutGroup grid;
        private LayoutElement layoutElement;
        private float lastWidth = -1f;
        private int lastChildCount = -1;

        public static StoreGridMetrics Calculate(
            float width,
            int itemCount,
            float horizontalPadding = DefaultHorizontalPadding,
            float verticalPadding = DefaultVerticalPadding,
            float columnSpacing = DefaultColumnSpacing,
            float rowSpacing = DefaultRowSpacing,
            float cardHeight = DefaultCardHeight,
            float minimumTwoColumnCardWidth = DefaultMinimumTwoColumnCardWidth)
        {
            float safeWidth = Mathf.Max(0f, width);
            float twoColumnWidth =
                (safeWidth - horizontalPadding * 2f - columnSpacing) / 2f;
            int columns = twoColumnWidth >= minimumTwoColumnCardWidth ? 2 : 1;
            float totalColumnSpacing = columnSpacing * Mathf.Max(0, columns - 1);
            float cardWidth = Mathf.Max(
                0f,
                (safeWidth - horizontalPadding * 2f - totalColumnSpacing) / columns);
            int rows = itemCount > 0
                ? Mathf.CeilToInt(itemCount / (float)columns)
                : 0;
            float preferredHeight = verticalPadding * 2f;
            if (rows > 0)
            {
                preferredHeight += rows * cardHeight + (rows - 1) * rowSpacing;
            }

            return new StoreGridMetrics
            {
                columns = columns,
                rows = rows,
                cardWidth = cardWidth,
                preferredHeight = preferredHeight
            };
        }

        public void Refresh()
        {
            ApplyLayoutIfNeeded(true);
        }

        private void Awake()
        {
            CacheComponents();
            ApplyLayoutIfNeeded(true);
        }

        private void OnEnable()
        {
            ApplyLayoutIfNeeded(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayoutIfNeeded(false);
        }

        private void OnTransformChildrenChanged()
        {
            ApplyLayoutIfNeeded(false);
        }

        private void CacheComponents()
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            if (grid == null) grid = GetComponent<GridLayoutGroup>();
            if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        }

        private void ApplyLayoutIfNeeded(bool force)
        {
            CacheComponents();
            if (rectTransform == null || grid == null || layoutElement == null)
            {
                return;
            }

            float width = rectTransform.rect.width;
            int childCount = transform.childCount;
            if (width <= 1f ||
                (!force && Mathf.Abs(width - lastWidth) < 0.1f && childCount == lastChildCount))
            {
                return;
            }

            StoreGridMetrics metrics = Calculate(width, childCount);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = metrics.columns;
            grid.cellSize = new Vector2(metrics.cardWidth, DefaultCardHeight);
            grid.spacing = new Vector2(DefaultColumnSpacing, DefaultRowSpacing);
            grid.padding = new RectOffset(
                Mathf.RoundToInt(DefaultHorizontalPadding),
                Mathf.RoundToInt(DefaultHorizontalPadding),
                Mathf.RoundToInt(DefaultVerticalPadding),
                Mathf.RoundToInt(DefaultVerticalPadding));
            grid.childAlignment = TextAnchor.UpperCenter;

            layoutElement.minHeight = metrics.preferredHeight;
            layoutElement.preferredHeight = metrics.preferredHeight;
            lastWidth = width;
            lastChildCount = childCount;
        }
    }
}
