using System;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulation;
using Xunit;

namespace ChaosChess.AI.Tests.Simulation;

public sealed class CardUsePlanTraceRecorderTests
{
    [Fact]
    public void Record_ValidPlan_ReturnsAcceptedTrace()
    {
        var recorder = new CardUsePlanTraceRecorder();
        GameState state = CreateState(PieceColor.White, Card("charge"));
        var plan = new CardUsePlan(
            "charge",
            PieceColor.White,
            CardTargetSelection.None());

        CardUsePlanTrace trace = recorder.Record(state, plan);

        Assert.True(trace.Accepted);
        Assert.True(trace.Validation.IsValid);
        Assert.Equal(CardPlanValidationCode.Valid, trace.Code);
        Assert.Equal(trace.Validation.Reason, trace.Reason);
        Assert.Same(plan, trace.Plan);
    }

    [Fact]
    public void Record_InvalidPlan_ReturnsRejectedTraceWithValidationCode()
    {
        var recorder = new CardUsePlanTraceRecorder();
        GameState state = CreateState(PieceColor.White, Card("charge"));
        var plan = new CardUsePlan(
            "charge",
            PieceColor.Black,
            CardTargetSelection.None());

        CardUsePlanTrace trace = recorder.Record(state, plan);

        Assert.False(trace.Accepted);
        Assert.False(trace.Validation.IsValid);
        Assert.Equal(CardPlanValidationCode.ActorDoesNotMatchSideToMove, trace.Code);
        Assert.Equal(trace.Validation.Reason, trace.Reason);
        Assert.Same(plan, trace.Plan);
    }

    [Fact]
    public void Record_NullGameState_ReturnsRejectedTrace()
    {
        var recorder = new CardUsePlanTraceRecorder();
        var plan = new CardUsePlan(
            "charge",
            PieceColor.White,
            CardTargetSelection.None());

        CardUsePlanTrace trace = recorder.Record(null, plan);

        Assert.False(trace.Accepted);
        Assert.Equal(CardPlanValidationCode.NullGameState, trace.Code);
        Assert.Same(plan, trace.Plan);
    }

    [Fact]
    public void Record_NullPlan_ReturnsRejectedTrace()
    {
        var recorder = new CardUsePlanTraceRecorder();
        GameState state = CreateState(PieceColor.White, Card("charge"));

        CardUsePlanTrace trace = recorder.Record(state, (CardUsePlan?)null);

        Assert.False(trace.Accepted);
        Assert.Equal(CardPlanValidationCode.NullPlan, trace.Code);
        Assert.Null(trace.Plan);
    }

    [Fact]
    public void Record_DoesNotMutateGameState()
    {
        var recorder = new CardUsePlanTraceRecorder();
        CardInfo card = Card("fire");
        PieceInfo piece = Piece(PieceKind.King, PieceColor.White, new Square(0, 0), "k");
        TileEffectInfo tileEffect = TileEffect("existing", new Square(1, 1));
        GameState state = CreateState(
            PieceColor.Black,
            card,
            pieces: new[] { piece },
            tileEffects: new[] { tileEffect });
        var plan = new CardUsePlan(
            "fire",
            PieceColor.Black,
            CardTargetSelection.BoardSquare(new Square(4, 4)));

        CardUsePlanTrace trace = recorder.Record(state, plan);

        Assert.True(trace.Accepted);
        Assert.Same(card, Assert.Single(state.AvailableCards));
        Assert.Same(piece, Assert.Single(state.BoardState.Pieces));
        Assert.Same(tileEffect, Assert.Single(state.TileEffects));
    }

    [Fact]
    public void Record_Recommendation_PreservesPlanScoreAndSkipCode()
    {
        var recorder = new CardUsePlanTraceRecorder();
        GameState state = CreateState(PieceColor.White, Card("charge"));
        var plan = new CardUsePlan(
            "charge",
            PieceColor.White,
            CardTargetSelection.None());
        var planScore = new CardPlanScore(
            3,
            new[] { new CardPlanScoreComponent("charge.test", 3, "Test.") });
        var recommendation = new CardUseRecommendation(
            state.AvailableCards[0],
            baseScore: 1,
            projectedScore: 4,
            effectiveGain: 4,
            plan,
            planScore,
            CardPlanSkipCode.None,
            planSkipReason: null,
            planLegalCandidateCount: 2);

        CardUsePlanTrace trace = recorder.Record(state, recommendation);

        Assert.True(trace.Accepted);
        Assert.Same(plan, trace.Plan);
        Assert.Same(planScore, trace.PlanScore);
        Assert.Equal(CardPlanSkipCode.None, trace.PlanSkipCode);
        Assert.Equal(2, recommendation.PlanLegalCandidateCount);
        Assert.Null(trace.PlanSkipReason);
    }

    [Fact]
    public void Constructor_RejectsNullValidator()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CardUsePlanTraceRecorder(null!));
    }

    [Fact]
    public void TraceConstructor_RejectsNullValidation()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CardUsePlanTrace(null, null!));
    }

    [Fact]
    public void TraceConstructor_RejectsInconsistentSkipReason()
    {
        CardPlanValidationResult validation = CardPlanValidationResult.Valid();

        Assert.Throws<ArgumentException>(
            () => new CardUsePlanTrace(
                null,
                validation,
                null,
                CardPlanSkipCode.None,
                "skip"));
        Assert.Throws<ArgumentException>(
            () => new CardUsePlanTrace(
                null,
                validation,
                null,
                CardPlanSkipCode.NoBenefit,
                null));
    }

    private static CardInfo Card(string id, int remainingUses = 1)
    {
        return new CardInfo(id, "Mobility", remainingUses);
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode = "p")
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static TileEffectInfo TileEffect(string id, Square square)
    {
        return new TileEffectInfo(
            id,
            "Fire",
            square,
            PieceColor.Black,
            remainingTurns: 1);
    }

    private static GameState CreateState(
        PieceColor sideToMove,
        CardInfo? card = null,
        PieceInfo[]? pieces = null,
        TileEffectInfo[]? tileEffects = null)
    {
        var board = new BoardState(
            pieces ?? Array.Empty<PieceInfo>(),
            sideToMove,
            CastlingRights.None,
            enPassantTarget: null,
            halfmoveClock: 0,
            fullmoveNumber: 1);

        return new GameState(
            board,
            card == null ? Array.Empty<CardInfo>() : new[] { card },
            tileEffects ?? Array.Empty<TileEffectInfo>());
    }
}
