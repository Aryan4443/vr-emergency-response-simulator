using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// Door from section 9. Toggles between open and closed and logs door_opened the first time
    /// it is opened. Closing it again is not a separate logged action, so the review shows which
    /// doors the user actually went through rather than how often they fidgeted with a handle.
    /// </summary>
    [AddComponentMenu("VR Sim/Interactive Door")]
    public class InteractiveDoor : InteractableObject
    {
        [Header("Door")]
        [Tooltip("Collider disabled while the door is open, so the user can walk through.")]
        [SerializeField] Collider m_BlockingCollider;

        [Tooltip("Local Y rotation applied to the pivot while the door is open.")]
        [SerializeField] float m_OpenAngle = 90f;

        [Tooltip("Transform rotated when the door opens. Defaults to this object.")]
        [SerializeField] Transform m_Pivot;

        Quaternion m_ClosedRotation;
        bool m_HasLoggedOpening;

        /// <summary>True while the door stands open.</summary>
        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (m_Pivot == null)
                m_Pivot = transform;

            m_ClosedRotation = m_Pivot.localRotation;
        }

        protected override void OnInteract()
        {
            IsOpen = !IsOpen;
            ApplyOpenState();

            if (IsOpen && !m_HasLoggedOpening)
            {
                m_HasLoggedOpening = true;
                Session.OpenDoor(ObjectId);
            }
        }

        void ApplyOpenState()
        {
            if (m_Pivot != null)
            {
                m_Pivot.localRotation = IsOpen
                    ? m_ClosedRotation * Quaternion.Euler(0f, m_OpenAngle, 0f)
                    : m_ClosedRotation;
            }

            if (m_BlockingCollider != null)
                m_BlockingCollider.enabled = !IsOpen;
        }

        /// <summary>Closes the door and clears the log latch so the scenario can be replayed.</summary>
        public void ResetDoor()
        {
            IsOpen = false;
            m_HasLoggedOpening = false;
            ApplyOpenState();
        }
    }
}
