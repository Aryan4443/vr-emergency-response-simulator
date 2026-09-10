using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// Fire alarm from section 9. The first activation starts the emergency: it scores, logs
    /// alarm_activated, and raises <see cref="InteractableObject.Interacted"/> so the scene can
    /// play the alarm audio and switch on the warning light.
    ///
    /// Latches after the first use, so the alarm cannot be pressed repeatedly for points.
    /// </summary>
    [AddComponentMenu("VR Sim/Fire Alarm")]
    public class FireAlarm : InteractableObject
    {
        [Header("Alarm")]
        [Tooltip("Warning light switched on when the alarm is raised. Optional.")]
        [SerializeField] AlarmStrobe m_Strobe;

        /// <summary>True once the alarm has been triggered this run.</summary>
        public bool IsActivated { get; private set; }

        /// <summary>Warning light driven by this alarm.</summary>
        public AlarmStrobe Strobe
        {
            get => m_Strobe;
            set => m_Strobe = value;
        }

        protected override bool CanInteract() => !IsActivated;

        protected override void OnInteract()
        {
            IsActivated = true;
            Session.ActivateAlarm(ObjectId);

            if (m_Strobe == null)
                m_Strobe = FindAnyObjectByType<AlarmStrobe>();

            m_Strobe?.Activate();
        }

        /// <summary>Clears the latch so the scenario can be replayed.</summary>
        public void ResetAlarm()
        {
            IsActivated = false;
            m_Strobe?.SetActive(false);
        }
    }
}
