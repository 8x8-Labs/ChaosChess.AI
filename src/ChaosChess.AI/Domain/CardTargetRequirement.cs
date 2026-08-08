using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Domain
{
    public sealed class CardTargetRequirement
    {
        public CardTargetRequirement(CardTargetKind kind, int count)
            : this(kind, count, CardTargetOwnerRelation.Any, Array.Empty<PieceKind>())
        {
        }

        public CardTargetRequirement(
            CardTargetKind kind,
            int count,
            CardTargetOwnerRelation ownerRelation,
            IEnumerable<PieceKind> allowedPieceKinds)
        {
            EnsureValidTargetKind(kind);
            EnsureValidOwnerRelation(ownerRelation);

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Target count cannot be negative.");
            }

            if (kind == CardTargetKind.None && count != 0)
            {
                throw new ArgumentException("None target requirement must have a count of 0.", nameof(count));
            }

            if (kind != CardTargetKind.None && count == 0)
            {
                throw new ArgumentException("Targeted card requirement must have at least one target.", nameof(count));
            }

            if (kind == CardTargetKind.PieceAndSquare && count != 2)
            {
                throw new ArgumentException("Piece and square target requirements must have exactly two targets.", nameof(count));
            }

            if (kind == CardTargetKind.OrderedPieces && count < 1)
            {
                throw new ArgumentException("Ordered piece target requirements must have at least one target.", nameof(count));
            }

            IReadOnlyList<PieceKind> allowedKinds = CopyAllowedPieceKinds(allowedPieceKinds);

            if (!CanRestrictPieceTarget(kind) && ownerRelation != CardTargetOwnerRelation.Any)
            {
                throw new ArgumentException("Only piece targets can restrict owner relation.", nameof(ownerRelation));
            }

            if (!CanRestrictPieceTarget(kind) && allowedKinds.Count != 0)
            {
                throw new ArgumentException("Only piece targets can restrict allowed piece kinds.", nameof(allowedPieceKinds));
            }

            Kind = kind;
            Count = count;
            OwnerRelation = ownerRelation;
            AllowedPieceKinds = allowedKinds;
        }

        public CardTargetKind Kind { get; }

        public int Count { get; }

        public CardTargetOwnerRelation OwnerRelation { get; }

        public IReadOnlyList<PieceKind> AllowedPieceKinds { get; }

        public static CardTargetRequirement None()
        {
            return new CardTargetRequirement(CardTargetKind.None, count: 0);
        }

        public static CardTargetRequirement Piece(
            CardTargetOwnerRelation ownerRelation,
            params PieceKind[] allowedPieceKinds)
        {
            return new CardTargetRequirement(
                CardTargetKind.PieceAtSquare,
                count: 1,
                ownerRelation,
                allowedPieceKinds);
        }

        public static CardTargetRequirement Piece(
            CardTargetOwnerRelation ownerRelation,
            int count,
            IEnumerable<PieceKind> allowedPieceKinds)
        {
            return new CardTargetRequirement(
                CardTargetKind.PieceAtSquare,
                count,
                ownerRelation,
                allowedPieceKinds);
        }

        public static CardTargetRequirement PieceAndSquare(
            CardTargetOwnerRelation ownerRelation,
            params PieceKind[] allowedPieceKinds)
        {
            return new CardTargetRequirement(
                CardTargetKind.PieceAndSquare,
                count: 2,
                ownerRelation,
                allowedPieceKinds);
        }

        public static CardTargetRequirement OrderedPieces(
            CardTargetOwnerRelation ownerRelation,
            int count,
            IEnumerable<PieceKind> allowedPieceKinds)
        {
            return new CardTargetRequirement(
                CardTargetKind.OrderedPieces,
                count,
                ownerRelation,
                allowedPieceKinds);
        }

        private static void EnsureValidTargetKind(CardTargetKind kind)
        {
            switch (kind)
            {
                case CardTargetKind.None:
                case CardTargetKind.PieceAtSquare:
                case CardTargetKind.BoardSquare:
                case CardTargetKind.OrderedSquares:
                case CardTargetKind.PieceAndSquare:
                case CardTargetKind.OrderedPieces:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown card target kind.");
            }
        }

        private static bool CanRestrictPieceTarget(CardTargetKind kind)
        {
            return kind == CardTargetKind.PieceAtSquare ||
                kind == CardTargetKind.PieceAndSquare ||
                kind == CardTargetKind.OrderedPieces;
        }

        private static void EnsureValidOwnerRelation(CardTargetOwnerRelation ownerRelation)
        {
            switch (ownerRelation)
            {
                case CardTargetOwnerRelation.Self:
                case CardTargetOwnerRelation.Opponent:
                case CardTargetOwnerRelation.Any:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ownerRelation), ownerRelation, "Unknown target owner relation.");
            }
        }

        private static IReadOnlyList<PieceKind> CopyAllowedPieceKinds(IEnumerable<PieceKind> allowedPieceKinds)
        {
            if (allowedPieceKinds == null)
            {
                throw new ArgumentNullException(nameof(allowedPieceKinds));
            }

            var copy = new List<PieceKind>();
            foreach (PieceKind kind in allowedPieceKinds)
            {
                if (kind == PieceKind.Unknown)
                {
                    throw new ArgumentOutOfRangeException(nameof(allowedPieceKinds), kind, "Unknown piece kind.");
                }

                if (!copy.Contains(kind))
                {
                    copy.Add(kind);
                }
            }

            return new ReadOnlyCollection<PieceKind>(copy);
        }
    }
}
