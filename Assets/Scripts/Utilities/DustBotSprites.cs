using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    public static class DustBotSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Player { get { return Load("dustbot-player"); } }
        public static Sprite Sock { get { return Load("sock"); } }
        public static Sprite Wall { get { return Load("wall"); } }
        public static Sprite Cord { get { return Load("cord"); } }
        public static Sprite WetSpot { get { return Load("wet-spot"); } }
        public static Sprite Toy { get { return Load("toy"); } }
        public static Sprite Crumbs { get { return Load("crumbs"); } }
        public static Sprite Dock { get { return Load("dock"); } }
        public static Sprite DustBunny { get { return Load("dust-bunny"); } }
        public static Sprite Cat { get { return Load("cat"); } }

        public static Sprite Cosmetic(string cosmeticId)
        {
            return string.IsNullOrEmpty(cosmeticId)
                ? null
                : Load("Cosmetics/" + cosmeticId);
        }

        public static Sprite ForCell(CellContent content)
        {
            switch (content)
            {
                case CellContent.Dock: return Dock;
                case CellContent.Crumb: return Crumbs;
                case CellContent.Wall: return Wall;
                case CellContent.Sock: return Sock;
                case CellContent.Cord: return Cord;
                case CellContent.WetSpot: return WetSpot;
                case CellContent.Toy: return Toy;
                default: return null;
            }
        }

        private static Sprite Load(string resourceName)
        {
            Sprite sprite;
            if (Cache.TryGetValue(resourceName, out sprite))
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>("Sprites/" + resourceName);
            if (texture == null)
            {
                Debug.LogWarning("DustBot sprite could not be loaded: " + resourceName);
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "DustBot " + resourceName;
            Cache[resourceName] = sprite;
            return sprite;
        }
    }
}
