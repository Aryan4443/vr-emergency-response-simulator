namespace VRSim.Scenario.Definitions
{
    /// <summary>
    /// How demanding a scenario is meant to be, so the level-select screen can order scenarios
    /// and steer a first-time user towards one they can finish unaided.
    /// </summary>
    public enum ScenarioDifficulty
    {
        /// <summary>A first drill: few objectives, generous time, one obvious hazard.</summary>
        Introductory,

        /// <summary>The expected level for someone who has completed a drill before.</summary>
        Standard,

        /// <summary>Several competing hazards, or a decision that has to be made quickly.</summary>
        Challenging,
    }
}
