#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DustBot.Editor
{
    public static class CosmeticStoreLayoutValidationRunner
    {
        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        private const float MatchWidthOrHeight = 0.5f;
        private const float StoreWidthFraction = 0.89f;
        private const float ContentHorizontalPadding = 20f;

        [MenuItem("DustBot/Validate Cosmetic Store Layout")]
        public static void RunBatch()
        {
            ValidatePhone("iPhone SE (2nd/3rd gen)", 750, 1334, 750, true);
            ValidatePhone("iPhone 15", 1179, 2556, 1179, true);
            ValidatePhone("iPhone 16 Pro Max", 1320, 2868, 1320, true);
            ValidatePhone("Narrow fallback", 640, 1600, 480, false);
            ValidateCatalogAndPreviews();
            Debug.Log("COSMETIC_STORE_LAYOUT PASS");
        }

        private static void ValidateCatalogAndPreviews()
        {
            CosmeticCategory[] categories =
            {
                CosmeticCategory.DustBotSkin,
                CosmeticCategory.PathTrail,
                CosmeticCategory.CrumbStyle,
                CosmeticCategory.CatSkin,
                CosmeticCategory.DockDesign,
                CosmeticCategory.TileTheme,
                CosmeticCategory.RoomTheme,
                CosmeticCategory.Bundle
            };

            for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
            {
                var items = CosmeticCatalog.ForCategory(categories[categoryIndex]);
                Require(items.Count > 0, categories[categoryIndex] + " category was empty.");
                for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                {
                    CosmeticDefinition item = items[itemIndex];
                    Require(!string.IsNullOrEmpty(item.id), "Cosmetic ID was empty.");
                    Require(!string.IsNullOrEmpty(item.assetKey), item.id + " had no asset key.");
                    Require(!string.IsNullOrEmpty(item.description), item.id + " had no description.");
                    if (item.category == CosmeticCategory.TileTheme)
                        Require(CosmeticSpriteLibrary.Tile(item.id, false) != null, item.id + " tile preview failed.");
                    else if (item.category == CosmeticCategory.RoomTheme)
                        Require(CosmeticSpriteLibrary.Room(item.id) != null, item.id + " room preview failed.");
                    else if (item.category != CosmeticCategory.Bundle)
                        Require(CosmeticSpriteLibrary.Preview(item) != null, item.id + " preview failed.");
                }
            }
        }

        private static void ValidatePhone(
            string name,
            int screenWidth,
            int screenHeight,
            int safeAreaWidth,
            bool expectTwoColumns)
        {
            float scale = CanvasScale(screenWidth, screenHeight);
            float safeCanvasWidth = safeAreaWidth / scale;
            float viewportWidth = safeCanvasWidth * StoreWidthFraction;
            float gridWidth = viewportWidth - ContentHorizontalPadding;
            StoreGridMetrics metrics = ResponsiveStoreGrid.Calculate(gridWidth, 12);
            float usedWidth = ResponsiveStoreGrid.DefaultHorizontalPadding * 2f +
                metrics.cardWidth * metrics.columns +
                ResponsiveStoreGrid.DefaultColumnSpacing * (metrics.columns - 1);

            Require(metrics.cardWidth > 0f, name + " produced a non-positive card width.");
            Require(usedWidth <= gridWidth + 0.01f, name + " cards overflow the viewport.");
            Require(metrics.columns == (expectTwoColumns ? 2 : 1),
                name + " selected " + metrics.columns + " columns unexpectedly.");
            Require(metrics.preferredHeight > ResponsiveStoreGrid.DefaultCardHeight,
                name + " did not provide a vertically scrollable content height.");
            ValidateRuntimeGrid(name, gridWidth, metrics);

            Debug.Log(string.Format(
                "COSMETIC_STORE_LAYOUT {0} screen={1}x{2} safeWidth={3} viewport={4:F1} grid={5:F1} columns={6} cardWidth={7:F1} margin={8:F1}",
                name,
                screenWidth,
                screenHeight,
                safeAreaWidth,
                viewportWidth,
                gridWidth,
                metrics.columns,
                metrics.cardWidth,
                ResponsiveStoreGrid.DefaultHorizontalPadding));
        }

        private static void ValidateRuntimeGrid(
            string name,
            float gridWidth,
            StoreGridMetrics expected)
        {
            GameObject gridObject = new GameObject(
                name + " Runtime Grid",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(LayoutElement),
                typeof(ResponsiveStoreGrid));
            try
            {
                RectTransform rect = gridObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(gridWidth, 100f);
                for (int i = 0; i < 12; i++)
                {
                    GameObject child = new GameObject("Card " + i, typeof(RectTransform));
                    child.transform.SetParent(gridObject.transform, false);
                }

                gridObject.GetComponent<ResponsiveStoreGrid>().Refresh();
                GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
                LayoutElement layout = gridObject.GetComponent<LayoutElement>();
                Require(grid.constraintCount == expected.columns,
                    name + " runtime grid did not apply the calculated column count.");
                Require(Mathf.Abs(grid.cellSize.x - expected.cardWidth) < 0.01f,
                    name + " runtime grid did not apply the calculated card width.");
                Require(Mathf.Abs(layout.preferredHeight - expected.preferredHeight) < 0.01f,
                    name + " runtime grid did not preserve vertical scrolling height.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gridObject);
            }
        }

        private static float CanvasScale(float screenWidth, float screenHeight)
        {
            float widthScale = screenWidth / ReferenceWidth;
            float heightScale = screenHeight / ReferenceHeight;
            float logScale = Mathf.Lerp(
                Mathf.Log(widthScale, 2f),
                Mathf.Log(heightScale, 2f),
                MatchWidthOrHeight);
            return Mathf.Pow(2f, logScale);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
