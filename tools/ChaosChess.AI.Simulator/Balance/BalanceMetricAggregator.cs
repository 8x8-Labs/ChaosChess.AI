using System;
using System.Collections.Generic;
using ChaosChess.AI.Simulation.Metrics;

namespace ChaosChess.AI.Simulator.Balance
{
    public static class BalanceMetricAggregator
    {
        public static BalanceMetricAggregate Aggregate(BalanceCardDecisionMetricCollection metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            int recommendedEventCount = 0;
            var recommendedCountByCard = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var contributionByComponent = new SortedDictionary<string, int>(StringComparer.Ordinal);

            foreach (CardDecisionMetricEvent decisionEvent in metrics.DecisionEvents)
            {
                if (!decisionEvent.Recommended)
                {
                    continue;
                }

                recommendedEventCount++;
                Add(recommendedCountByCard, decisionEvent.CardId, 1);
            }

            foreach (CardScoreComponentMetricEvent componentEvent in metrics.ComponentEvents)
            {
                Add(contributionByComponent, componentEvent.ComponentCode, componentEvent.Contribution);
            }

            return new BalanceMetricAggregate(
                metrics.DecisionEvents.Count,
                recommendedEventCount,
                recommendedCountByCard,
                contributionByComponent);
        }

        private static void Add(
            IDictionary<string, int> values,
            string key,
            int amount)
        {
            if (values.TryGetValue(key, out int current))
            {
                values[key] = checked(current + amount);
                return;
            }

            values.Add(key, amount);
        }
    }
}
