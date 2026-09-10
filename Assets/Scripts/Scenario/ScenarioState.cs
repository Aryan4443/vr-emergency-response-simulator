namespace VRSim.Scenario
{
    /// <summary>
    /// Scenario lifecycle states from section 8 of the project specification.
    /// </summary>
    public enum ScenarioState
    {
        /// <summary>Scene is loaded and systems are initialised.</summary>
        Ready,

        /// <summary>Instructions and controls are displayed.</summary>
        Briefing,

        /// <summary>Timer runs and user actions are evaluated.</summary>
        Active,

        /// <summary>User entered a hazard zone or performed an unsafe action.</summary>
        Warning,

        /// <summary>User reached the correct safe zone.</summary>
        Completed,

        /// <summary>User exceeded the time limit or triggered a critical failure.</summary>
        Failed,

        /// <summary>Results and improvement guidance are displayed.</summary>
        Review,

        /// <summary>Session data is cleared and the scenario can be replayed.</summary>
        Reset,
    }
}
