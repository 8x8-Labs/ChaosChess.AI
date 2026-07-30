using System;

namespace ChaosChess.AI.Decision
{
    public sealed class EloCardProfile
    {
        public EloCardProfile(
            int minimumScoreGain = 1,
            int maximumCardsPerTurn = 1)
        {
            if (minimumScoreGain < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumScoreGain),
                    minimumScoreGain,
                    "Minimum score gain cannot be negative.");
            }

            if (maximumCardsPerTurn < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumCardsPerTurn),
                    maximumCardsPerTurn,
                    "Maximum cards per turn must be positive.");
            }

            MinimumScoreGain = minimumScoreGain;
            MaximumCardsPerTurn = maximumCardsPerTurn;
        }

        public int MinimumScoreGain { get; }

        public int MaximumCardsPerTurn { get; }
    }
}
