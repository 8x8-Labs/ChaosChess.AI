using System;

namespace ChaosChess.AI.Domain
{
    public sealed class CardPlanValidationResult
    {
        private CardPlanValidationResult(
            CardPlanValidationCode code,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Validation reason cannot be empty.", nameof(reason));
            }

            Code = code;
            Reason = reason;
        }

        public CardPlanValidationCode Code { get; }

        public string Reason { get; }

        public bool IsValid => Code == CardPlanValidationCode.Valid;

        public static CardPlanValidationResult Valid()
        {
            return new CardPlanValidationResult(
                CardPlanValidationCode.Valid,
                "Plan is valid.");
        }

        public static CardPlanValidationResult Invalid(
            CardPlanValidationCode code,
            string reason)
        {
            if (code == CardPlanValidationCode.Valid)
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, "Use Valid for valid results.");
            }

            return new CardPlanValidationResult(code, reason);
        }
    }
}
