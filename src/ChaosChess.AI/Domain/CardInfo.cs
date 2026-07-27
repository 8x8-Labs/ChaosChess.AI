using System;

namespace ChaosChess.AI.Domain
{
    public sealed class CardInfo
    {
        public CardInfo(string id, string category, int remainingUses)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Card category cannot be empty.", nameof(category));
            }

            if (remainingUses < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingUses), remainingUses, "Remaining uses cannot be negative.");
            }

            Id = id;
            Category = category;
            RemainingUses = remainingUses;
        }

        public string Id { get; }

        public string Category { get; }

        public int RemainingUses { get; }
    }
}
