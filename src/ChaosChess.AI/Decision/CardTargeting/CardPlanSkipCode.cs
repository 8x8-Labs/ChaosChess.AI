namespace ChaosChess.AI.Decision.CardTargeting
{
    public enum CardPlanSkipCode
    {
        None = 0,
        UnsupportedCard = 1,
        MissingStrategy = 2,
        NoLegalCandidate = 3,
        NoBenefit = 4,
        InvalidActor = 5,
        EngineObservationUnavailable = 6
    }
}
