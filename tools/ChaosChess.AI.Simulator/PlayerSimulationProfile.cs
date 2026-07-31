using System;

namespace ChaosChess.AI.Simulator
{
    public sealed class PlayerSimulationProfile
    {
        public PlayerSimulationProfile(
            string profileId,
            string decisionProfileId,
            int maxCardsPerTurn,
            bool useRandomTieBreak,
            int cardScoreBias = 0,
            int? engineElo = null)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Profile ID cannot be empty.", nameof(profileId));
            }

            if (string.IsNullOrWhiteSpace(decisionProfileId))
            {
                throw new ArgumentException("Decision profile ID cannot be empty.", nameof(decisionProfileId));
            }

            if (maxCardsPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCardsPerTurn), maxCardsPerTurn, "Maximum cards per turn cannot be negative.");
            }

            if (engineElo.HasValue && engineElo.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(engineElo), engineElo, "Engine ELO must be positive when supplied.");
            }

            ProfileId = profileId;
            DecisionProfileId = decisionProfileId;
            MaxCardsPerTurn = maxCardsPerTurn;
            UseRandomTieBreak = useRandomTieBreak;
            CardScoreBias = cardScoreBias;
            EngineElo = engineElo;
        }

        public string ProfileId { get; }

        public string DecisionProfileId { get; }

        public int MaxCardsPerTurn { get; }

        public bool UseRandomTieBreak { get; }

        public int CardScoreBias { get; }

        public int? EngineElo { get; }
    }
}
