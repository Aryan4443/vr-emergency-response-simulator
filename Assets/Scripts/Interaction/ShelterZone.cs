using UnityEngine;
using VRSim.Evaluation;
using VRSim.Scenario;

namespace VRSim.Interaction
{
    /// <summary>
    /// The space under a desk, where the user is meant to take cover during an earthquake.
    ///
    /// Two separate things are being taught here, so they are two separate objectives: getting
    /// under cover quickly, and staying there until the shaking stops. Standing up between
    /// tremors is the mistake that actually injures people, so leaving early does not complete
    /// the second objective.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("VR Sim/Shelter Zone")]
    public class ShelterZone : ScenarioComponent
    {
        [Header("Shelter")]
        [SerializeField] string m_ShelterId = "Shelter_A";

        [Tooltip("Objective completed on getting under cover while the ground is moving.")]
        [SerializeField] string m_TakeCoverObjectiveId = "drop_cover_hold";

        [Tooltip("Objective completed by still being under cover when the shaking ends.")]
        [SerializeField] string m_StayUnderCoverObjectiveId = "wait_for_shaking";

        [SerializeField] EarthquakeController m_Earthquake;

        /// <summary>True while the user is under this shelter.</summary>
        public bool IsOccupied { get; private set; }

        public string ShelterId
        {
            get => m_ShelterId;
            set => m_ShelterId = value;
        }

        public EarthquakeController Earthquake
        {
            get => m_Earthquake;
            set => m_Earthquake = value;
        }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            m_ShelterId = name;
        }

        void Awake() => m_Earthquake ??= FindAnyObjectByType<EarthquakeController>();

        void OnEnable()
        {
            if (m_Earthquake != null)
                m_Earthquake.ShakeEnded += OnShakeEnded;
        }

        void OnDisable()
        {
            if (m_Earthquake != null)
                m_Earthquake.ShakeEnded -= OnShakeEnded;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            IsOccupied = true;

            // Taking cover only counts as a response while there is something to respond to.
            if (m_Earthquake == null || !m_Earthquake.IsShaking)
                return;

            Session?.RecordScenarioAction("took_cover", m_ShelterId, ScoreEvent.HazardIdentified,
                successful: true);
            Session?.CompleteObjective(m_TakeCoverObjectiveId);
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            IsOccupied = false;

            // Leaving mid-tremor is the unsafe action this scenario exists to discourage.
            if (m_Earthquake != null && m_Earthquake.IsShaking)
            {
                Session?.RecordScenarioAction("left_cover_during_shaking", m_ShelterId,
                    ScoreEvent.UnsafeAreaEntered, successful: false);
            }
        }

        void OnShakeEnded()
        {
            if (IsOccupied)
                Session?.CompleteObjective(m_StayUnderCoverObjectiveId);
        }
    }
}
