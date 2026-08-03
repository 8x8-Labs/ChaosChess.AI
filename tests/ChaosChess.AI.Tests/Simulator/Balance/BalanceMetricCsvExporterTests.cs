using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulation.Metrics;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance
{
    public sealed class BalanceMetricCsvExporterTests
    {
        [Fact]
        public void Export_WritesStableDecisionAndComponentHeaders()
        {
            var metrics = new BalanceCardDecisionMetricCollection(
                Array.Empty<CardDecisionMetricEvent>(),
                Array.Empty<CardScoreComponentMetricEvent>());

            BalanceMetricCsvExportResult result = BalanceMetricCsvExporter.Export(metrics);

            Assert.Equal(
                "event_id,ply_index,actor,card_id,category,remaining_uses,offered,supported,eligible,legal_candidate_count,plan_selected,recommended,applied_status,code,base_score,plan_score_total,combined_gain_before_clamp,effective_gain,targeting_threshold,minimum_score_gain,selected_plan_card_id",
                result.DecisionCsv);
            Assert.Equal(
                "event_id,card_id,candidate_rank,component_code,raw_value,weight,contribution,profile_id",
                result.ComponentCsv);
        }

        [Fact]
        public void Export_WritesDecisionRowsInInputOrder()
        {
            var metrics = new BalanceCardDecisionMetricCollection(
                new[]
                {
                    Decision("event-1", "charge", PieceColor.White, baseScore: 8),
                    Decision("event-2", "fire", PieceColor.Black, baseScore: null)
                },
                Array.Empty<CardScoreComponentMetricEvent>());

            string[] rows = BalanceMetricCsvExporter.Export(metrics).DecisionCsv.Split('\n');

            Assert.Contains("event-1,0,White,charge,Mobility,1,true,true,true,3,true,true,NotAvailable,Recommended,8,5,,13,1,1,charge", rows[1]);
            Assert.Contains("event-2,0,Black,fire,Mobility,1,true,true,true,3,true,true,NotAvailable,Recommended,,5,,13,1,1,fire", rows[2]);
        }

        [Fact]
        public void Export_WritesComponentRowsInInputOrderAndEscapesCsvValues()
        {
            var metrics = new BalanceCardDecisionMetricCollection(
                Array.Empty<CardDecisionMetricEvent>(),
                new[]
                {
                    new CardScoreComponentMetricEvent(
                        "event-1",
                        "fire",
                        candidateRank: 0,
                        "fire.center,control",
                        rawValue: 2,
                        weight: 3,
                        contribution: 6,
                        "profile\"1"),
                    new CardScoreComponentMetricEvent(
                        "event-2",
                        "charge",
                        candidateRank: null,
                        "charge.movable_pawns",
                        rawValue: null,
                        weight: 2,
                        contribution: 4,
                        "profile-2")
                });

            string[] rows = BalanceMetricCsvExporter.Export(metrics).ComponentCsv.Split('\n');

            Assert.Equal("event-1,fire,0,\"fire.center,control\",2,3,6,\"profile\"\"1\"", rows[1]);
            Assert.Equal("event-2,charge,,charge.movable_pawns,,2,4,profile-2", rows[2]);
        }

        [Fact]
        public void Export_NullMetrics_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => BalanceMetricCsvExporter.Export(null!));
        }

        private static CardDecisionMetricEvent Decision(
            string eventId,
            string cardId,
            PieceColor actor,
            int? baseScore)
        {
            return new CardDecisionMetricEvent(
                eventId,
                plyIndex: 0,
                actor,
                cardId,
                "Mobility",
                remainingUses: 1,
                offered: true,
                supported: true,
                eligible: true,
                legalCandidateCount: 3,
                planSelected: true,
                recommended: true,
                CardDecisionAppliedStatus.NotAvailable,
                CardDecisionMetricCode.Recommended,
                baseScore,
                planScoreTotal: 5,
                combinedGainBeforeClamp: null,
                effectiveGain: 13,
                targetingThreshold: 1,
                minimumScoreGain: 1,
                new CardUsePlan(
                    cardId,
                    actor,
                    CardTargetSelection.None()));
        }
    }
}
