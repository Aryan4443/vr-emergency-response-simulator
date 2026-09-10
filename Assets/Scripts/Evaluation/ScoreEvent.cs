namespace VRSim.Evaluation
{
    /// <summary>
    /// Scoring events from section 10 of the project specification.
    /// </summary>
    public enum ScoreEvent
    {
        CorrectSafeExit,
        HazardIdentified,
        AlarmActivated,
        EvacuationMapViewed,
        UnsafeAreaEntered,
        WrongExitSelected,
        CriticalUnsafeAction,
        SuccessfulCompletion,
    }
}
