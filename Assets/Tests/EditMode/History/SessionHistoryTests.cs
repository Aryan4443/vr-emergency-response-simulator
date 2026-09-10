using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRSim.Evaluation;
using VRSim.Evaluation.History;

namespace VRSim.Tests
{
    public class SessionHistoryTests
    {
        const string FireScenario = "fire_evacuation_01";
        const string EarthquakeScenario = "earthquake_01";

        /// <summary>
        /// A clean run by default, so a test only has to state the one thing it is about. Runs
        /// carry the map and alarm events unless a test asks otherwise, because a missing event is
        /// itself a mistake the history reports on.
        /// </summary>
        static ScenarioResult Run(
            int score,
            bool completed = true,
            float timeSeconds = 100f,
            int unsafeZoneEntries = 0,
            int incorrectActions = 0,
            bool raisedAlarm = true,
            bool readMap = true,
            string scenarioId = FireScenario)
        {
            var result = new ScenarioResult
            {
                scenarioId = scenarioId,
                completed = completed,
                completionTimeSeconds = timeSeconds,
                unsafeZoneEntries = unsafeZoneEntries,
                incorrectActions = incorrectActions,
                selectedExit = completed ? "north_exit" : null,
                score = score,
            };

            if (readMap)
                result.events.Add(new ActionEvent { actionType = "map_viewed", successful = true });

            if (raisedAlarm)
                result.events.Add(new ActionEvent { actionType = "alarm_activated", successful = true });

            return result;
        }

        static SessionHistory HistoryOf(params ScenarioResult[] runs) => new SessionHistory(runs);

        [Test]
        public void EmptyHistoryReportsNoRuns()
        {
            var history = HistoryOf();

            Assert.AreEqual(0, history.TotalRuns);
            Assert.AreEqual(0, history.CompletedRuns);
            Assert.AreEqual(0f, history.CompletionRate, 0.0001f);
        }

        [Test]
        public void EmptyHistoryReportsNoFiguresRatherThanZeroes()
        {
            var history = HistoryOf();

            Assert.IsNull(history.BestScore);
            Assert.IsNull(history.AverageScore);
            Assert.IsNull(history.BestCompletionTimeSeconds);
            Assert.IsNull(history.AverageUnsafeZoneEntries);
        }

        [Test]
        public void EmptyHistoryHasNoMostCommonMistake()
        {
            Assert.IsNull(HistoryOf().MostCommonMistake);
        }

        [Test]
        public void EmptyHistoryHasNotEnoughDataForATrend()
        {
            Assert.AreEqual(ImprovementTrend.NotEnoughData, HistoryOf().ImprovementTrend);
        }

        [Test]
        public void EmptyHistorySummarySaysNothingHasBeenRecorded()
        {
            var lines = HistoryOf().Summary().ToList();

            Assert.AreEqual(1, lines.Count);
            StringAssert.Contains("No sessions", lines[0]);
        }

        [Test]
        public void ANullResultListIsTreatedAsAnEmptyHistory()
        {
            var history = new SessionHistory(null);

            Assert.AreEqual(0, history.TotalRuns);
            Assert.AreEqual(ImprovementTrend.NotEnoughData, history.ImprovementTrend);
        }

        [Test]
        public void NullRunsInTheListAreIgnored()
        {
            var history = HistoryOf(Run(50), null, Run(70));

            Assert.AreEqual(2, history.TotalRuns);
            Assert.AreEqual(70, history.BestScore);
        }

        [Test]
        public void ChangingTheSourceListAfterwardsDoesNotChangeTheHistory()
        {
            var source = new List<ScenarioResult> { Run(50) };
            var history = new SessionHistory(source);

            source.Add(Run(90));

            Assert.AreEqual(1, history.TotalRuns);
            Assert.AreEqual(50, history.BestScore);
        }

        [Test]
        public void CountsCompletedRunsSeparatelyFromTotalRuns()
        {
            var history = HistoryOf(Run(50), Run(60), Run(10, completed: false), Run(70));

            Assert.AreEqual(4, history.TotalRuns);
            Assert.AreEqual(3, history.CompletedRuns);
        }

        [Test]
        public void CompletionRateIsAFractionRatherThanIntegerDivision()
        {
            var history = HistoryOf(
                Run(50),
                Run(10, completed: false),
                Run(10, completed: false),
                Run(10, completed: false));

            Assert.AreEqual(0.25f, history.CompletionRate, 0.0001f);
        }

        [Test]
        public void ReportsTheBestScoreAcrossRuns()
        {
            var history = HistoryOf(Run(40), Run(85), Run(60));

            Assert.AreEqual(85, history.BestScore);
        }

        [Test]
        public void AverageScoreRoundsHalvesAwayFromZero()
        {
            var history = HistoryOf(Run(10), Run(11));

            Assert.AreEqual(11, history.AverageScore);
        }

        [Test]
        public void ASingleRunIsItsOwnBestAndAverageScore()
        {
            var history = HistoryOf(Run(63, timeSeconds: 77f));

            Assert.AreEqual(1, history.TotalRuns);
            Assert.AreEqual(63, history.BestScore);
            Assert.AreEqual(63, history.AverageScore);
            Assert.AreEqual(77f, history.BestCompletionTimeSeconds.Value, 0.001f);
        }

        [Test]
        public void ReportsTheBestTimeAcrossRuns()
        {
            var history = HistoryOf(
                Run(50, timeSeconds: 150f),
                Run(60, timeSeconds: 92.4f),
                Run(55, timeSeconds: 200f));

            Assert.AreEqual(92.4f, history.BestCompletionTimeSeconds.Value, 0.001f);
        }

        [Test]
        public void BestTimeIgnoresRunsThatDidNotComplete()
        {
            var history = HistoryOf(
                Run(-10, completed: false, timeSeconds: 10f),
                Run(50, timeSeconds: 150f),
                Run(60, timeSeconds: 92.4f));

            Assert.AreEqual(92.4f, history.BestCompletionTimeSeconds.Value, 0.001f);
        }

        [Test]
        public void AllIncompleteRunsHaveNoBestTime()
        {
            var history = HistoryOf(
                Run(-10, completed: false, timeSeconds: 30f),
                Run(-20, completed: false, timeSeconds: 45f));

            Assert.IsNull(history.BestCompletionTimeSeconds);
            Assert.AreEqual(0f, history.CompletionRate, 0.0001f);
        }

        [Test]
        public void AverageUnsafeZoneEntriesIsRoundedToOneDecimal()
        {
            var history = HistoryOf(
                Run(50, unsafeZoneEntries: 1),
                Run(50, unsafeZoneEntries: 2),
                Run(50, unsafeZoneEntries: 2));

            Assert.AreEqual(1.7f, history.AverageUnsafeZoneEntries.Value, 0.001f);
        }

        [Test]
        public void MostCommonMistakeNamesUnsafeAreasWhenTheyDominate()
        {
            var history = HistoryOf(
                Run(50, unsafeZoneEntries: 3),
                Run(50, unsafeZoneEntries: 2, incorrectActions: 1));

            Assert.AreEqual("Entered unsafe areas", history.MostCommonMistake);
        }

        [Test]
        public void MostCommonMistakeNamesIncorrectActionsWhenTheyDominate()
        {
            var history = HistoryOf(
                Run(50, incorrectActions: 2),
                Run(50, incorrectActions: 1, unsafeZoneEntries: 1));

            Assert.AreEqual("Incorrect actions", history.MostCommonMistake);
        }

        [Test]
        public void MostCommonMistakeNamesUnfinishedRunsWhenTheyDominate()
        {
            var history = HistoryOf(
                Run(-10, completed: false),
                Run(-20, completed: false),
                Run(-15, completed: false));

            Assert.AreEqual("Did not reach a safe exit", history.MostCommonMistake);
        }

        [Test]
        public void MostCommonMistakeNoticesTheAlarmBeingSkipped()
        {
            var history = HistoryOf(
                Run(50, raisedAlarm: false),
                Run(60, raisedAlarm: false));

            Assert.AreEqual("Did not raise the alarm", history.MostCommonMistake);
        }

        [Test]
        public void MostCommonMistakeNoticesTheMapBeingSkipped()
        {
            var history = HistoryOf(
                Run(50, readMap: false),
                Run(60, readMap: false));

            Assert.AreEqual("Did not read the evacuation map", history.MostCommonMistake);
        }

        [Test]
        public void AFlawlessHistoryHasNoMostCommonMistake()
        {
            var history = HistoryOf(Run(85), Run(90));

            Assert.IsNull(history.MostCommonMistake);
        }

        [Test]
        public void TiedMistakesFavourTheMoreSafetyCriticalOne()
        {
            var history = HistoryOf(Run(-10, completed: false, unsafeZoneEntries: 1));

            Assert.AreEqual("Did not reach a safe exit", history.MostCommonMistake);
        }

        [Test]
        public void ThreeRunsAreNotEnoughForATrend()
        {
            var history = HistoryOf(Run(10), Run(50), Run(90));

            Assert.AreEqual(ImprovementTrend.NotEnoughData, history.ImprovementTrend);
        }

        [Test]
        public void RisingScoresInTheLaterHalfCountAsImproving()
        {
            var history = HistoryOf(Run(10), Run(20), Run(40), Run(50));

            Assert.AreEqual(ImprovementTrend.Improving, history.ImprovementTrend);
        }

        [Test]
        public void FallingScoresInTheLaterHalfCountAsDeclining()
        {
            var history = HistoryOf(Run(50), Run(40), Run(20), Run(10));

            Assert.AreEqual(ImprovementTrend.Declining, history.ImprovementTrend);
        }

        [Test]
        public void AChangeSmallerThanOneScoringEventCountsAsSteady()
        {
            var history = HistoryOf(Run(50), Run(52), Run(53), Run(51));

            Assert.AreEqual(ImprovementTrend.Steady, history.ImprovementTrend);
        }

        [Test]
        public void AChangeOfExactlyOneScoringEventCountsAsImproving()
        {
            var history = HistoryOf(Run(10), Run(10), Run(15), Run(15));

            Assert.AreEqual(ImprovementTrend.Improving, history.ImprovementTrend);
        }

        [Test]
        public void TheMiddleRunIsIgnoredWhenTheRunCountIsOdd()
        {
            // The middle run is extreme enough to swing the verdict if either half counted it.
            var history = HistoryOf(Run(10), Run(10), Run(-1000), Run(12), Run(12));

            Assert.AreEqual(ImprovementTrend.Steady, history.ImprovementTrend);
        }

        [Test]
        public void FilterByScenarioKeepsOnlyMatchingRuns()
        {
            var history = HistoryOf(
                Run(50, scenarioId: FireScenario),
                Run(60, scenarioId: EarthquakeScenario),
                Run(70, scenarioId: FireScenario));

            var fireOnly = history.FilterByScenario(FireScenario);

            Assert.AreEqual(2, fireOnly.TotalRuns);
            Assert.IsTrue(fireOnly.Runs.All(run => run.scenarioId == FireScenario));
            Assert.AreEqual(70, fireOnly.BestScore);
        }

        [Test]
        public void FilterByScenarioMatchesTheIdExactly()
        {
            var history = HistoryOf(Run(50, scenarioId: FireScenario));

            Assert.AreEqual(0, history.FilterByScenario("Fire_Evacuation_01").TotalRuns);
        }

        [Test]
        public void FilterByScenarioWithoutAnIdReturnsAnEmptyHistory()
        {
            var history = HistoryOf(Run(50), Run(60));

            Assert.AreEqual(0, history.FilterByScenario(null).TotalRuns);
            Assert.AreEqual(0, history.FilterByScenario(string.Empty).TotalRuns);
        }

        [Test]
        public void FilterByScenarioLeavesTheOriginalHistoryUnchanged()
        {
            var history = HistoryOf(
                Run(50, scenarioId: FireScenario),
                Run(60, scenarioId: EarthquakeScenario),
                Run(70, scenarioId: FireScenario));

            history.FilterByScenario(EarthquakeScenario);

            Assert.AreEqual(3, history.TotalRuns);
        }

        [Test]
        public void SummaryReportsEveryDashboardFigure()
        {
            var history = HistoryOf(
                Run(40, timeSeconds: 120f, unsafeZoneEntries: 1),
                Run(80, timeSeconds: 90f, unsafeZoneEntries: 3));

            var lines = history.Summary().ToList();

            Assert.IsTrue(lines.Any(l => l.StartsWith("Sessions:") && l.Contains("2")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Completed:") && l.Contains("2 of 2") && l.Contains("100%")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Best score:") && l.Contains("80")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Average score:") && l.Contains("60")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Best time:") && l.Contains("1:30")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Unsafe areas per run:") && l.Contains("2.0")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Most common mistake:") && l.Contains("unsafe areas")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Trend:") && l.Contains("Not enough runs")));
        }

        [Test]
        public void SummaryShowsADashWhenNoRunReachedAnExit()
        {
            var history = HistoryOf(
                Run(-10, completed: false, timeSeconds: 30f),
                Run(-20, completed: false, timeSeconds: 45f));

            var lines = history.Summary().ToList();

            Assert.IsTrue(lines.Any(l => l.StartsWith("Best time:") && l.Contains("—")));
            Assert.IsTrue(lines.Any(l => l.StartsWith("Completed:") && l.Contains("0 of 2")));
        }
    }
}
