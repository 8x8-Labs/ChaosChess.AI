using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision
{
    public sealed class CardUseRecommendation
    {
        public CardUseRecommendation(
            CardInfo card,
            int baseScore,
            int projectedScore,
            int effectiveGain)
            : this(
                card,
                baseScore,
                projectedScore,
                effectiveGain,
                plan: null,
                planScore: null,
                CardPlanSkipCode.None,
                planSkipReason: null,
                planLegalCandidateCount: null)
        {
        }

        public CardUseRecommendation(
            CardInfo card,
            int baseScore,
            int projectedScore,
            int effectiveGain,
            CardUsePlan? plan,
            CardPlanScore? planScore,
            CardPlanSkipCode planSkipCode,
            string? planSkipReason,
            int? planLegalCandidateCount = null)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));

            if ((plan == null) != (planScore == null))
            {
                throw new ArgumentException("Plan and plan score must both be supplied or both be null.");
            }

            if (planSkipCode == CardPlanSkipCode.None && planSkipReason != null)
            {
                throw new ArgumentException("Plan skip reason must be null when no skip code is present.", nameof(planSkipReason));
            }

            if (planSkipCode != CardPlanSkipCode.None && string.IsNullOrWhiteSpace(planSkipReason))
            {
                throw new ArgumentException("Plan skip reason cannot be empty when skip code is present.", nameof(planSkipReason));
            }

            if (planLegalCandidateCount.HasValue && planLegalCandidateCount.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(planLegalCandidateCount), planLegalCandidateCount.Value, "Plan legal candidate count cannot be negative.");
            }

            BaseScore = baseScore;
            ProjectedScore = projectedScore;
            EffectiveGain = effectiveGain;
            Plan = plan;
            PlanScore = planScore;
            PlanSkipCode = planSkipCode;
            PlanSkipReason = planSkipReason;
            PlanLegalCandidateCount = planLegalCandidateCount;
        }

        public CardInfo Card { get; }

        public int BaseScore { get; }

        public int ProjectedScore { get; }

        public int EffectiveGain { get; }

        public CardUsePlan? Plan { get; }

        public CardPlanScore? PlanScore { get; }

        public CardPlanSkipCode PlanSkipCode { get; }

        public string? PlanSkipReason { get; }

        public int? PlanLegalCandidateCount { get; }
    }
}
