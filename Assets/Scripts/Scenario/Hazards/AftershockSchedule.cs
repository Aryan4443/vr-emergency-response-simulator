using System;
using System.Collections.Generic;

namespace VRSim.Scenario.Hazards
{
    /// <summary>
    /// The sequence of shakes in an earthquake scenario. The whole schedule is generated up front
    /// from a seed, and is then played back by explicit ticks so the timing rules can be tested
    /// without running the player loop.
    ///
    /// Why determinism matters: the specification asks for a trainee's performance to be compared
    /// across repeated sessions, and for the same drill to be run by different people and marked
    /// against each other. Neither comparison means anything if one run happened to get its second
    /// aftershock while the trainee was still in the stairwell and another did not. Seeding the
    /// schedule makes a run reproducible: an instructor can hand out the same seed to a whole
    /// cohort, replay a recorded session and see the same events, and vary the seed deliberately
    /// when the point of the exercise is that the trainee should not be able to memorise it.
    ///
    /// The generator is a small xorshift written out here rather than <c>System.Random</c>, whose
    /// output for a given seed is only promised to be stable within one runtime. A trainee running
    /// the drill on a headset and an instructor reviewing it in the editor must get the same
    /// schedule from the same seed, so the generator has to be part of this class.
    /// </summary>
    public class AftershockSchedule
    {
        readonly List<Aftershock> m_Shocks = new List<Aftershock>();
        readonly int m_Seed;

        uint m_State;
        int m_NextIndex;
        int m_ActiveIndex = -1;

        /// <param name="seed">Seed for the schedule. The same seed always gives the same shocks.</param>
        /// <param name="shockCount">How many shakes the scenario contains.</param>
        /// <param name="firstShockDelaySeconds">
        /// Quiet time before the first shake, giving the trainee a chance to get their bearings
        /// before the room starts moving.
        /// </param>
        /// <param name="minimumGapSeconds">Shortest quiet period between two shakes.</param>
        /// <param name="maximumGapSeconds">Longest quiet period between two shakes.</param>
        /// <param name="minimumDurationSeconds">Shortest a shake lasts.</param>
        /// <param name="maximumDurationSeconds">Longest a shake lasts.</param>
        /// <param name="minimumMagnitude">Weakest shake, clamped to nought-to-one.</param>
        /// <param name="maximumMagnitude">Strongest shake, clamped to nought-to-one.</param>
        public AftershockSchedule(
            int seed,
            int shockCount = 4,
            float firstShockDelaySeconds = 15f,
            float minimumGapSeconds = 20f,
            float maximumGapSeconds = 45f,
            float minimumDurationSeconds = 3f,
            float maximumDurationSeconds = 8f,
            float minimumMagnitude = 0.25f,
            float maximumMagnitude = 1f)
        {
            m_Seed = seed;
            m_State = ToState(seed);

            var count = Math.Max(0, shockCount);
            var firstDelay = Math.Max(0f, firstShockDelaySeconds);
            var minGap = Math.Max(0f, minimumGapSeconds);
            var maxGap = Math.Max(minGap, maximumGapSeconds);
            var minDuration = Math.Max(0f, minimumDurationSeconds);
            var maxDuration = Math.Max(minDuration, maximumDurationSeconds);
            var minMagnitude = Clamp01(minimumMagnitude);
            var maxMagnitude = Math.Max(minMagnitude, Clamp01(maximumMagnitude));

            Generate(count, firstDelay, minGap, maxGap, minDuration, maxDuration, minMagnitude, maxMagnitude);
        }

        /// <summary>Raised on the tick a shake begins, carrying the shock that has started.</summary>
        public event Action<Aftershock> ShakeStarted;

        /// <summary>Raised on the tick a shake finishes, carrying the shock that has ended.</summary>
        public event Action<Aftershock> ShakeEnded;

        /// <summary>The seed this schedule was generated from, so a run can be written to the report.</summary>
        public int Seed => m_Seed;

        /// <summary>Every shake in the scenario, in chronological order and never overlapping.</summary>
        public IReadOnlyList<Aftershock> Shocks => m_Shocks;

        /// <summary>Seconds of ticks accumulated since construction or the last reset.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>True while a shake is in progress.</summary>
        public bool IsShaking { get; private set; }

        /// <summary>
        /// How hard the room is shaking right now, from nought to one, and nought when still. Held
        /// flat for the length of a shock: shaping the movement within one shake is the scene
        /// layer's job, so that the model stays comparable between runs.
        /// </summary>
        public float CurrentMagnitude { get; private set; }

        /// <summary>True once every shake has been and gone.</summary>
        public bool IsComplete => !IsShaking && m_NextIndex >= m_Shocks.Count;

        /// <summary>
        /// Seconds from the start of the scenario at which the last shake finishes, or nought when
        /// there are no shakes. Useful for choosing a sensible scenario time limit.
        /// </summary>
        public float TotalDurationSeconds =>
            m_Shocks.Count == 0 ? 0f : m_Shocks[m_Shocks.Count - 1].EndSeconds;

        /// <summary>
        /// Advances the schedule and raises the events any shake boundaries crossed by this tick.
        /// A single long tick may span a whole shock, so starts and ends are drained in
        /// chronological order rather than one per tick. Non-positive deltas are ignored, because
        /// running the clock backwards would replay events that have already fired.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return;

            ElapsedSeconds += deltaSeconds;

            while (true)
            {
                if (IsShaking)
                {
                    var active = m_Shocks[m_ActiveIndex];
                    if (ElapsedSeconds < active.EndSeconds)
                        return;

                    IsShaking = false;
                    CurrentMagnitude = 0f;
                    m_ActiveIndex = -1;
                    ShakeEnded?.Invoke(active);
                    continue;
                }

                if (m_NextIndex >= m_Shocks.Count)
                    return;

                var next = m_Shocks[m_NextIndex];
                if (ElapsedSeconds < next.StartSeconds)
                    return;

                m_ActiveIndex = m_NextIndex;
                m_NextIndex++;
                IsShaking = true;
                CurrentMagnitude = next.Magnitude;
                ShakeStarted?.Invoke(next);
            }
        }

        /// <summary>
        /// Rewinds to the start of the scenario. The generated shocks are deliberately kept, so a
        /// replay is the same drill again rather than a new one; a different drill means a new
        /// schedule with a different seed.
        /// </summary>
        public void Reset()
        {
            ElapsedSeconds = 0f;
            IsShaking = false;
            CurrentMagnitude = 0f;
            m_NextIndex = 0;
            m_ActiveIndex = -1;
        }

        void Generate(
            int count,
            float firstDelay,
            float minGap,
            float maxGap,
            float minDuration,
            float maxDuration,
            float minMagnitude,
            float maxMagnitude)
        {
            var time = firstDelay;

            for (var i = 0; i < count; i++)
            {
                // The gap is measured from the end of the previous shock, which is what keeps the
                // shocks from ever overlapping and lets Tick treat them as a simple queue.
                if (i > 0)
                    time += NextInRange(minGap, maxGap);

                var duration = NextInRange(minDuration, maxDuration);
                var magnitude = NextInRange(minMagnitude, maxMagnitude);

                m_Shocks.Add(new Aftershock(i, time, duration, magnitude));

                time += duration;
            }
        }

        float NextInRange(float minimum, float maximum) =>
            minimum + (NextUnitValue() * (maximum - minimum));

        /// <summary>A value in the half-open range nought to one, from the seeded generator.</summary>
        float NextUnitValue()
        {
            // Xorshift32. Cheap, has no platform-specific behaviour, and is more than good enough
            // for scattering a handful of shakes across a few minutes.
            m_State ^= m_State << 13;
            m_State ^= m_State >> 17;
            m_State ^= m_State << 5;

            // Only the top 24 bits are used, which is exactly the precision a float can hold and
            // avoids the rounding to 1.0 that dividing by uint.MaxValue would allow.
            return (m_State >> 8) * (1f / 16777216f);
        }

        /// <summary>
        /// Turns a seed into generator state.
        ///
        /// The seed is avalanched first. Instructors reach for small, memorable seeds such as 1, 2
        /// and 3, and xorshift started from a value that small dribbles out several near-identical
        /// values before it mixes properly, which would make those seeds produce near-identical
        /// drills. The mixing step is a bijection, so distinct seeds still give distinct states.
        /// Xorshift is also stuck at nought forever if it ever starts there, so that one state is
        /// mapped onto a fixed non-zero constant.
        /// </summary>
        static uint ToState(int seed)
        {
            var state = unchecked((uint)seed);

            unchecked
            {
                state ^= 0x9E3779B9u;
                state *= 0x85EBCA6Bu;
                state ^= state >> 13;
                state *= 0xC2B2AE35u;
                state ^= state >> 16;
            }

            return state == 0u ? 0x9E3779B9u : state;
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }
}
