using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardTargetingModule
    {
        private readonly CardTargetStrategyRegistry _strategyRegistry;

        public CardTargetingModule(CardTargetStrategyRegistry? strategyRegistry = null)
        {
            _strategyRegistry = strategyRegistry ?? DefaultCardTargetStrategyCatalog.CreateRegistry();
        }

        public CardPlanDecisionResult DecideBestPlan(
            GameState gameState,
            CardInfo card,
            PieceColor actor,
            CardTargetingOptions? options = null,
            IEnumerable<MoveCandidate>? engineTopMoves = null)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            return _strategyRegistry.Decide(
                gameState,
                card,
                actor,
                options,
                engineTopMoves);
        }
    }
}
