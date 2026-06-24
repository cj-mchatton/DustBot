using System;
using System.Collections.Generic;

namespace DustBot
{
    [Serializable]
    public class LevelProgressRecord
    {
        public int levelNumber;
        public int stars;
        public bool completed;
        public bool dustBunnyCollected;
        public int hintsUsed;
        public bool milestoneRewardClaimed;
    }

    [Serializable]
    public class DailyChallengeData
    {
        public string lastCompletedDate = string.Empty;
        public int currentStreak;
        public int bestStreak;
        public string activeDate = string.Empty;
        public int bestStars;
        public bool dustBunnyCollected;
        public bool baseRewardClaimed;
        public bool noHintBonusClaimed;
        public bool firstAttemptBonusClaimed;
        public bool streakRewardClaimed;
        public string lastAttemptDate = string.Empty;
        public int attemptCount;
        public int totalCompleted;
        public int noHintCompletions;
    }

    [Serializable]
    public class CosmeticInventoryData
    {
        public List<string> ownedCosmeticIds = new List<string>
        {
            CosmeticCatalog.DefaultBot,
            CosmeticCatalog.DefaultPath
        };
        public string activeBotSkinId = CosmeticCatalog.DefaultBot;
        public string activePathColorId = CosmeticCatalog.DefaultPath;
        public string activeTileThemeId = CosmeticCatalog.DefaultTileTheme;
        public string activeDockDesignId = CosmeticCatalog.DefaultDock;
        public string activeWinAnimationId = CosmeticCatalog.DefaultWinAnimation;
        public string activeFailureAnimationId = CosmeticCatalog.DefaultFailureAnimation;
        public string activeRoomBackgroundId = CosmeticCatalog.DefaultRoomBackground;
        public List<string> purchaseHistory = new List<string>();
    }

    [Serializable]
    public class PlayerSettingsData
    {
        public bool soundEnabled = true;
        public bool musicEnabled = true;
        public bool hapticsEnabled = true;
    }

    [Serializable]
    public class PlayerProgressData
    {
        public int saveVersion = 4;
        public int highestUnlockedMainLevel = 1;
        public int totalStars;
        public int dustCoins = 30;
        public int masterCleanProgress = 1;
        public int endlessBestScore;
        public int endlessCurrentLevel = 1;
        public string endlessRunSeed = "CozyRun";
        public int roomsCleaned;
        public int totalDustBunnies;
        public List<LevelProgressRecord> mainLevels = new List<LevelProgressRecord>();
        public List<string> collectedDustBunnyLevelIds = new List<string>();
        public List<string> rewardedCompletionLevelIds = new List<string>();
        public DailyChallengeData daily = new DailyChallengeData();
        public CosmeticInventoryData cosmetics = new CosmeticInventoryData();
        public PlayerSettingsData settings = new PlayerSettingsData();
    }
}
