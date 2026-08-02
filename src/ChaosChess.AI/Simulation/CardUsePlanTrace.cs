using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulation
{
    public sealed class CardUsePlanTrace
    {
        public CardUsePlanTrace(
            CardUsePlan? plan,
            CardPlanValidationResult validation)
            : this(
                plan,
                validation,
                planScore: null,
                CardPlanSkipCode.None,
                planSkipReason: null)
        {
        }

        public CardUsePlanTrace(
            CardUsePlan? plan,
            CardPlanValidationResult validation,
            CardPlanScore? planScore,
            CardPlanSkipCode planSkipCode,
            string? planSkipReason)
        {
            Plan = plan;
            Validation = validation ?? throw new ArgumentNullException(nameof(validation));

            if (planSkipCode == CardPlanSkipCode.None && planSkipReason != null)
            {
                throw new ArgumentException("Plan skip reason must be null when no skip code is present.", nameof(planSkipReason));
            }

            if (planSkipCode != CardPlanSkipCode.None && string.IsNullOrWhiteSpace(planSkipReason))
            {
                throw new ArgumentException("Plan skip reason cannot be empty when skip code is present.", nameof(planSkipReason));
            }

            PlanScore = planScore;
            PlanSkipCode = planSkipCode;
            PlanSkipReason = planSkipReason;
        }

        public CardUsePlan? Plan { get; }

        public CardPlanValidationResult Validation { get; }

        public CardPlanScore? PlanScore { get; }

        public CardPlanSkipCode PlanSkipCode { get; }

        public string? PlanSkipReason { get; }

        public bool Accepted => Validation.IsValid;

        public CardPlanValidationCode Code => Validation.Code;

        public string Reason => Validation.Reason;
    }
}
