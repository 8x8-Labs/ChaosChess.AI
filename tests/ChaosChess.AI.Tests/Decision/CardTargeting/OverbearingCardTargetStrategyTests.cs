using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class OverbearingCardTargetStrategyTests
{
    private readonly OverbearingCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsNoneTargetWhenOpponentCanRetreat()
    {
        GameState state = State(
            PieceColor.White,
            Card("overbearing"),
            pieces: new[]
            {
                Piece(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
                Piece(PieceKind.Rook, PieceColor.Black, new Square(3, 6), "r")
            });

        CardPlanDecisionResult result = strategy.Decide(new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(CardTargetKind.None, result.SelectedCandidate!.Plan.Target.Kind);
        Assert.Equal(1, result.LegalCandidateCount);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "overbearing.retreatable_opponents" && component.RawValue == 1);
    }

    [Fact]
    public void Decide_SkipsWhenNoOpponentCanRetreat()
    {
        GameState state = State(
            PieceColor.White,
            Card("overbearing"),
            pieces: new[]
            {
                Piece(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
                Piece(PieceKind.Rook, PieceColor.Black, new Square(3, 7), "r")
            });

        CardPlanDecisionResult result = strategy.Decide(new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
        Assert.Equal(1, result.LegalCandidateCount);
    }

    [Fact]
    public void Decide_RejectsMismatchedCardOrActor()
    {
        GameState state = State(PieceColor.White, Card("charge"));

        CardPlanDecisionResult mismatched = strategy.Decide(new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.White));
        CardPlanDecisionResult invalidActor = strategy.Decide(new CardTargetStrategyContext(
            State(PieceColor.White, Card("overbearing")),
            Card("overbearing"),
            PieceColor.Black));

        Assert.False(mismatched.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, mismatched.SkipCode);
        Assert.False(invalidActor.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, invalidActor.SkipCode);
    }

    [Fact]
    public void Constructor_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new OverbearingCardTargetStrategy(null!));
    }

    private static GameState State(
        PieceColor sideToMove,
        CardInfo card,
        PieceInfo[]? pieces = null)
    {
        return new GameState(
            new BoardState(
                pieces ?? Array.Empty<PieceInfo>(),
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { card },
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "Control", 1);
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
