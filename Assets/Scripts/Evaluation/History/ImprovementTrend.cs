namespace VRSim.Evaluation.History
{
    /// <summary>
    /// Direction of travel across a series of runs, used by the anonymous progress dashboard.
    ///
    /// Deliberately coarse: the prototype scores runs out of a small set of events, so reporting a
    /// precise percentage of improvement would imply an accuracy the score does not have.
    /// </summary>
    public enum ImprovementTrend
    {
        /// <summary>Too few runs to say anything useful about progress.</summary>
        NotEnoughData,

        /// <summary>Later runs scored meaningfully better than earlier ones.</summary>
        Improving,

        /// <summary>Scores moved by less than one scoring event, which is noise rather than progress.</summary>
        Steady,

        /// <summary>Later runs scored meaningfully worse than earlier ones.</summary>
        Declining,
    }
}
