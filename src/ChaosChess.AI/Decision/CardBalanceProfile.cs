using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Decision.CardTargeting;

namespace ChaosChess.AI.Decision
{
    public sealed class CardBalanceProfile
    {
        private readonly IReadOnlyDictionary<string, int> _categoryScores;
        private readonly IReadOnlyDictionary<string, int> _cardScores;

        public CardBalanceProfile(
            string profileId,
            int schemaVersion,
            IReadOnlyDictionary<string, int>? categoryScores,
            IReadOnlyDictionary<string, int>? cardScores,
            int minimumScoreGain,
            int maximumCardsPerTurn,
            CardTargetingProfile targetingProfile)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Profile id cannot be empty.", nameof(profileId));
            }

            if (schemaVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "Schema version must be positive.");
            }

            if (minimumScoreGain < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumScoreGain), minimumScoreGain, "Minimum score gain cannot be negative.");
            }

            if (maximumCardsPerTurn < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCardsPerTurn), maximumCardsPerTurn, "Maximum cards per turn must be positive.");
            }

            ProfileId = profileId;
            SchemaVersion = schemaVersion;
            _categoryScores = CopyScores(categoryScores, nameof(categoryScores));
            _cardScores = CopyScores(cardScores, nameof(cardScores));
            MinimumScoreGain = minimumScoreGain;
            MaximumCardsPerTurn = maximumCardsPerTurn;
            TargetingProfile = targetingProfile ?? throw new ArgumentNullException(nameof(targetingProfile));
        }

        public string ProfileId { get; }

        public int SchemaVersion { get; }

        public IReadOnlyDictionary<string, int> CategoryScores => _categoryScores;

        public IReadOnlyDictionary<string, int> CardScores => _cardScores;

        public int MinimumScoreGain { get; }

        public int MaximumCardsPerTurn { get; }

        public CardTargetingProfile TargetingProfile { get; }

        private static IReadOnlyDictionary<string, int> CopyScores(
            IReadOnlyDictionary<string, int>? scores,
            string parameterName)
        {
            var copy = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (scores == null)
            {
                return new ReadOnlyDictionary<string, int>(copy);
            }

            foreach (KeyValuePair<string, int> score in scores)
            {
                if (string.IsNullOrWhiteSpace(score.Key))
                {
                    throw new ArgumentException("Score key cannot be empty.", parameterName);
                }

                copy.Add(score.Key, score.Value);
            }

            return new ReadOnlyDictionary<string, int>(copy);
        }
    }
}
