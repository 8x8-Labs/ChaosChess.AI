using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Domain
{
    public sealed class DefaultCardPlanningCatalog
    {
        private static readonly PieceKind[] PawnKinds =
        {
            PieceKind.Pawn
        };

        private static readonly PieceKind[] KnightKinds =
        {
            PieceKind.Knight
        };

        private static readonly PieceKind[] RookKinds =
        {
            PieceKind.Rook
        };

        private static readonly PieceKind[] DarkHandTargetKinds =
        {
            PieceKind.Pawn,
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook
        };

        private static readonly PieceKind[] GodsMoveTargetKinds =
        {
            PieceKind.Pawn,
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook
        };

        private static readonly PieceKind[] StandardPromotableKinds =
        {
            PieceKind.Pawn,
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook,
            PieceKind.Queen
        };

        private static readonly PieceKind[] AllMobileKinds =
        {
            PieceKind.Pawn,
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook,
            PieceKind.Queen,
            PieceKind.King,
            PieceKind.Amazon,
            PieceKind.Chancellor,
            PieceKind.KnightRider
        };

        private static readonly PieceKind[] NonKingNonWallKinds =
        {
            PieceKind.Pawn,
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook,
            PieceKind.Queen,
            PieceKind.Amazon,
            PieceKind.Chancellor,
            PieceKind.KnightRider
        };

        private static readonly PieceKind[] MissingPromotionTargetKinds =
        {
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook,
            PieceKind.Amazon,
            PieceKind.Chancellor,
            PieceKind.KnightRider
        };

        private static readonly PieceKind[] NonKingNonQueenKinds =
        {
            PieceKind.Pawn,
            PieceKind.Knight,
            PieceKind.Bishop,
            PieceKind.Rook,
            PieceKind.Wall,
            PieceKind.Amazon,
            PieceKind.Chancellor,
            PieceKind.KnightRider
        };

        private static readonly CardPlanningDefinition[] DefaultDefinitions =
        {
            CardPlanningDefinition.Supported("agile", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("aim", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("arena", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("at_mine", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("blessing", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("castle_knight", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, KnightKinds)),
            CardPlanningDefinition.Supported("caterpillar", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, KnightKinds)),
            CardPlanningDefinition.Supported("chaotic_knight", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, KnightKinds)),
            CardPlanningDefinition.Supported("charge", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("checkmate_declaration", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("cobweb", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("concentration", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, StandardPromotableKinds)),
            CardPlanningDefinition.Supported("dark_hand", CardTargetRequirement.Piece(CardTargetOwnerRelation.Opponent, DarkHandTargetKinds)),
            CardPlanningDefinition.Supported("democracy", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("desperado", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, NonKingNonWallKinds)),
            CardPlanningDefinition.Supported("destroyer_tank_cards", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("dimension_disturbance", CardTargetRequirement.OrderedPieces(CardTargetOwnerRelation.Opponent, 2, NonKingNonQueenKinds)),
            CardPlanningDefinition.Supported("dimension_instability", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, KnightKinds)),
            CardPlanningDefinition.Supported("father_enemy", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("fast_march", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("fire", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("gaslighting", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("giant", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop })),
            CardPlanningDefinition.Supported("gods_move", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, GodsMoveTargetKinds)),
            CardPlanningDefinition.Supported("honey_trap", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("jumping_platform", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("limitless", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, AllMobileKinds)),
            CardPlanningDefinition.Supported("magnet", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("missing_promotion", CardTargetRequirement.Piece(CardTargetOwnerRelation.Opponent, MissingPromotionTargetKinds)),
            CardPlanningDefinition.Supported("mutiny", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("obey_order", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("overbearing", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("peace_zone", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("portal", CardTargetKind.OrderedSquares, 2),
            CardPlanningDefinition.Supported("position_swap", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("psilocybin_mushroom", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("rampart", CardTargetKind.OrderedSquares, 2),
            CardPlanningDefinition.Supported("shuffle_board", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("sneak_pawn", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("stag_fight", CardTargetKind.None, 0),
            CardPlanningDefinition.Supported("sunset_blade", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("sync", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("teleport", CardTargetRequirement.PieceAndSquare(CardTargetOwnerRelation.Self, PawnKinds)),
            CardPlanningDefinition.Supported("time_bomb", CardTargetKind.BoardSquare, 1),
            CardPlanningDefinition.Supported("thunderclap_flash", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, RookKinds)),
            CardPlanningDefinition.Supported("transmigration", CardTargetRequirement.Piece(CardTargetOwnerRelation.Opponent, MissingPromotionTargetKinds)),
            CardPlanningDefinition.Supported("weird_castling", CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, NonKingNonWallKinds)),
            CardPlanningDefinition.Supported("windmill", CardTargetKind.None, 0)
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
