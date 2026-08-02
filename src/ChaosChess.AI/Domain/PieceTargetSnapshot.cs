using System;

namespace ChaosChess.AI.Domain
{
    public sealed class PieceTargetSnapshot
    {
        public PieceTargetSnapshot(
            Square square,
            PieceColor expectedColor,
            PieceKind expectedKind)
        {
            EnsureValidColor(expectedColor);

            if (expectedKind == PieceKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedKind), expectedKind, "Expected piece kind cannot be unknown.");
            }

            Square = square;
            ExpectedColor = expectedColor;
            ExpectedKind = expectedKind;
        }

        public Square Square { get; }

        public PieceColor ExpectedColor { get; }

        public PieceKind ExpectedKind { get; }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
