using System;

namespace VRSim.Evaluation
{
    /// <summary>
    /// Scenario clock from section 7. Driven by explicit ticks so the timing rules can be
    /// tested without running the player loop.
    ///
    /// An optional limit supports the Failed state in section 8: exceeding it stops the clock
    /// and raises <see cref="Expired"/> exactly once.
    /// </summary>
    public class ScenarioTimer
    {
        readonly float m_LimitSeconds;

        /// <param name="limitSeconds">
        /// Time limit in seconds. Zero or less means the scenario is untimed.
        /// </param>
        public ScenarioTimer(float limitSeconds = 0f)
        {
            m_LimitSeconds = limitSeconds;
        }

        /// <summary>Raised the first tick on which the limit is reached.</summary>
        public event Action Expired;

        public bool IsRunning { get; private set; }

        public float ElapsedSeconds { get; private set; }

        public bool HasExpired { get; private set; }

        /// <summary>Seconds left before the limit, clamped at zero. Zero when untimed.</summary>
        public float RemainingSeconds =>
            HasLimit ? Math.Max(0f, m_LimitSeconds - ElapsedSeconds) : 0f;

        bool HasLimit => m_LimitSeconds > 0f;

        /// <summary>Begins a fresh run from zero.</summary>
        public void Start()
        {
            ElapsedSeconds = 0f;
            HasExpired = false;
            IsRunning = true;
        }

        /// <summary>Holds the elapsed time in place, for the pause menu.</summary>
        public void Pause() => IsRunning = false;

        /// <summary>Continues from the held time.</summary>
        public void Resume() => IsRunning = true;

        /// <summary>Stops the clock and clears the elapsed time for a replay.</summary>
        public void Reset()
        {
            IsRunning = false;
            HasExpired = false;
            ElapsedSeconds = 0f;
        }

        /// <summary>Advances the clock. Ignored unless the timer is running.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsRunning)
                return;

            ElapsedSeconds += deltaSeconds;

            if (!HasLimit || HasExpired || ElapsedSeconds < m_LimitSeconds)
                return;

            HasExpired = true;
            IsRunning = false;
            Expired?.Invoke();
        }
    }
}
