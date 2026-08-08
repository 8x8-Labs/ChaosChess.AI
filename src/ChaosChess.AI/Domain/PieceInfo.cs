using System;

namespace ChaosChess.AI.Domain
{
    public sealed class PieceInfo
    {
        public PieceInfo(PieceColor color, Square square, string fenCode)
            : this(InferKind(fenCode), color, square, fenCode)
        {
        }

        public PieceInfo(PieceKind kind, PieceColor color, Square square, string fenCode)
            : this(kind, color, square, fenCode, isPromotioned: false, startSquare: null)
        {
        }

        public PieceInfo(
            PieceKind kind,
            PieceColor color,
            Square square,
            string fenCode,
            bool isPromotioned,
            Square? startSquare)
        {
            if (fenCode == null)
            {
                throw new ArgumentNullException(nameof(fenCode));
            }

            if (fenCode.Length != 1 || !IsAsciiLetter(fenCode[0]))
            {
                throw new ArgumentException("FEN code must be one ASCII letter.", nameof(fenCode));
            }

            Kind = kind;
            Color = color;
            Square = square;
            FenCode = char.ToLowerInvariant(fenCode[0]).ToString();
            IsPromotioned = isPromotioned;
            StartSquare = startSquare;
        }

        public PieceKind Kind { get; }

        public PieceColor Color { get; }

        public Square Square { get; }

        public string FenCode { get; }

        public bool IsPromotioned { get; }

        public Square? StartSquare { get; }

        public static PieceKind InferKind(string fenCode)
        {
            if (string.IsNullOrEmpty(fenCode))
            {
                return PieceKind.Unknown;
            }

            switch (char.ToLowerInvariant(fenCode[0]))
            {
                case 'p':
                    return PieceKind.Pawn;
                case 'n':
                    return PieceKind.Knight;
                case 'b':
                    return PieceKind.Bishop;
                case 'r':
                    return PieceKind.Rook;
                case 'q':
                    return PieceKind.Queen;
                case 'k':
                    return PieceKind.King;
                case 'a':
                    return PieceKind.Wall;
                case 's':
                    return PieceKind.Amazon;
                case 'y':
                    return PieceKind.Chancellor;
                case 'z':
                    return PieceKind.KnightRider;
                default:
                    return PieceKind.Unknown;
            }
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z');
        }
    }
}
