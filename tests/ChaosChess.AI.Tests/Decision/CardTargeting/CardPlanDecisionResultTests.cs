using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class CardPlanDecisionResultTests
{
    [Fact]
    public void Selected_CreatesSelectedResult()
    {
        CardPlanCandidate candidate = Candidate("charge", 3, 0);

        CardPlanDecisionResult result = CardPlanDecisionResult.Selected(candidate);

        Assert.True(result.HasSelection);
        Assert.Same(candidate, result.SelectedCandidate);
        Assert.Equal(CardPlanSkipCode.None, result.SkipCode);
        Assert.Equal(1, result.LegalCandidateCount);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Fact]
    public void Selected_WithLegalCandidateCount_PreservesCount()
    {
        CardPlanCandidate candidate = Candidate("charge", 3, 0);

        CardPlanDecisionResult result = CardPlanDecisionResult.Selected(
            candidate,
            legalCandidateCount: 4);

        Assert.True(result.HasSelection);
        Assert.Equal(4, result.LegalCandidateCount);
    }

    [Fact]
    public void Skipped_CreatesSkippedResult()
    {
        CardPlanDecisionResult result = CardPlanDecisionResult.Skipped(
            CardPlanSkipCode.NoLegalCandidate,
            "No legal candidate.");

        Assert.False(result.HasSelection);
        Assert.Null(result.SelectedCandidate);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
        Assert.Equal(0, result.LegalCandidateCount);
        Assert.Equal("No legal candidate.", result.Reason);
    }

    [Fact]
    public void Skipped_NoneCode_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CardPlanDecisionResult.Skipped(CardPlanSkipCode.None, "No skip."));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CardPlanDecisionResult.Selected(Candidate("charge", 3, 0), legalCandidateCount: -1));
    }

    [Fact]
    public void CandidateCompareByRank_OrdersByScoreDescendingThenEnumerationIndex()
    {
        CardPlanCandidate lateHigh = Candidate("fire", 10, 5);
        CardPlanCandidate earlyTie = Candidate("portal", 10, 1);
        CardPlanCandidate low = Candidate("agile", 2, 0);
        var candidates = new List<CardPlanCandidate>
        {
            low,
            lateHigh,
            earlyTie
        };

        candidates.Sort(CardPlanCandidate.CompareByRank);

        Assert.Same(earlyTie, candidates[0]);
        Assert.Same(lateHigh, candidates[1]);
        Assert.Same(low, candidates[2]);
    }

    [Fact]
    public void Candidate_InvalidArguments_Throw()
    {
        CardInfo card = new CardInfo("charge", "Mobility", 1);
        CardUsePlan plan = new CardUsePlan(
            "charge",
            PieceColor.White,
            CardTargetSelection.None());
        CardPlanScore score = Score(1);

        Assert.Throws<ArgumentNullException>(
            () => new CardPlanCandidate(null!, plan, score, 0));
        Assert.Throws<ArgumentNullException>(
            () => new CardPlanCandidate(card, null!, score, 0));
        Assert.Throws<ArgumentNullException>(
            () => new CardPlanCandidate(card, plan, null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardPlanCandidate(card, plan, score, -1));
    }

    [Fact]
    public void Options_DefaultsAndValidation_AreStable()
    {
        var defaults = new CardTargetingOptions();

        Assert.Equal(1, defaults.ActivationThreshold);
        Assert.Equal(16, defaults.MaximumPortalEndpointCandidates);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetingOptions(activationThreshold: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetingOptions(maximumPortalEndpointCandidates: 1));
    }

    private static CardPlanCandidate Candidate(
        string cardId,
        int score,
        int enumerationIndex)
    {
        var card = new CardInfo(cardId, "Mobility", 1);
        var plan = new CardUsePlan(
            cardId,
            PieceColor.White,
            CardTargetSelection.None());

        return new CardPlanCandidate(
            card,
            plan,
            Score(score),
            enumerationIndex);
    }

    private static CardPlanScore Score(int total)
    {
        return new CardPlanScore(
            total,
            new[]
            {
                new CardPlanScoreComponent("component", total, "Test component.")
            });
    }
}
