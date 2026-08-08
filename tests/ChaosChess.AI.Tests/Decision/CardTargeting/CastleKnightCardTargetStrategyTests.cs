using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class CastleKnightCardTargetStrategyTests
{
    private readonly CastleKnightCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsActorKnightWithNearestActorRook()
    {
        var nearKnight = Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n");
        var farKnight = Piece(PieceKind.Knight, PieceColor.White, new Square(6, 6), "n");
        var rook = Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r");
        GameState state = State("castle_knight", nearKnight, farKnight, rook);

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(nearKnight.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "castle_knight.merge_distance");
    }

    [Fact]
    public void Decide_NoActorRookSkips()
    {
        GameState state = State(
            "castle_knight",
            Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_WrongCardIdReturnsUnsupported()
    {
        GameState state = State(
            "fire",
            Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n"),
            Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Constructor_InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new CastleKnightCardTargetStrategy(null!));
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
            new[] { new CardInfo(cardId, "Transformation", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, Square square, string fenCode)
    {
        return new PieceInfo(kind, color, square, fenCode);
    }
}
