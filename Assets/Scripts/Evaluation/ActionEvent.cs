using System;

namespace VRSim.Evaluation
{
    /// <summary>
    /// A single logged interaction, matching the ActionEvent shape in section 11.
    /// Field names are lower camel case because they are serialised directly to JSON.
    /// </summary>
    [Serializable]
    public class ActionEvent
    {
        /// <summary>Event name such as alarm_activated, door_opened or hazard_entered.</summary>
        public string actionType;

        /// <summary>Identifier of the object involved, for example FireAlarm_A.</summary>
        public string objectId;

        /// <summary>Seconds since the scenario timer started.</summary>
        public float timestampSeconds;

        /// <summary>Whether the action was the safe or correct one.</summary>
        public bool successful;
    }
}
