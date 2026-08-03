namespace ChaosChess.AI.Decision.TurnPlanning
{
    public enum TurnPlanSkipCode
    {
        None = 0,
        NoLegalMove = 1,
        UnsupportedCardEffect = 2,
        CoarseCardEffectNotAllowed = 3,
        CardApplicationFailed = 4,
        EngineObservationUnavailable = 5,
        MoveFilterRejected = 6,
        StateMismatch = 7,
        TimeoutOrCanceled = 8,
        PostCardMoveAnalysisDeferred = 9,
        EngineCallLimitExceeded = 10
    }
}
