using System;
using System.Collections.Generic;

namespace VRSim.Scenario.Hazards
{
    /// <summary>
    /// A hazard that grows outwards from an origin along a one-dimensional corridor, with all
    /// positions measured in metres. Driven by explicit ticks so the spreading rules can be tested
    /// without running the player loop, exactly as <c>ScenarioTimer</c> is.
    ///
    /// The corridor is modelled as a number line and the hazard as an interval centred on the
    /// origin, so <see cref="CurrentReach"/> extends in both directions. A hazard that starts at
    /// one end of a corridor is simply one whose watch points all lie on the same side.
    ///
    /// Why a delay exists: the smoke in the fire scenario is currently a fixed trigger volume that
    /// never changes, so the route out is either open or shut for the whole run and nothing the
    /// user does is urgent. A hazard that sits still for a few seconds and only then begins to
    /// close the corridor gives the user a window in which acting early is rewarded and dithering
    /// is punished. That time pressure is the whole point: it is what a static volume cannot
    /// express, and it is what makes two runs of the same scenario comparable on speed rather than
    /// only on route choice.
    /// </summary>
    public class SpreadingHazard
    {
        /// <summary>
        /// Watch points sorted by distance from the origin, so that a single long tick reports
        /// them in the order the hazard would physically have reached them.
        /// </summary>
        readonly List<WatchPoint> m_WatchPoints = new List<WatchPoint>();

        readonly float m_OriginPosition;
        readonly float m_SpreadRate;
        readonly float m_MaximumReach;
        readonly float m_StartDelaySeconds;

        /// <param name="originPosition">Where along the corridor the hazard starts, in metres.</param>
        /// <param name="spreadRate">
        /// How fast the edge of the hazard travels, in metres per second. Negative values are
        /// treated as zero: a hazard never retreats.
        /// </param>
        /// <param name="maximumReach">
        /// The furthest the hazard ever reaches from its origin, in metres. Negative values are
        /// treated as zero, which leaves a hazard occupying only its origin.
        /// </param>
        /// <param name="startDelaySeconds">
        /// Seconds of grace before the hazard begins to spread. Negative values are treated as
        /// zero. See the type remarks for why this matters.
        /// </param>
        public SpreadingHazard(
            float originPosition,
            float spreadRate,
            float maximumReach,
            float startDelaySeconds = 0f)
        {
            m_OriginPosition = originPosition;
            m_SpreadRate = Math.Max(0f, spreadRate);
            m_MaximumReach = Math.Max(0f, maximumReach);
            m_StartDelaySeconds = Math.Max(0f, startDelaySeconds);
        }

        /// <summary>
        /// Raised the first tick on which the hazard covers a registered watch point, carrying the
        /// watched position. Each watch point fires exactly once until <see cref="Reset"/> is
        /// called, so a listener can safely close a door or fail an objective in the handler.
        /// </summary>
        public event Action<float> ReachedWatchPoint;

        /// <summary>Where along the corridor the hazard started, in metres.</summary>
        public float OriginPosition => m_OriginPosition;

        /// <summary>How fast the edge of the hazard travels, in metres per second.</summary>
        public float SpreadRate => m_SpreadRate;

        /// <summary>The furthest the hazard ever reaches from its origin, in metres.</summary>
        public float MaximumReach => m_MaximumReach;

        /// <summary>Seconds of grace before the hazard begins to spread.</summary>
        public float StartDelaySeconds => m_StartDelaySeconds;

        /// <summary>Seconds of ticks accumulated since construction or the last reset.</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>
        /// How far the hazard currently extends either side of its origin, in metres. Never
        /// negative and never above <see cref="MaximumReach"/>.
        /// </summary>
        public float CurrentReach { get; private set; }

        /// <summary>True once the delay has elapsed and the hazard is actually growing.</summary>
        public bool HasStartedSpreading => ElapsedSeconds >= m_StartDelaySeconds;

        /// <summary>
        /// True once the hazard has grown as far as it ever will, so the scene layer can stop
        /// updating its visuals and settle on a final state.
        /// </summary>
        public bool IsAtMaximum => CurrentReach >= m_MaximumReach;

        /// <summary>
        /// Registers a position on the corridor to be watched. <see cref="ReachedWatchPoint"/>
        /// fires for it on the first tick that covers it. Watch points are only evaluated during
        /// <see cref="Tick"/>, so one added to an already-covered stretch of corridor fires on the
        /// next tick rather than immediately. Duplicate positions are ignored, because two
        /// listeners registering the same doorway should not produce two events.
        /// </summary>
        public void AddWatchPoint(float position)
        {
            var distance = Math.Abs(position - m_OriginPosition);

            for (var i = 0; i < m_WatchPoints.Count; i++)
            {
                if (m_WatchPoints[i].Position == position)
                    return;
            }

            var watchPoint = new WatchPoint(position, distance);

            // Insert in ascending distance order so that a tick long enough to swallow several
            // watch points still reports them nearest-first.
            var index = 0;
            while (index < m_WatchPoints.Count && m_WatchPoints[index].Distance <= distance)
                index++;

            m_WatchPoints.Insert(index, watchPoint);
        }

        /// <summary>
        /// True when <paramref name="position"/> lies inside the hazard. The origin is always
        /// inside it, even before the hazard has begun to spread.
        /// </summary>
        public bool Contains(float position) =>
            Math.Abs(position - m_OriginPosition) <= CurrentReach;

        /// <summary>
        /// Advances the hazard. Non-positive deltas are ignored, because rewinding a hazard would
        /// let a watch point be crossed twice.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return;

            ElapsedSeconds += deltaSeconds;

            // Derive the reach from the total elapsed time rather than accumulating it tick by
            // tick, so a long run of small ticks does not drift away from the stated spread rate.
            var spreadingSeconds = Math.Max(0f, ElapsedSeconds - m_StartDelaySeconds);
            CurrentReach = Math.Min(m_MaximumReach, spreadingSeconds * m_SpreadRate);

            RaiseNewlyReachedWatchPoints();
        }

        /// <summary>
        /// Returns the hazard to its starting size and re-arms every watch point, so a scenario
        /// can be replayed from the beginning without rebuilding the hazard.
        /// </summary>
        public void Reset()
        {
            ElapsedSeconds = 0f;
            CurrentReach = 0f;

            for (var i = 0; i < m_WatchPoints.Count; i++)
                m_WatchPoints[i].HasBeenReached = false;
        }

        void RaiseNewlyReachedWatchPoints()
        {
            for (var i = 0; i < m_WatchPoints.Count; i++)
            {
                var watchPoint = m_WatchPoints[i];

                // The list is sorted by distance, so the first one still out of reach means every
                // later one is too.
                if (watchPoint.Distance > CurrentReach)
                    return;

                if (watchPoint.HasBeenReached)
                    continue;

                watchPoint.HasBeenReached = true;
                ReachedWatchPoint?.Invoke(watchPoint.Position);
            }
        }

        /// <summary>A watched position and whether the hazard has already covered it.</summary>
        class WatchPoint
        {
            public WatchPoint(float position, float distance)
            {
                Position = position;
                Distance = distance;
            }

            public float Position { get; }

            public float Distance { get; }

            public bool HasBeenReached { get; set; }
        }
    }
}
