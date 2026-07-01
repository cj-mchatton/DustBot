using System;

namespace DustBot
{
    public enum GenerationMode
    {
        ProductionCampaign,
        DevelopmentCampaign,
        CatTesting,
        ObstacleTesting,
        TutorialTesting,
        MazeTesting
    }

    public enum RouteModifierStyle
    {
        Mixed,
        Sticky,
        OneWay,
        Fragile
    }

    public static class LevelGenerationConfig
    {
        public const int ProductionLevelCount = LevelManifest.MainJourneyLevelCount;
        public const int DevelopmentLevelCount = 30;
        public const int CatTestingLevelCount = 24;
        public const int ObstacleTestingLevelCount = 18;
        public const int TutorialTestingLevelCount = 8;
        public const int MazeTestingLevelCount = 20;

        private static GenerationMode activeMode = DefaultMode;

        public static bool DeveloperToolsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        public static GenerationMode DefaultMode
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return GenerationMode.DevelopmentCampaign;
#else
                return GenerationMode.ProductionCampaign;
#endif
            }
        }

        public static GenerationMode ActiveMode
        {
            get { return DeveloperToolsEnabled ? activeMode : GenerationMode.ProductionCampaign; }
        }

        public static void SetActiveMode(GenerationMode mode)
        {
            if (!Enum.IsDefined(typeof(GenerationMode), mode))
            {
                throw new ArgumentOutOfRangeException("mode");
            }

            activeMode = DeveloperToolsEnabled ? mode : GenerationMode.ProductionCampaign;
        }

        public static int LevelCount(GenerationMode mode)
        {
            switch (mode)
            {
                case GenerationMode.DevelopmentCampaign: return DevelopmentLevelCount;
                case GenerationMode.CatTesting: return CatTestingLevelCount;
                case GenerationMode.ObstacleTesting: return ObstacleTestingLevelCount;
                case GenerationMode.TutorialTesting: return TutorialTestingLevelCount;
                case GenerationMode.MazeTesting: return MazeTestingLevelCount;
                default: return ProductionLevelCount;
            }
        }

        public static string DisplayName(GenerationMode mode)
        {
            switch (mode)
            {
                case GenerationMode.DevelopmentCampaign: return "DEVELOPMENT CAMPAIGN";
                case GenerationMode.CatTesting: return "CAT TESTING";
                case GenerationMode.ObstacleTesting: return "OBSTACLE TESTING";
                case GenerationMode.TutorialTesting: return "TUTORIAL TESTING";
                case GenerationMode.MazeTesting: return "MAZE TESTING";
                default: return "PRODUCTION CAMPAIGN";
            }
        }
    }
}
