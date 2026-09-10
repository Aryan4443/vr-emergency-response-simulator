using System;
using System.Collections.Generic;

namespace VRSim.Evaluation
{
    /// <summary>
    /// Turns scenario events into the prototype score described in section 10.
    ///
    /// Rewards are granted once per run, so repeatedly triggering the alarm or re-reading the
    /// evacuation map cannot inflate a score. Penalties apply every time, because section 11
    /// reports how many unsafe entries and incorrect actions a run contained.
    ///
    /// The score is prototype feedback only. It is not a safety certification or an official
    /// training qualification.
    /// </summary>
    public class ScoreBoard
    {
        static readonly IReadOnlyDictionary<ScoreEvent, int> Values = new Dictionary<ScoreEvent, int>
        {
            { ScoreEvent.CorrectSafeExit, 40 },
            { ScoreEvent.HazardIdentified, 20 },
            { ScoreEvent.AlarmActivated, 10 },
            { ScoreEvent.EvacuationMapViewed, 5 },
            { ScoreEvent.UnsafeAreaEntered, -15 },
            { ScoreEvent.WrongExitSelected, -25 },
            { ScoreEvent.CriticalUnsafeAction, -30 },
            { ScoreEvent.SuccessfulCompletion, 30 },
        };

        readonly Dictionary<ScoreEvent, int> m_Counts = new Dictionary<ScoreEvent, int>();

        /// <summary>Raised with the new total whenever a recorded event changes the score.</summary>
        public event Action<int> ScoreChanged;

        /// <summary>
        /// The run total. Section 10 sets no lower bound, so a run where penalties outweigh
        /// rewards reports a negative score rather than being flattened to zero.
        /// </summary>
        public int Total { get; private set; }

        public static int ValueOf(ScoreEvent scoreEvent) => Values[scoreEvent];

        /// <summary>How many times <paramref name="scoreEvent"/> happened this run.</summary>
        public int CountOf(ScoreEvent scoreEvent) =>
            m_Counts.TryGetValue(scoreEvent, out var count) ? count : 0;

        /// <summary>Records an event and applies its value when it is eligible to score.</summary>
        public void Record(ScoreEvent scoreEvent)
        {
            var alreadyHappened = CountOf(scoreEvent) > 0;
            m_Counts[scoreEvent] = CountOf(scoreEvent) + 1;

            var value = ValueOf(scoreEvent);
            var isReward = value > 0;
            if (isReward && alreadyHappened)
                return;

            Total += value;
            ScoreChanged?.Invoke(Total);
        }

        /// <summary>Clears the run so the scenario can be replayed.</summary>
        public void Reset()
        {
            m_Counts.Clear();
            Total = 0;
            ScoreChanged?.Invoke(Total);
        }
    }
}
