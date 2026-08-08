namespace ChaosChess.AI.Domain.CardEffects
{
    public enum CardEffectPrimitiveKind
    {
        MovePiece = 0,
        RemovePiece = 1,
        CreatePiece = 2,
        ChangePieceKind = 3,
        ChangeOwner = 4,
        SetMovementOverride = 5,
        AddTileEffect = 6,
        RemoveTileEffect = 7,
        AddPieceEffect = 8,
        AddGlobalEffect = 9,
        FlipBoardPerspective = 10,
        MergeSelectedPieceIntoNearestAlly = 11,
        SwapSelectedPieceWithActorKing = 12,
        AddMirroredTileEffectPair = 13
    }
}
