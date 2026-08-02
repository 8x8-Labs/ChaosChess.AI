using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class ChargeCardTargetStrategyTests
{
    private readonly ChargeCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_ScoresMovableActorPawns()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[]
            {
                Pawn(PieceColor.White, new Square(0, 1)),
                Pawn(PieceColor.White, new Square(1, 1)),
                Pawn(PieceColor.Black, new Square(2, 6))
            });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(CardTargetKind.None, result.SelectedCandidate!.Plan.Target.Kind);
        Assert.Equal(4, ComponentValue(result.SelectedCandidate.Score, "charge.movable_pawns"));
        Assert.Equal(4, result.SelectedCandidate.Score.Total);
    }

    [Fact]
    public void Decide_ScoresPromotionReach()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Pawn(PieceColor.White, new Square(4, 6)) });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(2, ComponentValue(result.SelectedCandidate!.Score, "charge.movable_pawns"));
        Assert.Equal(8, ComponentValue(result.SelectedCandidate.Score, "charge.promotion_reach"));
        Assert.Equal(10, result.SelectedCandidate.Score.Total);
    }

    [Fact]
    public void Decide_BlackDirectionMirrorsWhite()
    {
        GameState state = State(
            PieceColor.Black,
            pieces: new[] { Pawn(PieceColor.Black, new Square(4, 1)) });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.Black));

        Assert.True(result.HasSelection);
        Assert.Equal(10, result.SelectedCandidate!.Score.Total);
    }

    [Fact]
    public void Decide_ScoresBlockedActorPawns()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[]
            {
                Pawn(PieceColor.White, new Square(0, 1)),
                Pawn(PieceColor.White, new Square(1, 1)),
                Pawn(PieceColor.Black, new Square(1, 2))
            });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(-1, ComponentValue(result.SelectedCandidate!.Score, "charge.blocked_pawns"));
        Assert.Equal(1, result.SelectedCandidate.Score.Total);
    }

    [Fact]
    public void Decide_NoMovableActorPawn_ReturnsNoBenefit()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[]
            {
                Pawn(PieceColor.White, new Square(0, 1)),
                Pawn(PieceColor.Black, new Square(0, 2))
            });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_ThresholdAboveActivationScore_ReturnsNoBenefit()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Pawn(PieceColor.White, new Square(0, 1)) });

        CardPlanDecisionResult result = strategy.Decide(
            new CardTargetStrategyContext(
                state,
                state.AvailableCards[0],
                PieceColor.White,
                new CardTargetingOptions(activationThreshold: 3)));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_ActorMismatch_ReturnsInvalidActor()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Pawn(PieceColor.White, new Square(0, 1)) });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.Black));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        PieceColor actor)
    {
        return new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            actor);
    }

    private static int ComponentValue(CardPlanScore score, string code)
    {
        foreach (CardPlanScoreComponent component in score.Components)
        {
            if (component.Code == code)
            {
                return component.Value;
            }
        }

        throw new InvalidOperationException("Component was not found.");
    }

    private static GameState State(
        PieceColor sideToMove,
        PieceInfo[] pieces)
    {
        return new GameState(
            new BoardState(
                pieces,
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo("charge", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Pawn(PieceColor color, Square square)
    {
        return new PieceInfo(PieceKind.Pawn, color, square, "p");
    }
}
