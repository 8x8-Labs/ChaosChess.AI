using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulation
{
    public sealed class CardUsePlanTraceRecorder
    {
        private readonly CardUsePlanValidator _validator;

        public CardUsePlanTraceRecorder()
            : this(new CardUsePlanValidator())
        {
        }

        public CardUsePlanTraceRecorder(CardUsePlanValidator validator)
        {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public CardUsePlanTrace Record(
            GameState? gameState,
            CardUsePlan? plan)
        {
            CardPlanValidationResult validation = _validator.Validate(gameState, plan);
            return new CardUsePlanTrace(plan, validation);
        }
    }
}
