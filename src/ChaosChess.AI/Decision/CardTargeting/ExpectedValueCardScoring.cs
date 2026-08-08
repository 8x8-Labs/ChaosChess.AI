using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    internal static class ExpectedValueCardScoring
    {
        public static PieceColor OpponentOf(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        public static int PieceValue(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return 1;
                case PieceKind.Knight:
                case PieceKind.Bishop:
                case PieceKind.King:
                    return 3;
                case PieceKind.Rook:
                    return 5;
                case PieceKind.KnightRider:
                    return 7;
                case PieceKind.Queen:
                case PieceKind.Chancellor:
                    return 9;
                case PieceKind.Amazon:
                    return 13;
                default:
                    return 0;
            }
        }

        public static int CenterScore(Square square)
        {
            int distance = Math.Abs(square.File - 3) + Math.Abs(square.Rank - 3);
            int score = 4 - distance;
            return score > 0 ? score : 0;
        }

        public static bool IsGaslightingTargetKind(PieceKind kind)
        {
            return kind == PieceKind.Pawn ||
                kind == PieceKind.Knight ||
                kind == PieceKind.Bishop ||
                kind == PieceKind.Wall ||
                kind == PieceKind.Amazon ||
                kind == PieceKind.Chancellor ||
                kind == PieceKind.KnightRider;
        }

        public static bool IsDimensionDisturbanceTargetKind(PieceKind kind)
        {
            return kind != PieceKind.King &&
                kind != PieceKind.Queen &&
                kind != PieceKind.Unknown;
        }

        public static bool IsShuffleBoardTargetKind(PieceKind kind)
        {
            return kind != PieceKind.King &&
                kind != PieceKind.Queen &&
                kind != PieceKind.Unknown;
        }

        public static bool IsArenaTargetKind(PieceKind kind)
        {
            return kind != PieceKind.King &&
                kind != PieceKind.Unknown;
        }
    }
}
