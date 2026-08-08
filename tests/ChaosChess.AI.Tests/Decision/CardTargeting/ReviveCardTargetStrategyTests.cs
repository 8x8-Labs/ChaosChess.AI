using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class ReviveCardTargetStrategyTests
{
    [Fact]
    public void Decide_ScoresHighestValueCapturedActorPiece()
    {
        GameState state = State(
            PieceColor.White,
            capturedPieces: new CapturedPieceState(
                new[] { PieceKind.Knight, PieceKind.Amazon },
                new[] { PieceKind.Queen }));
        var strategy = new ReviveCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal("revive", result.SelectedCandidate!.Plan.CardId);
        Assert.Equal(new Square(3, 3), result.SelectedCandidate.Plan.Target.Squares[0]);
        CardPlanScoreComponent pieceValue = Component(
            result.SelectedCandidate.Score,
            "revive.piece_value");
        Assert.Equal(13, pieceValue.RawValue);
        Assert.Equal(52, pieceValue.Value);
    }

    [Fact]
    public void Decide_UsesWallFallbackWhenActorHasNoCapturedPieces()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[]
            {
                Piece(PieceKind.King, PieceColor.White, new Square(4, 0), "k")
            });
        var strategy = new ReviveCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Contains(
            result.SelectedCandidate!.Score.Components,
            component => component.Code == "revive.piece_value" && component.RawValue == 0);
    }

    [Fact]
    public void Decide_WrongCardIdReturnsUnsupported()
    {
        GameState state = State(PieceColor.White, cardId: "fire");
        var strategy = new ReviveCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Constructor_InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ReviveCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        CardInfo card,
        PieceColor actor)
    {
        return new CardTargetStrategyContext(state, card, actor);
    }

    private static CardPlanScoreComponent Component(CardPlanScore score, string code)
    {
        foreach (CardPlanScoreComponent component in score.Components)
        {
            if (component.Code == code)
            {
                return component;
            }
        }

        throw new InvalidOperationException("Component was not found.");
    }

    private static GameState State(
        PieceColor sideToMove,
        PieceInfo[]? pieces = null,
        CapturedPieceState? capturedPieces = null,
        string cardId = "revive")
    {
        return new GameState(
            new BoardState(
                pieces ?? Array.Empty<PieceInfo>(),
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo(cardId, "Transformation", 1) },
            Array.Empty<TileEffectInfo>(),
            capturedPieces);
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode)
    {
        return new PieceInfo(kind, color, square, fenCode);
    }
}
