using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VRSim.Interaction;

namespace VRSim.Core
{
    /// <summary>
    /// One key per hand for using the simulation at a desk.
    ///
    /// The XR Device Simulator needs a modifier held to pick a controller before its trigger key
    /// does anything, which is awkward to explain and awkward to press. This gives each hand a
    /// single key that uses whatever that hand points at, and falls back to where the user is
    /// looking when the hand is not aimed at anything.
    ///
    /// A development aid only. It disables itself when a real headset is driving the rig, so it
    /// cannot affect behaviour on the Quest.
    /// </summary>
    [AddComponentMenu("VR Sim/Desktop Quick Interact")]
    public class DesktopQuickInteract : MonoBehaviour
    {
        public enum Hand
        {
            Left,
            Right,
        }

        [Header("Hands")]
        [SerializeField] Transform m_LeftHand;
        [SerializeField] Transform m_RightHand;

        [Header("Reach")]
        [Tooltip("How far a hand can reach, in metres.")]
        [SerializeField] float m_MaxDistance = 20f;

        [Header("Keys")]
        [SerializeField] Key m_LeftKey = Key.Q;
        [SerializeField] Key m_RightKey = Key.E;

        public Transform LeftHand
        {
            get => m_LeftHand;
            set => m_LeftHand = value;
        }

        public Transform RightHand
        {
            get => m_RightHand;
            set => m_RightHand = value;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[m_LeftKey].wasPressedThisFrame)
                Activate(Hand.Left);

            if (keyboard[m_RightKey].wasPressedThisFrame)
                Activate(Hand.Right);
        }

        /// <summary>
        /// Uses whatever <paramref name="hand"/> points at. Returns whether anything was used, so
        /// the behaviour can be tested without synthesising key presses.
        /// </summary>
        public bool Activate(Hand hand)
        {
            var origin = OriginFor(hand);
            if (origin == null)
                return false;

            var ray = new Ray(origin.position, origin.forward);

            // Scene objects first: they are solid, so a physics cast is the honest test of aim.
            if (Physics.Raycast(ray, out var hit, m_MaxDistance)
                && hit.collider.GetComponentInParent<InteractableObject>() is { } interactable)
            {
                interactable.Interact();
                return true;
            }

            return TryPressButton(ray);
        }

        Transform OriginFor(Hand hand)
        {
            var preferred = hand == Hand.Left ? m_LeftHand : m_RightHand;
            if (preferred != null)
                return preferred;

            // Without a controller the user's gaze is the most predictable substitute.
            return Camera.main != null ? Camera.main.transform : null;
        }

        /// <summary>
        /// UI has no colliders, so buttons are tested by intersecting the ray with each active
        /// button's rectangle in world space.
        /// </summary>
        bool TryPressButton(Ray ray)
        {
            var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && b.interactable);

            Button closest = null;
            var closestDistance = float.MaxValue;

            foreach (var button in buttons)
            {
                if (!RayHitsRect(ray, (RectTransform)button.transform, out var distance))
                    continue;

                if (distance > m_MaxDistance || distance >= closestDistance)
                    continue;

                closest = button;
                closestDistance = distance;
            }

            if (closest == null)
                return false;

            closest.onClick.Invoke();
            return true;
        }

        static bool RayHitsRect(Ray ray, RectTransform rect, out float distance)
        {
            distance = 0f;

            var plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out var enter) || enter <= 0f)
            {
                // The panel may be facing away from the ray; try the other side too.
                plane = new Plane(-rect.forward, rect.position);
                if (!plane.Raycast(ray, out enter) || enter <= 0f)
                    return false;
            }

            var local = rect.InverseTransformPoint(ray.GetPoint(enter));
            if (!rect.rect.Contains(local))
                return false;

            distance = enter;
            return true;
        }
    }
}
