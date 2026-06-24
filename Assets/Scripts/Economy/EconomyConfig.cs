using System;
using System.Collections.Generic;

namespace DustBot
{
    public static class EconomyConfig
    {
        public const int BaseLevelCompletionCoins = 10;
        public const int TwoStarBonusCoins = 5;
        public const int ThreeStarBonusCoins = 10;
        public const int DustBunnyBonusCoins = 15;
        public const int NormalHintCost = 50;
        public const int DailyHintCost = 75;
        public const int FreeTutorialHintThroughLevel = 3;

        public const int DailyBaseCompletionCoins = 75;
        public const int DailyTwoStarBonusCoins = 20;
        public const int DailyThreeStarBonusCoins = 50;
        public const int DailyDustBunnyBonusCoins = 25;
        public const int DailyNoHintBonusCoins = 25;
        public const int DailyFirstAttemptBonusCoins = 25;

        public const int MilestoneFrequency = 25;
        public const int MilestoneRewardCoins = 100;

        private static readonly int[] DailyStreakRewards =
        {
            75, 90, 110, 130, 150, 175, 225
        };

        public static int StarBonusFor(int stars, bool daily)
        {
            if (daily)
            {
                if (stars >= 3) return DailyThreeStarBonusCoins;
                return stars >= 2 ? DailyTwoStarBonusCoins : 0;
            }

            if (stars >= 3) return ThreeStarBonusCoins;
            return stars >= 2 ? TwoStarBonusCoins : 0;
        }

        public static int DailyStreakReward(int streak)
        {
            if (streak <= 0)
            {
                return 0;
            }

            int weeklyDay = (streak - 1) % DailyStreakRewards.Length;
            return DailyStreakRewards[weeklyDay];
        }

        public static int CosmeticPrice(string cosmeticId)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(cosmeticId);
            return definition == null ? int.MaxValue : definition.coinPrice;
        }
    }

    [Serializable]
    public sealed class CosmeticDefinition
    {
        public string id;
        public string displayName;
        public CosmeticCategory category;
        public CosmeticRarity rarity;
        public int coinPrice;
        public int bunnyRequirement;
        public int levelRequirement;
        public int starRequirement;
        public int dailyCompletionRequirement;
        public int noHintDailyRequirement;
        public int streakRequirement;
        public bool requiresMasterClean;
        public string colorHex;
        public string secondaryColorHex;
        public string[] bundleItemIds;
    }

    public enum CosmeticCategory
    {
        BotSkin,
        PathColor,
        TileTheme,
        DockDesign,
        WinAnimation,
        FailureAnimation,
        RoomBackground,
        Bundle
    }

    public enum CosmeticRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class CosmeticCatalog
    {
        public const string DefaultBot = "bot_classic";
        public const string DefaultPath = "path_mint";
        public const string DefaultTileTheme = "tile_cozy_kitchen";
        public const string DefaultDock = "dock_classic";
        public const string DefaultWinAnimation = "win_sparkle";
        public const string DefaultFailureAnimation = "fail_sock_jam";
        public const string DefaultRoomBackground = "room_apartment";

        private static readonly List<CosmeticDefinition> Entries = new List<CosmeticDefinition>
        {
            Item(DefaultBot, "Classic DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Common, 0, "#FFFFFF"),
            Item("bot_gold", "Gold DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Legendary, 10000, "#FFE09A", stars: 300),
            Item("bot_midnight", "Midnight DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Rare, 2500, "#61708D", level: 100),
            Item("bot_bubblegum", "Bubblegum DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Uncommon, 1000, "#FFB5D8", bunnies: 15),
            Item("bot_honey", "Honey Polish", CosmeticCategory.BotSkin, CosmeticRarity.Uncommon, 750, "#FFE2A0"),
            Item("bot_lavender", "Bunny Lavender", CosmeticCategory.BotSkin, CosmeticRarity.Rare, 2000, "#E5D6FF", bunnies: 10),
            Item("bot_rusty", "Rusty DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Common, 500, "#C48B68", level: 50),
            Item("bot_space", "Space DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Epic, 6500, "#B9C8FF", level: 500),
            Item("bot_cat", "Cat-Eared DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Epic, 5000, "#FFE0C2", bunnies: 50),
            Item("bot_frog", "FrogBot", CosmeticCategory.BotSkin, CosmeticRarity.Rare, 3000, "#A9E79A", bunnies: 25),
            Item("bot_retro", "Retro Vacuum", CosmeticCategory.BotSkin, CosmeticRarity.Rare, 3250, "#F4C06A", daily: 7),
            Item("bot_arcade", "Arcade DustBot", CosmeticCategory.BotSkin, CosmeticRarity.Epic, 7500, "#C5A1FF", stars: 500),

            Item(DefaultPath, "Mint Green", CosmeticCategory.PathColor, CosmeticRarity.Common, 0, "#2B8B74"),
            Item("path_blue", "Classic Blue", CosmeticCategory.PathColor, CosmeticRarity.Common, 350, "#5599D7"),
            Item("path_sunset", "Sunset Orange", CosmeticCategory.PathColor, CosmeticRarity.Uncommon, 900, "#F58B4C", level: 75),
            Item("path_coral", "Coral Route", CosmeticCategory.PathColor, CosmeticRarity.Uncommon, 750, "#E96F68"),
            Item("path_neon", "Neon Purple", CosmeticCategory.PathColor, CosmeticRarity.Rare, 3500, "#A85CFF", daily: 5),
            Item("path_gold", "Gold Trail", CosmeticCategory.PathColor, CosmeticRarity.Epic, 7000, "#F4BE38", stars: 350),
            Item("path_bubble", "Bubble Trail", CosmeticCategory.PathColor, CosmeticRarity.Rare, 2500, "#6DD6E7", bunnies: 35),
            Item("path_space", "Space Trail", CosmeticCategory.PathColor, CosmeticRarity.Epic, 8000, "#6A7EFF", level: 1000),
            Item("path_rainbow", "Rainbow Trail", CosmeticCategory.PathColor, CosmeticRarity.Legendary, 20000, "#F36D80", stars: 1000),

            Item(DefaultTileTheme, "Cozy Kitchen", CosmeticCategory.TileTheme, CosmeticRarity.Common, 0, "#F6EFE0", "#EDE4D0"),
            Item("tile_living", "Living Room", CosmeticCategory.TileTheme, CosmeticRarity.Common, 500, "#EEDDC6", "#DFC9AC"),
            Item("tile_bathroom", "Bathroom Tile", CosmeticCategory.TileTheme, CosmeticRarity.Uncommon, 1000, "#E1F1F4", "#CDE4E9", level: 100),
            Item("tile_arcade", "Arcade Floor", CosmeticCategory.TileTheme, CosmeticRarity.Rare, 3000, "#2F3558", "#454D7B", daily: 5),
            Item("tile_space", "Space Station", CosmeticCategory.TileTheme, CosmeticRarity.Epic, 8000, "#303B59", "#212B44", level: 250),
            Item("tile_garden", "Garden Patio", CosmeticCategory.TileTheme, CosmeticRarity.Rare, 2250, "#DCE8CB", "#C8DAB2", bunnies: 30),
            Item("tile_garage", "Garage Floor", CosmeticCategory.TileTheme, CosmeticRarity.Uncommon, 1250, "#D7D5CE", "#C2C1BB", level: 200),
            Item("tile_candy", "Candy Room", CosmeticCategory.TileTheme, CosmeticRarity.Epic, 6000, "#FFE0EE", "#DCCBFF", stars: 500),

            Item(DefaultDock, "Classic Dock", CosmeticCategory.DockDesign, CosmeticRarity.Common, 0, "#FFFFFF"),
            Item("dock_gold", "Gold Dock", CosmeticCategory.DockDesign, CosmeticRarity.Rare, 3000, "#FFD875", stars: 200),
            Item("dock_neon", "Neon Dock", CosmeticCategory.DockDesign, CosmeticRarity.Rare, 3500, "#BC8BFF", daily: 7),
            Item("dock_wood", "Wooden Dock", CosmeticCategory.DockDesign, CosmeticRarity.Uncommon, 1000, "#C58C62", level: 100),
            Item("dock_space", "Space Dock", CosmeticCategory.DockDesign, CosmeticRarity.Epic, 7000, "#9FB6FF", level: 750),
            Item("dock_catbed", "Cat Bed Dock", CosmeticCategory.DockDesign, CosmeticRarity.Epic, 5500, "#FFD3C4", bunnies: 50),

            Item(DefaultWinAnimation, "Sparkle Clean", CosmeticCategory.WinAnimation, CosmeticRarity.Common, 0, "#F7C657"),
            Item("win_confetti", "Confetti Burst", CosmeticCategory.WinAnimation, CosmeticRarity.Uncommon, 1000, "#F58B79", level: 75),
            Item("win_coins", "Coin Shower", CosmeticCategory.WinAnimation, CosmeticRarity.Rare, 2500, "#F4BE38", stars: 250),
            Item("win_dance", "DustBot Dance", CosmeticCategory.WinAnimation, CosmeticRarity.Epic, 6000, "#78D3B5", noHintDaily: 10),
            Item("win_fireworks", "Fireworks Pop", CosmeticCategory.WinAnimation, CosmeticRarity.Legendary, 12000, "#9E80FF", stars: 750),
            Item("win_bubbles", "Bubble Burst", CosmeticCategory.WinAnimation, CosmeticRarity.Rare, 3000, "#77DCEA", bunnies: 40),

            Item(DefaultFailureAnimation, "Sock Jam", CosmeticCategory.FailureAnimation, CosmeticRarity.Common, 0, "#F0796F"),
            Item("fail_dizzy", "Dizzy Spin", CosmeticCategory.FailureAnimation, CosmeticRarity.Common, 400, "#F7C657"),
            Item("fail_explosion", "Tiny Explosion", CosmeticCategory.FailureAnimation, CosmeticRarity.Rare, 2500, "#F07155", level: 250),
            Item("fail_sad", "Sad Beep", CosmeticCategory.FailureAnimation, CosmeticRarity.Uncommon, 900, "#719CD4", stars: 100),
            Item("fail_slide", "Slippery Slide", CosmeticCategory.FailureAnimation, CosmeticRarity.Rare, 2750, "#77CBE8", daily: 10),
            Item("fail_faceplant", "DustBot Faceplant", CosmeticCategory.FailureAnimation, CosmeticRarity.Epic, 5000, "#C48B68", bunnies: 60),

            Item(DefaultRoomBackground, "Apartment", CosmeticCategory.RoomBackground, CosmeticRarity.Common, 0, "#F8F5EC"),
            Item("room_kitchen", "Kitchen", CosmeticCategory.RoomBackground, CosmeticRarity.Common, 500, "#FFF2D8", level: 50),
            Item("room_bedroom", "Bedroom", CosmeticCategory.RoomBackground, CosmeticRarity.Uncommon, 900, "#F4E7F5", stars: 100),
            Item("room_bathroom", "Bathroom", CosmeticCategory.RoomBackground, CosmeticRarity.Uncommon, 1100, "#E5F4F5", level: 150),
            Item("room_garage", "Garage", CosmeticCategory.RoomBackground, CosmeticRarity.Rare, 2500, "#E0DDD4", level: 500),
            Item("room_arcade", "Arcade", CosmeticCategory.RoomBackground, CosmeticRarity.Epic, 7000, "#242947", daily: 14),
            Item("room_space", "Space Room", CosmeticCategory.RoomBackground, CosmeticRarity.Legendary, 15000, "#171F38", level: 2000, master: true),
            Item("room_cabin", "Cozy Cabin", CosmeticCategory.RoomBackground, CosmeticRarity.Rare, 3000, "#F0D7B2", streak: 7),

            Bundle("bundle_gold", "Gold Bundle", CosmeticRarity.Legendary, 16000, new[] { "bot_gold", "path_gold", "dock_gold", "win_coins" }, stars: 500),
            Bundle("bundle_neon", "Neon Bundle", CosmeticRarity.Epic, 10000, new[] { "path_neon", "tile_arcade", "dock_neon" }, daily: 10),
            Bundle("bundle_space", "Space Bundle", CosmeticRarity.Legendary, 18000, new[] { "bot_space", "path_space", "tile_space", "dock_space", "room_space" }, level: 2000, master: true),
            Bundle("bundle_cozy", "Cozy Bundle", CosmeticRarity.Rare, 4500, new[] { "tile_living", "dock_wood", "room_cabin" }, streak: 7),
            Bundle("bundle_cat", "Cat Bundle", CosmeticRarity.Epic, 8500, new[] { "bot_cat", "dock_catbed" }, bunnies: 75),
            Bundle("bundle_arcade", "Arcade Bundle", CosmeticRarity.Legendary, 14000, new[] { "bot_arcade", "tile_arcade", "room_arcade" }, stars: 750)
        };

        public static IReadOnlyList<CosmeticDefinition> All
        {
            get { return Entries; }
        }

        public static CosmeticDefinition Find(string id)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].id == id)
                {
                    return Entries[i];
                }
            }

            return null;
        }

        public static IReadOnlyList<CosmeticDefinition> ForCategory(CosmeticCategory category)
        {
            List<CosmeticDefinition> matches = new List<CosmeticDefinition>();
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].category == category)
                {
                    matches.Add(Entries[i]);
                }
            }

            return matches;
        }

        private static CosmeticDefinition Item(
            string id,
            string name,
            CosmeticCategory category,
            CosmeticRarity rarity,
            int price,
            string color,
            string secondary = "",
            int level = 0,
            int stars = 0,
            int bunnies = 0,
            int daily = 0,
            int noHintDaily = 0,
            int streak = 0,
            bool master = false)
        {
            return new CosmeticDefinition
            {
                id = id,
                displayName = name,
                category = category,
                rarity = rarity,
                coinPrice = price,
                colorHex = color,
                secondaryColorHex = secondary,
                levelRequirement = level,
                starRequirement = stars,
                bunnyRequirement = bunnies,
                dailyCompletionRequirement = daily,
                noHintDailyRequirement = noHintDaily,
                streakRequirement = streak,
                requiresMasterClean = master
            };
        }

        private static CosmeticDefinition Bundle(
            string id,
            string name,
            CosmeticRarity rarity,
            int price,
            string[] items,
            int level = 0,
            int stars = 0,
            int bunnies = 0,
            int daily = 0,
            int streak = 0,
            bool master = false)
        {
            CosmeticDefinition definition = Item(
                id,
                name,
                CosmeticCategory.Bundle,
                rarity,
                price,
                "#F4BE38",
                level: level,
                stars: stars,
                bunnies: bunnies,
                daily: daily,
                streak: streak,
                master: master);
            definition.bundleItemIds = items;
            return definition;
        }
    }
}
