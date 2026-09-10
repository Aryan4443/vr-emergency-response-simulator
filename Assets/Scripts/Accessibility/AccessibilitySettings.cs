using System;
using UnityEngine;

namespace VRSim.Accessibility
{
    /// <summary>
    /// The comfort and accessibility options required by section 13.
    ///
    /// Every value is clamped on assignment, so a malformed settings file or a stray UI slider can
    /// never leave the user with unreadable text or a movement speed that causes discomfort.
    /// Plain C# with no Unity dependency beyond JSON, so the rules are unit tested directly.
    /// </summary>
    [Serializable]
    public class AccessibilitySettings
    {
        public const float MinTextScale = 0.8f;
        public const float MaxTextScale = 2.5f;
        public const float MinMovementSpeed = 0.5f;
        public const float MaxMovementSpeed = 4f;
        public const float MinSnapTurnDegrees = 15f;
        public const float MaxSnapTurnDegrees = 90f;

        /// <summary>Eye height applied to the rig in seated mode, in metres.</summary>
        public const float SeatedEyeHeight = 1.2f;

        /// <summary>Eye height applied to the rig in standing mode, in metres.</summary>
        public const float StandingEyeHeight = 1.7f;

        [SerializeField] bool m_SeatedMode;
        [SerializeField] bool m_SubtitlesEnabled = true;
        [SerializeField] bool m_SnapTurnEnabled = true;
        [SerializeField] float m_SnapTurnDegrees = 45f;
        [SerializeField] float m_TextScale = 1f;
        [SerializeField] bool m_HighContrast;
        [SerializeField] float m_MovementSpeed = 2f;

        /// <summary>Raised whenever a setting actually changes value.</summary>
        public event Action Changed;

        /// <summary>Seated play, which lowers the rig rather than moving the camera for the user.</summary>
        public bool SeatedMode
        {
            get => m_SeatedMode;
            set => Set(ref m_SeatedMode, value);
        }

        /// <summary>Subtitles for critical audio such as the alarm and spoken warnings.</summary>
        public bool SubtitlesEnabled
        {
            get => m_SubtitlesEnabled;
            set => Set(ref m_SubtitlesEnabled, value);
        }

        /// <summary>Snap turning, which is far more comfortable than smooth turning in VR.</summary>
        public bool SnapTurnEnabled
        {
            get => m_SnapTurnEnabled;
            set => Set(ref m_SnapTurnEnabled, value);
        }

        public float SnapTurnDegrees
        {
            get => m_SnapTurnDegrees;
            set => Set(ref m_SnapTurnDegrees, Mathf.Clamp(value, MinSnapTurnDegrees, MaxSnapTurnDegrees));
        }

        /// <summary>Multiplier applied to every piece of on-screen text.</summary>
        public float TextScale
        {
            get => m_TextScale;
            set => Set(ref m_TextScale, Mathf.Clamp(value, MinTextScale, MaxTextScale));
        }

        /// <summary>High-contrast palette for users who need it.</summary>
        public bool HighContrast
        {
            get => m_HighContrast;
            set => Set(ref m_HighContrast, value);
        }

        /// <summary>Continuous movement speed in metres per second, for users who prefer it.</summary>
        public float MovementSpeed
        {
            get => m_MovementSpeed;
            set => Set(ref m_MovementSpeed, Mathf.Clamp(value, MinMovementSpeed, MaxMovementSpeed));
        }

        /// <summary>Rig eye height implied by the current mode.</summary>
        public float EyeHeight => m_SeatedMode ? SeatedEyeHeight : StandingEyeHeight;

        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>Restores settings, falling back to the defaults when the text is unusable.</summary>
        public static AccessibilitySettings FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new AccessibilitySettings();

            try
            {
                return JsonUtility.FromJson<AccessibilitySettings>(json) ?? new AccessibilitySettings();
            }
            catch (Exception)
            {
                // A corrupt settings file must never stop someone starting the simulation.
                return new AccessibilitySettings();
            }
        }

        void Set<T>(ref T field, T value)
        {
            if (Equals(field, value))
                return;

            field = value;
            Changed?.Invoke();
        }
    }
}
