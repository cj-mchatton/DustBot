using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DustBot
{
    public static class DustBotTheme
    {
        public static readonly Color Background = new Color32(248, 245, 236, 255);
        public static readonly Color Panel = new Color32(255, 253, 247, 252);
        public static readonly Color PanelSoft = new Color32(235, 244, 238, 255);
        public static readonly Color Ink = new Color32(35, 62, 57, 255);
        public static readonly Color MutedInk = new Color32(105, 126, 118, 255);
        public static readonly Color Mint = new Color32(104, 207, 172, 255);
        public static readonly Color MintDark = new Color32(43, 139, 116, 255);
        public static readonly Color Yellow = new Color32(248, 198, 91, 255);
        public static readonly Color Coral = new Color32(241, 121, 111, 255);
        public static readonly Color Error = new Color32(218, 66, 71, 255);
        public static readonly Color Warning = new Color32(190, 132, 48, 255);
        public static readonly Color Blue = new Color32(106, 178, 226, 255);
        public static readonly Color TileA = new Color32(246, 239, 224, 255);
        public static readonly Color TileB = new Color32(237, 228, 208, 255);
        public static readonly Color Blocker = new Color32(105, 81, 64, 255);
        public static readonly Color Overlay = new Color(0.08f, 0.12f, 0.1f, 0.62f);
    }

    public static class UIFactory
    {
        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return font;
            }
        }

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        public static RectTransform Stretch(GameObject gameObject)
        {
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = SpriteFactory.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string value,
            int size,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Text text = gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            UnityAction onClick,
            Color color,
            int fontSize = 42)
        {
            Image image = CreatePanel(name, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            Outline rim = image.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.06f, 0.14f, 0.11f, 0.18f);
            rim.effectDistance = new Vector2(2f, -2f);
            rim.useGraphicAlpha = true;
            Shadow shadow = image.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.08f, 0.16f, 0.12f, 0.18f);
            shadow.effectDistance = new Vector2(0f, -5f);
            shadow.useGraphicAlpha = true;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.7f);
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            button.gameObject.AddComponent<ButtonPressFeedback>();
            Image shine = CreatePanel("Soft Highlight", button.transform, new Color(1f, 1f, 1f, 0.14f));
            SetAnchors(
                shine.rectTransform,
                new Vector2(0.025f, 0.55f),
                new Vector2(0.975f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            shine.raycastTarget = false;
            Text text = CreateText("Label", button.transform, label, fontSize, Color.white);
            Stretch(text.gameObject);
            text.rectTransform.offsetMin = new Vector2(12f, 6f);
            text.rectTransform.offsetMax = new Vector2(-12f, -6f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = fontSize;
            return button;
        }

        public static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static Text GetButtonText(Button button)
        {
            return button.GetComponentInChildren<Text>();
        }
    }
}
