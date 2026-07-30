using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision
{
    public sealed class ConfiguredCardScorer : ICardScorer
    {
        private const int MinimumProjectedScore = -99;
        private const int MaximumProjectedScore = 99;

        private readonly IReadOnlyDictionary<string, int> _categoryScores;
        private readonly IReadOnlyDictionary<string, int> _cardScores;

        public ConfiguredCardScorer(
            IReadOnlyDictionary<string, int>? categoryScores = null,
            IReadOnlyDictionary<string, int>? cardScores = null)
        {
            _categoryScores = CopyScores(categoryScores);
            _cardScores = CopyScores(cardScores);
        }

        public CardScore Score(CardScoringContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            int baseScore = GetBaseScore(context.Card);
            int projectedScore = ClampProjectedScore(context.CurrentScore + baseScore);
            int effectiveGain = projectedScore - context.CurrentScore;

            return new CardScore(
                context.Card,
                baseScore,
                projectedScore,
                effectiveGain);
        }

        private int GetBaseScore(CardInfo card)
        {
            if (_cardScores.TryGetValue(card.Id, out int cardScore))
            {
                return cardScore;
            }

            return _categoryScores.TryGetValue(card.Category, out int categoryScore)
                ? categoryScore
                : 0;
        }

        private static IReadOnlyDictionary<string, int> CopyScores(
            IReadOnlyDictionary<string, int>? scores)
        {
            var copy = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (scores == null)
            {
                return copy;
            }

            foreach (KeyValuePair<string, int> score in scores)
            {
                if (string.IsNullOrWhiteSpace(score.Key))
                {
                    throw new ArgumentException("Score key cannot be empty.", nameof(scores));
                }

                copy.Add(score.Key, score.Value);
            }

            return copy;
        }

        private static int ClampProjectedScore(int score)
        {
            if (score < MinimumProjectedScore)
            {
                return MinimumProjectedScore;
            }

            return score > MaximumProjectedScore ? MaximumProjectedScore : score;
        }
    }
}
