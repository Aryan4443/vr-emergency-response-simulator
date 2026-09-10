using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRSim.Interaction
{
    /// <summary>
    /// Base behaviour for objects that can be selected, grabbed, pressed or inspected, matching
    /// InteractableObject in section 7.
    ///
    /// <see cref="Interact"/> is the single entry point. The scene wires an XR Interaction Toolkit
    /// interactable's select or activate event to it, which keeps the scenario logic independent
    /// of which interactor triggered it and lets the desktop simulator drive the same path.
    /// </summary>
    public abstract class InteractableObject : ScenarioComponent
    {
        [Header("Interactable")]
        [Tooltip("Identifier written to the action log, for example FireAlarm_A.")]
        [SerializeField] string m_ObjectId;

        [Tooltip("Raised after a successful interaction, for animation, audio and haptics.")]
        [SerializeField] UnityEvent m_Interacted = new UnityEvent();

        /// <summary>Identifier written to the action log.</summary>
        public string ObjectId
        {
            get => string.IsNullOrEmpty(m_ObjectId) ? name : m_ObjectId;
            set => m_ObjectId = value;
        }

        /// <summary>Raised after a successful interaction.</summary>
        public UnityEvent Interacted => m_Interacted;

        protected virtual void Reset() => m_ObjectId = name;

        /// <summary>
        /// Selecting an XR interactable on the same object counts as interacting with it, so a ray
        /// interactor, a direct grab and the desktop simulator all reach the same code path
        /// without any per-object event wiring in the scene.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (TryGetComponent<XRBaseInteractable>(out var interactable))
                interactable.selectEntered.AddListener(OnSelectEntered);
        }

        protected virtual void OnDisable()
        {
            if (TryGetComponent<XRBaseInteractable>(out var interactable))
                interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        void OnSelectEntered(SelectEnterEventArgs args) => Interact();

        /// <summary>
        /// Called by the interactor. Does nothing when the object cannot currently be used, so a
        /// latching object such as the fire alarm reports only its first activation.
        /// </summary>
        public void Interact()
        {
            if (Session == null || !CanInteract())
                return;

            OnInteract();
            m_Interacted.Invoke();
        }

        /// <summary>Whether this object will respond to another interaction.</summary>
        protected virtual bool CanInteract() => true;

        /// <summary>Reports the interaction to the scenario.</summary>
        protected abstract void OnInteract();
    }
}
