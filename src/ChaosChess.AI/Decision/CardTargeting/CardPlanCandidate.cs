using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardPlanCandidate
    {
        public CardPlanCandidate(
            CardInfo card,
            CardUsePlan plan,
            CardPlanScore score,
            int enumerationIndex)
        {
            if (enumerationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enumerationIndex),
                    enumerationIndex,
                    "Enumeration index cannot be negative.");
            }

            Card = card ?? throw new ArgumentNullException(nameof(card));
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Score = score ?? throw new ArgumentNullException(nameof(score));
            EnumerationIndex = enumerationIndex;
        }

        public CardInfo Card { get; }

        public CardUsePlan Plan { get; }

        public CardPlanScore Score { get; }

        public int EnumerationIndex { get; }

        public static int CompareByRank(
            CardPlanCandidate? left,
            CardPlanCandidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int scoreComparison = right.Score.Total.CompareTo(left.Score.Total);
            return scoreComparison != 0
                ? scoreComparison
                : left.EnumerationIndex.CompareTo(right.EnumerationIndex);
        }
    }
}
