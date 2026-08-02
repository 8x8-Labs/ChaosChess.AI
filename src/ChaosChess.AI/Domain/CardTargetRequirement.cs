using System;

namespace ChaosChess.AI.Domain
{
    public sealed class CardTargetRequirement
    {
        public CardTargetRequirement(CardTargetKind kind, int count)
        {
            EnsureValidTargetKind(kind);

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

            Kind = kind;
            Count = count;
        }

        public CardTargetKind Kind { get; }

        public int Count { get; }

        public static CardTargetRequirement None()
        {
            return new CardTargetRequirement(CardTargetKind.None, count: 0);
        }

        private static void EnsureValidTargetKind(CardTargetKind kind)
        {
            switch (kind)
            {
                case CardTargetKind.None:
                case CardTargetKind.PieceAtSquare:
                case CardTargetKind.BoardSquare:
                case CardTargetKind.OrderedSquares:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown card target kind.");
            }
        }
    }
}
