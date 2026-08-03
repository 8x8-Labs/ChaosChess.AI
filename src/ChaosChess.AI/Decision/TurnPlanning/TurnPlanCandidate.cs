using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlanCandidate
    {
        private TurnPlanCandidate(
            TurnPlan? plan,
            TurnPlanSkipCode skipCode,
            string? skipReason,
            int originalIndex,
            CardUsePlan? skippedCardPlan = null,
            CardEffectApplicationStatus? skippedCardApplicationStatus = null,
            CardEffectApplicationCode? skippedCardApplicationCode = null)
        {
            EnsureValidSkipCode(skipCode);

            if (originalIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(originalIndex),
                    originalIndex,
                    "Original index cannot be negative.");
            }

            if (plan == null && skipCode == TurnPlanSkipCode.None)
            {
                throw new ArgumentException("Skipped candidates require a skip code.", nameof(skipCode));
            }

            if (plan != null && skipCode != TurnPlanSkipCode.None)
            {
                throw new ArgumentException("Selected candidates cannot carry a skip code.", nameof(skipCode));
            }

            if (skipCode == TurnPlanSkipCode.None && skipReason != null)
            {
                throw new ArgumentException(
                    "Skip reason must be null when skip code is None.",
                    nameof(skipReason));
            }

            if (skipCode == TurnPlanSkipCode.None &&
                (skippedCardPlan != null ||
                skippedCardApplicationStatus.HasValue ||
                skippedCardApplicationCode.HasValue))
            {
                throw new ArgumentException(
                    "Selected candidates cannot carry skipped card metadata.",
                    nameof(skippedCardPlan));
            }

            if (skipCode != TurnPlanSkipCode.None && string.IsNullOrWhiteSpace(skipReason))
            {
                throw new ArgumentException(
                    "Skip reason cannot be empty when skip code is present.",
                    nameof(skipReason));
            }

            if (skippedCardApplicationStatus.HasValue != skippedCardApplicationCode.HasValue)
            {
                throw new ArgumentException(
                    "Skipped card application status and code must be provided together.",
                    nameof(skippedCardApplicationCode));
            }

            Plan = plan;
            SkipCode = skipCode;
            SkipReason = skipReason;
            OriginalIndex = originalIndex;
            SkippedCardPlan = skippedCardPlan;
            SkippedCardApplicationStatus = skippedCardApplicationStatus;
            SkippedCardApplicationCode = skippedCardApplicationCode;
        }

        public TurnPlan? Plan { get; }

        public TurnPlanSkipCode SkipCode { get; }

        public string? SkipReason { get; }

        public int OriginalIndex { get; }

        public CardUsePlan? SkippedCardPlan { get; }

        public CardEffectApplicationStatus? SkippedCardApplicationStatus { get; }

        public CardEffectApplicationCode? SkippedCardApplicationCode { get; }

        public bool HasPlan => Plan != null;

        public static TurnPlanCandidate Selected(TurnPlan plan, int originalIndex)
        {
            return new TurnPlanCandidate(
                plan ?? throw new ArgumentNullException(nameof(plan)),
                TurnPlanSkipCode.None,
                skipReason: null,
                originalIndex);
        }

        public static TurnPlanCandidate Skipped(
            TurnPlanSkipCode skipCode,
            string skipReason,
            int originalIndex)
        {
            return new TurnPlanCandidate(
                plan: null,
                skipCode,
                skipReason,
                originalIndex);
        }

        public static TurnPlanCandidate SkippedCardEffect(
            CardEffectPlanningResult planningResult,
            bool allowCoarseCardEffects,
            int originalIndex)
        {
            if (planningResult == null)
            {
                throw new ArgumentNullException(nameof(planningResult));
            }

            TurnPlanSkipCode skipCode = ToSkipCode(
                planningResult.Status,
                allowCoarseCardEffects);

            return new TurnPlanCandidate(
                plan: null,
                skipCode,
                planningResult.Reason,
                originalIndex,
                planningResult.Plan,
                planningResult.Status,
                planningResult.Code);
        }

        public static TurnPlanCandidate SkippedCardEffect(
            CardEffectPlanningResult planningResult,
            TurnPlanSkipCode skipCode,
            string skipReason,
            int originalIndex)
        {
            if (planningResult == null)
            {
                throw new ArgumentNullException(nameof(planningResult));
            }

            if (skipCode == TurnPlanSkipCode.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(skipCode),
                    skipCode,
                    "Skipped card effect candidates require a skip code.");
            }

            return new TurnPlanCandidate(
                plan: null,
                skipCode,
                skipReason,
                originalIndex,
                planningResult.Plan,
                planningResult.Status,
                planningResult.Code);
        }

        public static int CompareByRank(
            TurnPlanCandidate? left,
            TurnPlanCandidate? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            if (!left.HasPlan || !right.HasPlan)
            {
                if (left.HasPlan != right.HasPlan)
                {
                    return left.HasPlan ? -1 : 1;
                }

                return left.OriginalIndex.CompareTo(right.OriginalIndex);
            }

            int scoreComparison = right.Plan!.Score.Total.CompareTo(left.Plan!.Score.Total);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            int keyComparison = string.CompareOrdinal(
                left.Plan.DeterministicRankKey,
                right.Plan.DeterministicRankKey);
            if (keyComparison != 0)
            {
                return keyComparison;
            }

            return left.OriginalIndex.CompareTo(right.OriginalIndex);
        }

        private static void EnsureValidSkipCode(TurnPlanSkipCode skipCode)
        {
            if (skipCode != TurnPlanSkipCode.None &&
                skipCode != TurnPlanSkipCode.NoLegalMove &&
                skipCode != TurnPlanSkipCode.UnsupportedCardEffect &&
                skipCode != TurnPlanSkipCode.CoarseCardEffectNotAllowed &&
                skipCode != TurnPlanSkipCode.CardApplicationFailed &&
                skipCode != TurnPlanSkipCode.EngineObservationUnavailable &&
                skipCode != TurnPlanSkipCode.MoveFilterRejected &&
                skipCode != TurnPlanSkipCode.StateMismatch &&
                skipCode != TurnPlanSkipCode.TimeoutOrCanceled &&
                skipCode != TurnPlanSkipCode.PostCardMoveAnalysisDeferred &&
                skipCode != TurnPlanSkipCode.EngineCallLimitExceeded)
            {
                throw new ArgumentOutOfRangeException(nameof(skipCode), skipCode, "Unknown turn plan skip code.");
            }
        }

        private static TurnPlanSkipCode ToSkipCode(
            CardEffectApplicationStatus status,
            bool allowCoarseCardEffects)
        {
            switch (status)
            {
                case CardEffectApplicationStatus.Unsupported:
                    return TurnPlanSkipCode.UnsupportedCardEffect;

                case CardEffectApplicationStatus.Coarse:
                    return allowCoarseCardEffects
                        ? TurnPlanSkipCode.PostCardMoveAnalysisDeferred
                        : TurnPlanSkipCode.CoarseCardEffectNotAllowed;

                case CardEffectApplicationStatus.Failed:
                    return TurnPlanSkipCode.CardApplicationFailed;

                case CardEffectApplicationStatus.Exact:
                    return TurnPlanSkipCode.PostCardMoveAnalysisDeferred;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown card application status.");
            }
        }
    }
}
