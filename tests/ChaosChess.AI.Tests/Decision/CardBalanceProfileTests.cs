using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using Xunit;

namespace ChaosChess.AI.Tests.Decision;

public sealed class CardBalanceProfileTests
{
    [Fact]
    public void P10Baseline_MatchesCurrentDefaultValues()
    {
        CardBalanceProfile profile = CardBalanceProfileCatalog.CreateP10Baseline();

        Assert.Equal("p10-v0.3.0-baseline", profile.ProfileId);
        Assert.Equal(1, profile.SchemaVersion);
        Assert.Equal(1, profile.MinimumScoreGain);
        Assert.Equal(1, profile.MaximumCardsPerTurn);
        Assert.Empty(profile.CardScores);
        Assert.Equal(1, profile.TargetingProfile.ActivationThreshold);
        Assert.Equal(16, profile.TargetingProfile.MaximumPortalEndpointCandidates);

        Assert.Equal(10, profile.CategoryScores["Tactical"]);
        Assert.Equal(8, profile.CategoryScores["Defensive"]);
        Assert.Equal(8, profile.CategoryScores["Mobility"]);
        Assert.Equal(10, profile.CategoryScores["BoardControl"]);
        Assert.Equal(7, profile.CategoryScores["Summon"]);
        Assert.Equal(7, profile.CategoryScores["Transformation"]);
        Assert.Equal(5, profile.CategoryScores["Utility"]);
    }

    [Fact]
    public void P10Baseline_IncludesKnownTargetComponentWeights()
    {
        CardTargetingProfile profile = CardTargetingProfile.CreateP10Baseline();

        Assert.Equal(1, profile.ComponentWeights["agile.actor_pawn"]);
        Assert.Equal(1, profile.ComponentWeights["charge.movable_pawns"]);
        Assert.Equal(1, profile.ComponentWeights["fire.center_control"]);
        Assert.Equal(1, profile.ComponentWeights["peace.enemy_capture_buffer"]);
        Assert.Equal(1, profile.ComponentWeights["portal.endpoint_distance"]);
        Assert.Equal(20, profile.ComponentWeights.Count);
    }

    [Fact]
    public void Profile_DefensivelyCopiesScoreDictionaries()
    {
        var categoryScores = new Dictionary<string, int>
        {
            ["Mobility"] = 8
        };
        var cardScores = new Dictionary<string, int>
        {
            ["agile"] = 3
        };

        var profile = new CardBalanceProfile(
            "custom",
            schemaVersion: 1,
            categoryScores,
            cardScores,
            minimumScoreGain: 1,
            maximumCardsPerTurn: 1,
            CardTargetingProfile.CreateP10Baseline());

        categoryScores["Mobility"] = 99;
        cardScores["agile"] = 99;

        Assert.Equal(8, profile.CategoryScores["Mobility"]);
        Assert.Equal(3, profile.CardScores["agile"]);
        Assert.Throws<NotSupportedException>(
            () => ((IDictionary<string, int>)profile.CategoryScores).Add("Utility", 1));
    }

    [Fact]
    public void TargetingProfile_DefensivelyCopiesComponentWeights()
    {
        var weights = new Dictionary<string, int>
        {
            ["fire.center_control"] = 2
        };

        var profile = new CardTargetingProfile(
            activationThreshold: 1,
            maximumPortalEndpointCandidates: 16,
            weights);

        weights["fire.center_control"] = 99;

        Assert.Equal(2, profile.ComponentWeights["fire.center_control"]);
        Assert.Throws<NotSupportedException>(
            () => ((IDictionary<string, int>)profile.ComponentWeights).Add("fire.test", 1));
    }

    [Fact]
    public void Profile_RejectsInvalidValues()
    {
        Assert.Throws<ArgumentException>(
            () => new CardBalanceProfile(
                "",
                1,
                null,
                null,
                1,
                1,
                CardTargetingProfile.CreateP10Baseline()));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardBalanceProfile(
                "custom",
                0,
                null,
                null,
                1,
                1,
                CardTargetingProfile.CreateP10Baseline()));
        Assert.Throws<ArgumentException>(
            () => new CardBalanceProfile(
                "custom",
                1,
                new Dictionary<string, int> { [""] = 1 },
                null,
                1,
                1,
                CardTargetingProfile.CreateP10Baseline()));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardBalanceProfile(
                "custom",
                1,
                null,
                null,
                -1,
                1,
                CardTargetingProfile.CreateP10Baseline()));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardBalanceProfile(
                "custom",
                1,
                null,
                null,
                1,
                0,
                CardTargetingProfile.CreateP10Baseline()));
        Assert.Throws<ArgumentNullException>(
            () => new CardBalanceProfile(
                "custom",
                1,
                null,
                null,
                1,
                1,
                null!));
    }

    [Fact]
    public void TargetingProfile_RejectsInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetingProfile(
                activationThreshold: -1,
                maximumPortalEndpointCandidates: 16,
                componentWeights: null));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetingProfile(
                activationThreshold: 1,
                maximumPortalEndpointCandidates: 1,
                componentWeights: null));
        Assert.Throws<ArgumentException>(
            () => new CardTargetingProfile(
                activationThreshold: 1,
                maximumPortalEndpointCandidates: 16,
                new Dictionary<string, int> { [""] = 1 }));
    }

    [Fact]
    public void Baseline_CanConstructCurrentDecisionDependencies()
    {
        CardBalanceProfile profile = CardBalanceProfileCatalog.CreateP10Baseline();
        var scorer = new ConfiguredCardScorer(
            profile.CategoryScores,
            profile.CardScores);
        var eloProfile = new EloCardProfile(
            profile.MinimumScoreGain,
            profile.MaximumCardsPerTurn);
        var targetingOptions = new CardTargetingOptions(
            profile.TargetingProfile.ActivationThreshold,
            profile.TargetingProfile.MaximumPortalEndpointCandidates);
        CardInfo agile = new CardInfo("agile", "Mobility", remainingUses: 1);
        var module = new CardDecisionModule(scorer, eloProfile);

        CardDecisionResult result = module.Decide(
            CreateState(agile),
            Evaluation(totalScore: 0),
            PieceColor.White,
            new CardTargetingModule(),
            targetingOptions,
            engineTopMoves: new[] { new MoveCandidate("e2e4", 10, null) });

        CardUseRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Same(agile, recommendation.Card);
        Assert.Equal(8, recommendation.BaseScore);
        Assert.Equal(17, recommendation.EffectiveGain);
    }

    private static GameState CreateState(CardInfo card)
    {
        var board = new BoardState(
            new[] { new PieceInfo(PieceKind.Pawn, PieceColor.White, new Square(4, 1), "p") },
            PieceColor.White,
            CastlingRights.None,
            enPassantTarget: null,
            halfmoveClock: 0,
            fullmoveNumber: 1);

        return new GameState(
            board,
            new[] { card },
            Array.Empty<TileEffectInfo>());
    }

    private static EvaluationResult Evaluation(int totalScore)
    {
        return new EvaluationResult(
            boardScore: 0,
            mateIn: null,
            threat: 0,
            advantage: 0,
            totalScore);
    }
}
