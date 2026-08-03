using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class CardEffectApplierPlanningProbe : ICardEffectPlanningProbe
    {
        private readonly DefaultCardEffectDefinitionCatalog _definitionCatalog;
        private readonly CardEffectApplier _applier;

        public CardEffectApplierPlanningProbe(
            DefaultCardEffectDefinitionCatalog? definitionCatalog = null,
            CardEffectApplier? applier = null)
        {
            _definitionCatalog = definitionCatalog ?? new DefaultCardEffectDefinitionCatalog();
            _applier = applier ?? new CardEffectApplier();
        }

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

            if (!_definitionCatalog.TryGetDefinition(plan.CardId, out CardEffectDefinition definition))
            {
                return CardEffectPlanningResult.Unsupported(
                    plan,
                    CardEffectApplicationCode.UnsupportedEffect,
                    "Card effect definition is missing.");
            }

            var context = new CardEffectApplicationContext(
                gameState,
                plan,
                plan.Actor,
                caster: plan.Actor,
                owner: plan.Actor);
            CardEffectApplicationResult result = _applier.Apply(definition, context);

            return CardEffectPlanningResult.FromApplicationResult(plan, result);
        }
    }
}
