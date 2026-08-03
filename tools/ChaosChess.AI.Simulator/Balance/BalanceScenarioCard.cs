using System;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceScenarioCard
    {
        public BalanceScenarioCard(
            string cardId,
            string category,
            int remainingUses)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card id cannot be empty.", nameof(cardId));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be empty.", nameof(category));
            }

            if (remainingUses < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingUses), remainingUses, "Remaining uses cannot be negative.");
            }

            CardId = cardId;
            Category = category;
            RemainingUses = remainingUses;
        }

        public string CardId { get; }

        public string Category { get; }

        public int RemainingUses { get; }
    }
}
