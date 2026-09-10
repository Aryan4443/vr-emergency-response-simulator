using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// Trigger volume marking an unsafe area, per section 9. Entering it penalises the run and
    /// puts the scenario into Warning; leaving it clears the warning.
    ///
    /// Attach to an object with a trigger collider. The visual and audio warning are driven by
    /// the scenario events, so a hazard is never signalled by colour alone (section 13).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("VR Sim/Hazard Zone")]
    public class HazardZone : ScenarioComponent
    {
        [Header("Hazard")]
        [Tooltip("Identifier written to the action log, for example HazardZone_A.")]
        [SerializeField] string m_ZoneId = "HazardZone_A";

        /// <summary>Identifier written to the action log.</summary>
        public string ZoneId
        {
            get => m_ZoneId;
            set => m_ZoneId = value;
        }

        void Reset()
        {
            // A hazard is an area to walk into, never something to collide with.
            GetComponent<Collider>().isTrigger = true;
            m_ZoneId = name;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            Session?.EnterHazard(m_ZoneId);
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            Session?.LeaveHazard(m_ZoneId);
        }
    }
}
