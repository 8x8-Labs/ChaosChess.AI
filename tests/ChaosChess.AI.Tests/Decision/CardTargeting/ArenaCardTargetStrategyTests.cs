using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class ArenaCardTargetStrategyTests
{
    private readonly ArenaCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_AveragesRandomOpponentSelection()
    {
        GameState state = State(
            Piece(PieceKind.Pawn, PieceColor.Black, "a7", "p"),
            Piece(PieceKind.Queen, PieceColor.Black, "d5", "q"),
            Piece(PieceKind.Rook, PieceColor.Black, "e5", "r"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Contains(
            result.SelectedCandidate!.Score.Components,
            component => component.Code == "arena.expected_selected_material");
        Assert.Equal(CardTargetKind.None, result.SelectedCandidate.Plan.Target.Kind);
    }

    [Fact]
    public void Decide_WithoutRandomCandidates_ReturnsNoBenefit()
    {
        GameState state = State(Piece(PieceKind.King, PieceColor.Black, "e8", "k"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new ArenaCardTargetStrategy(null!));
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
            new[] { new CardInfo("arena", "Tactical", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square, string fenCode)
    {
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }
}
