using UnityEngine;
using VRSim.Scenario;

namespace VRSim.Interaction
{
    /// <summary>
    /// Shared base for scene objects that report interactions to the scenario. Resolves the
    /// <see cref="ScenarioManager"/> from the inspector reference when one is set, and otherwise
    /// finds the one in the scene.
    /// </summary>
    public abstract class ScenarioComponent : MonoBehaviour
    {
        [Header("Scenario")]
        [Tooltip("Leave empty to use the Scenario Manager found in the scene.")]
        [SerializeField] protected ScenarioManager m_ScenarioManager;

        /// <summary>
        /// The scenario this object reports to. Falls back to the one in the scene when the
        /// reference is left empty, and can be set explicitly so a test or a scene holding more
        /// than one scenario is never ambiguous.
        /// </summary>
        public ScenarioManager ScenarioManager
        {
            get
            {
                if (m_ScenarioManager == null)
                    m_ScenarioManager = FindAnyObjectByType<ScenarioManager>();

                return m_ScenarioManager;
            }
            set => m_ScenarioManager = value;
        }

        protected ScenarioManager Manager => ScenarioManager;

        protected ScenarioSession Session => Manager != null ? Manager.Session : null;

        /// <summary>True when the collider belongs to the player rig.</summary>
        protected static bool IsPlayer(Component other) => other.CompareTag("Player");
    }
}
