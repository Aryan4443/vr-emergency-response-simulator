using System;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace VRSim.Accessibility
{
    /// <summary>
    /// Applies the comfort and accessibility options from section 13 to the rig and stores them
    /// between sessions, matching AccessibilityManager in section 7.
    ///
    /// Settings are stored as JSON next to the scenario results rather than in PlayerPrefs, so
    /// the file can be inspected and deleted during testing.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR Sim/Accessibility Manager")]
    public class AccessibilityManager : MonoBehaviour
    {
        const string FileName = "accessibility.json";

        [Header("Rig")]
        [Tooltip("Leave empty to use the XR Origin found in the scene.")]
        [SerializeField] XROrigin m_Origin;

        [Header("Persistence")]
        [Tooltip("Load saved settings on start and save them whenever they change.")]
        [SerializeField] bool m_Persist = true;

        AccessibilitySettings m_Settings;
        string m_Path;

        /// <summary>Raised after the settings are applied, for UI text to resize and recolour.</summary>
        public event Action<AccessibilitySettings> SettingsApplied;

        /// <summary>The live settings. Changing a property applies and stores it immediately.</summary>
        public AccessibilitySettings Settings
        {
            get
            {
                if (m_Settings == null)
                    LoadSettings();

                return m_Settings;
            }
        }

        void Awake()
        {
            if (m_Settings == null)
                LoadSettings();
        }

        void Start() => Apply();

        void LoadSettings()
        {
            m_Path = Path.Combine(Application.persistentDataPath, FileName);

            if (m_Persist && File.Exists(m_Path))
            {
                try
                {
                    m_Settings = AccessibilitySettings.FromJson(File.ReadAllText(m_Path));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not read accessibility settings: {exception.Message}", this);
                    m_Settings = new AccessibilitySettings();
                }
            }
            else
            {
                m_Settings = new AccessibilitySettings();
            }

            m_Settings.Changed += OnSettingsChanged;
        }

        void OnSettingsChanged()
        {
            Apply();
            Save();
        }

        /// <summary>Pushes the current settings onto the rig and notifies listeners.</summary>
        public void Apply()
        {
            var settings = Settings;

            if (m_Origin == null)
                m_Origin = FindAnyObjectByType<XROrigin>();

            if (m_Origin != null)
            {
                // Seated mode changes the tracking origin rather than moving the camera for the
                // user, because section 13 rules out forced camera movement.
                m_Origin.RequestedTrackingOriginMode = settings.SeatedMode
                    ? XROrigin.TrackingOriginMode.Device
                    : XROrigin.TrackingOriginMode.Floor;
                m_Origin.CameraYOffset = settings.EyeHeight;
            }

            foreach (var provider in FindObjectsByType<SnapTurnProvider>(FindObjectsSortMode.None))
            {
                provider.enabled = settings.SnapTurnEnabled;
                provider.turnAmount = settings.SnapTurnDegrees;
            }

            // Smooth turning is the discomfort risk, so it is only available when the user has
            // deliberately turned snap turning off.
            foreach (var provider in FindObjectsByType<ContinuousTurnProvider>(FindObjectsSortMode.None))
                provider.enabled = !settings.SnapTurnEnabled;

            foreach (var provider in FindObjectsByType<ContinuousMoveProvider>(FindObjectsSortMode.None))
                provider.moveSpeed = settings.MovementSpeed;

            SettingsApplied?.Invoke(settings);
        }

        /// <summary>Writes the settings to disk. Failures are logged, never thrown.</summary>
        public void Save()
        {
            if (!m_Persist)
                return;

            try
            {
                m_Path ??= Path.Combine(Application.persistentDataPath, FileName);
                File.WriteAllText(m_Path, Settings.ToJson());
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not store accessibility settings: {exception.Message}", this);
            }
        }

        // Convenience entry points for UI toggles and sliders.
        public void SetSeatedMode(bool seated) => Settings.SeatedMode = seated;
        public void SetSubtitles(bool enabled) => Settings.SubtitlesEnabled = enabled;
        public void SetSnapTurn(bool enabled) => Settings.SnapTurnEnabled = enabled;
        public void SetTextScale(float scale) => Settings.TextScale = scale;
        public void SetHighContrast(bool enabled) => Settings.HighContrast = enabled;
        public void SetMovementSpeed(float speed) => Settings.MovementSpeed = speed;
    }
}
