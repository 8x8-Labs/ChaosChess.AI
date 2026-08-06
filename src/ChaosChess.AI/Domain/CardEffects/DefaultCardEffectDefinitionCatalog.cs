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
                "at_mine",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "ATMine",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "blessing",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "Blessing",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "caterpillar",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "z",
                        durationTurns: 2,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "chaotic_knight",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddPieceEffect,
                        effectType: "ChaoticKnight",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
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
                "checkmate_declaration",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddGlobalEffect,
                        effectType: "CheckmateDeclaration",
                        durationTurns: 4)
                }),
            new CardEffectDefinition(
                "cobweb",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "Cobweb",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "concentration",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "a",
                        durationTurns: 5,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "dark_hand",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Opponent, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "a",
                        durationTurns: 2,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "democracy",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddGlobalEffect,
                        effectType: "Democracy",
                        durationTurns: null)
                }),
            new CardEffectDefinition(
                "dimension_instability",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddPieceEffect,
                        effectType: "DimensionInstability",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "desperado",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddPieceEffect,
                        effectType: "Desperado",
                        durationTurns: 1,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "destroyer_tank_cards",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddGlobalEffect,
                        effectType: "DestroyerTank",
                        durationTurns: 1)
                }),
            new CardEffectDefinition(
                "father_enemy",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddPieceEffect,
                        effectType: "FatherEnemy",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
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
                "giant",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddPieceEffect,
                        effectType: "Giant",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
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
                "gods_move",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.ChangePieceKind,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "jumping_platform",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "JumpingPlatform",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "limitless",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "a",
                        durationTurns: null,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "missing_promotion",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Opponent, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.ChangePieceKind,
                        pieceKind: PieceKind.Pawn,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "mutiny",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddGlobalEffect,
                        effectType: "Mutiny",
                        durationTurns: 3)
                }),
            new CardEffectDefinition(
                "obey_order",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "ObeyOrder",
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
                }),
            new CardEffectDefinition(
                "psilocybin_mushroom",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "PsilocybinMushroom",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "sneak_pawn",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "e",
                        durationTurns: 1,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "stag_fight",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddGlobalEffect,
                        effectType: "StagFight",
                        durationTurns: 3)
                }),
            new CardEffectDefinition(
                "sunset_blade",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddPieceEffect,
                        effectType: "SunsetBlade",
                        durationTurns: -1,
                        tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "time_bomb",
                CardTargetQuery.EmptySquare(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddTileEffect,
                        effectType: "TimeBomb",
                        durationTurns: 3,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
                }),
            new CardEffectDefinition(
                "thunderclap_flash",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.SetMovementOverride,
                        movementOverrideCode: "m",
                        durationTurns: 1,
                        targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece)
                }),
            new CardEffectDefinition(
                "windmill",
                CardTargetQuery.None(),
                new[]
                {
                    new CardEffectPrimitive(
                        CardEffectPrimitiveKind.AddGlobalEffect,
                        effectType: "Windmill",
                        durationTurns: 2)
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
