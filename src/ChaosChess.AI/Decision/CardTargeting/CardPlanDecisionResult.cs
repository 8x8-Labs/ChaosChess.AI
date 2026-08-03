using System;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardPlanDecisionResult
    {
        private CardPlanDecisionResult(
            CardPlanCandidate? selectedCandidate,
            CardPlanSkipCode skipCode,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Decision reason cannot be empty.", nameof(reason));
            }

            SelectedCandidate = selectedCandidate;
            SkipCode = skipCode;
            Reason = reason;
        }

        public CardPlanCandidate? SelectedCandidate { get; }

        public CardPlanSkipCode SkipCode { get; }

        public string Reason { get; }

        public bool HasSelection => SelectedCandidate != null;

        public static CardPlanDecisionResult Selected(CardPlanCandidate candidate)
        {
            return new CardPlanDecisionResult(
                candidate ?? throw new ArgumentNullException(nameof(candidate)),
                CardPlanSkipCode.None,
                "Plan selected.");
        }

        public static CardPlanDecisionResult Skipped(
            CardPlanSkipCode skipCode,
            string reason)
        {
            if (skipCode == CardPlanSkipCode.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(skipCode),
                    skipCode,
                    "Use Selected for successful plan decisions.");
            }

            return new CardPlanDecisionResult(null, skipCode, reason);
        }
    }
}
