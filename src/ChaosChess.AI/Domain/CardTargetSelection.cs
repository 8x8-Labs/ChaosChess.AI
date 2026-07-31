using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class CardTargetSelection
    {
        private readonly ReadOnlyCollection<Square> _squares;

        private CardTargetSelection(
            CardTargetKind kind,
            PieceTargetSnapshot? piece,
            IEnumerable<Square> squares)
        {
            Kind = kind;
            Piece = piece;
            _squares = CopySquares(squares);
        }

        public CardTargetKind Kind { get; }

        public PieceTargetSnapshot? Piece { get; }

        public IReadOnlyList<Square> Squares => _squares;

        public static CardTargetSelection None()
        {
            return new CardTargetSelection(
                CardTargetKind.None,
                piece: null,
                squares: Array.Empty<Square>());
        }

        public static CardTargetSelection PieceAtSquare(PieceTargetSnapshot piece)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            return new CardTargetSelection(
                CardTargetKind.PieceAtSquare,
                piece,
                new[] { piece.Square });
        }

        public static CardTargetSelection BoardSquare(Square square)
        {
            return new CardTargetSelection(
                CardTargetKind.BoardSquare,
                piece: null,
                new[] { square });
        }

        public static CardTargetSelection OrderedSquares(IEnumerable<Square> squares)
        {
            if (squares == null)
            {
                throw new ArgumentNullException(nameof(squares));
            }

            var copy = new List<Square>();

            foreach (Square square in squares)
            {
                copy.Add(square);
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException("Ordered square target must contain at least one square.", nameof(squares));
            }

            return new CardTargetSelection(
                CardTargetKind.OrderedSquares,
                piece: null,
                squares: copy);
        }

        private static ReadOnlyCollection<Square> CopySquares(IEnumerable<Square> squares)
        {
            if (squares == null)
            {
                throw new ArgumentNullException(nameof(squares));
            }

            return new List<Square>(squares).AsReadOnly();
        }
    }
}
