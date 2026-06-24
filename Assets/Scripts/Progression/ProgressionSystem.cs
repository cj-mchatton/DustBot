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
            LevelProgressRecord record = FindMainRecord(levelNumber);
            return record == null ? 0 : record.stars;
        }

        public bool HasDustBunny(int levelNumber)
        {
            LevelProgressRecord record = FindMainRecord(levelNumber);
            return record != null && record.dustBunnyCollected;
        }

        public bool IsMainJourneyComplete()
        {
            return GetStars(LevelManifest.MainJourneyLevelCount) > 0;
        }

        public int ApplyResult(LevelResult result, DateTime today)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            ResetRewardBreakdown(result);
            int awardedCoins;
            switch (result.mode)
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
            result.levelNumber = Math.Max(1, Math.Min(LevelManifest.MainJourneyLevelCount, result.levelNumber));
            result.stars = Math.Max(1, Math.Min(3, result.stars));
            LevelProgressRecord record = FindMainRecord(result.levelNumber);

            if (record == null)
            {
                record = new LevelProgressRecord { levelNumber = result.levelNumber };
                Data.mainLevels.Add(record);
            }

            bool wasCompleted = record.completed;
            int previousStars = record.stars;
            bool hadBunny = record.dustBunnyCollected;
            record.completed = true;
            record.stars = Math.Max(record.stars, result.stars);
            record.dustBunnyCollected |= result.collectedBonus;
            if (result.usedHint)
            {
                record.hintsUsed++;
            }

            Data.totalStars += record.stars - previousStars;
            Data.highestUnlockedMainLevel = Math.Max(
                Data.highestUnlockedMainLevel,
                Math.Min(LevelManifest.MainJourneyLevelCount, result.levelNumber + 1));

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

        private LevelProgressRecord FindMainRecord(int levelNumber)
        {
            for (int i = 0; i < Data.mainLevels.Count; i++)
            {
                if (Data.mainLevels[i].levelNumber == levelNumber)
                {
                    return Data.mainLevels[i];
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

            if (Data.daily == null)
            {
                Data.daily = new DailyChallengeData();
            }

            if (Data.settings == null)
            {
                Data.settings = new PlayerSettingsData();
            }

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
            EnsureDefaultOwned(CosmeticCatalog.DefaultFailureAnimation);
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
            Data.cosmetics.activeFailureAnimationId = SanitizeActive(
                Data.cosmetics.activeFailureAnimationId,
                CosmeticCategory.FailureAnimation,
                CosmeticCatalog.DefaultFailureAnimation);
            Data.cosmetics.activeRoomBackgroundId = SanitizeActive(
                Data.cosmetics.activeRoomBackgroundId,
                CosmeticCategory.RoomBackground,
                CosmeticCatalog.DefaultRoomBackground);

            if (Data.cosmetics.purchaseHistory == null)
            {
                Data.cosmetics.purchaseHistory = new List<string>();
            }

            if (Data.collectedDustBunnyLevelIds == null)
            {
                Data.collectedDustBunnyLevelIds = new List<string>();
            }

            if (Data.rewardedCompletionLevelIds == null)
            {
                Data.rewardedCompletionLevelIds = new List<string>();
            }

            SortedDictionary<int, LevelProgressRecord> records =
                new SortedDictionary<int, LevelProgressRecord>();
            for (int i = 0; i < Data.mainLevels.Count; i++)
            {
                LevelProgressRecord source = Data.mainLevels[i];
                if (source == null ||
                    source.levelNumber < 1 ||
                    source.levelNumber > LevelManifest.MainJourneyLevelCount)
                {
                    continue;
                }

                LevelProgressRecord record;
                if (!records.TryGetValue(source.levelNumber, out record))
                {
                    record = new LevelProgressRecord { levelNumber = source.levelNumber };
                    records.Add(source.levelNumber, record);
                }

                record.completed |= source.completed;
                record.stars = Math.Max(record.stars, Math.Max(0, Math.Min(3, source.stars)));
                record.dustBunnyCollected |= source.dustBunnyCollected;
                record.hintsUsed = Math.Max(record.hintsUsed, Math.Max(0, source.hintsUsed));
                record.milestoneRewardClaimed |= source.milestoneRewardClaimed;
            }

            Data.mainLevels = new List<LevelProgressRecord>(records.Values);
            Data.totalStars = 0;
            int unlockedFromRecords = 1;
            for (int i = 0; i < Data.mainLevels.Count; i++)
            {
                LevelProgressRecord record = Data.mainLevels[i];
                Data.totalStars += record.stars;
                if (record.completed)
                {
                    unlockedFromRecords = Math.Max(
                        unlockedFromRecords,
                        Math.Min(LevelManifest.MainJourneyLevelCount, record.levelNumber + 1));
                }
            }

            Data.highestUnlockedMainLevel = Math.Max(
                unlockedFromRecords,
                Math.Max(1, Math.Min(LevelManifest.MainJourneyLevelCount, Data.highestUnlockedMainLevel)));
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
            Data.saveVersion = Math.Max(4, Data.saveVersion);
        }

        private void EnsureDefaultOwned(string cosmeticId)
        {
            if (!Data.cosmetics.ownedCosmeticIds.Contains(cosmeticId))
            {
                Data.cosmetics.ownedCosmeticIds.Add(cosmeticId);
            }
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
