using System;

namespace ChaosChess.AI.Simulation.Metrics
{
    public sealed class CardScoreComponentMetricEvent
    {
        public CardScoreComponentMetricEvent(
            string eventId,
            string cardId,
            int? candidateRank,
            string componentCode,
            int? rawValue,
            int weight,
            int contribution,
            string profileId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
            }

            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card id cannot be empty.", nameof(cardId));
            }

            if (candidateRank.HasValue && candidateRank.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(candidateRank), candidateRank, "Candidate rank cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(componentCode))
            {
                throw new ArgumentException("Component code cannot be empty.", nameof(componentCode));
            }

            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Profile id cannot be empty.", nameof(profileId));
            }

            EventId = eventId;
            CardId = cardId;
            CandidateRank = candidateRank;
            ComponentCode = componentCode;
            RawValue = rawValue;
            Weight = weight;
            Contribution = contribution;
            ProfileId = profileId;
        }

        public string EventId { get; }

        public string CardId { get; }

        public int? CandidateRank { get; }

        public string ComponentCode { get; }

        public int? RawValue { get; }

        public int Weight { get; }

        public int Contribution { get; }

        public string ProfileId { get; }
    }
}
