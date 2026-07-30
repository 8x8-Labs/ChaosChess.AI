using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision
{
    public sealed class CardUseRecommendation
    {
        public CardUseRecommendation(
            CardInfo card,
            int baseScore,
            int projectedScore,
            int effectiveGain)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            BaseScore = baseScore;
            ProjectedScore = projectedScore;
            EffectiveGain = effectiveGain;
        }

        public CardInfo Card { get; }

        public int BaseScore { get; }

        public int ProjectedScore { get; }

        public int EffectiveGain { get; }
    }
}
