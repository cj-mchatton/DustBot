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
        public string description;
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
        public LevelCategory categoryCompletionRequirement;
        public int catLevelCompletionRequirement;
        public int perfectCleanRequirement;
        public string colorHex;
        public string secondaryColorHex;
        public CosmeticAssetType affectedAssetType;
        public string previewSpriteKey;
        public string assetKey;
        public string animationKey;
        public string particleEffectKey;
        public string materialEffectKey;
        public string visualStyle;
        public bool isNew;
        public string[] bundleItemIds;
    }

    public enum CosmeticCategory
    {
        DustBotSkin,
        PathTrail,
        TileTheme,
        DockDesign,
        // Legacy celebration slots remain save-compatible, but are not sold.
        WinAnimation,
        FailureAnimation,
        RoomTheme,
        Bundle,
        CrumbStyle,
        CatSkin
    }

    public enum CosmeticAssetType
    {
        DustBot,
        Path,
        Crumb,
        Cat,
        Dock,
        Tile,
        Room,
        Multiple,
        LegacyCelebration
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
        public const string DefaultCrumbStyle = "crumb_classic";
        public const string DefaultCatSkin = "cat_classic";
        public const string DefaultWinAnimation = "win_sparkle";
        public const string DefaultFailureAnimation = "fail_sock_jam";
        public const string DefaultRoomBackground = "room_apartment";

        private static readonly List<CosmeticDefinition> Entries = new List<CosmeticDefinition>
        {
            Item(DefaultBot, "Classic DustBot", CosmeticCategory.DustBotSkin, CosmeticRarity.Common, 0, "#FFFFFF"),
            Item("bot_gold", "Crowned Gold", CosmeticCategory.DustBotSkin, CosmeticRarity.Legendary, 10000, "#FFE09A", stars: 300),
            Item("bot_midnight", "Moonlight Rover", CosmeticCategory.DustBotSkin, CosmeticRarity.Rare, 2500, "#61708D", categoryComplete: LevelCategory.Medium),
            Item("bot_bubblegum", "Bubble Bow Bot", CosmeticCategory.DustBotSkin, CosmeticRarity.Uncommon, 1000, "#FFB5D8", bunnies: 15),
            Item("bot_honey", "Honeybee Helper", CosmeticCategory.DustBotSkin, CosmeticRarity.Uncommon, 750, "#FFE2A0"),
            Item("bot_lavender", "Bunny-Eared Bot", CosmeticCategory.DustBotSkin, CosmeticRarity.Rare, 2000, "#E5D6FF", bunnies: 10),
            Item("bot_rusty", "Patchwork Rover", CosmeticCategory.DustBotSkin, CosmeticRarity.Common, 500, "#C48B68", categoryComplete: LevelCategory.Easy),
            Item("bot_space", "Orbital DustBot", CosmeticCategory.DustBotSkin, CosmeticRarity.Epic, 6500, "#B9C8FF", categoryComplete: LevelCategory.Hard),
            Item("bot_cat", "Cat-Eared DustBot", CosmeticCategory.DustBotSkin, CosmeticRarity.Epic, 5000, "#FFE0C2", catLevels: 25),
            Item("bot_frog", "FrogBot", CosmeticCategory.DustBotSkin, CosmeticRarity.Rare, 3000, "#A9E79A", bunnies: 25),
            Item("bot_retro", "Retro Vacuum", CosmeticCategory.DustBotSkin, CosmeticRarity.Rare, 3250, "#F4C06A", daily: 7),
            Item("bot_arcade", "Arcade Cabinet Bot", CosmeticCategory.DustBotSkin, CosmeticRarity.Epic, 7500, "#C5A1FF", stars: 500),

            Item(DefaultPath, "Mint Ribbon", CosmeticCategory.PathTrail, CosmeticRarity.Common, 0, "#2B8B74"),
            Item("path_blue", "Bubble Dots", CosmeticCategory.PathTrail, CosmeticRarity.Common, 350, "#5599D7"),
            Item("path_sunset", "Falling Leaves", CosmeticCategory.PathTrail, CosmeticRarity.Uncommon, 900, "#D57A42", level: 75),
            Item("path_coral", "Paw Print Path", CosmeticCategory.PathTrail, CosmeticRarity.Uncommon, 750, "#9B5A72"),
            Item("path_neon", "Lofi Pulse", CosmeticCategory.PathTrail, CosmeticRarity.Rare, 3500, "#A85CFF", daily: 5),
            Item("path_gold", "Star Parade", CosmeticCategory.PathTrail, CosmeticRarity.Epic, 7000, "#F4BE38", stars: 350),
            Item("path_bubble", "Bubble Trail", CosmeticCategory.PathTrail, CosmeticRarity.Rare, 2500, "#6DD6E7", bunnies: 35),
            Item("path_space", "Comet Trail", CosmeticCategory.PathTrail, CosmeticRarity.Epic, 8000, "#6A7EFF", categoryComplete: LevelCategory.Expert),
            Item("path_rainbow", "Prism Ribbon", CosmeticCategory.PathTrail, CosmeticRarity.Legendary, 20000, "#F36D80", stars: 700),

            Item("crumb_classic", "Classic Crumbs", CosmeticCategory.CrumbStyle, CosmeticRarity.Common, 0, "#C99762"),
            Item("crumb_cookie", "Cookie Bits", CosmeticCategory.CrumbStyle, CosmeticRarity.Common, 450, "#B8773D"),
            Item("crumb_cereal", "Cereal Loops", CosmeticCategory.CrumbStyle, CosmeticRarity.Uncommon, 900, "#F0B34D", stars: 75),
            Item("crumb_dust", "Fluffy Dust Piles", CosmeticCategory.CrumbStyle, CosmeticRarity.Uncommon, 1000, "#9AA7B3", categoryComplete: LevelCategory.Easy),
            Item("crumb_leaf", "Garden Leaves", CosmeticCategory.CrumbStyle, CosmeticRarity.Rare, 2200, "#77A85D", bunnies: 50),
            Item("crumb_candy", "Wrapped Candy", CosmeticCategory.CrumbStyle, CosmeticRarity.Rare, 2600, "#F0719C", perfectCleans: 30),
            Item("crumb_popcorn", "Movie Popcorn", CosmeticCategory.CrumbStyle, CosmeticRarity.Epic, 4200, "#F5D86A", daily: 12),

            Item("cat_classic", "Orange Cat", CosmeticCategory.CatSkin, CosmeticRarity.Common, 0, "#E9954F"),
            Item("cat_tuxedo", "Tuxedo Cat", CosmeticCategory.CatSkin, CosmeticRarity.Common, 600, "#3F4650"),
            Item("cat_sleepy", "Sleepy Cat", CosmeticCategory.CatSkin, CosmeticRarity.Uncommon, 1100, "#B9A58D", catLevels: 10),
            Item("cat_fancy", "Fancy Cat", CosmeticCategory.CatSkin, CosmeticRarity.Rare, 2600, "#EEE4D4", perfectCleans: 25),
            Item("cat_space", "Astro Cat", CosmeticCategory.CatSkin, CosmeticRarity.Epic, 6000, "#8EA6E8", categoryComplete: LevelCategory.CatChase),
            Item("cat_ghost", "Ghost Cat", CosmeticCategory.CatSkin, CosmeticRarity.Epic, 5500, "#D9F4F1", catLevels: 35),
            Item("cat_robot", "Robot Cat", CosmeticCategory.CatSkin, CosmeticRarity.Legendary, 9000, "#92A6B8", stars: 600),
            Item("cat_tiger", "Tiny Tiger", CosmeticCategory.CatSkin, CosmeticRarity.Rare, 3200, "#E59A3D", bunnies: 60),

            Item(DefaultTileTheme, "Cozy Kitchen", CosmeticCategory.TileTheme, CosmeticRarity.Common, 0, "#F6EFE0", "#EDE4D0"),
            Item("tile_living", "Living Room", CosmeticCategory.TileTheme, CosmeticRarity.Common, 500, "#EEDDC6", "#DFC9AC"),
            Item("tile_bathroom", "Bathroom Tile", CosmeticCategory.TileTheme, CosmeticRarity.Uncommon, 1000, "#E1F1F4", "#CDE4E9", level: 100),
            Item("tile_arcade", "Arcade Floor", CosmeticCategory.TileTheme, CosmeticRarity.Rare, 3000, "#2F3558", "#454D7B", daily: 5),
            Item("tile_space", "Space Station", CosmeticCategory.TileTheme, CosmeticRarity.Epic, 8000, "#303B59", "#212B44", categoryComplete: LevelCategory.Hard),
            Item("tile_garden", "Garden Patio", CosmeticCategory.TileTheme, CosmeticRarity.Rare, 2250, "#DCE8CB", "#C8DAB2", bunnies: 30),
            Item("tile_garage", "Garage Floor", CosmeticCategory.TileTheme, CosmeticRarity.Uncommon, 1250, "#D7D5CE", "#C2C1BB", level: 200),
            Item("tile_candy", "Candy Room", CosmeticCategory.TileTheme, CosmeticRarity.Epic, 6000, "#FFE0EE", "#DCCBFF", stars: 500),

            Item(DefaultDock, "Classic Dock", CosmeticCategory.DockDesign, CosmeticRarity.Common, 0, "#FFFFFF"),
            Item("dock_gold", "Gold Dock", CosmeticCategory.DockDesign, CosmeticRarity.Rare, 3000, "#FFD875", stars: 200),
            Item("dock_neon", "Neon Dock", CosmeticCategory.DockDesign, CosmeticRarity.Rare, 3500, "#BC8BFF", daily: 7),
            Item("dock_wood", "Wooden Dock", CosmeticCategory.DockDesign, CosmeticRarity.Uncommon, 1000, "#C58C62", level: 100),
            Item("dock_space", "Space Dock", CosmeticCategory.DockDesign, CosmeticRarity.Epic, 7000, "#9FB6FF", categoryComplete: LevelCategory.Expert),
            Item("dock_catbed", "Cat Bed Dock", CosmeticCategory.DockDesign, CosmeticRarity.Epic, 5500, "#FFD3C4", catLevels: 50),

            Item(DefaultWinAnimation, "Sparkle Clean", CosmeticCategory.WinAnimation, CosmeticRarity.Common, 0, "#F7C657"),
            Item("win_confetti", "Confetti Burst", CosmeticCategory.WinAnimation, CosmeticRarity.Uncommon, 1000, "#F58B79", level: 75),
            Item("win_coins", "Coin Shower", CosmeticCategory.WinAnimation, CosmeticRarity.Rare, 2500, "#F4BE38", stars: 250),
            Item("win_dance", "DustBot Dance", CosmeticCategory.WinAnimation, CosmeticRarity.Epic, 6000, "#78D3B5", noHintDaily: 10),
            Item("win_fireworks", "Fireworks Pop", CosmeticCategory.WinAnimation, CosmeticRarity.Legendary, 12000, "#9E80FF", stars: 750),
            Item("win_bubbles", "Bubble Burst", CosmeticCategory.WinAnimation, CosmeticRarity.Rare, 3000, "#77DCEA", bunnies: 40),

            Item(DefaultRoomBackground, "Cozy Apartment", CosmeticCategory.RoomTheme, CosmeticRarity.Common, 0, "#F8F1DE", "#E5D6BE"),
            Item("room_kitchen", "Bright Kitchen", CosmeticCategory.RoomTheme, CosmeticRarity.Common, 500, "#FFF0B8", "#F7C657", level: 50),
            Item("room_candy", "Candy Room", CosmeticCategory.RoomTheme, CosmeticRarity.Uncommon, 900, "#FFE0EE", "#BFA7FF", stars: 100),
            Item("room_bathroom", "Bathroom", CosmeticCategory.RoomTheme, CosmeticRarity.Uncommon, 1100, "#D8F4FF", "#8FD6EA", level: 150),
            Item("room_garage", "Garage Workshop", CosmeticCategory.RoomTheme, CosmeticRarity.Rare, 2500, "#6F746F", "#C88B4A", categoryComplete: LevelCategory.Hard),
            Item("room_arcade", "Arcade Room", CosmeticCategory.RoomTheme, CosmeticRarity.Epic, 7000, "#242947", "#C45CFF", daily: 14),
            Item("room_space", "Space Station", CosmeticCategory.RoomTheme, CosmeticRarity.Legendary, 15000, "#171F38", "#6A7EFF", categoryComplete: LevelCategory.Expert, master: true),
            Item("room_cabin", "Cozy Cabin", CosmeticCategory.RoomTheme, CosmeticRarity.Rare, 3000, "#7A4E2D", "#F0D7B2", streak: 7),

            Bundle("bundle_gold", "Gold Bundle", CosmeticRarity.Legendary, 16000, new[] { "bot_gold", "path_gold", "dock_gold", "win_coins" }, stars: 500),
            Bundle("bundle_neon", "Neon Bundle", CosmeticRarity.Epic, 10000, new[] { "path_neon", "tile_arcade", "dock_neon" }, daily: 10),
            Bundle("bundle_space", "Space Bundle", CosmeticRarity.Legendary, 18000, new[] { "bot_space", "path_space", "tile_space", "dock_space", "room_space", "cat_space" }, level: 255, master: true),
            Bundle("bundle_cozy", "Cozy Bundle", CosmeticRarity.Rare, 4500, new[] { "tile_living", "dock_wood", "room_cabin" }, streak: 7),
            Bundle("bundle_cat", "Cat Bundle", CosmeticRarity.Epic, 8500, new[] { "bot_cat", "dock_catbed", "cat_tuxedo", "path_coral" }, bunnies: 75),
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
            bool master = false,
            LevelCategory categoryComplete = LevelCategory.None,
            int catLevels = 0,
            int perfectCleans = 0)
        {
            return new CosmeticDefinition
            {
                id = id,
                displayName = name,
                description = DescriptionFor(category),
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
                requiresMasterClean = master,
                categoryCompletionRequirement = categoryComplete,
                catLevelCompletionRequirement = catLevels,
                perfectCleanRequirement = perfectCleans,
                affectedAssetType = AssetTypeFor(category),
                previewSpriteKey = id,
                assetKey = id,
                animationKey = AnimationFor(category, id),
                particleEffectKey = category == CosmeticCategory.PathTrail ? id : string.Empty,
                materialEffectKey = id == "path_rainbow" || id == "bot_arcade" ? "animated" : string.Empty,
                visualStyle = id,
                isNew = (category == CosmeticCategory.CrumbStyle && id != DefaultCrumbStyle) ||
                        (category == CosmeticCategory.CatSkin && id != DefaultCatSkin)
            };
        }

        private static CosmeticAssetType AssetTypeFor(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.DustBotSkin: return CosmeticAssetType.DustBot;
                case CosmeticCategory.PathTrail: return CosmeticAssetType.Path;
                case CosmeticCategory.CrumbStyle: return CosmeticAssetType.Crumb;
                case CosmeticCategory.CatSkin: return CosmeticAssetType.Cat;
                case CosmeticCategory.DockDesign: return CosmeticAssetType.Dock;
                case CosmeticCategory.TileTheme: return CosmeticAssetType.Tile;
                case CosmeticCategory.RoomTheme: return CosmeticAssetType.Room;
                case CosmeticCategory.Bundle: return CosmeticAssetType.Multiple;
                default: return CosmeticAssetType.LegacyCelebration;
            }
        }

        private static string DescriptionFor(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.DustBotSkin: return "A distinct silhouette and personality for your cleaning hero.";
                case CosmeticCategory.PathTrail: return "A readable route with its own shape, texture, and motion.";
                case CosmeticCategory.CrumbStyle: return "Replaces crumbs with a new kind of object to clean.";
                case CosmeticCategory.CatSkin: return "Changes the chase pet while keeping the threat easy to read.";
                case CosmeticCategory.DockDesign: return "A different charging station for the end of every route.";
                case CosmeticCategory.TileTheme: return "A patterned floor treatment for the puzzle board.";
                case CosmeticCategory.RoomTheme: return "A complete room mood with its own backdrop pattern.";
                case CosmeticCategory.Bundle: return "A matching set that unlocks several cosmetic slots together.";
                default: return "A legacy celebration retained for existing save data.";
            }
        }

        private static string AnimationFor(CosmeticCategory category, string id)
        {
            if (category == CosmeticCategory.DustBotSkin)
                return id == DefaultBot ? "gentle_bob" : "character_idle";
            if (category == CosmeticCategory.CatSkin)
                return id == "cat_ghost" ? "float" : "pet_idle";
            if (category == CosmeticCategory.PathTrail)
                return id == "path_neon" || id == "path_rainbow" ? "pulse" : "flow";
            return string.Empty;
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
