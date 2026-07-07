using System;
using System.Collections.Generic;

namespace DustBot
{
    public sealed class LevelCategoryProfile
    {
        public LevelCategory category;
        public string displayName;
        public string description;
        public int levelCount;
        public int catLevelCount;
        public int mazeLevelCount;
    }

    public static class LevelCategoryCatalog
    {
        public const int TotalLevelCount = 255;

        private static readonly LevelCategory[] OrderedCategories =
        {
            LevelCategory.Easy,
            LevelCategory.Medium,
            LevelCategory.Hard,
            LevelCategory.Expert,
            LevelCategory.CatChase
        };

        private static readonly Dictionary<LevelCategory, LevelCategoryProfile> Profiles =
            new Dictionary<LevelCategory, LevelCategoryProfile>
            {
                { LevelCategory.Easy, Profile(LevelCategory.Easy, "EASY", "Five focused lessons in cleaning, collecting, cats, and route planning.", 5, 1, 0) },
                { LevelCategory.Medium, Profile(LevelCategory.Medium, "MEDIUM", "Maze puzzles with some cat chase levels.", 50, 15, 35) },
                { LevelCategory.Hard, Profile(LevelCategory.Hard, "HARD", "Large maze challenges.", 50, 5, 45) },
                { LevelCategory.Expert, Profile(LevelCategory.Expert, "EXPERT", "The hardest DustBot puzzles.", 100, 25, 75) },
                { LevelCategory.CatChase, Profile(LevelCategory.CatChase, "CAT CHASE", "Only cat chase levels.", 50, 50, 0) }
            };

        public static IReadOnlyList<LevelCategory> All { get { return OrderedCategories; } }

        public static LevelCategoryProfile Get(LevelCategory category)
        {
            LevelCategoryProfile profile;
            if (!Profiles.TryGetValue(category, out profile))
            {
                throw new ArgumentOutOfRangeException("category");
            }
            return profile;
        }

        public static int Count(LevelCategory category) { return Get(category).levelCount; }
        public static string Name(LevelCategory category) { return Get(category).displayName; }

        public static bool IsCatLevel(LevelCategory category, int levelNumber)
        {
            levelNumber = ClampLevel(category, levelNumber);
            switch (category)
            {
                case LevelCategory.Easy:
                    return levelNumber == 4;
                case LevelCategory.Medium:
                    return levelNumber == 3 || levelNumber == 6 || levelNumber == 10 ||
                           levelNumber == 13 || levelNumber == 17 || levelNumber == 20 ||
                           levelNumber == 23 || levelNumber == 27 || levelNumber == 30 ||
                           levelNumber == 33 || levelNumber == 37 || levelNumber == 40 ||
                           levelNumber == 43 || levelNumber == 47 || levelNumber == 50;
                case LevelCategory.Hard:
                    return levelNumber % 10 == 0;
                case LevelCategory.Expert:
                    return levelNumber % 4 == 0;
                case LevelCategory.CatChase:
                    return true;
                default:
                    return false;
            }
        }

        public static DifficultyTier Difficulty(LevelCategory category, int levelNumber)
        {
            levelNumber = ClampLevel(category, levelNumber);
            switch (category)
            {
                case LevelCategory.Easy: return levelNumber <= 3 ? DifficultyTier.Tutorial : DifficultyTier.Easy;
                case LevelCategory.Medium: return DifficultyTier.Medium;
                case LevelCategory.Hard: return DifficultyTier.Hard;
                case LevelCategory.Expert: return DifficultyTier.Expert;
                case LevelCategory.CatChase:
                    if (levelNumber <= 4) return DifficultyTier.Easy;
                    if (levelNumber <= 14) return DifficultyTier.Medium;
                    if (levelNumber <= 24) return DifficultyTier.Hard;
                    return DifficultyTier.Expert;
                default: return DifficultyTier.Easy;
            }
        }

        public static int ClampLevel(LevelCategory category, int levelNumber)
        {
            return Math.Max(1, Math.Min(Count(category), levelNumber));
        }

        public static string LevelName(LevelCategory category, int levelNumber)
        {
            return Name(category) + " " + ClampLevel(category, levelNumber);
        }

        private static LevelCategoryProfile Profile(
            LevelCategory category, string name, string description, int count, int cats, int mazes)
        {
            return new LevelCategoryProfile
            {
                category = category,
                displayName = name,
                description = description,
                levelCount = count,
                catLevelCount = cats,
                mazeLevelCount = mazes
            };
        }
    }
}
