using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class DefaultCardPlanningCatalog
    {
        private static readonly CardPlanningDefinition[] DefaultDefinitions =
        {
            CardPlanningDefinition.Supported("agile", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("aim", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("caterpillar", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("charge", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("concentration", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("fast_march", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("fire", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("limitless", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("peace_zone", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("portal", CardTargetKind.OrderedSquares, 2),
            CardPlanningDefinition.Supported("sneak_pawn", CardTargetKind.PieceAtSquare, 1),
            CardPlanningDefinition.Supported("thunderclap_flash", CardTargetKind.PieceAtSquare, 1)
        };

        private readonly IReadOnlyDictionary<string, CardPlanningDefinition> _definitions;

        public DefaultCardPlanningCatalog()
            : this(DefaultDefinitions)
        {
        }

        public DefaultCardPlanningCatalog(IEnumerable<CardPlanningDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var copy = new Dictionary<string, CardPlanningDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (CardPlanningDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Definition collection cannot contain null.", nameof(definitions));
                }

                copy.Add(definition.CardId, definition);
            }

            _definitions = new ReadOnlyDictionary<string, CardPlanningDefinition>(copy);
        }

        public IReadOnlyDictionary<string, CardPlanningDefinition> Definitions => _definitions;

        public CardPlanningDefinition GetDefinition(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            return _definitions.TryGetValue(cardId, out CardPlanningDefinition definition)
                ? definition
                : CardPlanningDefinition.Unsupported(cardId);
        }
    }
}
