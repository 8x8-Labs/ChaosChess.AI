namespace ChaosChess.AI.Simulation
{
    public enum SimulationTerminationReason
    {
        HorizonReached,
        NoEngineCandidates,
        NoMoveRecommendations,
        KingRemoved,
        MoveBlocked,
        Checkmate,
        Stalemate,
        UnsupportedEffectEncountered
    }
}
