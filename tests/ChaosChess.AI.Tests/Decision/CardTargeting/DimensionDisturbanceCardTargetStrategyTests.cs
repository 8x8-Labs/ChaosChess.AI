using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class DimensionDisturbanceCardTargetStrategyTests
{
    private readonly DimensionDisturbanceCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsOrderedOpponentPiecePairByExpectedValue()
    {
        GameState state = State(
            Piece(PieceKind.Pawn, PieceColor.Black, "a7", "p"),
            Piece(PieceKind.Rook, PieceColor.Black, "d5", "r"),
            Piece(PieceKind.Amazon, PieceColor.Black, "e5", "s"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(CardTargetKind.OrderedPieces, result.SelectedCandidate!.Plan.Target.Kind);
        Assert.Equal(2, result.SelectedCandidate.Plan.Target.Pieces.Count);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "dimension_disturbance.target_material");
    }

    [Fact]
    public void Decide_WithoutTwoLegalOpponentPieces_ReturnsNoLegalCandidate()
    {
        GameState state = State(
            Piece(PieceKind.Queen, PieceColor.Black, "d8", "q"),
            Piece(PieceKind.King, PieceColor.Black, "e8", "k"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new DimensionDisturbanceCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(GameState state)
    {
        return new CardTargetStrategyContext(state, state.AvailableCards[0], PieceColor.White);
    }

    private static GameState State(params PieceInfo[] pieces)
    {
        return new GameState(
            new BoardState(
                pieces,
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo("dimension_disturbance", "Tactical", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square, string fenCode)
    {
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }
}
