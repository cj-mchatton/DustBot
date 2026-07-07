using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    /// <summary>
    /// Loads polished cosmetic art when available, with the lightweight cached
    /// procedural drawings retained as a safe fallback for missing resources.
    /// </summary>
    public static class CosmeticSpriteLibrary
    {
        private const int Size = 64;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Preview(CosmeticDefinition definition)
        {
            if (definition == null) return SpriteFactory.Circle;
            switch (definition.category)
            {
                case CosmeticCategory.DustBotSkin: return DustBot(definition.id);
                case CosmeticCategory.CrumbStyle: return Crumb(definition.id);
                case CosmeticCategory.CatSkin: return Cat(definition.id);
                case CosmeticCategory.DockDesign: return Dock(definition.id);
                case CosmeticCategory.PathTrail: return PathNode(definition.id);
                default: return SpriteFactory.RoundedRect;
            }
        }

        public static Sprite DustBot(string id)
        {
            if (id == CosmeticCatalog.DefaultBot) return DustBotSprites.Player;
            Sprite polished = DustBotSprites.Cosmetic(id);
            if (polished != null) return polished;
            return Get("bot:" + id, delegate(Canvas c) { DrawBot(c, id); });
        }

        public static Sprite Crumb(string id)
        {
            if (string.IsNullOrEmpty(id) || id == CosmeticCatalog.DefaultCrumbStyle)
                return DustBotSprites.Crumbs;
            Sprite polished = DustBotSprites.Cosmetic(id);
            if (polished != null) return polished;
            return Get("crumb:" + id, delegate(Canvas c) { DrawCrumb(c, id); });
        }

        public static Sprite Cat(string id)
        {
            if (string.IsNullOrEmpty(id) || id == CosmeticCatalog.DefaultCatSkin)
                return DustBotSprites.Cat;
            Sprite polished = DustBotSprites.Cosmetic(id);
            if (polished != null) return polished;
            return Get("cat:" + id, delegate(Canvas c) { DrawCat(c, id); });
        }

        public static Sprite Dock(string id)
        {
            if (string.IsNullOrEmpty(id) || id == CosmeticCatalog.DefaultDock)
                return DustBotSprites.Dock;
            Sprite polished = DustBotSprites.Cosmetic(id);
            if (polished != null) return polished;
            return Get("dock:" + id, delegate(Canvas c) { DrawDock(c, id); });
        }

        public static Sprite PathNode(string id)
        {
            if (string.IsNullOrEmpty(id) || id == CosmeticCatalog.DefaultPath)
                return SpriteFactory.Circle;
            Sprite polished = DustBotSprites.Cosmetic(id);
            if (polished != null) return polished;
            return Get("path:" + id, delegate(Canvas c) { DrawPathNode(c, id); });
        }

        public static Sprite Tile(string id, bool alternate)
        {
            return Get("tile:" + id + ":" + alternate, delegate(Canvas c)
            {
                CosmeticDefinition definition = CosmeticCatalog.Find(id);
                Color primary = Parse(definition == null ? string.Empty : definition.colorHex, DustBotTheme.TileA);
                Color secondary = Parse(definition == null ? string.Empty : definition.secondaryColorHex, DustBotTheme.TileB);
                DrawTile(c, id, alternate ? secondary : primary, alternate ? primary : secondary);
            });
        }

        public static Sprite Room(string id)
        {
            return Get("room:" + id, delegate(Canvas c)
            {
                CosmeticDefinition definition = CosmeticCatalog.Find(id);
                Color primary = Parse(definition == null ? string.Empty : definition.colorHex, DustBotTheme.Background);
                Color secondary = Parse(definition == null ? string.Empty : definition.secondaryColorHex, Color.white);
                DrawRoom(c, id, primary, secondary);
            });
        }

        private static Sprite Get(string key, Action<Canvas> draw)
        {
            Sprite sprite;
            if (Cache.TryGetValue(key, out sprite)) return sprite;
            Canvas canvas = new Canvas(Size);
            draw(canvas);
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "Cosmetic " + key;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(canvas.Pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Cosmetic " + key;
            Cache[key] = sprite;
            return sprite;
        }

        private static void DrawBot(Canvas c, string id)
        {
            Color body = ColorFor(id, new Color32(120, 190, 175, 255));
            Color dark = Color.Lerp(body, new Color(0.12f, 0.16f, 0.18f), 0.48f);
            if (id == "bot_retro")
            {
                c.Ellipse(12, 20, 42, 43, dark); c.Ellipse(15, 23, 40, 41, body);
                c.Rect(34, 27, 58, 34, dark); c.Circle(17, 20, 6, dark); c.Circle(41, 20, 6, dark);
            }
            else if (id == "bot_arcade")
            {
                c.RoundedRect(13, 12, 51, 53, 6, dark); c.Rect(18, 27, 46, 47, body);
                c.Rect(21, 31, 43, 43, new Color32(42, 54, 88, 255));
                c.Rect(25, 34, 29, 38, new Color32(255, 112, 190, 255));
                c.Rect(34, 38, 39, 42, new Color32(95, 237, 216, 255));
            }
            else
            {
                c.Ellipse(9, 10, 55, 52, dark); c.Ellipse(13, 14, 51, 49, body);
            }

            if (id == "bot_gold")
            {
                c.Triangle(18, 49, 25, 62, 31, 49, new Color32(246, 190, 55, 255));
                c.Triangle(27, 49, 34, 63, 40, 49, new Color32(255, 218, 91, 255));
                c.Triangle(36, 49, 43, 61, 49, 49, new Color32(246, 190, 55, 255));
            }
            else if (id == "bot_midnight")
            {
                c.Circle(43, 43, 9, new Color32(242, 231, 169, 255));
                c.Circle(47, 46, 8, body);
            }
            else if (id == "bot_bubblegum")
            {
                c.Circle(20, 50, 8, new Color32(246, 98, 166, 255));
                c.Circle(34, 50, 8, new Color32(246, 98, 166, 255));
                c.Circle(27, 50, 4, new Color32(255, 224, 237, 255));
            }
            else if (id == "bot_honey")
            {
                c.Rect(14, 25, 50, 30, new Color32(74, 61, 42, 255));
                c.Rect(13, 37, 51, 42, new Color32(74, 61, 42, 255));
                c.Ellipse(3, 28, 18, 43, new Color32(232, 247, 242, 210));
                c.Ellipse(46, 28, 61, 43, new Color32(232, 247, 242, 210));
            }
            else if (id == "bot_lavender")
            {
                c.Ellipse(14, 44, 25, 63, body); c.Ellipse(39, 44, 50, 63, body);
            }
            else if (id == "bot_space")
            {
                c.Ellipse(14, 31, 50, 59, new Color32(166, 222, 244, 175));
                c.Rect(17, 31, 47, 35, new Color32(73, 92, 139, 255));
            }
            else if (id == "bot_cat")
            {
                c.Triangle(11, 45, 18, 63, 29, 48, body);
                c.Triangle(35, 48, 47, 63, 54, 44, body);
            }
            else if (id == "bot_frog")
            {
                c.Circle(18, 50, 10, body); c.Circle(46, 50, 10, body);
                c.Circle(18, 52, 4, new Color32(35, 52, 38, 255));
                c.Circle(46, 52, 4, new Color32(35, 52, 38, 255));
            }
            else if (id == "bot_rusty")
            {
                c.Rect(14, 18, 27, 31, new Color32(126, 75, 57, 255));
                c.Circle(44, 41, 5, new Color32(224, 175, 113, 255));
            }

            if (id != "bot_arcade")
            {
                c.Circle(24, 35, 4, new Color32(248, 252, 244, 255));
                c.Circle(40, 35, 4, new Color32(248, 252, 244, 255));
                c.Circle(24, 35, 2, dark); c.Circle(40, 35, 2, dark);
                c.Rect(25, 22, 39, 25, dark);
            }
        }

        private static void DrawCrumb(Canvas c, string id)
        {
            if (id == "crumb_cookie")
            {
                c.Circle(32, 32, 22, new Color32(181, 119, 61, 255));
                c.Circle(23, 39, 3, new Color32(82, 54, 38, 255)); c.Circle(39, 37, 3, new Color32(82, 54, 38, 255));
                c.Circle(31, 23, 3, new Color32(82, 54, 38, 255)); c.Circle(44, 21, 2, new Color32(82, 54, 38, 255));
            }
            else if (id == "crumb_cereal")
            {
                c.Circle(20, 38, 10, new Color32(240, 179, 77, 255)); c.Circle(20, 38, 5, Color.clear);
                c.Circle(42, 31, 11, new Color32(235, 104, 90, 255)); c.Circle(42, 31, 5, Color.clear);
                c.Circle(27, 18, 8, new Color32(122, 191, 119, 255)); c.Circle(27, 18, 4, Color.clear);
            }
            else if (id == "crumb_dust")
            {
                Color dust = new Color32(143, 157, 168, 255);
                c.Circle(21, 27, 13, dust); c.Circle(36, 34, 17, dust); c.Circle(48, 25, 10, dust); c.Rect(13, 18, 53, 29, dust);
            }
            else if (id == "crumb_leaf")
            {
                c.Ellipse(12, 20, 35, 49, new Color32(87, 150, 77, 255)); c.Line(18, 22, 34, 47, 3, new Color32(57, 105, 52, 255));
                c.Ellipse(34, 12, 54, 37, new Color32(199, 132, 61, 255)); c.Line(37, 14, 52, 34, 3, new Color32(132, 83, 45, 255));
            }
            else if (id == "crumb_candy")
            {
                c.RoundedRect(18, 21, 46, 43, 5, new Color32(240, 113, 156, 255));
                c.Triangle(5, 22, 18, 32, 5, 42, new Color32(126, 203, 212, 255));
                c.Triangle(59, 22, 46, 32, 59, 42, new Color32(126, 203, 212, 255));
                c.Rect(28, 22, 34, 42, new Color32(255, 215, 112, 255));
            }
            else
            {
                Color corn = new Color32(247, 219, 106, 255);
                c.Circle(22, 31, 12, corn); c.Circle(34, 39, 13, corn); c.Circle(45, 29, 11, corn); c.Rect(15, 13, 50, 28, new Color32(224, 76, 70, 255));
            }
        }

        private static void DrawCat(Canvas c, string id)
        {
            Color fur = ColorFor(id, new Color32(226, 148, 71, 255));
            Color dark = Color.Lerp(fur, new Color(0.12f, 0.13f, 0.15f), 0.5f);
            if (id == "cat_ghost")
            {
                c.Ellipse(12, 14, 52, 55, fur); c.Circle(24, 39, 3, dark); c.Circle(40, 39, 3, dark);
                c.Triangle(12, 14, 21, 24, 28, 13, fur); c.Triangle(28, 13, 38, 24, 52, 14, fur);
                return;
            }
            if (id == "cat_robot")
            {
                c.RoundedRect(11, 16, 53, 52, 6, dark); c.Rect(15, 20, 49, 48, fur);
                c.Triangle(12, 48, 18, 62, 28, 49, fur); c.Triangle(36, 49, 47, 62, 52, 48, fur);
                c.Rect(21, 34, 28, 40, new Color32(101, 245, 222, 255)); c.Rect(36, 34, 43, 40, new Color32(101, 245, 222, 255));
                return;
            }
            c.Ellipse(10, 10, 54, 49, dark); c.Ellipse(14, 14, 50, 47, fur);
            c.Triangle(12, 43, 19, 62, 30, 47, fur); c.Triangle(34, 47, 46, 62, 53, 42, fur);
            c.Circle(24, 36, 3, new Color32(43, 44, 45, 255)); c.Circle(40, 36, 3, new Color32(43, 44, 45, 255));
            c.Triangle(28, 28, 32, 24, 36, 28, new Color32(211, 107, 112, 255));
            if (id == "cat_sleepy")
            {
                c.Line(19, 36, 28, 34, 2, dark); c.Line(36, 34, 45, 36, 2, dark);
            }
            else if (id == "cat_fancy")
            {
                c.Circle(23, 16, 7, new Color32(132, 53, 78, 255)); c.Circle(41, 16, 7, new Color32(132, 53, 78, 255)); c.Circle(32, 16, 4, new Color32(245, 198, 79, 255));
            }
            else if (id == "cat_space")
            {
                c.Ellipse(7, 9, 57, 59, new Color32(189, 228, 244, 135)); c.Rect(12, 15, 52, 20, new Color32(83, 103, 160, 255));
            }
            else if (id == "cat_tiger")
            {
                c.Line(19, 49, 23, 40, 3, dark); c.Line(31, 51, 32, 41, 3, dark); c.Line(45, 49, 41, 40, 3, dark);
            }
            else if (id == "cat_tuxedo")
            {
                c.Triangle(22, 18, 32, 34, 42, 18, Color.white); c.Circle(32, 24, 4, Color.white);
            }
        }

        private static void DrawDock(Canvas c, string id)
        {
            Color color = ColorFor(id, Color.white);
            Color dark = Color.Lerp(color, new Color(0.12f, 0.13f, 0.15f), 0.45f);
            if (id == "dock_catbed")
            {
                c.Ellipse(5, 8, 59, 43, dark); c.Ellipse(10, 13, 54, 38, color);
                c.Circle(18, 45, 8, color); c.Circle(46, 45, 8, color);
            }
            else if (id == "dock_space")
            {
                c.Ellipse(6, 8, 58, 38, dark); c.Ellipse(12, 13, 52, 34, color); c.Rect(20, 35, 44, 58, dark); c.Circle(32, 46, 7, new Color32(111, 213, 236, 255));
            }
            else if (id == "dock_wood")
            {
                c.RoundedRect(7, 10, 57, 48, 7, dark); c.Rect(12, 15, 52, 43, color);
                c.Line(14, 22, 50, 22, 3, dark); c.Line(14, 34, 50, 34, 3, dark);
            }
            else
            {
                c.RoundedRect(7, 9, 57, 48, 8, dark); c.RoundedRect(12, 14, 52, 43, 6, color);
                c.Rect(25, 42, 39, 59, dark); c.Circle(32, 50, 4, id == "dock_neon" ? new Color32(113, 255, 222, 255) : new Color32(255, 231, 108, 255));
            }
        }

        private static void DrawPathNode(Canvas c, string id)
        {
            Color color = ColorFor(id, DustBotTheme.MintDark);
            if (id == "path_coral")
            {
                c.Ellipse(18, 10, 46, 34, color); c.Circle(14, 40, 8, color); c.Circle(29, 48, 8, color); c.Circle(45, 43, 8, color);
            }
            else if (id == "path_gold")
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = Mathf.PI * 2f * i / 5f + Mathf.PI * 0.5f;
                    float b = a + Mathf.PI / 5f;
                    c.Triangle(32, 32, 32 + Mathf.Cos(a) * 27, 32 + Mathf.Sin(a) * 27, 32 + Mathf.Cos(b) * 10, 32 + Mathf.Sin(b) * 10, color);
                }
                c.Circle(32, 32, 12, color);
            }
            else if (id == "path_sunset")
            {
                c.Ellipse(13, 8, 51, 56, color); c.Line(16, 12, 48, 52, 5, Color.Lerp(color, Color.black, 0.3f));
            }
            else if (id == "path_blue" || id == "path_bubble")
            {
                c.Circle(32, 32, 25, color); c.Circle(32, 32, 17, Color.clear); c.Circle(22, 45, 5, new Color(1f, 1f, 1f, 0.65f));
            }
            else if (id == "path_space")
            {
                c.Circle(25, 34, 14, color); c.Triangle(32, 20, 61, 32, 31, 46, color); c.Circle(22, 39, 4, Color.white);
            }
            else
            {
                c.RoundedRect(7, 21, 57, 43, 10, color); c.Circle(17, 45, 5, new Color(1f, 1f, 1f, 0.7f)); c.Circle(43, 19, 4, new Color(1f, 1f, 1f, 0.6f));
            }
        }

        private static void DrawTile(Canvas c, string id, Color baseColor, Color accent)
        {
            c.RoundedRect(0, 0, 63, 63, 8, baseColor);
            Color detail = Color.Lerp(accent, Color.white, 0.18f);
            if (id == "tile_bathroom")
            {
                c.Line(0, 31, 63, 31, 2, detail); c.Line(31, 0, 31, 63, 2, detail);
            }
            else if (id == "tile_arcade")
            {
                c.Rect(5, 5, 13, 13, detail); c.Rect(50, 50, 58, 58, detail); c.Line(12, 52, 52, 12, 2, detail);
            }
            else if (id == "tile_space")
            {
                c.Circle(13, 48, 2, detail); c.Circle(46, 42, 3, detail); c.Circle(36, 14, 2, detail); c.Line(7, 7, 57, 7, 2, detail);
            }
            else if (id == "tile_garden")
            {
                c.Line(0, 18, 63, 8, 3, detail); c.Line(0, 45, 63, 55, 3, detail); c.Line(22, 0, 16, 63, 3, detail); c.Line(48, 0, 42, 63, 3, detail);
            }
            else if (id == "tile_garage")
            {
                for (int x = -20; x < 80; x += 14) c.Line(x, 0, x + 40, 63, 3, detail);
            }
            else if (id == "tile_candy")
            {
                c.Circle(32, 32, 21, detail); c.Circle(32, 32, 14, baseColor); c.Circle(32, 32, 7, detail);
            }
            else
            {
                c.Line(6, 10, 58, 10, 2, detail); c.Line(6, 54, 58, 54, 2, detail);
            }
        }

        private static void DrawRoom(Canvas c, string id, Color primary, Color secondary)
        {
            c.Rect(0, 0, 63, 63, primary);
            if (id == "room_space")
            {
                c.Circle(11, 49, 2, secondary); c.Circle(46, 52, 3, secondary); c.Circle(36, 21, 2, secondary); c.Circle(57, 9, 2, secondary);
                c.Ellipse(6, 3, 27, 16, secondary);
            }
            else if (id == "room_arcade")
            {
                for (int x = 0; x < 64; x += 16) c.Line(x, 0, 32, 63, 2, new Color(secondary.r, secondary.g, secondary.b, 0.45f));
            }
            else if (id == "room_candy")
            {
                c.Circle(10, 52, 8, secondary); c.Circle(52, 45, 11, secondary); c.Circle(31, 14, 9, secondary);
            }
            else if (id == "room_bathroom")
            {
                for (int p = 0; p <= 64; p += 16) { c.Line(0, p, 63, p, 1, secondary); c.Line(p, 0, p, 63, 1, secondary); }
            }
            else if (id == "room_garage")
            {
                for (int p = -32; p < 96; p += 18) c.Line(p, 0, p + 45, 63, 4, new Color(secondary.r, secondary.g, secondary.b, 0.24f));
            }
            else if (id == "room_cabin")
            {
                for (int p = 8; p < 64; p += 12) c.Line(0, p, 63, p, 3, secondary);
            }
            else
            {
                c.Rect(0, 0, 63, 14, secondary); c.Circle(52, 52, 8, new Color(secondary.r, secondary.g, secondary.b, 0.35f));
            }
        }

        private static Color ColorFor(string id, Color fallback)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(id);
            return Parse(definition == null ? string.Empty : definition.colorHex, fallback);
        }

        private static Color Parse(string hex, Color fallback)
        {
            Color color;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out color) ? color : fallback;
        }

        private sealed class Canvas
        {
            private readonly int size;
            public readonly Color32[] Pixels;

            public Canvas(int size)
            {
                this.size = size;
                Pixels = new Color32[size * size];
            }

            public void Rect(int x0, int y0, int x1, int y1, Color color)
            {
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) Set(x, y, color);
            }

            public void RoundedRect(int x0, int y0, int x1, int y1, int radius, Color color)
            {
                Rect(x0 + radius, y0, x1 - radius, y1, color); Rect(x0, y0 + radius, x1, y1 - radius, color);
                Circle(x0 + radius, y0 + radius, radius, color); Circle(x1 - radius, y0 + radius, radius, color);
                Circle(x0 + radius, y1 - radius, radius, color); Circle(x1 - radius, y1 - radius, radius, color);
            }

            public void Circle(int cx, int cy, int radius, Color color)
            {
                int rr = radius * radius;
                for (int y = cy - radius; y <= cy + radius; y++)
                    for (int x = cx - radius; x <= cx + radius; x++)
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= rr) Set(x, y, color);
            }

            public void Ellipse(int x0, int y0, int x1, int y1, Color color)
            {
                float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
                float rx = Mathf.Max(0.5f, (x1 - x0) * 0.5f), ry = Mathf.Max(0.5f, (y1 - y0) * 0.5f);
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                    if (((x - cx) * (x - cx)) / (rx * rx) + ((y - cy) * (y - cy)) / (ry * ry) <= 1f) Set(x, y, color);
            }

            public void Triangle(float ax, float ay, float bx, float by, float cx, float cy, Color color)
            {
                int minX = Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx))), maxX = Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx)));
                int minY = Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy))), maxY = Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy)));
                float d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy);
                if (Mathf.Abs(d) < 0.001f) return;
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    float u = ((by - cy) * (x - cx) + (cx - bx) * (y - cy)) / d;
                    float v = ((cy - ay) * (x - cx) + (ax - cx) * (y - cy)) / d;
                    if (u >= 0f && v >= 0f && u + v <= 1f) Set(x, y, color);
                }
            }

            public void Line(float x0, float y0, float x1, float y1, int width, Color color)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))));
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Circle(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), Mathf.Max(1, width / 2), color);
                }
            }

            private void Set(int x, int y, Color color)
            {
                if (x < 0 || y < 0 || x >= size || y >= size) return;
                Pixels[y * size + x] = color;
            }
        }
    }
}
