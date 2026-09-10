using UnityEngine;
using UnityEngine.InputSystem;
using VRSim.Scenario;

namespace VRSim.UI
{
    /// <summary>
    /// The pause menu and immediate exit option required by section 13.
    ///
    /// The panel is placed in front of the user when it opens rather than being locked to the
    /// head, so nothing moves the camera for them. Opening it suspends the run in place: the
    /// clock stops and interactions are ignored until it closes, so a user who needs a break
    /// never loses progress or picks up a penalty for standing still.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Sim/Pause Menu")]
    public class PauseMenu : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject m_PausePanel;
        [SerializeField] GameObject m_AccessibilityPanel;

        [Header("Scenario")]
        [SerializeField] ScenarioManager m_ScenarioManager;
        [SerializeField] ScenarioHud m_Hud;

        [Header("Placement")]
        [Tooltip("How far in front of the user the panel appears, in metres.")]
        [SerializeField] float m_Distance = 1.8f;

        [Header("Keys")]
        [SerializeField] Key m_ToggleKey = Key.Escape;

        Transform m_Camera;

        /// <summary>True while the pause menu is showing.</summary>
        public bool IsOpen => m_PausePanel != null && m_PausePanel.activeSelf;

        void Awake()
        {
            m_ScenarioManager ??= FindAnyObjectByType<ScenarioManager>();
            m_Hud ??= FindAnyObjectByType<ScenarioHud>();

            if (m_PausePanel != null) m_PausePanel.SetActive(false);
            if (m_AccessibilityPanel != null) m_AccessibilityPanel.SetActive(false);
        }

        void Start() => m_Camera = Camera.main != null ? Camera.main.transform : null;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[m_ToggleKey].wasPressedThisFrame)
                Toggle();
        }

        /// <summary>Opens the menu when closed and closes it when open.</summary>
        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (m_PausePanel == null)
                return;

            m_ScenarioManager?.Session.Pause();
            PlaceInFrontOfUser(m_PausePanel.transform);
            m_PausePanel.SetActive(true);
        }

        /// <summary>Closes the menu and continues the run from where it stopped.</summary>
        public void Close()
        {
            if (m_PausePanel != null) m_PausePanel.SetActive(false);
            if (m_AccessibilityPanel != null) m_AccessibilityPanel.SetActive(false);

            m_ScenarioManager?.Session.Resume();
        }

        /// <summary>Restarts the drill from the briefing, discarding the current run.</summary>
        public void RestartDrill()
        {
            Close();
            m_ScenarioManager?.Session.Abandon();
            m_Hud?.ShowBriefing();
        }

        /// <summary>Swaps to the comfort and accessibility options.</summary>
        public void ShowAccessibility()
        {
            if (m_AccessibilityPanel == null)
                return;

            PlaceInFrontOfUser(m_AccessibilityPanel.transform);
            m_AccessibilityPanel.SetActive(true);

            if (m_PausePanel != null)
                m_PausePanel.SetActive(false);
        }

        /// <summary>Returns from the settings to the pause menu.</summary>
        public void BackToPause()
        {
            if (m_AccessibilityPanel != null) m_AccessibilityPanel.SetActive(false);
            if (m_PausePanel == null)
                return;

            PlaceInFrontOfUser(m_PausePanel.transform);
            m_PausePanel.SetActive(true);
        }

        /// <summary>
        /// Leaves the drill entirely. In the editor this stops play mode; in a build it quits.
        /// Section 13 asks for an exit that is always available and takes effect immediately.
        /// </summary>
        public void ExitSimulation()
        {
            m_ScenarioManager?.Session.Abandon();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void PlaceInFrontOfUser(Transform panel)
        {
            if (m_Camera == null)
                m_Camera = Camera.main != null ? Camera.main.transform : null;

            if (m_Camera == null)
                return;

            // Level with the eyes and facing the user, but not parented to the head.
            var forward = m_Camera.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();
            panel.position = m_Camera.position + forward * m_Distance;
            panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
