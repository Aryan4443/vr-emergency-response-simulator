using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// Trigger volume in front of an exit, per section 9. The correct exit validates the chosen
    /// route and ends the scenario; any other exit is penalised once and the run continues so
    /// the user can recover and find the safe route.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("VR Sim/Exit Zone")]
    public class ExitZone : ScenarioComponent
    {
        [Header("Exit")]
        [Tooltip("Identifier written to the result, for example north_exit.")]
        [SerializeField] string m_ExitId = "north_exit";

        [Tooltip("Tick on the one exit that is the safe evacuation route.")]
        [SerializeField] bool m_IsCorrectExit;

        public string ExitId
        {
            get => m_ExitId;
            set => m_ExitId = value;
        }

        public bool IsCorrectExit
        {
            get => m_IsCorrectExit;
            set => m_IsCorrectExit = value;
        }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            m_ExitId = name;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            Session?.ReachExit(m_ExitId, m_IsCorrectExit);
        }
    }
}
