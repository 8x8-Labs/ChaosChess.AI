namespace ChaosChess.AI.Simulator
{
    public enum GameTerminationReason
    {
        Checkmate,
        Stalemate,
        KingRemoved,
        NoEngineCandidates,
        NoRecommendations,
        MoveBlocked,
        UnsupportedEffect,
        MaxPly,
        InvalidTransition
    }
}
