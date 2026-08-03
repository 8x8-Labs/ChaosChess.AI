using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulation.Metrics;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance
{
    public sealed class BalanceMetricAggregatorTests
    {
        [Fact]
        public void Aggregate_CountsRecommendedCardsAndComponentContributions()
        {
            var metrics = new BalanceCardDecisionMetricCollection(
                new[]
                {
                    Decision("event-1", "charge", recommended: true),
                    Decision("event-2", "charge", recommended: true),
                    Decision("event-3", "fire", recommended: false)
                },
                new[]
                {
                    Component("event-1:component-0", "charge", "charge.movable_pawns", 2),
                    Component("event-2:component-0", "charge", "charge.movable_pawns", 3),
                    Component("event-2:component-1", "charge", "charge.blocked_pawns", -1)
                });

            BalanceMetricAggregate aggregate = BalanceMetricAggregator.Aggregate(metrics);

            Assert.Equal(3, aggregate.DecisionEventCount);
            Assert.Equal(2, aggregate.RecommendedEventCount);
            Assert.Equal(2, aggregate.RecommendedCountByCard["charge"]);
            Assert.False(aggregate.RecommendedCountByCard.ContainsKey("fire"));
            Assert.Equal(5, aggregate.ContributionByComponent["charge.movable_pawns"]);
            Assert.Equal(-1, aggregate.ContributionByComponent["charge.blocked_pawns"]);
        }

        [Fact]
        public void Aggregate_EmptyMetrics_ReturnsEmptyAggregate()
        {
            var metrics = new BalanceCardDecisionMetricCollection(
                Array.Empty<CardDecisionMetricEvent>(),
                Array.Empty<CardScoreComponentMetricEvent>());

            BalanceMetricAggregate aggregate = BalanceMetricAggregator.Aggregate(metrics);

            Assert.Equal(0, aggregate.DecisionEventCount);
            Assert.Equal(0, aggregate.RecommendedEventCount);
            Assert.Empty(aggregate.RecommendedCountByCard);
            Assert.Empty(aggregate.ContributionByComponent);
        }

        [Fact]
        public void Aggregate_NullMetrics_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => BalanceMetricAggregator.Aggregate(null!));
        }

        private static CardDecisionMetricEvent Decision(
            string eventId,
            string cardId,
            bool recommended)
        {
            return new CardDecisionMetricEvent(
                eventId,
                plyIndex: 0,
                PieceColor.White,
                cardId,
                "Mobility",
                remainingUses: 1,
                offered: true,
                supported: true,
                eligible: true,
                legalCandidateCount: recommended ? 1 : 0,
                planSelected: recommended,
                recommended,
                CardDecisionAppliedStatus.NotAvailable,
                recommended ? CardDecisionMetricCode.Recommended : CardDecisionMetricCode.NoBenefit,
                baseScore: recommended ? 8 : null,
                planScoreTotal: recommended ? 5 : null,
                combinedGainBeforeClamp: null,
                effectiveGain: recommended ? 13 : null,
                targetingThreshold: 1,
                minimumScoreGain: 1,
                recommended
                    ? new CardUsePlan(cardId, PieceColor.White, CardTargetSelection.None())
                    : null);
        }

        private static CardScoreComponentMetricEvent Component(
            string eventId,
            string cardId,
            string componentCode,
            int contribution)
        {
            return new CardScoreComponentMetricEvent(
                eventId,
                cardId,
                candidateRank: null,
                componentCode,
                rawValue: null,
                weight: 1,
                contribution,
                "p10-v0.3.0-baseline");
        }
    }
}
