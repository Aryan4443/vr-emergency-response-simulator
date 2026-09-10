namespace VRSim.Scenario.Hazards
{
    /// <summary>
    /// One shake in an earthquake scenario: when it starts, how long it lasts and how hard it is.
    /// A value type, because a shock is a plain reading from a schedule and never changes once the
    /// schedule has been generated.
    /// </summary>
    public readonly struct Aftershock
    {
        public Aftershock(int index, float startSeconds, float durationSeconds, float magnitude)
        {
            Index = index;
            StartSeconds = startSeconds;
            DurationSeconds = durationSeconds;
            Magnitude = magnitude;
        }

        /// <summary>Nought-based position in the schedule, for the action log and the HUD.</summary>
        public int Index { get; }

        /// <summary>Seconds from the start of the scenario at which the shake begins.</summary>
        public float StartSeconds { get; }

        /// <summary>How long the shake lasts, in seconds.</summary>
        public float DurationSeconds { get; }

        /// <summary>How hard the shake is, from nought to one.</summary>
        public float Magnitude { get; }

        /// <summary>Seconds from the start of the scenario at which the shake finishes.</summary>
        public float EndSeconds => StartSeconds + DurationSeconds;
    }
}
