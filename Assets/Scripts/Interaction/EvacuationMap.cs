using UnityEngine;

namespace VRSim.Interaction
{
    /// <summary>
    /// Evacuation map from section 9. Inspecting it scores once and logs map_viewed; the
    /// Interacted event drives showing the route information panel.
    /// </summary>
    [AddComponentMenu("VR Sim/Evacuation Map")]
    public class EvacuationMap : InteractableObject
    {
        /// <summary>True once the user has read the map this run.</summary>
        public bool HasBeenViewed { get; private set; }

        protected override void OnInteract()
        {
            HasBeenViewed = true;
            Session.ViewMap(ObjectId);
        }

        /// <summary>Clears the viewed flag so the scenario can be replayed.</summary>
        public void ResetMap() => HasBeenViewed = false;
    }
}
