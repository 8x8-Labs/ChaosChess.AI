using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision
{
    public sealed class CardDecisionResult
    {
        private readonly ReadOnlyCollection<CardUseRecommendation> _recommendations;

        public CardDecisionResult(
            IEnumerable<CardUseRecommendation> recommendations,
            int initialScore,
            int finalProjectedScore)
        {
            if (recommendations == null)
            {
                throw new ArgumentNullException(nameof(recommendations));
            }

            var copy = new List<CardUseRecommendation>();

            foreach (CardUseRecommendation recommendation in recommendations)
            {
                if (recommendation == null)
                {
                    throw new ArgumentException(
                        "Recommendation collection cannot contain null.",
                        nameof(recommendations));
                }

                copy.Add(recommendation);
            }

            _recommendations = copy.AsReadOnly();
            InitialScore = initialScore;
            FinalProjectedScore = finalProjectedScore;
        }

        public IReadOnlyList<CardUseRecommendation> Recommendations => _recommendations;

        public int InitialScore { get; }

        public int FinalProjectedScore { get; }

        public bool ShouldUseCards => _recommendations.Count > 0;
    }
}
