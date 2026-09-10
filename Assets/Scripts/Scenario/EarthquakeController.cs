using System;
using UnityEngine;
using VRSim.Scenario.Hazards;

namespace VRSim.Scenario
{
    /// <summary>
    /// Runs the shaking for the earthquake scenario.
    ///
    /// The environment moves, not the player. Section 13 rules out forced camera movement, and
    /// shaking someone's viewpoint in a headset is one of the most reliable ways to make them
    /// motion sick. Displacing the building around a stationary viewer conveys the same event and
    /// stays comfortable, which is why the shake is applied to a scene transform rather than to
    /// the rig.
    ///
    /// The schedule is deterministic for a given seed so an instructor and a trainee reviewing the
    /// same drill see the same sequence of tremors.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Sim/Earthquake Controller")]
    public class EarthquakeController : MonoBehaviour
    {
        [Header("Schedule")]
        [Tooltip("Same seed gives the same drill, so runs stay comparable.")]
        [SerializeField] int m_Seed = 20260910;

        [SerializeField] int m_ShockCount = 4;
        [SerializeField] float m_FirstShockDelaySeconds = 6f;

        [Header("Shake")]
        [Tooltip("Transform displaced while the ground shakes. Never the player rig.")]
        [SerializeField] Transform m_Building;

        [Tooltip("Maximum displacement in metres at full magnitude.")]
        [SerializeField] float m_MaximumOffset = 0.09f;

        [Tooltip("How fast the building oscillates, in hertz.")]
        [SerializeField] float m_Frequency = 7f;

        [Header("Scenario")]
        [SerializeField] ScenarioManager m_ScenarioManager;

        AftershockSchedule m_Schedule;
        Vector3 m_BuildingRest;
        bool m_HasRest;

        /// <summary>Raised when a tremor starts, with its magnitude between 0 and 1.</summary>
        public event Action<float> ShakeStarted;

        /// <summary>Raised when a tremor ends.</summary>
        public event Action ShakeEnded;

        /// <summary>True while the ground is moving.</summary>
        public bool IsShaking => m_Schedule != null && m_Schedule.IsShaking;

        /// <summary>Severity of the current tremor, 0 when still.</summary>
        public float CurrentMagnitude => m_Schedule?.CurrentMagnitude ?? 0f;

        /// <summary>True once every scheduled tremor has finished.</summary>
        public bool IsComplete => m_Schedule != null && m_Schedule.IsComplete;

        public AftershockSchedule Schedule => m_Schedule;

        void Awake()
        {
            m_ScenarioManager ??= FindAnyObjectByType<ScenarioManager>();
            BuildSchedule();

            if (m_Building != null)
            {
                m_BuildingRest = m_Building.position;
                m_HasRest = true;
            }
        }

        void BuildSchedule()
        {
            m_Schedule = new AftershockSchedule(m_Seed, m_ShockCount, m_FirstShockDelaySeconds);
            m_Schedule.ShakeStarted += shock => ShakeStarted?.Invoke(shock.Magnitude);
            m_Schedule.ShakeEnded += _ => ShakeEnded?.Invoke();
        }

        void OnEnable()
        {
            if (m_ScenarioManager != null)
                m_ScenarioManager.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            if (m_ScenarioManager != null)
                m_ScenarioManager.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(ScenarioState previous, ScenarioState next)
        {
            // A replay has to start from an unshaken building and the same tremor sequence.
            if (next == ScenarioState.Ready)
            {
                m_Schedule.Reset();
                RestoreRest();
            }
        }

        void Update()
        {
            if (m_ScenarioManager == null || m_Schedule == null)
                return;

            // Paused or finished runs must not keep shaking the room.
            var running = m_ScenarioManager.State is ScenarioState.Active or ScenarioState.Warning
                          && !m_ScenarioManager.Session.IsPaused;

            if (!running)
            {
                RestoreRest();
                return;
            }

            m_Schedule.Tick(Time.deltaTime);
            ApplyShake();
        }

        void ApplyShake()
        {
            if (m_Building == null || !m_HasRest)
                return;

            if (!m_Schedule.IsShaking)
            {
                RestoreRest();
                return;
            }

            // Two frequencies that do not divide evenly keep the motion from looking like a loop.
            var t = Time.time * m_Frequency * Mathf.PI * 2f;
            var amplitude = m_MaximumOffset * m_Schedule.CurrentMagnitude;

            var offset = new Vector3(
                Mathf.Sin(t) * amplitude,
                Mathf.Sin(t * 1.7f) * amplitude * 0.35f,
                Mathf.Cos(t * 1.3f) * amplitude);

            m_Building.position = m_BuildingRest + offset;
        }

        void RestoreRest()
        {
            if (m_Building != null && m_HasRest)
                m_Building.position = m_BuildingRest;
        }

        /// <summary>The transform displaced while shaking. Assigned by the level builder.</summary>
        public Transform Building
        {
            get => m_Building;
            set
            {
                m_Building = value;
                if (m_Building == null)
                    return;

                m_BuildingRest = m_Building.position;
                m_HasRest = true;
            }
        }
    }
}
