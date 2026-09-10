using UnityEngine;
using VRSim.Evaluation;
using VRSim.Scenario;

namespace VRSim.Interaction
{
    /// <summary>
    /// Ground beneath something that comes down in a quake: glazing, tall shelving, masonry.
    ///
    /// Unlike the fire drill's smoke, this is only dangerous while the ground is moving. Standing
    /// by a window between tremors is fine; being there when the next one arrives is not. That
    /// distinction is the lesson, so the penalty is tied to the shaking rather than to the volume.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("VR Sim/Falling Hazard Zone")]
    public class FallingHazardZone : ScenarioComponent
    {
        [Header("Hazard")]
        [SerializeField] string m_ZoneId = "FallingHazard_A";

        [Tooltip("Objective failed by being caught under this during a tremor.")]
        [SerializeField] string m_AvoidObjectiveId = "avoid_falling_hazards";

        [SerializeField] EarthquakeController m_Earthquake;

        bool m_PlayerInside;
        bool m_AlreadyPenalisedThisShock;

        public string ZoneId
        {
            get => m_ZoneId;
            set => m_ZoneId = value;
        }

        public EarthquakeController Earthquake
        {
            get => m_Earthquake;
            set => m_Earthquake = value;
        }

        /// <summary>True while the user is standing under this hazard.</summary>
        public bool IsPlayerInside => m_PlayerInside;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            m_ZoneId = name;
        }

        void Awake() => m_Earthquake ??= FindAnyObjectByType<EarthquakeController>();

        void OnEnable()
        {
            if (m_Earthquake == null)
                return;

            m_Earthquake.ShakeStarted += OnShakeStarted;
            m_Earthquake.ShakeEnded += OnShakeEnded;
        }

        void OnDisable()
        {
            if (m_Earthquake == null)
                return;

            m_Earthquake.ShakeStarted -= OnShakeStarted;
            m_Earthquake.ShakeEnded -= OnShakeEnded;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            m_PlayerInside = true;
            PenaliseIfCaught();
        }

        void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
                m_PlayerInside = false;
        }

        void OnShakeStarted(float magnitude) => PenaliseIfCaught();

        void OnShakeEnded() => m_AlreadyPenalisedThisShock = false;

        /// <summary>
        /// One penalty per tremor, however long the user lingers. Repeating it every frame would
        /// bury the score without teaching anything extra.
        /// </summary>
        void PenaliseIfCaught()
        {
            if (!m_PlayerInside || m_AlreadyPenalisedThisShock)
                return;

            if (m_Earthquake == null || !m_Earthquake.IsShaking)
                return;

            m_AlreadyPenalisedThisShock = true;
            Session?.RecordScenarioAction("caught_under_falling_hazard", m_ZoneId,
                ScoreEvent.UnsafeAreaEntered, successful: false);
        }
    }
}
