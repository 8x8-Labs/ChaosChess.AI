using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class CardTargetSelection
    {
        private readonly ReadOnlyCollection<PieceTargetSnapshot> _pieces;
        private readonly ReadOnlyCollection<Square> _squares;

        private CardTargetSelection(
            CardTargetKind kind,
            PieceTargetSnapshot? piece,
            IEnumerable<PieceTargetSnapshot> pieces,
            IEnumerable<Square> squares)
        {
            Kind = kind;
            Piece = piece;
            _pieces = CopyPieces(pieces);
            _squares = CopySquares(squares);
        }

        public CardTargetKind Kind { get; }

        public PieceTargetSnapshot? Piece { get; }

        public IReadOnlyList<PieceTargetSnapshot> Pieces => _pieces;

        public IReadOnlyList<Square> Squares => _squares;

        public static CardTargetSelection None()
        {
            return new CardTargetSelection(
                CardTargetKind.None,
                piece: null,
                pieces: Array.Empty<PieceTargetSnapshot>(),
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
                new[] { piece },
                new[] { piece.Square });
        }

        public static CardTargetSelection BoardSquare(Square square)
        {
            return new CardTargetSelection(
                CardTargetKind.BoardSquare,
                piece: null,
                pieces: Array.Empty<PieceTargetSnapshot>(),
                new[] { square });
        }

        public static CardTargetSelection PieceAndSquare(
            PieceTargetSnapshot piece,
            Square square)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            return new CardTargetSelection(
                CardTargetKind.PieceAndSquare,
                piece,
                new[] { piece },
                new[] { square });
        }

        public static CardTargetSelection OrderedPieces(IEnumerable<PieceTargetSnapshot> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            var copy = new List<PieceTargetSnapshot>();
            var squares = new List<Square>();

            foreach (PieceTargetSnapshot piece in pieces)
            {
                if (piece == null)
                {
                    throw new ArgumentException("Ordered piece target cannot contain null.", nameof(pieces));
                }

                copy.Add(piece);
                squares.Add(piece.Square);
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException("Ordered piece target must contain at least one piece.", nameof(pieces));
            }

            return new CardTargetSelection(
                CardTargetKind.OrderedPieces,
                piece: copy[0],
                pieces: copy,
                squares: squares);
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
                pieces: Array.Empty<PieceTargetSnapshot>(),
                squares: copy);
        }

        private static ReadOnlyCollection<PieceTargetSnapshot> CopyPieces(IEnumerable<PieceTargetSnapshot> pieces)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            return new List<PieceTargetSnapshot>(pieces).AsReadOnly();
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
