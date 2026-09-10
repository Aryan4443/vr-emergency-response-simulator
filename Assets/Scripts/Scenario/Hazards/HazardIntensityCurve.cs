namespace VRSim.Scenario.Hazards
{
    /// <summary>
    /// How the severity of a hazard climbs from nothing to its peak over time.
    /// </summary>
    public enum HazardIntensityCurve
    {
        /// <summary>
        /// Severity rises at a constant rate. Predictable, and therefore the right choice when the
        /// scenario is being used to measure reaction time and the hazard must not surprise.
        /// </summary>
        Linear,

        /// <summary>
        /// Severity rises slowly at first and then accelerates, which is how a fire behaves once it
        /// finds fresh fuel. It reads as a hazard the user could have escaped had they moved sooner.
        /// </summary>
        EaseIn,
    }
}
