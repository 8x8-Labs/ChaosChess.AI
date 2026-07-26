using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class BoardState
    {
        private readonly ReadOnlyCollection<PieceInfo> _pieces;

        public BoardState(
            IEnumerable<PieceInfo> pieces,
            PieceColor sideToMove,
            CastlingRights castlingRights,
            Square? enPassantTarget,
            int halfmoveClock,
            int fullmoveNumber)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(nameof(pieces));
            }

            CastlingRightsExtensions.EnsureValid(castlingRights);

            if (halfmoveClock < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(halfmoveClock), halfmoveClock, "Halfmove clock cannot be negative.");
            }

            if (fullmoveNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fullmoveNumber), fullmoveNumber, "Fullmove number must be at least 1.");
            }

            var copy = new List<PieceInfo>();
            var occupiedSquares = new HashSet<Square>();

            foreach (PieceInfo piece in pieces)
            {
                if (piece == null)
                {
                    throw new ArgumentException("Piece collection cannot contain null.", nameof(pieces));
                }

                if (!occupiedSquares.Add(piece.Square))
                {
                    throw new ArgumentException($"Multiple pieces occupy {piece.Square}.", nameof(pieces));
                }

                copy.Add(piece);
            }

            _pieces = copy.AsReadOnly();
            SideToMove = sideToMove;
            CastlingRights = castlingRights;
            EnPassantTarget = enPassantTarget;
            HalfmoveClock = halfmoveClock;
            FullmoveNumber = fullmoveNumber;
        }

        public IReadOnlyList<PieceInfo> Pieces => _pieces;

        public PieceColor SideToMove { get; }

        public CastlingRights CastlingRights { get; }

        public Square? EnPassantTarget { get; }

        public int HalfmoveClock { get; }

        public int FullmoveNumber { get; }

        public PieceInfo? FindPiece(Square square)
        {
            foreach (PieceInfo piece in _pieces)
            {
                if (piece.Square == square)
                {
                    return piece;
                }
            }

            return null;
        }
    }
}
