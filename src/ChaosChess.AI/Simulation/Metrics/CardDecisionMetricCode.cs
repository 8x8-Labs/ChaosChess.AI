namespace ChaosChess.AI.Simulation.Metrics
{
    public enum CardDecisionMetricCode
    {
        None = 0,
        NotOffered = 1,
        UnsupportedCard = 2,
        Ineligible = 3,
        NoLegalCandidate = 4,
        NoBenefit = 5,
        BelowMinimumScoreGain = 6,
        Recommended = 7,
        AppliedUnavailable = 8,
        ExecutionSkipped = 9,
        ExecutionFailed = 10
    }
}
