using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class DefaultCardEffectDefinitionCatalog
    {
        private static readonly CardEffectDefinition[] DefaultDefinitions =
        {
            new CardEffectDefinition(
                "agile",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "u",
                        durationTurns: 1,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "aim",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "t",
                        durationTurns: 1,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "charge",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(CardEffectPrimitiveKind.MovePiece)
                }),
            new CardEffectDefinition(
                "fast_march",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "f",
                        durationTurns: 1,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "fire",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "Fire",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "peace_zone",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "Peace",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "portal",
                CardTargetQuery.OrderedEmptySquares(2),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "Portal",
                        durationTurns: -1,
                        sharedRemainingUses: 2,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
                        targetIndex: 0,
                        destinationTargetIndex: 1),
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "Portal",
                        durationTurns: -1,
                        sharedRemainingUses: 2,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
                        targetIndex: 1,
                        destinationTargetIndex: 0)
                })
        };

        private readonly IReadOnlyDictionary<string, CardEffectDefinition> _definitions;

        public DefaultCardEffectDefinitionCatalog()
            : this(DefaultDefinitions)
        {
        }

        public DefaultCardEffectDefinitionCatalog(IEnumerable<CardEffectDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var copy = new Dictionary<string, CardEffectDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (CardEffectDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Definition collection cannot contain null.", nameof(definitions));
                }

                copy.Add(definition.CardId, definition);
            }

            _definitions = new ReadOnlyDictionary<string, CardEffectDefinition>(copy);
        }

        public IReadOnlyDictionary<string, CardEffectDefinition> Definitions => _definitions;

        public CardEffectDefinition? FindDefinition(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            return _definitions.TryGetValue(cardId, out CardEffectDefinition definition)
                ? definition
                : null;
        }

        public bool TryGetDefinition(string cardId, out CardEffectDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            return _definitions.TryGetValue(cardId, out definition);
        }
    }
}
