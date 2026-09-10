using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// A route that is impassable during the emergency. Forcing it is the critical unsafe action
    /// from sections 8 and 10, which ends the run in failure.
    ///
    /// Reachable either by interacting with the barrier or by walking into a trigger placed
    /// beyond it, so a user cannot bypass the block by ignoring the prompt.
    /// </summary>
    [AddComponentMenu("VR Sim/Blocked Route")]
    public class BlockedRoute : InteractableObject
    {
        protected override void OnInteract() => Session.TriggerCriticalUnsafeAction(ObjectId);

        void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
                Interact();
        }
    }
}
