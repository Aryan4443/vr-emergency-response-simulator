using System;
using System.Collections.Generic;

namespace VRSim.Evaluation
{
    /// <summary>
    /// Anonymous record of one scenario run, matching the ScenarioResult shape in section 11.
    ///
    /// Deliberately carries nothing that identifies a person or a device: the prototype stores
    /// results locally and only describes what happened during the run.
    /// </summary>
    [Serializable]
    public class ScenarioResult
    {
        public string scenarioId;
        public bool completed;
        public float completionTimeSeconds;
        public int unsafeZoneEntries;
        public int incorrectActions;
        public string selectedExit;
        public int score;

        /// <summary>
        /// When the run was recorded, as UTC ticks. Anything comparing progress across sessions
        /// needs to order runs, and a file name cannot be trusted for that: two runs saved inside
        /// the same millisecond produce names that sort by their random suffix. Carrying the time
        /// in the record itself also makes a stored result self-describing.
        ///
        /// Zero means the run predates this field.
        /// </summary>
        public long recordedAtUtcTicks;

        public List<ActionEvent> events = new List<ActionEvent>();
    }
}
