using System.Collections.Generic;

namespace VRSim.Evaluation
{
    /// <summary>
    /// Collects the anonymous action log described in section 7 and composes the
    /// <see cref="ScenarioResult"/> from section 11 at the end of a run.
    /// </summary>
    public class PerformanceLogger
    {
        readonly ScenarioTimer m_Timer;
        readonly List<ActionEvent> m_Events = new List<ActionEvent>();

        public PerformanceLogger(ScenarioTimer timer)
        {
            m_Timer = timer;
        }

        /// <summary>Events recorded so far, oldest first.</summary>
        public IReadOnlyList<ActionEvent> Events => m_Events;

        /// <summary>Records one interaction, stamped with the current scenario time.</summary>
        public void Record(string actionType, string objectId, bool successful)
        {
            m_Events.Add(new ActionEvent
            {
                actionType = actionType,
                objectId = objectId,
                timestampSeconds = m_Timer.ElapsedSeconds,
                successful = successful,
            });
        }

        /// <summary>Builds the run summary that is shown on the results screen and stored.</summary>
        public ScenarioResult BuildResult(string scenarioId, ScoreBoard board, bool completed, string selectedExit)
        {
            var result = new ScenarioResult
            {
                scenarioId = scenarioId,
                completed = completed,
                completionTimeSeconds = m_Timer.ElapsedSeconds,
                unsafeZoneEntries = board.CountOf(ScoreEvent.UnsafeAreaEntered),
                incorrectActions = board.CountOf(ScoreEvent.WrongExitSelected)
                                   + board.CountOf(ScoreEvent.CriticalUnsafeAction),
                selectedExit = selectedExit,
                score = board.Total,
            };

            result.events.AddRange(m_Events);
            return result;
        }

        /// <summary>Clears the log so the scenario can be replayed.</summary>
        public void Reset() => m_Events.Clear();
    }
}
