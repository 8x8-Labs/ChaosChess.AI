using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulation.Metrics;
using Xunit;

namespace ChaosChess.AI.Tests.Simulation.Metrics;

public sealed class CardDecisionMetricEventTests
{
    [Fact]
    public void Constructor_PreservesDecisionFields()
    {
        CardUsePlan plan = Plan("agile");

        var metric = new CardDecisionMetricEvent(
            "game-1:p0:agile",
            plyIndex: 0,
            PieceColor.White,
            "agile",
            "Mobility",
            remainingUses: 1,
            offered: true,
            supported: true,
            eligible: true,
            legalCandidateCount: 2,
            planSelected: true,
            recommended: true,
            CardDecisionAppliedStatus.NotAvailable,
            CardDecisionMetricCode.Recommended,
            baseScore: 8,
            planScoreTotal: 5,
            combinedGainBeforeClamp: 13,
            effectiveGain: 13,
            targetingThreshold: 1,
            minimumScoreGain: 1,
            plan);

        Assert.Equal("game-1:p0:agile", metric.EventId);
        Assert.Equal(0, metric.PlyIndex);
        Assert.Equal(PieceColor.White, metric.Actor);
        Assert.Equal("agile", metric.CardId);
        Assert.Equal("Mobility", metric.Category);
        Assert.Equal(1, metric.RemainingUses);
        Assert.True(metric.Offered);
        Assert.True(metric.Supported);
        Assert.True(metric.Eligible);
        Assert.Equal(2, metric.LegalCandidateCount);
        Assert.True(metric.LegalCandidateAvailable);
        Assert.True(metric.PlanSelected);
        Assert.True(metric.Recommended);
        Assert.Equal(CardDecisionAppliedStatus.NotAvailable, metric.AppliedStatus);
        Assert.Equal(CardDecisionMetricCode.Recommended, metric.Code);
        Assert.Equal(8, metric.BaseScore);
        Assert.Equal(5, metric.PlanScoreTotal);
        Assert.Equal(13, metric.CombinedGainBeforeClamp);
        Assert.Equal(13, metric.EffectiveGain);
        Assert.Equal(1, metric.TargetingThreshold);
        Assert.Equal(1, metric.MinimumScoreGain);
        Assert.Same(plan, metric.SelectedPlan);
    }

    [Fact]
    public void Constructor_AllowsUnavailableScoresForEarlySkip()
    {
        var metric = new CardDecisionMetricEvent(
            "game-1:p0:unknown",
            plyIndex: 0,
            PieceColor.Black,
            "unknown",
            "Utility",
            remainingUses: 1,
            offered: true,
            supported: false,
            eligible: false,
            legalCandidateCount: 0,
            planSelected: false,
            recommended: false,
            CardDecisionAppliedStatus.NotAvailable,
            CardDecisionMetricCode.UnsupportedCard,
            baseScore: null,
            planScoreTotal: null,
            combinedGainBeforeClamp: null,
            effectiveGain: null,
            targetingThreshold: 1,
            minimumScoreGain: 1,
            selectedPlan: null);

        Assert.False(metric.LegalCandidateAvailable);
        Assert.Null(metric.BaseScore);
        Assert.Null(metric.PlanScoreTotal);
        Assert.Null(metric.CombinedGainBeforeClamp);
        Assert.Null(metric.EffectiveGain);
    }

    [Fact]
    public void Constructor_RejectsInvalidDecisionIdentity()
    {
        Assert.Throws<ArgumentException>(
            () => Valid(eventId: ""));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Valid(plyIndex: -1));
        Assert.Throws<ArgumentException>(
            () => Valid(cardId: ""));
        Assert.Throws<ArgumentException>(
            () => Valid(category: ""));
    }

    [Fact]
    public void Constructor_RejectsNegativeCountsAndThresholds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Valid(remainingUses: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Valid(legalCandidateCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Valid(targetingThreshold: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Valid(minimumScoreGain: -1));
    }

    [Fact]
    public void Constructor_RejectsRecommendedWithoutSelectedPlan()
    {
        Assert.Throws<ArgumentException>(
            () => Valid(planSelected: false, recommended: true, selectedPlan: null));
    }

    [Fact]
    public void Constructor_RejectsSelectedPlanMismatch()
    {
        Assert.Throws<ArgumentException>(
            () => new CardDecisionMetricEvent(
                "game-1:p0:agile",
                plyIndex: 0,
                PieceColor.White,
                "agile",
                "Mobility",
                remainingUses: 1,
                offered: true,
                supported: true,
                eligible: true,
                legalCandidateCount: 1,
                planSelected: true,
                recommended: false,
                CardDecisionAppliedStatus.NotAvailable,
                CardDecisionMetricCode.NoBenefit,
                baseScore: 8,
                planScoreTotal: 2,
                combinedGainBeforeClamp: 10,
                effectiveGain: 10,
                targetingThreshold: 1,
                minimumScoreGain: 1,
                selectedPlan: null));
        Assert.Throws<ArgumentException>(
            () => Valid(planSelected: false, recommended: false, selectedPlan: Plan("agile")));
    }

    [Fact]
    public void ComponentMetric_PreservesRawWeightAndContribution()
    {
        var component = new CardScoreComponentMetricEvent(
            "game-1:p0:fire",
            "fire",
            candidateRank: 0,
            "fire.center_control",
            rawValue: 2,
            weight: 3,
            contribution: 6,
            "baseline-v0.3.0");

        Assert.Equal("game-1:p0:fire", component.EventId);
        Assert.Equal("fire", component.CardId);
        Assert.Equal(0, component.CandidateRank);
        Assert.Equal("fire.center_control", component.ComponentCode);
        Assert.Equal(2, component.RawValue);
        Assert.Equal(3, component.Weight);
        Assert.Equal(6, component.Contribution);
        Assert.Equal("baseline-v0.3.0", component.ProfileId);
    }

    [Fact]
    public void ComponentMetric_AllowsUnavailableRawValue()
    {
        var component = new CardScoreComponentMetricEvent(
            "game-1:p0:charge",
            "charge",
            candidateRank: null,
            "charge.movable_pawns",
            rawValue: null,
            weight: 2,
            contribution: 4,
            "baseline-v0.3.0");

        Assert.Null(component.CandidateRank);
        Assert.Null(component.RawValue);
    }

    [Fact]
    public void ComponentMetric_RejectsInvalidIdentity()
    {
        Assert.Throws<ArgumentException>(
            () => Component(eventId: ""));
        Assert.Throws<ArgumentException>(
            () => Component(cardId: ""));
        Assert.Throws<ArgumentException>(
            () => Component(componentCode: ""));
        Assert.Throws<ArgumentException>(
            () => Component(profileId: ""));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Component(candidateRank: -1));
    }

    private static CardDecisionMetricEvent Valid(
        string eventId = "game-1:p0:agile",
        int plyIndex = 0,
        string cardId = "agile",
        string category = "Mobility",
        int remainingUses = 1,
        int legalCandidateCount = 1,
        bool planSelected = true,
        bool recommended = true,
        int targetingThreshold = 1,
        int minimumScoreGain = 1,
        CardUsePlan? selectedPlan = null)
    {
        selectedPlan ??= planSelected ? Plan(cardId) : null;

        return new CardDecisionMetricEvent(
            eventId,
            plyIndex,
            PieceColor.White,
            cardId,
            category,
            remainingUses,
            offered: true,
            supported: true,
            eligible: true,
            legalCandidateCount,
            planSelected,
            recommended,
            CardDecisionAppliedStatus.NotAvailable,
            recommended ? CardDecisionMetricCode.Recommended : CardDecisionMetricCode.NoBenefit,
            baseScore: planSelected ? 8 : null,
            planScoreTotal: planSelected ? 2 : null,
            combinedGainBeforeClamp: planSelected ? 10 : null,
            effectiveGain: planSelected ? 10 : null,
            targetingThreshold,
            minimumScoreGain,
            selectedPlan);
    }

    private static CardScoreComponentMetricEvent Component(
        string eventId = "game-1:p0:agile",
        string cardId = "agile",
        int? candidateRank = 0,
        string componentCode = "agile.actor_pawn",
        string profileId = "baseline-v0.3.0")
    {
        return new CardScoreComponentMetricEvent(
            eventId,
            cardId,
            candidateRank,
            componentCode,
            rawValue: 1,
            weight: 1,
            contribution: 1,
            profileId);
    }

    private static CardUsePlan Plan(string cardId)
    {
        return new CardUsePlan(
            cardId,
            PieceColor.White,
            CardTargetSelection.None());
    }
}
