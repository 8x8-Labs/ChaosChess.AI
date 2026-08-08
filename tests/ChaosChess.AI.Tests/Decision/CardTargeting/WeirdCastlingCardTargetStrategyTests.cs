using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class WeirdCastlingCardTargetStrategyTests
{
    private readonly WeirdCastlingCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsLowValueForwardActorPiece()
    {
        var king = Piece(PieceKind.King, PieceColor.White, new Square(4, 0), "k");
        var pawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 3), "p");
        var queen = Piece(PieceKind.Queen, PieceColor.White, new Square(3, 3), "q");
        GameState state = State("weird_castling", king, pawn, queen);

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(pawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "weird_castling.target_piece_value_penalty");
    }

    [Fact]
    public void Decide_MissingActorKingSkips()
    {
        GameState state = State(
            "weird_castling",
            Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 3), "p"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_WrongCardIdReturnsUnsupported()
    {
        GameState state = State(
            "fire",
            Piece(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
            Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 3), "p"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Constructor_InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new WeirdCastlingCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(GameState state)
    {
        return new CardTargetStrategyContext(state, state.AvailableCards[0], PieceColor.White);
    }

    private static GameState State(string cardId, params PieceInfo[] pieces)
    {
        return new GameState(
            new BoardState(
                pieces,
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo(cardId, "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, Square square, string fenCode)
    {
        return new PieceInfo(kind, color, square, fenCode);
    }
}
