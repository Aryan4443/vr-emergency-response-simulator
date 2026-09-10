using System;

namespace VRSim.Scenario.Hazards
{
    /// <summary>
    /// The severity of a hazard at a point, on a nought-to-one scale, rising over time and falling
    /// off with distance from the origin. Driven by explicit ticks so the curve can be tested
    /// without running the player loop.
    ///
    /// This is deliberately separate from <see cref="SpreadingHazard"/>: that model answers where
    /// the hazard is, this one answers how bad it is once you are in it. The scene layer will
    /// multiply the two, using the intensity to drive smoke density and to decide whether a stretch
    /// of corridor is still survivable, while the spread decides where any of that applies at all.
    /// </summary>
    public class HazardIntensity
    {
        readonly float m_RiseSeconds;
        readonly float m_FalloffDistance;
        readonly HazardIntensityCurve m_Curve;
        readonly float m_PeakIntensity;
        readonly float m_SurvivableThreshold;

        /// <param name="riseSeconds">
        /// Seconds taken to climb from nothing to <paramref name="peakIntensity"/>. Zero or less
        /// means the hazard is already at its peak from the outset, with no climb at all.
        /// </param>
        /// <param name="falloffDistance">
        /// Distance in metres at which the intensity has fallen away to nothing. Zero or less means
        /// the hazard is uniform and does not fall off at all, which suits a whole room filling
        /// with smoke rather than a seat of fire.
        /// </param>
        /// <param name="curve">The shape of the climb over time.</param>
        /// <param name="peakIntensity">
        /// The highest intensity ever reached, clamped to nought-to-one. Below one for a hazard
        /// that is unpleasant but never lethal.
        /// </param>
        /// <param name="survivableThreshold">
        /// The intensity at and above which a position counts as no longer survivable, clamped to
        /// nought-to-one. See <see cref="IsSurvivableAt"/>.
        /// </param>
        public HazardIntensity(
            float riseSeconds,
            float falloffDistance,
            HazardIntensityCurve curve = HazardIntensityCurve.Linear,
            float peakIntensity = 1f,
            float survivableThreshold = 0.75f)
        {
            m_RiseSeconds = Math.Max(0f, riseSeconds);
            m_FalloffDistance = Math.Max(0f, falloffDistance);
            m_Curve = curve;
            m_PeakIntensity = Clamp01(peakIntensity);
            m_SurvivableThreshold = Clamp01(survivableThreshold);
        }

        /// <summary>Seconds taken to climb from nothing to <see cref="PeakIntensity"/>.</summary>
        public float RiseSeconds => m_RiseSeconds;

        /// <summary>Distance in metres at which the intensity has fallen away to nothing.</summary>
        public float FalloffDistance => m_FalloffDistance;

        /// <summary>The shape of the climb over time.</summary>
        public HazardIntensityCurve Curve => m_Curve;

        /// <summary>The highest intensity this hazard ever reaches.</summary>
        public float PeakIntensity => m_PeakIntensity;

        /// <summary>The intensity at and above which a position is no longer survivable.</summary>
        public float SurvivableThreshold => m_SurvivableThreshold;

        /// <summary>Seconds of ticks accumulated since construction or the last reset.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>
        /// The intensity at the origin, where the hazard is at its worst. Equivalent to
        /// <c>IntensityAt(0f)</c>.
        /// </summary>
        public float CurrentIntensity => m_PeakIntensity * ShapedProgress;

        /// <summary>True once the hazard has climbed as far as it ever will.</summary>
        public bool IsAtPeak => ElapsedSeconds >= m_RiseSeconds;

        /// <summary>
        /// How far through the climb the hazard is, from nought to one, before the curve is
        /// applied. Kept public because a countdown bar wants the raw progress, not the shaped one.
        /// </summary>
        public float Progress => m_RiseSeconds <= 0f ? 1f : Clamp01(ElapsedSeconds / m_RiseSeconds);

        float ShapedProgress => Shape(Progress, m_Curve);

        /// <summary>
        /// The intensity a given distance from the origin, from nought to one. The sign of
        /// <paramref name="distanceFromOrigin"/> is ignored, so either side of the origin reads the
        /// same. Falls off linearly with distance and is nought at or beyond
        /// <see cref="FalloffDistance"/>.
        /// </summary>
        public float IntensityAt(float distanceFromOrigin)
        {
            var distance = Math.Abs(distanceFromOrigin);

            // A hazard with no stated falloff distance is uniform: it is exactly as bad everywhere.
            if (m_FalloffDistance <= 0f)
                return CurrentIntensity;

            if (distance >= m_FalloffDistance)
                return 0f;

            var falloff = 1f - (distance / m_FalloffDistance);
            return CurrentIntensity * falloff;
        }

        /// <summary>
        /// True while a position is still survivable, which the scenario uses to decide whether
        /// standing there merely costs points or ends the run. Compared with a threshold rather
        /// than a hard boundary so that the same model can express thick-but-passable smoke.
        /// </summary>
        public bool IsSurvivableAt(float distanceFromOrigin) =>
            IntensityAt(distanceFromOrigin) < m_SurvivableThreshold;

        /// <summary>Advances the climb. Non-positive deltas are ignored: a hazard never cools.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return;

            ElapsedSeconds += deltaSeconds;
        }

        /// <summary>Returns the hazard to nothing so the scenario can be replayed.</summary>
        public void Reset()
        {
            ElapsedSeconds = 0f;
        }

        static float Shape(float progress, HazardIntensityCurve curve)
        {
            switch (curve)
            {
                // Squaring gives the slow start and accelerating finish of a fire taking hold,
                // while still passing through nought and one so the peak is unchanged.
                case HazardIntensityCurve.EaseIn:
                    return progress * progress;
                default:
                    return progress;
            }
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }
    }
}
