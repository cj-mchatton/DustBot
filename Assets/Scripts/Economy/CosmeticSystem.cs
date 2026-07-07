using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustBot
{
    public sealed class CosmeticSystem
    {
        private readonly ProgressionSystem progression;
        private readonly EconomySystem economy;

        public CosmeticSystem(ProgressionSystem progression, EconomySystem economy)
        {
            this.progression = progression;
            this.economy = economy;
            GrantMilestoneCosmetics();
        }

        public bool Owns(string cosmeticId)
        {
            return progression.Data.cosmetics.ownedCosmeticIds.Contains(cosmeticId);
        }

        public bool TryUnlockOrSelect(string cosmeticId)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(cosmeticId);
            if (definition == null)
            {
                return false;
            }

            if (!Owns(cosmeticId))
            {
                if (!RequirementsMet(definition))
                {
                    return false;
                }

                if (definition.coinPrice > 0 && !economy.TrySpend(definition.coinPrice))
                {
                    return false;
                }

                progression.Data.cosmetics.ownedCosmeticIds.Add(cosmeticId);
                progression.Data.cosmetics.purchaseHistory.Add(cosmeticId);
                if (definition.category == CosmeticCategory.Bundle &&
                    definition.bundleItemIds != null)
                {
                    for (int i = 0; i < definition.bundleItemIds.Length; i++)
                    {
                        if (!Owns(definition.bundleItemIds[i]))
                        {
                            progression.Data.cosmetics.ownedCosmeticIds.Add(definition.bundleItemIds[i]);
                        }
                    }
                }
            }

            if (definition.category == CosmeticCategory.Bundle)
            {
                EquipBundle(definition);
            }
            else
            {
                Select(definition);
            }
            return true;
        }

        public string Status(CosmeticDefinition definition)
        {
            if (IsActive(definition))
            {
                return "SELECTED";
            }

            if (Owns(definition.id))
            {
                return "OWNED";
            }

            string lockReason = LockReason(definition);
            if (!string.IsNullOrEmpty(lockReason))
            {
                return "LOCKED";
            }

            return definition.coinPrice + " COINS";
        }

        public string LockReason(CosmeticDefinition definition)
        {
            if (definition.levelRequirement > 0 &&
                progression.TotalCompleted < definition.levelRequirement)
            {
                return "Complete " + definition.levelRequirement + " main levels";
            }

            if (definition.categoryCompletionRequirement != LevelCategory.None &&
                !progression.IsCategoryComplete(definition.categoryCompletionRequirement))
                return "Complete " + LevelCategoryCatalog.Name(definition.categoryCompletionRequirement);

            if (definition.catLevelCompletionRequirement > progression.CatLevelsCompleted)
                return "Complete " + definition.catLevelCompletionRequirement + " cat levels";

            if (definition.perfectCleanRequirement > progression.PerfectCleanCount)
                return "Earn " + definition.perfectCleanRequirement + " Perfect Cleans";

            if (definition.starRequirement > progression.Data.totalStars)
            {
                return "Earn " + definition.starRequirement + " stars";
            }

            if (definition.bunnyRequirement > progression.Data.totalDustBunnies)
            {
                return "Collect " + definition.bunnyRequirement + " Dust Bunnies";
            }

            if (definition.dailyCompletionRequirement > progression.Data.daily.totalCompleted)
            {
                return "Complete " + definition.dailyCompletionRequirement + " Daily Challenges";
            }

            if (definition.noHintDailyRequirement > progression.Data.daily.noHintCompletions)
            {
                return "Complete " + definition.noHintDailyRequirement + " Dailies without hints";
            }

            if (definition.streakRequirement > progression.Data.daily.bestStreak)
            {
                return "Reach a " + definition.streakRequirement + "-day streak";
            }

            if (definition.requiresMasterClean && !progression.IsMainJourneyComplete())
            {
                return "Complete Expert to reach Master Clean";
            }

            return string.Empty;
        }

        public bool RequirementsMet(CosmeticDefinition definition)
        {
            return string.IsNullOrEmpty(LockReason(definition));
        }

        public Color ActiveBotTint
        {
            get { return ParseColor(progression.Data.cosmetics.activeBotSkinId, Color.white); }
        }

        public Color ActivePathColor
        {
            get { return ParseColor(progression.Data.cosmetics.activePathColorId, DustBotTheme.MintDark); }
        }

        public Color ActiveTileA
        {
            get { return ParsePrimary(progression.Data.cosmetics.activeTileThemeId, DustBotTheme.TileA); }
        }

        public Color ActiveTileB
        {
            get { return ParseSecondary(progression.Data.cosmetics.activeTileThemeId, DustBotTheme.TileB); }
        }

        public Color ActiveDockTint
        {
            get { return ParsePrimary(progression.Data.cosmetics.activeDockDesignId, Color.white); }
        }

        public Color ActiveRoomBackground
        {
            get { return ParsePrimary(progression.Data.cosmetics.activeRoomBackgroundId, DustBotTheme.Background); }
        }

        public string ActiveWinAnimationId
        {
            get { return progression.Data.cosmetics.activeWinAnimationId; }
        }

        public string ActiveFailureAnimationId
        {
            get
            {
                return string.IsNullOrEmpty(progression.Data.cosmetics.activeFailureAnimationId)
                    ? CosmeticCatalog.DefaultFailureAnimation
                    : progression.Data.cosmetics.activeFailureAnimationId;
            }
        }

        public string ActiveBotSkinId
        {
            get { return progression.Data.cosmetics.activeBotSkinId; }
        }

        public string ActivePathTrailId
        {
            get { return progression.Data.cosmetics.activePathColorId; }
        }

        public string ActiveCrumbStyleId
        {
            get { return progression.Data.cosmetics.activeCrumbStyleId; }
        }

        public string ActiveCatSkinId
        {
            get { return progression.Data.cosmetics.activeCatSkinId; }
        }

        public string ActiveDockDesignId
        {
            get { return progression.Data.cosmetics.activeDockDesignId; }
        }

        public string ActiveTileThemeId
        {
            get { return progression.Data.cosmetics.activeTileThemeId; }
        }

        public string ActiveRoomThemeId
        {
            get { return progression.Data.cosmetics.activeRoomBackgroundId; }
        }

        public string GrantMilestoneCosmetics()
        {
            IReadOnlyList<CosmeticDefinition> all = CosmeticCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                CosmeticDefinition definition = all[i];
                if (definition.coinPrice == 0 &&
                    definition.category != CosmeticCategory.Bundle &&
                    !Owns(definition.id) &&
                    RequirementsMet(definition))
                {
                    progression.Data.cosmetics.ownedCosmeticIds.Add(definition.id);
                    return definition.displayName;
                }
            }

            return string.Empty;
        }

        private bool IsActive(CosmeticDefinition definition)
        {
            switch (definition.category)
            {
                case CosmeticCategory.DustBotSkin:
                    return progression.Data.cosmetics.activeBotSkinId == definition.id;
                case CosmeticCategory.PathTrail:
                    return progression.Data.cosmetics.activePathColorId == definition.id;
                case CosmeticCategory.CrumbStyle:
                    return progression.Data.cosmetics.activeCrumbStyleId == definition.id;
                case CosmeticCategory.CatSkin:
                    return progression.Data.cosmetics.activeCatSkinId == definition.id;
                case CosmeticCategory.TileTheme:
                    return progression.Data.cosmetics.activeTileThemeId == definition.id;
                case CosmeticCategory.DockDesign:
                    return progression.Data.cosmetics.activeDockDesignId == definition.id;
                case CosmeticCategory.WinAnimation:
                    return progression.Data.cosmetics.activeWinAnimationId == definition.id;
                case CosmeticCategory.FailureAnimation:
                    return progression.Data.cosmetics.activeFailureAnimationId == definition.id;
                case CosmeticCategory.RoomTheme:
                    return progression.Data.cosmetics.activeRoomBackgroundId == definition.id;
                default:
                    return false;
            }
        }

        private void Select(CosmeticDefinition definition)
        {
            switch (definition.category)
            {
                case CosmeticCategory.DustBotSkin:
                    progression.Data.cosmetics.activeBotSkinId = definition.id;
                    break;
                case CosmeticCategory.PathTrail:
                    progression.Data.cosmetics.activePathColorId = definition.id;
                    break;
                case CosmeticCategory.CrumbStyle:
                    progression.Data.cosmetics.activeCrumbStyleId = definition.id;
                    break;
                case CosmeticCategory.CatSkin:
                    progression.Data.cosmetics.activeCatSkinId = definition.id;
                    break;
                case CosmeticCategory.TileTheme:
                    progression.Data.cosmetics.activeTileThemeId = definition.id;
                    break;
                case CosmeticCategory.DockDesign:
                    progression.Data.cosmetics.activeDockDesignId = definition.id;
                    break;
                case CosmeticCategory.WinAnimation:
                    progression.Data.cosmetics.activeWinAnimationId = definition.id;
                    break;
                case CosmeticCategory.FailureAnimation:
                    progression.Data.cosmetics.activeFailureAnimationId = definition.id;
                    break;
                case CosmeticCategory.RoomTheme:
                    progression.Data.cosmetics.activeRoomBackgroundId = definition.id;
                    break;
            }
        }

        private void EquipBundle(CosmeticDefinition bundle)
        {
            if (bundle.bundleItemIds == null)
            {
                return;
            }

            for (int i = 0; i < bundle.bundleItemIds.Length; i++)
            {
                CosmeticDefinition item = CosmeticCatalog.Find(bundle.bundleItemIds[i]);
                if (item != null && Owns(item.id))
                {
                    Select(item);
                }
            }
        }

        private static Color ParseColor(string cosmeticId, Color fallback)
        {
            return ParsePrimary(cosmeticId, fallback);
        }

        private static Color ParsePrimary(string cosmeticId, Color fallback)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(cosmeticId);
            Color color;
            return definition != null &&
                   ColorUtility.TryParseHtmlString(definition.colorHex, out color)
                ? color
                : fallback;
        }

        private static Color ParseSecondary(string cosmeticId, Color fallback)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(cosmeticId);
            Color color;
            return definition != null &&
                   !string.IsNullOrEmpty(definition.secondaryColorHex) &&
                   ColorUtility.TryParseHtmlString(definition.secondaryColorHex, out color)
                ? color
                : fallback;
        }
    }
}
