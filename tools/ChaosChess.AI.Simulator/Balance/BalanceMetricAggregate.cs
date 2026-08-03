using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceMetricAggregate
    {
        public BalanceMetricAggregate(
            int decisionEventCount,
            int recommendedEventCount,
            IReadOnlyDictionary<string, int> recommendedCountByCard,
            IReadOnlyDictionary<string, int> contributionByComponent)
        {
            if (decisionEventCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decisionEventCount), decisionEventCount, "Decision event count cannot be negative.");
            }

            if (recommendedEventCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(recommendedEventCount), recommendedEventCount, "Recommended event count cannot be negative.");
            }

            if (recommendedEventCount > decisionEventCount)
            {
                throw new ArgumentException("Recommended event count cannot exceed decision event count.", nameof(recommendedEventCount));
            }

            DecisionEventCount = decisionEventCount;
            RecommendedEventCount = recommendedEventCount;
            RecommendedCountByCard = CopyMap(recommendedCountByCard, nameof(recommendedCountByCard));
            ContributionByComponent = CopyMap(contributionByComponent, nameof(contributionByComponent));
        }

        public int DecisionEventCount { get; }

        public int RecommendedEventCount { get; }

        public IReadOnlyDictionary<string, int> RecommendedCountByCard { get; }

        public IReadOnlyDictionary<string, int> ContributionByComponent { get; }

        private static ReadOnlyDictionary<string, int> CopyMap(
            IReadOnlyDictionary<string, int> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new SortedDictionary<string, int>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, int> entry in source)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    throw new ArgumentException("Aggregate keys cannot be empty.", parameterName);
                }

                copy.Add(entry.Key, entry.Value);
            }

            return new ReadOnlyDictionary<string, int>(copy);
        }
    }
}
