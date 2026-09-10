using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// Keeps the player's trigger collider under the headset.
    ///
    /// The XR camera moves independently of the rig root whenever the user physically walks
    /// around their room, so a collider parented to the rig would sit in the wrong place. Hazard
    /// and exit volumes have to react to where the user actually is.
    /// </summary>
    [AddComponentMenu("VR Sim/Player Body Follower")]
    public class PlayerBodyFollower : MonoBehaviour
    {
        [Tooltip("Usually the XR Origin camera. Leave empty to use the main camera.")]
        [SerializeField] Transform m_Target;

        /// <summary>Transform this collider tracks on the horizontal plane.</summary>
        public Transform Target
        {
            get => m_Target;
            set => m_Target = value;
        }

        void Start()
        {
            if (m_Target == null && Camera.main != null)
                m_Target = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (m_Target == null)
                return;

            // Track the head horizontally only; the collider stays standing on the floor so a
            // user in seated mode is still detected by the same volumes.
            var local = transform.parent != null
                ? transform.parent.InverseTransformPoint(m_Target.position)
                : m_Target.position;

            transform.localPosition = new Vector3(local.x, 0f, local.z);
        }
    }
}
