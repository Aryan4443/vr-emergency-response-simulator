using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VRSim.Evaluation.History
{
    /// <summary>
    /// Aggregates repeated runs of a scenario into the anonymous progress figures the
    /// specification asks for: comparing performance across repeated training sessions and
    /// feeding a dashboard that names no person and no device.
    ///
    /// The runs are supplied by the caller rather than read from disk. Reading is
    /// <see cref="ScenarioResultStore"/>'s job, and keeping the two apart means the whole of this
    /// class can be exercised in edit-mode tests without touching the file system.
    ///
    /// Runs are assumed to be in chronological order, oldest first. A <see cref="ScenarioResult"/>
    /// carries no timestamp, so this class cannot restore that order itself and the caller must
    /// preserve it for <see cref="ImprovementTrend"/> to mean anything.
    /// </summary>
    public class SessionHistory
    {
        /// <summary>
        /// Runs needed before a trend is reported at all.
        ///
        /// Four is the smallest number that puts two runs on each side of the halfway split. With
        /// three or fewer, the comparison is one run against one run, where a single unlucky
        /// wrong turn reads as a decline in ability.
        /// </summary>
        public const int MinimumRunsForTrend = 4;

        /// <summary>
        /// Score difference between the two halves that counts as real movement.
        ///
        /// Five points is the value of the smallest scoring event in <see cref="ScoreBoard"/>
        /// (reading the evacuation map), so anything below it cannot represent a behaviour change
        /// and is reported as steady.
        /// </summary>
        public const float TrendThresholdPoints = 5f;

        /// <summary>Shown wherever a figure exists but has no value to display.</summary>
        const string NoValue = "—";

        readonly List<ScenarioResult> m_Runs;

        /// <param name="results">
        /// Runs to summarise, oldest first. A null list is treated as an empty history, and null
        /// entries are dropped so that one unreadable result file cannot take the dashboard down.
        /// </param>
        public SessionHistory(IReadOnlyList<ScenarioResult> results)
        {
            m_Runs = results == null
                ? new List<ScenarioResult>()
                : results.Where(result => result != null).ToList();

            // Copied into a private list on construction, so a caller that keeps adding to its own
            // list cannot change figures a dashboard has already drawn.
            TotalRuns = m_Runs.Count;
            CompletedRuns = m_Runs.Count(result => result.completed);

            // Cast before dividing: both operands are integers, so without it every rate below one
            // would collapse to zero.
            CompletionRate = TotalRuns == 0 ? 0f : (float)CompletedRuns / TotalRuns;

            BestScore = TotalRuns == 0 ? (int?)null : m_Runs.Max(result => result.score);

            // MidpointRounding.AwayFromZero rather than the framework default, which rounds a half
            // to the nearest even number and would report an average of 10.5 as 10.
            AverageScore = TotalRuns == 0
                ? (int?)null
                : (int)Math.Round(m_Runs.Average(result => result.score), MidpointRounding.AwayFromZero);

            var completedRuns = m_Runs.Where(result => result.completed).ToList();
            BestCompletionTimeSeconds = completedRuns.Count == 0
                ? (float?)null
                : completedRuns.Min(result => result.completionTimeSeconds);

            // One decimal place: "1.7 unsafe areas per run" is readable, "1.6666666" is not.
            AverageUnsafeZoneEntries = TotalRuns == 0
                ? (float?)null
                : (float)Math.Round(m_Runs.Average(result => result.unsafeZoneEntries), 1,
                    MidpointRounding.AwayFromZero);

            MostCommonMistake = FindMostCommonMistake(m_Runs);
            ImprovementTrend = CalculateTrend(m_Runs);
        }

        /// <summary>The runs behind these figures, oldest first.</summary>
        public IReadOnlyList<ScenarioResult> Runs => m_Runs;

        /// <summary>How many runs the history holds.</summary>
        public int TotalRuns { get; }

        /// <summary>How many of those runs reached a safe exit.</summary>
        public int CompletedRuns { get; }

        /// <summary>
        /// Completed runs as a fraction from 0 to 1. An empty history reports zero rather than
        /// nothing, because a progress bar still has to be drawn before the first run.
        /// </summary>
        public float CompletionRate { get; }

        /// <summary>
        /// Highest score recorded, or null when there are no runs. Null rather than zero because
        /// <see cref="ScoreBoard"/> allows a real run to score zero or below, so zero would be
        /// indistinguishable from a genuine result.
        /// </summary>
        public int? BestScore { get; }

        /// <summary>
        /// Mean score rounded to the nearest point, or null when there are no runs. The score is
        /// whole numbers, so reporting a fractional average would suggest a precision it lacks.
        /// </summary>
        public int? AverageScore { get; }

        /// <summary>
        /// Fastest time among runs that actually reached a safe exit, or null when none did.
        /// Abandoned runs are excluded because their timer stops when the user gives up, which
        /// would otherwise record giving up early as a personal best.
        /// </summary>
        public float? BestCompletionTimeSeconds { get; }

        /// <summary>
        /// Mean unsafe-area entries per run to one decimal place, or null when there are no runs.
        /// Nullable for the same reason as <see cref="AverageScore"/>: zero is a real, and good,
        /// result, so it must not double as "nothing recorded".
        /// </summary>
        public float? AverageUnsafeZoneEntries { get; }

        /// <summary>
        /// Plain-language name of the mistake made most often across the history, or null when no
        /// run made any mistake at all.
        /// </summary>
        public string MostCommonMistake { get; }

        /// <summary>Whether scores are moving up, down or nowhere across the history.</summary>
        public ImprovementTrend ImprovementTrend { get; }

        /// <summary>
        /// A history containing only the runs of one scenario, so that a user practising the fire
        /// evacuation is not told they are declining because they also tried an earthquake once.
        ///
        /// The comparison is ordinal: a scenarioId such as fire_evacuation_01 is a stable
        /// identifier, not display text, so it must not be folded by the active culture. A null or
        /// empty id yields an empty history rather than every run, because asking for no scenario
        /// should not quietly mean asking for all of them.
        /// </summary>
        public SessionHistory FilterByScenario(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId))
                return new SessionHistory(new List<ScenarioResult>());

            return new SessionHistory(m_Runs
                .Where(result => string.Equals(result.scenarioId, scenarioId, StringComparison.Ordinal))
                .ToList());
        }

        /// <summary>
        /// The dashboard figures, one per line, in the same shape as
        /// <see cref="ScenarioReport.SummaryLines"/> so both screens can share a text layout.
        /// </summary>
        public IEnumerable<string> Summary()
        {
            if (TotalRuns == 0)
            {
                yield return "No sessions recorded yet.";
                yield break;
            }

            var completionPercent =
                (int)Math.Round(CompletionRate * 100f, MidpointRounding.AwayFromZero);

            yield return $"Sessions: {TotalRuns}";
            yield return $"Completed: {CompletedRuns} of {TotalRuns} ({completionPercent}%)";
            yield return $"Best score: {BestScore}";
            yield return $"Average score: {AverageScore}";
            yield return $"Best time: {(BestCompletionTimeSeconds.HasValue ? ScenarioReport.FormatTime(BestCompletionTimeSeconds.Value) : NoValue)}";

            // Formatted invariantly so the decimal separator does not follow the headset's locale
            // while the surrounding English prose does not.
            yield return string.Format(CultureInfo.InvariantCulture, "Unsafe areas per run: {0:0.0}",
                AverageUnsafeZoneEntries.Value);

            yield return $"Most common mistake: {MostCommonMistake ?? NoValue}";
            yield return $"Trend: {TrendLabel(this.ImprovementTrend)}";
        }

        /// <summary>
        /// Counts each kind of mistake across every run and names the commonest.
        ///
        /// The categories are listed most safety-critical first, which doubles as the tie-break:
        /// when two mistakes happen equally often the dashboard names the one that matters more,
        /// rather than whichever happened to sort first. Failing to evacuate outranks acting
        /// incorrectly, which outranks straying into a hazard; the last two are preparation habits
        /// and are only reported when nothing worse happened.
        ///
        /// Counting a per-run failure ("this run skipped the alarm", at most one per run) against a
        /// per-event tally ("this run entered smoke four times") is a deliberate simplification:
        /// the dashboard only needs to point at one thing to work on next.
        /// </summary>
        static string FindMostCommonMistake(List<ScenarioResult> runs)
        {
            var labels = new[]
            {
                "Did not reach a safe exit",
                "Incorrect actions",
                "Entered unsafe areas",
                "Did not raise the alarm",
                "Did not read the evacuation map",
            };

            var counts = new[]
            {
                runs.Count(result => !result.completed),
                runs.Sum(result => result.incorrectActions),
                runs.Sum(result => result.unsafeZoneEntries),
                runs.Count(result => !DidAction(result, "alarm_activated")),
                runs.Count(result => !DidAction(result, "map_viewed")),
            };

            var worst = -1;
            for (var i = 0; i < counts.Length; i++)
            {
                // Strictly greater, so an equal count leaves the earlier and graver label in place.
                if (counts[i] > 0 && (worst < 0 || counts[i] > counts[worst]))
                    worst = i;
            }

            return worst < 0 ? null : labels[worst];
        }

        /// <summary>
        /// Compares the mean score of the earlier half of the history against the later half.
        ///
        /// Halves rather than first-run-against-last-run: a single run swings on luck, and the
        /// point of the dashboard is to show a habit forming. When the run count is odd the middle
        /// run is left out of both halves so it cannot lend weight to either side.
        /// </summary>
        static ImprovementTrend CalculateTrend(List<ScenarioResult> runs)
        {
            if (runs.Count < MinimumRunsForTrend)
                return ImprovementTrend.NotEnoughData;

            var half = runs.Count / 2;
            var earlier = runs.Take(half).Average(result => (double)result.score);
            var later = runs.Skip(runs.Count - half).Average(result => (double)result.score);
            var change = later - earlier;

            if (change >= TrendThresholdPoints)
                return ImprovementTrend.Improving;

            if (change <= -TrendThresholdPoints)
                return ImprovementTrend.Declining;

            return ImprovementTrend.Steady;
        }

        static string TrendLabel(ImprovementTrend trend)
        {
            switch (trend)
            {
                case ImprovementTrend.Improving:
                    return "Improving";
                case ImprovementTrend.Declining:
                    return "Declining";
                case ImprovementTrend.Steady:
                    return "Steady";
                default:
                    return "Not enough runs yet";
            }
        }

        static bool DidAction(ScenarioResult result, string actionType) =>
            result.events != null &&
            result.events.Any(recorded => recorded != null && recorded.actionType == actionType);
    }
}
