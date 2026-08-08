using System;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class CardTargetQuery
    {
        public CardTargetQuery(
            CardTargetKind kind,
            CardTargetOwnerRelation ownerRelation,
            int count,
            bool requiresEmptySquares,
            bool requiresOccupiedSquares,
            bool allowsExistingTileEffect,
            bool isOrdered)
        {
            EnsureValidKind(kind);
            EnsureValidOwnerRelation(ownerRelation);

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Target count cannot be negative.");
            }

            if (requiresEmptySquares && requiresOccupiedSquares)
            {
                throw new ArgumentException(
                    "A target query cannot require empty and occupied squares at the same time.",
                    nameof(requiresOccupiedSquares));
            }

            ValidateCount(kind, count);

            if (kind == CardTargetKind.OrderedSquares && !isOrdered)
            {
                throw new ArgumentException("Ordered square targets must preserve order.", nameof(isOrdered));
            }

            if (kind == CardTargetKind.OrderedPieces && !isOrdered)
            {
                throw new ArgumentException("Ordered piece targets must preserve order.", nameof(isOrdered));
            }

            Kind = kind;
            OwnerRelation = ownerRelation;
            Count = count;
            RequiresEmptySquares = requiresEmptySquares;
            RequiresOccupiedSquares = requiresOccupiedSquares;
            AllowsExistingTileEffect = allowsExistingTileEffect;
            IsOrdered = isOrdered;
        }

        public CardTargetKind Kind { get; }

        public CardTargetOwnerRelation OwnerRelation { get; }

        public int Count { get; }

        public bool RequiresEmptySquares { get; }

        public bool RequiresOccupiedSquares { get; }

        public bool AllowsExistingTileEffect { get; }

        public bool IsOrdered { get; }

        public static CardTargetQuery None()
        {
            return new CardTargetQuery(
                CardTargetKind.None,
                CardTargetOwnerRelation.Any,
                count: 0,
                requiresEmptySquares: false,
                requiresOccupiedSquares: false,
                allowsExistingTileEffect: true,
                isOrdered: false);
        }

        public static CardTargetQuery Piece(
            CardTargetOwnerRelation ownerRelation,
            int count)
        {
            return new CardTargetQuery(
                CardTargetKind.PieceAtSquare,
                ownerRelation,
                count,
                requiresEmptySquares: false,
                requiresOccupiedSquares: true,
                allowsExistingTileEffect: true,
                isOrdered: false);
        }

        public static CardTargetQuery EmptySquare(int count = 1)
        {
            return new CardTargetQuery(
                CardTargetKind.BoardSquare,
                CardTargetOwnerRelation.Any,
                count,
                requiresEmptySquares: true,
                requiresOccupiedSquares: false,
                allowsExistingTileEffect: false,
                isOrdered: false);
        }

        public static CardTargetQuery OrderedEmptySquares(int count)
        {
            return new CardTargetQuery(
                CardTargetKind.OrderedSquares,
                CardTargetOwnerRelation.Any,
                count,
                requiresEmptySquares: true,
                requiresOccupiedSquares: false,
                allowsExistingTileEffect: false,
                isOrdered: true);
        }

        public static CardTargetQuery PieceAndEmptySquare(
            CardTargetOwnerRelation ownerRelation)
        {
            return new CardTargetQuery(
                CardTargetKind.PieceAndSquare,
                ownerRelation,
                count: 2,
                requiresEmptySquares: true,
                requiresOccupiedSquares: false,
                allowsExistingTileEffect: false,
                isOrdered: true);
        }

        public static CardTargetQuery OrderedPieces(
            CardTargetOwnerRelation ownerRelation,
            int count)
        {
            return new CardTargetQuery(
                CardTargetKind.OrderedPieces,
                ownerRelation,
                count,
                requiresEmptySquares: false,
                requiresOccupiedSquares: true,
                allowsExistingTileEffect: true,
                isOrdered: true);
        }

        private static void ValidateCount(CardTargetKind kind, int count)
        {
            switch (kind)
            {
                case CardTargetKind.None:
                    if (count != 0)
                    {
                        throw new ArgumentException("None target queries must have zero target count.", nameof(count));
                    }
                    break;
                case CardTargetKind.PieceAtSquare:
                case CardTargetKind.BoardSquare:
                    if (count != 1)
                    {
                        throw new ArgumentException("Single target queries must have exactly one target.", nameof(count));
                    }
                    break;
                case CardTargetKind.OrderedSquares:
                case CardTargetKind.OrderedPieces:
                    if (count < 1)
                    {
                        throw new ArgumentException("Ordered target queries must have at least one target.", nameof(count));
                    }
                    break;
                case CardTargetKind.PieceAndSquare:
                    if (count != 2)
                    {
                        throw new ArgumentException("Piece and square target queries must have exactly two targets.", nameof(count));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown target kind.");
            }
        }

        private static void EnsureValidKind(CardTargetKind kind)
        {
            if (kind != CardTargetKind.None &&
                kind != CardTargetKind.PieceAtSquare &&
                kind != CardTargetKind.BoardSquare &&
                kind != CardTargetKind.OrderedSquares &&
                kind != CardTargetKind.PieceAndSquare &&
                kind != CardTargetKind.OrderedPieces)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown target kind.");
            }
        }

        private static void EnsureValidOwnerRelation(CardTargetOwnerRelation ownerRelation)
        {
            if (ownerRelation != CardTargetOwnerRelation.Self &&
                ownerRelation != CardTargetOwnerRelation.Opponent &&
                ownerRelation != CardTargetOwnerRelation.Any)
            {
                throw new ArgumentOutOfRangeException(nameof(ownerRelation), ownerRelation, "Unknown target owner relation.");
            }
        }
    }
}
