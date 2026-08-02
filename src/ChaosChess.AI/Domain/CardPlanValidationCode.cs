namespace ChaosChess.AI.Domain
{
    public enum CardPlanValidationCode
    {
        Valid,
        NullGameState,
        NullPlan,
        CardNotInHand,
        CardHasNoRemainingUses,
        UnsupportedCard,
        ActorDoesNotMatchSideToMove,
        TargetKindMismatch,
        TargetCountMismatch,
        TargetPieceMissing,
        TargetPieceColorMismatch,
        TargetPieceKindMismatch,
        TargetSquareOccupied,
        TargetSquareHasTileEffect,
        DuplicateTargetSquare
    }
}
