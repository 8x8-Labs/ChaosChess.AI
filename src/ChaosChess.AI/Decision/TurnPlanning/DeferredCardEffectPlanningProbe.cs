using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class DeferredCardEffectPlanningProbe : ICardEffectPlanningProbe
    {
        public CardEffectPlanningResult Probe(
            GameState gameState,
            CardInfo card,
            CardUsePlan plan)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return CardEffectPlanningResult.Unsupported(
                plan,
                CardEffectApplicationCode.UnsupportedEffect,
                "Card effect application is deferred for this planner stage.");
        }
    }
}
