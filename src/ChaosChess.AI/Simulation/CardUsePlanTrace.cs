using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulation
{
    public sealed class CardUsePlanTrace
    {
        public CardUsePlanTrace(
            CardUsePlan? plan,
            CardPlanValidationResult validation)
        {
            Plan = plan;
            Validation = validation ?? throw new ArgumentNullException(nameof(validation));
        }

        public CardUsePlan? Plan { get; }

        public CardPlanValidationResult Validation { get; }

        public bool Accepted => Validation.IsValid;

        public CardPlanValidationCode Code => Validation.Code;

        public string Reason => Validation.Reason;
    }
}
