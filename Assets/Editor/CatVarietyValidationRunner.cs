#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DustBot.Editor
{
    public static class CatVarietyValidationRunner
    {
        [MenuItem("DustBot/Validate Cat Variety")]
        public static void RunBatch()
        {
            LevelLoader loader = new LevelLoader();
            HashSet<CatPuzzleArchetype> archetypes =
                new HashSet<CatPuzzleArchetype>();
            HashSet<string> boardSizes = new HashSet<string>();
            Queue<CatStrategyReport> recent = new Queue<CatStrategyReport>();
            Queue<CatPuzzleArchetype> recentArchetypes =
                new Queue<CatPuzzleArchetype>();
            int cats = 0;
            int multiCrumb = 0;
            int repeatedStrategies = 0;

            for (int number = 22; number <= 180; number++)
            {
                LevelDefinition level = loader.LoadMain(number);
                if (level.cat == null || !level.cat.IsEnabled) continue;

                cats++;
                archetypes.Add(level.catPuzzleArchetype);
                boardSizes.Add(level.width + "x" + level.height);
                if (level.Count(CellContent.Crumb) > 1) multiCrumb++;

                List<GridPosition> route =
                    CatObstacleSimulator.BuildExpectedRoute(level);
                CatStrategyReport strategy =
                    CatLevelVarietyEvaluator.Analyze(level, route);
                string validation;
                Require(
                    LevelValidator.TryValidate(level, out validation),
                    "Cat level " + number + " failed validation: " + validation);
                Require(
                    CatLevelVarietyEvaluator.MatchesArchetype(
                        level,
                        level.catPuzzleArchetype,
                        strategy),
                    "Cat level " + number + " missed its " +
                    level.catPuzzleArchetype + " strategy.");
                Require(
                    !recentArchetypes.Contains(level.catPuzzleArchetype),
                    "Cat archetype repeated inside the recent window at level " + number + ".");

                foreach (CatStrategyReport previous in recent)
                {
                    if (CatLevelVarietyEvaluator.Similarity(strategy, previous) >= 8)
                    {
                        repeatedStrategies++;
                        break;
                    }
                }

                recent.Enqueue(strategy);
                recentArchetypes.Enqueue(level.catPuzzleArchetype);
                while (recent.Count > 4) recent.Dequeue();
                while (recentArchetypes.Count > 4) recentArchetypes.Dequeue();

                Debug.Log(string.Format(
                    "CAT_VARIETY level={0} size={1}x{2} archetype={3} crumbs={4} pressure={5} reroute={6} backtracks={7} nearCatch={8} corridors={9} chokepoints={10} loops={11} pockets={12}",
                    number,
                    level.width,
                    level.height,
                    level.catPuzzleArchetype,
                    level.Count(CellContent.Crumb),
                    strategy.pressureScore,
                    strategy.routeChangeMoves,
                    strategy.backtrackMoves,
                    strategy.nearCatchTurns,
                    strategy.corridorCells,
                    strategy.chokepoints,
                    strategy.loops,
                    strategy.safePockets));
            }

            Require(cats >= 20, "Cat variety sample contained too few cat levels.");
            Require(archetypes.Count >= 12, "Cat variety sample used too few archetypes.");
            Require(boardSizes.Count >= 4, "Cat variety sample used too few board sizes.");
            Require(multiCrumb * 2 >= cats, "Most sampled cat levels still used one crumb.");
            Require(
                repeatedStrategies * 4 <= cats,
                "Too many sampled cat levels repeated a recent strategic fingerprint.");
            Debug.Log(string.Format(
                "CAT_VARIETY PASS cats={0} archetypes={1} boardSizes={2} multiCrumb={3} repeatedStrategies={4}",
                cats,
                archetypes.Count,
                boardSizes.Count,
                multiCrumb,
                repeatedStrategies));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
