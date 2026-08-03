using System;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardPlanScoreComponent
    {
        public CardPlanScoreComponent(
            string code,
            int value,
            string description)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Score component code cannot be empty.", nameof(code));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Score component description cannot be empty.", nameof(description));
            }

            Code = code;
            Value = value;
            Description = description;
        }

        public string Code { get; }

        public int Value { get; }

        public string Description { get; }
    }
}
