using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRSim.Accessibility;

namespace VRSim.UI
{
    /// <summary>
    /// In-headset controls for the comfort and accessibility options in section 13.
    ///
    /// Settings that exist but cannot be reached are not accessible, so every option the
    /// <see cref="AccessibilityManager"/> supports is exposed here and takes effect immediately,
    /// without leaving the drill.
    /// </summary>
    [AddComponentMenu("VR Sim/Accessibility Panel")]
    public class AccessibilityPanel : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] Toggle m_SeatedToggle;
        [SerializeField] Toggle m_SubtitlesToggle;
        [SerializeField] Toggle m_SnapTurnToggle;
        [SerializeField] Toggle m_HighContrastToggle;
        [SerializeField] Slider m_TextScaleSlider;
        [SerializeField] Slider m_MovementSpeedSlider;

        [Header("Readouts")]
        [SerializeField] TMP_Text m_TextScaleValue;
        [SerializeField] TMP_Text m_MovementSpeedValue;

        [SerializeField] AccessibilityManager m_Manager;

        bool m_Applying;

        void Awake() => m_Manager ??= FindAnyObjectByType<AccessibilityManager>();

        void OnEnable()
        {
            if (m_Manager == null)
                return;

            ShowCurrentValues();

            if (m_SeatedToggle != null) m_SeatedToggle.onValueChanged.AddListener(OnSeatedChanged);
            if (m_SubtitlesToggle != null) m_SubtitlesToggle.onValueChanged.AddListener(OnSubtitlesChanged);
            if (m_SnapTurnToggle != null) m_SnapTurnToggle.onValueChanged.AddListener(OnSnapTurnChanged);
            if (m_HighContrastToggle != null) m_HighContrastToggle.onValueChanged.AddListener(OnContrastChanged);
            if (m_TextScaleSlider != null) m_TextScaleSlider.onValueChanged.AddListener(OnTextScaleChanged);
            if (m_MovementSpeedSlider != null) m_MovementSpeedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }

        void OnDisable()
        {
            if (m_SeatedToggle != null) m_SeatedToggle.onValueChanged.RemoveListener(OnSeatedChanged);
            if (m_SubtitlesToggle != null) m_SubtitlesToggle.onValueChanged.RemoveListener(OnSubtitlesChanged);
            if (m_SnapTurnToggle != null) m_SnapTurnToggle.onValueChanged.RemoveListener(OnSnapTurnChanged);
            if (m_HighContrastToggle != null) m_HighContrastToggle.onValueChanged.RemoveListener(OnContrastChanged);
            if (m_TextScaleSlider != null) m_TextScaleSlider.onValueChanged.RemoveListener(OnTextScaleChanged);
            if (m_MovementSpeedSlider != null) m_MovementSpeedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
        }

        /// <summary>Pushes the stored settings into the controls without re-applying them.</summary>
        public void ShowCurrentValues()
        {
            var settings = m_Manager.Settings;
            m_Applying = true;

            if (m_SeatedToggle != null) m_SeatedToggle.isOn = settings.SeatedMode;
            if (m_SubtitlesToggle != null) m_SubtitlesToggle.isOn = settings.SubtitlesEnabled;
            if (m_SnapTurnToggle != null) m_SnapTurnToggle.isOn = settings.SnapTurnEnabled;
            if (m_HighContrastToggle != null) m_HighContrastToggle.isOn = settings.HighContrast;

            if (m_TextScaleSlider != null)
            {
                m_TextScaleSlider.minValue = AccessibilitySettings.MinTextScale;
                m_TextScaleSlider.maxValue = AccessibilitySettings.MaxTextScale;
                m_TextScaleSlider.value = settings.TextScale;
            }

            if (m_MovementSpeedSlider != null)
            {
                m_MovementSpeedSlider.minValue = AccessibilitySettings.MinMovementSpeed;
                m_MovementSpeedSlider.maxValue = AccessibilitySettings.MaxMovementSpeed;
                m_MovementSpeedSlider.value = settings.MovementSpeed;
            }

            m_Applying = false;
            RefreshReadouts();
        }

        void RefreshReadouts()
        {
            var settings = m_Manager.Settings;

            if (m_TextScaleValue != null)
                m_TextScaleValue.text = $"{settings.TextScale:0.0}x";

            if (m_MovementSpeedValue != null)
                m_MovementSpeedValue.text = $"{settings.MovementSpeed:0.0} m/s";
        }

        // Guarded so populating the controls does not write the values straight back.
        void OnSeatedChanged(bool value) { if (!m_Applying) m_Manager.SetSeatedMode(value); }
        void OnSubtitlesChanged(bool value) { if (!m_Applying) m_Manager.SetSubtitles(value); }
        void OnSnapTurnChanged(bool value) { if (!m_Applying) m_Manager.SetSnapTurn(value); }
        void OnContrastChanged(bool value) { if (!m_Applying) m_Manager.SetHighContrast(value); }

        void OnTextScaleChanged(float value)
        {
            if (m_Applying) return;
            m_Manager.SetTextScale(value);
            RefreshReadouts();
        }

        void OnSpeedChanged(float value)
        {
            if (m_Applying) return;
            m_Manager.SetMovementSpeed(value);
            RefreshReadouts();
        }
    }
}
