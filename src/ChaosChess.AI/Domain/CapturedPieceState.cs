using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain
{
    public sealed class CapturedPieceState
    {
        public static readonly CapturedPieceState Empty = new CapturedPieceState(
            Array.Empty<PieceKind>(),
            Array.Empty<PieceKind>());

        private readonly ReadOnlyCollection<PieceKind> _whitePieces;
        private readonly ReadOnlyCollection<PieceKind> _blackPieces;

        public CapturedPieceState(
            IEnumerable<PieceKind> whitePieces,
            IEnumerable<PieceKind> blackPieces)
        {
            _whitePieces = CopyPieces(whitePieces, nameof(whitePieces));
            _blackPieces = CopyPieces(blackPieces, nameof(blackPieces));
        }

        public IReadOnlyList<PieceKind> WhitePieces => _whitePieces;

        public IReadOnlyList<PieceKind> BlackPieces => _blackPieces;

        public IReadOnlyList<PieceKind> GetPieces(PieceColor color)
        {
            EnsureValidColor(color);
            return color == PieceColor.White ? _whitePieces : _blackPieces;
        }

        private static ReadOnlyCollection<PieceKind> CopyPieces(
            IEnumerable<PieceKind> pieces,
            string parameterName)
        {
            if (pieces == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new List<PieceKind>();
            foreach (PieceKind piece in pieces)
            {
                if (piece == PieceKind.Unknown)
                {
                    throw new ArgumentOutOfRangeException(parameterName, piece, "Captured piece kind cannot be unknown.");
                }

                copy.Add(piece);
            }

            return copy.AsReadOnly();
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
