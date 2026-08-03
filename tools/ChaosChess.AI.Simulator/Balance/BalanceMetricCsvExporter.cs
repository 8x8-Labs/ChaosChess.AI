using System;
using System.Collections.Generic;
using ChaosChess.AI.Simulation.Metrics;
using ChaosChess.AI.Simulator.Csv;

namespace ChaosChess.AI.Simulator.Balance
{
    public static class BalanceMetricCsvExporter
    {
        public static readonly IReadOnlyList<string> DecisionHeader = new[]
        {
            "event_id",
            "ply_index",
            "actor",
            "card_id",
            "category",
            "remaining_uses",
            "offered",
            "supported",
            "eligible",
            "legal_candidate_count",
            "plan_selected",
            "recommended",
            "applied_status",
            "code",
            "base_score",
            "plan_score_total",
            "combined_gain_before_clamp",
            "effective_gain",
            "targeting_threshold",
            "minimum_score_gain",
            "selected_plan_card_id"
        };

        public static readonly IReadOnlyList<string> ComponentHeader = new[]
        {
            "event_id",
            "card_id",
            "candidate_rank",
            "component_code",
            "raw_value",
            "weight",
            "contribution",
            "profile_id"
        };

        public static BalanceMetricCsvExportResult Export(BalanceCardDecisionMetricCollection metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            return new BalanceMetricCsvExportResult(
                ExportDecisionEvents(metrics.DecisionEvents),
                ExportComponentEvents(metrics.ComponentEvents));
        }

        private static string ExportDecisionEvents(
            IEnumerable<CardDecisionMetricEvent> events)
        {
            var rows = new List<IReadOnlyList<string?>>();
            rows.Add(DecisionHeader);

            foreach (CardDecisionMetricEvent metricEvent in events)
            {
                rows.Add(ToDecisionRow(metricEvent));
            }

            return CsvWriter.Write(rows);
        }

        private static string ExportComponentEvents(
            IEnumerable<CardScoreComponentMetricEvent> events)
        {
            var rows = new List<IReadOnlyList<string?>>();
            rows.Add(ComponentHeader);

            foreach (CardScoreComponentMetricEvent metricEvent in events)
            {
                rows.Add(ToComponentRow(metricEvent));
            }

            return CsvWriter.Write(rows);
        }

        private static IReadOnlyList<string?> ToDecisionRow(CardDecisionMetricEvent metricEvent)
        {
            if (metricEvent == null)
            {
                throw new ArgumentNullException(nameof(metricEvent));
            }

            return new[]
            {
                metricEvent.EventId,
                CsvWriter.FormatInvariant(metricEvent.PlyIndex),
                metricEvent.Actor.ToString(),
                metricEvent.CardId,
                metricEvent.Category,
                CsvWriter.FormatInvariant(metricEvent.RemainingUses),
                FormatBool(metricEvent.Offered),
                FormatBool(metricEvent.Supported),
                FormatBool(metricEvent.Eligible),
                CsvWriter.FormatInvariant(metricEvent.LegalCandidateCount),
                FormatBool(metricEvent.PlanSelected),
                FormatBool(metricEvent.Recommended),
                metricEvent.AppliedStatus.ToString(),
                metricEvent.Code.ToString(),
                FormatNullable(metricEvent.BaseScore),
                FormatNullable(metricEvent.PlanScoreTotal),
                FormatNullable(metricEvent.CombinedGainBeforeClamp),
                FormatNullable(metricEvent.EffectiveGain),
                CsvWriter.FormatInvariant(metricEvent.TargetingThreshold),
                CsvWriter.FormatInvariant(metricEvent.MinimumScoreGain),
                metricEvent.SelectedPlan?.CardId ?? string.Empty
            };
        }

        private static IReadOnlyList<string?> ToComponentRow(CardScoreComponentMetricEvent metricEvent)
        {
            if (metricEvent == null)
            {
                throw new ArgumentNullException(nameof(metricEvent));
            }

            return new[]
            {
                metricEvent.EventId,
                metricEvent.CardId,
                FormatNullable(metricEvent.CandidateRank),
                metricEvent.ComponentCode,
                FormatNullable(metricEvent.RawValue),
                CsvWriter.FormatInvariant(metricEvent.Weight),
                CsvWriter.FormatInvariant(metricEvent.Contribution),
                metricEvent.ProfileId
            };
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string FormatNullable(int? value)
        {
            return value.HasValue ? CsvWriter.FormatInvariant(value.Value) : string.Empty;
        }
    }
}
