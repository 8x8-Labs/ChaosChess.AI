using System;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlanScoreComponent
    {
        public TurnPlanScoreComponent(
            string code,
            int value,
            string description)
            : this(
                code,
                rawValue: value,
                weight: 1,
                contribution: value,
                description)
        {
        }

        public TurnPlanScoreComponent(
            string code,
            int rawValue,
            int weight,
            string description)
            : this(
                code,
                rawValue,
                weight,
                checked(rawValue * weight),
                description)
        {
            if (rawValue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rawValue),
                    rawValue,
                    "Raw value cannot be negative.");
            }
        }

        private TurnPlanScoreComponent(
            string code,
            int rawValue,
            int weight,
            int contribution,
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
            RawValue = rawValue;
            Weight = weight;
            Contribution = contribution;
            Value = contribution;
            Description = description;
        }

        public string Code { get; }

        public int RawValue { get; }

        public int Weight { get; }

        public int Contribution { get; }

        public int Value { get; }

        public string Description { get; }
    }
}
