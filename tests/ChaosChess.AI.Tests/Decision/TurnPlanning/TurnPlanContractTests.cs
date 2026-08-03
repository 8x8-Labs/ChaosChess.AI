using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.TurnPlanning;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.TurnPlanning;

public sealed class TurnPlanContractTests
{
    [Fact]
    public void MovePlan_ParsesNormalUciMove()
    {
        var move = new MovePlan(
            Move("e2e4"),
            originalIndex: 2,
            engineScore: 4,
            adjustmentScore: 1,
            adjustedScore: 5,
            filterReasons: new[] { "Peace tile entry bonus applied." });

        Assert.Equal("e2e4", move.UciMove);
        Assert.Equal(new Square(4, 1), move.Source);
        Assert.Equal(new Square(4, 3), move.Destination);
        Assert.Null(move.Promotion);
        Assert.Equal(2, move.OriginalIndex);
        Assert.Equal(4, move.EngineScore);
        Assert.Equal(1, move.AdjustmentScore);
        Assert.Equal(5, move.AdjustedScore);
        Assert.Equal("Peace tile entry bonus applied.", Assert.Single(move.FilterReasons));
    }

    [Fact]
    public void MovePlan_ParsesPromotionUciMove()
    {
        var move = new MovePlan(
            Move("e7e8Q"),
            originalIndex: 0,
            engineScore: 9,
            adjustmentScore: 0,
            adjustedScore: 9);

        Assert.Equal(new Square(4, 6), move.Source);
        Assert.Equal(new Square(4, 7), move.Destination);
        Assert.Equal('q', move.Promotion);
    }

    [Fact]
    public void MovePlan_RejectsInvalidUciMove()
    {
        Assert.Throws<ArgumentException>(
            () => new MovePlan(
                Move("e2e9"),
                originalIndex: 0,
                engineScore: 0,
                adjustmentScore: 0,
                adjustedScore: 0));
    }

    [Fact]
    public void MovePlan_FromRecommendationCopiesFilterReasons()
    {
        var reasons = new List<string> { "Fire tile entry risk applied." };
        var recommendation = new MoveRecommendation(
            Move("e2e4"),
            originalIndex: 1,
            engineScore: 3,
            adjustmentScore: -2,
            adjustedScore: 1,
            reasons);

        MovePlan move = MovePlan.FromRecommendation(recommendation);
        reasons.Clear();

        Assert.Equal("e2e4", move.UciMove);
        Assert.Equal("Fire tile entry risk applied.", Assert.Single(move.FilterReasons));
    }

    [Fact]
    public void TurnPlan_AllowsNoCardBaselineWithMove()
    {
        TurnPlan plan = CreateTurnPlan(
            cardPlan: null,
            movePlan: CreateMovePlan("e2e4"),
            rankKey: "no-card|e2e4");

        Assert.False(plan.UsesCard);
        Assert.True(plan.HasMove);
        Assert.Null(plan.CardPlan);
        Assert.Equal(CardEffectApplicationStatus.Exact, plan.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, plan.CardApplicationCode);
    }

    [Fact]
    public void TurnPlan_AllowsCardAndMovePlan()
    {
        var cardPlan = new CardUsePlan(
            "fire",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(3, 3)));

        TurnPlan plan = CreateTurnPlan(
            cardPlan,
            CreateMovePlan("e2e4"),
            "fire|d4|e2e4");

        Assert.True(plan.UsesCard);
        Assert.True(plan.HasMove);
        Assert.Same(cardPlan, plan.CardPlan);
    }

    [Fact]
    public void TurnPlan_RejectsNeitherCardNorMove()
    {
        Assert.Throws<ArgumentException>(
            () => CreateTurnPlan(
                cardPlan: null,
                movePlan: null,
                rankKey: "empty"));
    }

    [Fact]
    public void TurnPlan_RejectsCardActorMismatch()
    {
        var cardPlan = new CardUsePlan(
            "charge",
            PieceColor.Black,
            CardTargetSelection.None());

        Assert.Throws<ArgumentException>(
            () => new TurnPlan(
                PieceColor.White,
                "fen:1",
                Score(1),
                "charge|none",
                CardEffectApplicationStatus.Exact,
                CardEffectApplicationCode.Success,
                cardPlan,
                CreateMovePlan("e2e4")));
    }

    [Fact]
    public void TurnPlan_RejectsInvalidApplicationStatusCodePair()
    {
        Assert.Throws<ArgumentException>(
            () => new TurnPlan(
                PieceColor.White,
                "fen:1",
                Score(1),
                "bad-status",
                CardEffectApplicationStatus.Exact,
                CardEffectApplicationCode.CoarseApplied,
                cardPlan: null,
                movePlan: CreateMovePlan("e2e4")));
    }

    [Fact]
    public void TurnPlanScore_RequiresComponentSum()
    {
        Assert.Throws<ArgumentException>(
            () => new TurnPlanScore(
                2,
                new[] { new TurnPlanScoreComponent("move.engine", 1, "Engine score.") }));
    }

    [Fact]
    public void TurnPlanScore_DefensivelyCopiesComponents()
    {
        var components = new List<TurnPlanScoreComponent>
        {
            new TurnPlanScoreComponent("move.engine", 1, "Engine score.")
        };

        var score = new TurnPlanScore(1, components);
        components.Clear();

        Assert.Equal(1, score.Total);
        Assert.Single(score.Components);
    }

    [Fact]
    public void TurnPlanCandidate_SortsByScoreThenStableRankKeyThenOriginalIndex()
    {
        var candidates = new List<TurnPlanCandidate>
        {
            TurnPlanCandidate.Selected(CreateTurnPlan(null, CreateMovePlan("e2e4"), "no-card|e2e4", score: 5), 2),
            TurnPlanCandidate.Selected(CreateTurnPlan(null, CreateMovePlan("d2d4"), "no-card|d2d4", score: 5), 3),
            TurnPlanCandidate.Selected(CreateTurnPlan(null, CreateMovePlan("g1f3"), "no-card|g1f3", score: 7), 1),
            TurnPlanCandidate.Skipped(TurnPlanSkipCode.NoLegalMove, "No legal move.", 0)
        };

        candidates.Sort(TurnPlanCandidate.CompareByRank);

        Assert.Equal("g1f3", candidates[0].Plan!.MovePlan!.UciMove);
        Assert.Equal("d2d4", candidates[1].Plan!.MovePlan!.UciMove);
        Assert.Equal("e2e4", candidates[2].Plan!.MovePlan!.UciMove);
        Assert.False(candidates[3].HasPlan);
    }

    [Fact]
    public void TurnPlanCandidate_ValidatesSkipCodeAndReason()
    {
        Assert.Throws<ArgumentException>(
            () => TurnPlanCandidate.Skipped(
                TurnPlanSkipCode.None,
                "None is invalid for skipped candidate.",
                0));
        Assert.Throws<ArgumentException>(
            () => TurnPlanCandidate.Skipped(
                TurnPlanSkipCode.NoLegalMove,
                "",
                0));
    }

    [Fact]
    public void TurnPlannerOptions_DefaultsAreBoundedAndCoarseIsDisabled()
    {
        var options = new TurnPlannerOptions();

        Assert.Equal(3, options.NoCardMoveCandidateCount);
        Assert.Equal(3, options.CardCandidateCount);
        Assert.Equal(16, options.TargetCandidateCount);
        Assert.Equal(3, options.PostCardMoveCandidateCount);
        Assert.Equal(1, options.OpponentReplyCandidateCount);
        Assert.Equal(3, options.BeamWidth);
        Assert.Equal(32, options.MaximumEngineCallCount);
        Assert.False(options.AllowCoarseCardEffects);
        Assert.Null(options.Seed);
    }

    [Theory]
    [InlineData(0, 1, 1, 1, 0, 1, 1)]
    [InlineData(1, 0, 1, 1, 0, 1, 1)]
    [InlineData(1, 1, 0, 1, 0, 1, 1)]
    [InlineData(1, 1, 1, 0, 0, 1, 1)]
    [InlineData(1, 1, 1, 1, -1, 1, 1)]
    [InlineData(1, 1, 1, 1, 0, 0, 1)]
    [InlineData(1, 1, 1, 1, 0, 1, 0)]
    public void TurnPlannerOptions_RejectsInvalidCounts(
        int noCardMoveCandidateCount,
        int cardCandidateCount,
        int targetCandidateCount,
        int postCardMoveCandidateCount,
        int opponentReplyCandidateCount,
        int beamWidth,
        int maximumEngineCallCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TurnPlannerOptions(
                noCardMoveCandidateCount,
                cardCandidateCount,
                targetCandidateCount,
                postCardMoveCandidateCount,
                opponentReplyCandidateCount,
                beamWidth,
                maximumEngineCallCount));
    }

    private static TurnPlan CreateTurnPlan(
        CardUsePlan? cardPlan,
        MovePlan? movePlan,
        string rankKey,
        int score = 1)
    {
        return new TurnPlan(
            PieceColor.White,
            "fen:1",
            Score(score),
            rankKey,
            CardEffectApplicationStatus.Exact,
            CardEffectApplicationCode.Success,
            cardPlan,
            movePlan);
    }

    private static TurnPlanScore Score(int value)
    {
        return new TurnPlanScore(
            value,
            new[] { new TurnPlanScoreComponent("score.total", value, "Total score.") });
    }

    private static MovePlan CreateMovePlan(string uciMove)
    {
        return new MovePlan(
            Move(uciMove),
            originalIndex: 0,
            engineScore: 1,
            adjustmentScore: 0,
            adjustedScore: 1);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 13, mateIn: null);
    }
}
