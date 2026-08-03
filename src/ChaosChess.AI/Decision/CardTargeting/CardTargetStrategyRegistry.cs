using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardTargetStrategyRegistry
    {
        private readonly DefaultCardPlanningCatalog _planningCatalog;
        private readonly IReadOnlyDictionary<string, ICardTargetStrategy> _strategies;

        public CardTargetStrategyRegistry(
            IEnumerable<ICardTargetStrategy>? strategies = null,
            DefaultCardPlanningCatalog? planningCatalog = null)
        {
            _planningCatalog = planningCatalog ?? new DefaultCardPlanningCatalog();
            _strategies = CopyStrategies(strategies);
        }

        public IReadOnlyDictionary<string, ICardTargetStrategy> Strategies => _strategies;

        public bool TryGetStrategy(
            string cardId,
            out ICardTargetStrategy? strategy)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            return _strategies.TryGetValue(cardId, out strategy);
        }

        public CardPlanDecisionResult Decide(
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

            EnsureValidColor(actor);

            if (!_planningCatalog.GetDefinition(card.Id).IsSupported)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{card.Id}' is not supported for target planning.");
            }

            if (!TryGetStrategy(card.Id, out ICardTargetStrategy? strategy) ||
                strategy == null)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.MissingStrategy,
                    $"Card '{card.Id}' has no target strategy.");
            }

            CardPlanDecisionResult result = strategy.Decide(
                new CardTargetStrategyContext(gameState, card, actor, options, engineTopMoves));

            if (result == null)
            {
                throw new InvalidOperationException("Card target strategy returned no decision result.");
            }

            if (result.HasSelection &&
                !ReferenceEquals(result.SelectedCandidate!.Card, card))
            {
                throw new InvalidOperationException("Card target strategy returned a candidate for a different card.");
            }

            return result;
        }

        private static IReadOnlyDictionary<string, ICardTargetStrategy> CopyStrategies(
            IEnumerable<ICardTargetStrategy>? strategies)
        {
            var copy = new Dictionary<string, ICardTargetStrategy>(StringComparer.OrdinalIgnoreCase);

            if (strategies == null)
            {
                return new ReadOnlyDictionary<string, ICardTargetStrategy>(copy);
            }

            foreach (ICardTargetStrategy strategy in strategies)
            {
                if (strategy == null)
                {
                    throw new ArgumentException(
                        "Strategy collection cannot contain null.",
                        nameof(strategies));
                }

                if (string.IsNullOrWhiteSpace(strategy.CardId))
                {
                    throw new ArgumentException(
                        "Strategy card ID cannot be empty.",
                        nameof(strategies));
                }

                copy.Add(strategy.CardId, strategy);
            }

            return new ReadOnlyDictionary<string, ICardTargetStrategy>(copy);
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
