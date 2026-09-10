using UnityEngine;
using UnityEngine.XR;

namespace VRSim.Core
{
    /// <summary>
    /// Spawns the XR Device Simulator at runtime when no headset is connected, so the scenario
    /// can be driven from the mouse and keyboard during development.
    ///
    /// The simulator is spawned rather than saved into the scene on purpose. Its on-screen UI
    /// reads live input controls when it starts, which throws in a headless batch run where no
    /// keyboard or mouse is registered. Keeping it out of the scene asset means automated test
    /// and build runs never touch it.
    /// </summary>
    [AddComponentMenu("VR Sim/Desktop Simulator Bootstrap")]
    public class DesktopSimulatorBootstrap : MonoBehaviour
    {
        [Tooltip("XR Device Simulator prefab from the XR Interaction Toolkit samples.")]
        [SerializeField] GameObject m_SimulatorPrefab;

        void Awake()
        {
            if (m_SimulatorPrefab == null)
                return;

            // Never in an automated run, and never when a real headset is driving the rig.
            if (Application.isBatchMode || XRSettings.isDeviceActive)
                return;

            Instantiate(m_SimulatorPrefab).name = m_SimulatorPrefab.name;
        }
    }
}
