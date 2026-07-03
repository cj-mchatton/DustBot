using System;
using System.Collections.Generic;
using System.Globalization;

namespace DustBot
{
    public sealed class ProgressionSystem
    {
        public PlayerProgressData Data { get; private set; }

        public ProgressionSystem(PlayerProgressData data)
        {
            Data = data ?? new PlayerProgressData();
            Sanitize();
        }

        public int GetStars(int levelNumber)
        {
            LevelProgressRecord record = FindRecord(LevelCategory.Easy, levelNumber);
            return record == null ? 0 : record.stars;
        }

        public int GetStars(LevelCategory category, int levelNumber)
        {
            LevelProgressRecord record = FindRecord(category, levelNumber);
            return record == null ? 0 : record.stars;
        }

        public bool HasDustBunny(int levelNumber)
        {
            LevelProgressRecord record = FindRecord(LevelCategory.Easy, levelNumber);
            return record != null && record.dustBunnyCollected;
        }

        public bool HasDustBunny(LevelCategory category, int levelNumber)
        {
            LevelProgressRecord record = FindRecord(category, levelNumber);
            return record != null && record.dustBunnyCollected;
        }

        public bool HasPerfectClean(LevelCategory category, int levelNumber)
        {
            LevelProgressRecord record = FindRecord(category, levelNumber);
            return record != null && record.perfectClean;
        }

        public int CompletedCount(LevelCategory category)
        {
            int count = 0;
            for (int i = 0; i < Data.categoryLevels.Count; i++)
                if (Data.categoryLevels[i].category == category && Data.categoryLevels[i].completed) count++;
            return count;
        }

        public int StarsInCategory(LevelCategory category)
        {
            int stars = 0;
            for (int i = 0; i < Data.categoryLevels.Count; i++)
                if (Data.categoryLevels[i].category == category) stars += Data.categoryLevels[i].stars;
            return stars;
        }

        public int TotalCompleted
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Data.categoryLevels.Count; i++)
                    if (Data.categoryLevels[i].completed) count++;
                return count;
            }
        }

        public int CatLevelsCompleted
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Data.categoryLevels.Count; i++)
                    if (Data.categoryLevels[i].completed && Data.categoryLevels[i].catLevel) count++;
                return count;
            }
        }

        public int PerfectCleanCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Data.categoryLevels.Count; i++)
                    if (Data.categoryLevels[i].perfectClean) count++;
                return count;
            }
        }

        public bool IsCategoryComplete(LevelCategory category)
        {
            return CompletedCount(category) >= LevelCategoryCatalog.Count(category);
        }

        public bool IsCategoryUnlocked(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.Easy:
                case LevelCategory.Medium: return true;
                case LevelCategory.Hard: return CompletedCount(LevelCategory.Medium) >= 10;
                case LevelCategory.Expert: return CompletedCount(LevelCategory.Hard) >= 20;
                case LevelCategory.CatChase: return CatLevelsCompleted >= 1;
                default: return false;
            }
        }

        public string CategoryLockReason(LevelCategory category)
        {
            switch (category)
            {
                case LevelCategory.Hard: return "Complete 10 Medium levels to unlock Hard.";
                case LevelCategory.Expert: return "Complete 20 Hard levels to unlock Expert.";
                case LevelCategory.CatChase: return "Complete your first cat level in Medium to unlock Cat Chase mode.";
                default: return string.Empty;
            }
        }

        public int NextUnfinishedLevel(LevelCategory category)
        {
            int count = LevelCategoryCatalog.Count(category);
            for (int level = 1; level <= count; level++)
            {
                LevelProgressRecord record = FindRecord(category, level);
                if (record == null || !record.completed) return level;
            }
            return count;
        }

        public bool IsLevelUnlocked(LevelCategory category, int levelNumber)
        {
            if (!IsCategoryUnlocked(category)) return false;
            if (levelNumber <= 1) return true;
            LevelProgressRecord previous = FindRecord(category, levelNumber - 1);
            return previous != null && previous.completed;
        }

        public bool IsMainJourneyComplete()
        {
            return IsCategoryComplete(LevelCategory.Expert);
        }

        public int ApplyResult(LevelResult result, DateTime today)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            ResetRewardBreakdown(result);
            int awardedCoins;
            if (result.generationMode != GenerationMode.ProductionCampaign)
            {
                awardedCoins = result.dailyChallengeStyle
                    ? ApplyDevelopmentDailyStyleResult(result)
                    : ApplyRepeatableResult(result);
            }
            else switch (result.mode)
            {
                case GameMode.MainJourney:
                    awardedCoins = ApplyMainResult(result);
                    break;
                case GameMode.DailyChallenge:
                    awardedCoins = ApplyDailyResult(result, today.Date);
                    break;
                case GameMode.MasterClean:
                    Data.masterCleanProgress = Math.Max(Data.masterCleanProgress, result.levelNumber + 1);
                    awardedCoins = ApplyRepeatableResult(result);
                    break;
                case GameMode.EndlessClean:
                    Data.endlessBestScore = Math.Max(Data.endlessBestScore, result.levelNumber);
                    Data.endlessCurrentLevel = Math.Max(Data.endlessCurrentLevel, result.levelNumber + 1);
                    awardedCoins = ApplyRepeatableResult(result);
                    break;
                default:
                    awardedCoins = 0;
                    break;
            }

            Data.roomsCleaned++;
            Data.dustCoins += awardedCoins;
            result.coinsEarned = awardedCoins;
            return awardedCoins;
        }

        public int RegisterDailyAttempt(DateTime today)
        {
            string todayKey = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            EnsureDailyDate(todayKey);
            if (Data.daily.lastAttemptDate != todayKey)
            {
                Data.daily.lastAttemptDate = todayKey;
                Data.daily.attemptCount = 0;
            }

            Data.daily.attemptCount++;
            return Data.daily.attemptCount;
        }

        public void Reset()
        {
            Data = new PlayerProgressData();
        }

        private int ApplyMainResult(LevelResult result)
        {
            if (result.category == LevelCategory.None) return ApplyRepeatableResult(result);
            result.levelNumber = LevelCategoryCatalog.ClampLevel(result.category, result.levelNumber);
            result.stars = Math.Max(1, Math.Min(3, result.stars));
            LevelProgressRecord record = FindRecord(result.category, result.levelNumber);

            if (record == null)
            {
                record = new LevelProgressRecord { category = result.category, levelNumber = result.levelNumber };
                Data.categoryLevels.Add(record);
            }

            bool wasCompleted = record.completed;
            int previousStars = record.stars;
            bool hadBunny = record.dustBunnyCollected;
            record.completed = true;
            record.catLevel |= result.catLevel;
            record.stars = Math.Max(record.stars, result.stars);
            record.bestScore = Math.Max(record.bestScore, Math.Max(0, result.stars * 1000 - result.moves));
            record.dustBunnyCollected |= result.collectedBonus;
            record.perfectClean |= result.stars >= 3 && !result.usedHint &&
                                   (!result.bonusAvailable || result.collectedBonus);
            if (result.usedHint)
            {
                record.hintsUsed++;
            }

            Data.totalStars += record.stars - previousStars;
            Data.highestUnlockedMainLevel = Math.Max(
                Data.highestUnlockedMainLevel,
                Math.Min(LevelCategoryCatalog.TotalLevelCount, TotalCompleted + 1));
            Data.lastPlayedCategory = result.category;
            Data.lastPlayedLevel = NextUnfinishedLevel(result.category);

            if (!wasCompleted)
            {
                result.baseCoins = EconomyConfig.BaseLevelCompletionCoins;
            }

            result.starBonusCoins =
                EconomyConfig.StarBonusFor(record.stars, false) -
                EconomyConfig.StarBonusFor(previousStars, false);

            if (record.dustBunnyCollected && !hadBunny && ClaimDustBunny(result.levelId))
            {
                result.bunnyBonusCoins = EconomyConfig.DustBunnyBonusCoins;
            }

            if (!record.milestoneRewardClaimed &&
                !wasCompleted &&
                result.levelNumber % EconomyConfig.MilestoneFrequency == 0)
            {
                record.milestoneRewardClaimed = true;
                result.milestoneBonusCoins = EconomyConfig.MilestoneRewardCoins;
            }

            return RewardTotal(result);
        }

        private int ApplyDailyResult(LevelResult result, DateTime today)
        {
            string todayKey = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            EnsureDailyDate(todayKey);
            int previousStars = Data.daily.bestStars;
            bool firstCompletionToday = !Data.daily.baseRewardClaimed;

            if (firstCompletionToday)
            {
                AdvanceDailyStreak(today, todayKey);
                Data.daily.baseRewardClaimed = true;
                Data.daily.totalCompleted++;
                result.baseCoins = EconomyConfig.DailyBaseCompletionCoins;
            }

            Data.daily.bestStars = Math.Max(Data.daily.bestStars, Math.Max(1, Math.Min(3, result.stars)));
            result.starBonusCoins =
                EconomyConfig.StarBonusFor(Data.daily.bestStars, true) -
                EconomyConfig.StarBonusFor(previousStars, true);

            if (result.collectedBonus &&
                !Data.daily.dustBunnyCollected &&
                ClaimDustBunny(result.levelId))
            {
                Data.daily.dustBunnyCollected = true;
                result.bunnyBonusCoins = EconomyConfig.DailyDustBunnyBonusCoins;
            }

            if (!result.usedHint && !Data.daily.noHintBonusClaimed)
            {
                Data.daily.noHintBonusClaimed = true;
                Data.daily.noHintCompletions++;
                result.noHintBonusCoins = EconomyConfig.DailyNoHintBonusCoins;
            }

            if (result.firstAttempt && !Data.daily.firstAttemptBonusClaimed)
            {
                Data.daily.firstAttemptBonusClaimed = true;
                result.firstAttemptBonusCoins = EconomyConfig.DailyFirstAttemptBonusCoins;
            }

            if (firstCompletionToday && !Data.daily.streakRewardClaimed)
            {
                Data.daily.streakRewardClaimed = true;
                result.streakBonusCoins = EconomyConfig.DailyStreakReward(Data.daily.currentStreak);
            }

            result.dailyStreak = Data.daily.currentStreak;
            return RewardTotal(result);
        }

        private int ApplyRepeatableResult(LevelResult result)
        {
            if (!string.IsNullOrEmpty(result.levelId) &&
                !Data.rewardedCompletionLevelIds.Contains(result.levelId))
            {
                Data.rewardedCompletionLevelIds.Add(result.levelId);
                result.baseCoins = EconomyConfig.BaseLevelCompletionCoins;
                result.starBonusCoins = EconomyConfig.StarBonusFor(result.stars, false);
            }

            if (result.collectedBonus && ClaimDustBunny(result.levelId))
            {
                result.bunnyBonusCoins = EconomyConfig.DustBunnyBonusCoins;
            }

            return RewardTotal(result);
        }

        private int ApplyDevelopmentDailyStyleResult(LevelResult result)
        {
            bool firstReward = !string.IsNullOrEmpty(result.levelId) &&
                               !Data.rewardedCompletionLevelIds.Contains(result.levelId);
            if (firstReward)
            {
                Data.rewardedCompletionLevelIds.Add(result.levelId);
                result.baseCoins = EconomyConfig.DailyBaseCompletionCoins;
                result.starBonusCoins = EconomyConfig.StarBonusFor(result.stars, true);
                if (!result.usedHint)
                {
                    result.noHintBonusCoins = EconomyConfig.DailyNoHintBonusCoins;
                }
                if (result.firstAttempt)
                {
                    result.firstAttemptBonusCoins = EconomyConfig.DailyFirstAttemptBonusCoins;
                }
                result.dailyStreak = 3;
                result.streakBonusCoins = EconomyConfig.DailyStreakReward(result.dailyStreak);
            }

            if (result.collectedBonus && ClaimDustBunny(result.levelId))
            {
                result.bunnyBonusCoins = EconomyConfig.DailyDustBunnyBonusCoins;
            }

            return RewardTotal(result);
        }

        private void AdvanceDailyStreak(DateTime today, string todayKey)
        {
            DateTime lastDate;
            if (DateTime.TryParseExact(
                    Data.daily.lastCompletedDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out lastDate) &&
                lastDate.Date == today.AddDays(-1))
            {
                Data.daily.currentStreak++;
            }
            else
            {
                Data.daily.currentStreak = 1;
            }

            Data.daily.lastCompletedDate = todayKey;
            Data.daily.bestStreak = Math.Max(Data.daily.bestStreak, Data.daily.currentStreak);
        }

        private void EnsureDailyDate(string todayKey)
        {
            if (Data.daily.activeDate == todayKey)
            {
                return;
            }

            Data.daily.activeDate = todayKey;
            Data.daily.bestStars = 0;
            Data.daily.dustBunnyCollected = false;
            Data.daily.baseRewardClaimed = false;
            Data.daily.noHintBonusClaimed = false;
            Data.daily.firstAttemptBonusClaimed = false;
            Data.daily.streakRewardClaimed = false;
            Data.daily.lastAttemptDate = todayKey;
            Data.daily.attemptCount = 0;
        }

        private bool ClaimDustBunny(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) ||
                Data.collectedDustBunnyLevelIds.Contains(levelId))
            {
                return false;
            }

            Data.collectedDustBunnyLevelIds.Add(levelId);
            Data.totalDustBunnies++;
            return true;
        }

        private LevelProgressRecord FindRecord(LevelCategory category, int levelNumber)
        {
            for (int i = 0; i < Data.categoryLevels.Count; i++)
            {
                if (Data.categoryLevels[i].category == category &&
                    Data.categoryLevels[i].levelNumber == levelNumber)
                {
                    return Data.categoryLevels[i];
                }
            }

            return null;
        }

        private static void ResetRewardBreakdown(LevelResult result)
        {
            result.baseCoins = 0;
            result.starBonusCoins = 0;
            result.bunnyBonusCoins = 0;
            result.noHintBonusCoins = 0;
            result.firstAttemptBonusCoins = 0;
            result.streakBonusCoins = 0;
            result.milestoneBonusCoins = 0;
            result.cosmeticUnlocked = string.Empty;
        }

        private static int RewardTotal(LevelResult result)
        {
            return Math.Max(
                0,
                result.baseCoins +
                result.starBonusCoins +
                result.bunnyBonusCoins +
                result.noHintBonusCoins +
                result.firstAttemptBonusCoins +
                result.streakBonusCoins +
                result.milestoneBonusCoins);
        }

        private void Sanitize()
        {
            if (Data.mainLevels == null)
            {
                Data.mainLevels = new List<LevelProgressRecord>();
            }

            if (Data.categoryLevels == null || Data.saveVersion < 5)
            {
                Data.categoryLevels = new List<LevelProgressRecord>();
            }

            if (Data.daily == null)
            {
                Data.daily = new DailyChallengeData();
            }

            if (Data.settings == null)
            {
                Data.settings = new PlayerSettingsData();
            }

            Data.settings.soundVolume = Clamp01WithDefault(Data.settings.soundVolume, 1f);
            Data.settings.musicVolume = Clamp01WithDefault(Data.settings.musicVolume, 0.8f);

            if (Data.cosmetics == null)
            {
                Data.cosmetics = new CosmeticInventoryData();
            }

            if (Data.cosmetics.ownedCosmeticIds == null)
            {
                Data.cosmetics.ownedCosmeticIds = new List<string>();
            }

            if (!Data.cosmetics.ownedCosmeticIds.Contains(CosmeticCatalog.DefaultBot))
            {
                Data.cosmetics.ownedCosmeticIds.Add(CosmeticCatalog.DefaultBot);
            }

            if (!Data.cosmetics.ownedCosmeticIds.Contains(CosmeticCatalog.DefaultPath))
            {
                Data.cosmetics.ownedCosmeticIds.Add(CosmeticCatalog.DefaultPath);
            }

            EnsureDefaultOwned(CosmeticCatalog.DefaultTileTheme);
            EnsureDefaultOwned(CosmeticCatalog.DefaultDock);
            EnsureDefaultOwned(CosmeticCatalog.DefaultWinAnimation);
            EnsureDefaultOwned(CosmeticCatalog.DefaultRoomBackground);

            Data.cosmetics.activeBotSkinId = SanitizeActive(
                Data.cosmetics.activeBotSkinId,
                CosmeticCategory.BotSkin,
                CosmeticCatalog.DefaultBot);
            Data.cosmetics.activePathColorId = SanitizeActive(
                Data.cosmetics.activePathColorId,
                CosmeticCategory.PathColor,
                CosmeticCatalog.DefaultPath);
            Data.cosmetics.activeTileThemeId = SanitizeActive(
                Data.cosmetics.activeTileThemeId,
                CosmeticCategory.TileTheme,
                CosmeticCatalog.DefaultTileTheme);
            Data.cosmetics.activeDockDesignId = SanitizeActive(
                Data.cosmetics.activeDockDesignId,
                CosmeticCategory.DockDesign,
                CosmeticCatalog.DefaultDock);
            Data.cosmetics.activeWinAnimationId = SanitizeActive(
                Data.cosmetics.activeWinAnimationId,
                CosmeticCategory.WinAnimation,
                CosmeticCatalog.DefaultWinAnimation);
            Data.cosmetics.activeFailureAnimationId = CosmeticCatalog.DefaultFailureAnimation;
            Data.cosmetics.activeRoomBackgroundId = SanitizeActive(
                Data.cosmetics.activeRoomBackgroundId,
                CosmeticCategory.RoomBackground,
                CosmeticCatalog.DefaultRoomBackground);

            if (Data.cosmetics.purchaseHistory == null)
            {
                Data.cosmetics.purchaseHistory = new List<string>();
            }

            RemoveRetiredFailureCosmetics(Data.cosmetics.ownedCosmeticIds);
            RemoveRetiredFailureCosmetics(Data.cosmetics.purchaseHistory);

            if (Data.collectedDustBunnyLevelIds == null)
            {
                Data.collectedDustBunnyLevelIds = new List<string>();
            }

            if (Data.rewardedCompletionLevelIds == null)
            {
                Data.rewardedCompletionLevelIds = new List<string>();
            }

            SortedDictionary<string, LevelProgressRecord> records =
                new SortedDictionary<string, LevelProgressRecord>();
            for (int i = 0; i < Data.categoryLevels.Count; i++)
            {
                LevelProgressRecord source = Data.categoryLevels[i];
                if (source == null ||
                    source.category == LevelCategory.None ||
                    !Enum.IsDefined(typeof(LevelCategory), source.category) ||
                    source.levelNumber < 1 ||
                    source.levelNumber > LevelCategoryCatalog.Count(source.category))
                {
                    continue;
                }

                string key = ((int)source.category).ToString("D2", CultureInfo.InvariantCulture) +
                             "_" + source.levelNumber.ToString("D3", CultureInfo.InvariantCulture);
                LevelProgressRecord record;
                if (!records.TryGetValue(key, out record))
                {
                    record = new LevelProgressRecord { category = source.category, levelNumber = source.levelNumber };
                    records.Add(key, record);
                }

                record.completed |= source.completed;
                record.stars = Math.Max(record.stars, Math.Max(0, Math.Min(3, source.stars)));
                record.bestScore = Math.Max(record.bestScore, Math.Max(0, source.bestScore));
                record.catLevel |= source.catLevel;
                record.dustBunnyCollected |= source.dustBunnyCollected;
                record.hintsUsed = Math.Max(record.hintsUsed, Math.Max(0, source.hintsUsed));
                record.perfectClean |= source.perfectClean;
                record.milestoneRewardClaimed |= source.milestoneRewardClaimed;
            }

            Data.categoryLevels = new List<LevelProgressRecord>(records.Values);
            Data.mainLevels.Clear();
            Data.totalStars = 0;
            for (int i = 0; i < Data.categoryLevels.Count; i++)
            {
                LevelProgressRecord record = Data.categoryLevels[i];
                Data.totalStars += record.stars;
            }

            Data.highestUnlockedMainLevel = Math.Min(LevelCategoryCatalog.TotalLevelCount, TotalCompleted + 1);
            if (!Enum.IsDefined(typeof(LevelCategory), Data.lastPlayedCategory) ||
                Data.lastPlayedCategory == LevelCategory.None || !IsCategoryUnlocked(Data.lastPlayedCategory))
                Data.lastPlayedCategory = LevelCategory.Easy;
            Data.lastPlayedLevel = LevelCategoryCatalog.ClampLevel(Data.lastPlayedCategory, Data.lastPlayedLevel);
            Data.masterCleanProgress = Math.Max(1, Data.masterCleanProgress);
            Data.endlessBestScore = Math.Max(0, Data.endlessBestScore);
            Data.endlessCurrentLevel = Math.Max(1, Data.endlessCurrentLevel);
            if (string.IsNullOrEmpty(Data.endlessRunSeed))
            {
                Data.endlessRunSeed = "CozyRun";
            }

            Data.roomsCleaned = Math.Max(0, Data.roomsCleaned);
            Data.totalDustBunnies = Math.Max(
                Math.Max(0, Data.totalDustBunnies),
                Data.collectedDustBunnyLevelIds.Count);
            Data.dustCoins = Math.Max(0, Data.dustCoins);
            Data.daily.currentStreak = Math.Max(0, Data.daily.currentStreak);
            Data.daily.bestStreak = Math.Max(Data.daily.currentStreak, Data.daily.bestStreak);
            Data.daily.bestStars = Math.Max(0, Math.Min(3, Data.daily.bestStars));
            Data.daily.attemptCount = Math.Max(0, Data.daily.attemptCount);
            Data.daily.totalCompleted = Math.Max(0, Data.daily.totalCompleted);
            if (!string.IsNullOrEmpty(Data.daily.lastCompletedDate))
            {
                Data.daily.totalCompleted = Math.Max(1, Data.daily.totalCompleted);
            }
            Data.daily.noHintCompletions = Math.Max(0, Data.daily.noHintCompletions);
            Data.saveVersion = 5;
        }

        public void ResetCategory(LevelCategory category)
        {
            for (int i = Data.categoryLevels.Count - 1; i >= 0; i--)
                if (Data.categoryLevels[i].category == category) Data.categoryLevels.RemoveAt(i);
            Sanitize();
        }

        public void CompleteCategoryForDebug(LevelCategory category)
        {
            int count = LevelCategoryCatalog.Count(category);
            for (int level = 1; level <= count; level++)
            {
                LevelProgressRecord record = FindRecord(category, level);
                if (record == null)
                {
                    record = new LevelProgressRecord { category = category, levelNumber = level };
                    Data.categoryLevels.Add(record);
                }
                record.completed = true;
                record.stars = Math.Max(1, record.stars);
                record.catLevel |= LevelCategoryCatalog.IsCatLevel(category, level);
            }
            Sanitize();
        }

        private void EnsureDefaultOwned(string cosmeticId)
        {
            if (!Data.cosmetics.ownedCosmeticIds.Contains(cosmeticId))
            {
                Data.cosmetics.ownedCosmeticIds.Add(cosmeticId);
            }
        }

        private static void RemoveRetiredFailureCosmetics(List<string> cosmeticIds)
        {
            if (cosmeticIds == null)
            {
                return;
            }

            for (int i = cosmeticIds.Count - 1; i >= 0; i--)
            {
                string cosmeticId = cosmeticIds[i];
                if (!string.IsNullOrEmpty(cosmeticId) &&
                    cosmeticId.StartsWith("fail_", StringComparison.Ordinal))
                {
                    cosmeticIds.RemoveAt(i);
                }
            }
        }

        private static float Clamp01WithDefault(float value, float fallback)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return fallback;
            }

            return Math.Max(0.25f, Math.Min(1f, value));
        }

        private string SanitizeActive(
            string cosmeticId,
            CosmeticCategory category,
            string fallback)
        {
            CosmeticDefinition definition = CosmeticCatalog.Find(cosmeticId);
            if (definition == null ||
                definition.category != category ||
                !Data.cosmetics.ownedCosmeticIds.Contains(definition.id))
            {
                return fallback;
            }

            return definition.id;
        }
    }
}
