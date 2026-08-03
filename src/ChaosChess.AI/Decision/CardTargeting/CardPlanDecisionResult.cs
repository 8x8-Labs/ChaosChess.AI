using System;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardPlanDecisionResult
    {
        private CardPlanDecisionResult(
            CardPlanCandidate? selectedCandidate,
            CardPlanSkipCode skipCode,
            string reason,
            int legalCandidateCount)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Decision reason cannot be empty.", nameof(reason));
            }

            if (legalCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(legalCandidateCount), legalCandidateCount, "Legal candidate count cannot be negative.");
            }

            SelectedCandidate = selectedCandidate;
            SkipCode = skipCode;
            Reason = reason;
            LegalCandidateCount = legalCandidateCount;
        }

        public CardPlanCandidate? SelectedCandidate { get; }

        public CardPlanSkipCode SkipCode { get; }

        public string Reason { get; }

        public int LegalCandidateCount { get; }

        public bool HasSelection => SelectedCandidate != null;

        public static CardPlanDecisionResult Selected(CardPlanCandidate candidate)
        {
            return Selected(candidate, legalCandidateCount: 1);
        }

        public static CardPlanDecisionResult Selected(
            CardPlanCandidate candidate,
            int legalCandidateCount)
        {
            return new CardPlanDecisionResult(
                candidate ?? throw new ArgumentNullException(nameof(candidate)),
                CardPlanSkipCode.None,
                "Plan selected.",
                legalCandidateCount);
        }

        public static CardPlanDecisionResult Skipped(
            CardPlanSkipCode skipCode,
            string reason)
        {
            return Skipped(skipCode, reason, legalCandidateCount: 0);
        }

        public static CardPlanDecisionResult Skipped(
            CardPlanSkipCode skipCode,
            string reason,
            int legalCandidateCount)
        {
            if (skipCode == CardPlanSkipCode.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(skipCode),
                    skipCode,
                    "Use Selected for successful plan decisions.");
            }

            return new CardPlanDecisionResult(null, skipCode, reason, legalCandidateCount);
        }
    }
}
