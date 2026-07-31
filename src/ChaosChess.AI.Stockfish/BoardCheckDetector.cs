using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Stockfish
{
    public static class BoardCheckDetector
    {
        private static readonly int[,] KnightOffsets =
        {
            { 1, 2 },
            { 2, 1 },
            { 2, -1 },
            { 1, -2 },
            { -1, -2 },
            { -2, -1 },
            { -2, 1 },
            { -1, 2 }
        };

        private static readonly int[,] DiagonalDirections =
        {
            { 1, 1 },
            { 1, -1 },
            { -1, -1 },
            { -1, 1 }
        };

        private static readonly int[,] OrthogonalDirections =
        {
            { 1, 0 },
            { 0, -1 },
            { -1, 0 },
            { 0, 1 }
        };

        public static bool IsInCheck(BoardState boardState)
        {
            if (boardState == null)
            {
                throw new ArgumentNullException(nameof(boardState));
            }

            PieceInfo? king = FindKing(boardState, boardState.SideToMove);

            if (king == null)
            {
                return false;
            }

            var piecesBySquare = new Dictionary<Square, PieceInfo>();

            foreach (PieceInfo piece in boardState.Pieces)
            {
                piecesBySquare.Add(piece.Square, piece);
            }

            foreach (PieceInfo attacker in boardState.Pieces)
            {
                if (attacker.Color == king.Color)
                {
                    continue;
                }

                if (AttacksSquare(attacker, king.Square, piecesBySquare))
                {
                    return true;
                }
            }

            return false;
        }

        private static PieceInfo? FindKing(BoardState boardState, PieceColor color)
        {
            foreach (PieceInfo piece in boardState.Pieces)
            {
                if (piece.Color == color && piece.Kind == PieceKind.King)
                {
                    return piece;
                }
            }

            return null;
        }

        private static bool AttacksSquare(
            PieceInfo attacker,
            Square target,
            IReadOnlyDictionary<Square, PieceInfo> piecesBySquare)
        {
            switch (attacker.Kind)
            {
                case PieceKind.Pawn:
                    return PawnAttacks(attacker, target);
                case PieceKind.Knight:
                    return KnightAttacks(attacker.Square, target);
                case PieceKind.Bishop:
                    return RayAttacks(attacker.Square, target, piecesBySquare, DiagonalDirections, repeatKnightStep: false);
                case PieceKind.Rook:
                    return RayAttacks(attacker.Square, target, piecesBySquare, OrthogonalDirections, repeatKnightStep: false);
                case PieceKind.Queen:
                    return RayAttacks(attacker.Square, target, piecesBySquare, DiagonalDirections, repeatKnightStep: false) ||
                        RayAttacks(attacker.Square, target, piecesBySquare, OrthogonalDirections, repeatKnightStep: false);
                case PieceKind.King:
                    return ChebyshevDistance(attacker.Square, target) == 1;
                case PieceKind.Amazon:
                    return KnightAttacks(attacker.Square, target) ||
                        RayAttacks(attacker.Square, target, piecesBySquare, DiagonalDirections, repeatKnightStep: false) ||
                        RayAttacks(attacker.Square, target, piecesBySquare, OrthogonalDirections, repeatKnightStep: false);
                case PieceKind.Chancellor:
                    return KnightAttacks(attacker.Square, target) ||
                        RayAttacks(attacker.Square, target, piecesBySquare, OrthogonalDirections, repeatKnightStep: false);
                case PieceKind.KnightRider:
                    return RayAttacks(attacker.Square, target, piecesBySquare, KnightOffsets, repeatKnightStep: true);
                default:
                    return false;
            }
        }

        private static bool PawnAttacks(PieceInfo pawn, Square target)
        {
            int direction = pawn.Color == PieceColor.White ? 1 : -1;
            return target.Rank - pawn.Square.Rank == direction &&
                Math.Abs(target.File - pawn.Square.File) == 1;
        }

        private static bool KnightAttacks(Square attacker, Square target)
        {
            int fileDelta = Math.Abs(target.File - attacker.File);
            int rankDelta = Math.Abs(target.Rank - attacker.Rank);
            return (fileDelta == 1 && rankDelta == 2) ||
                (fileDelta == 2 && rankDelta == 1);
        }

        private static bool RayAttacks(
            Square attacker,
            Square target,
            IReadOnlyDictionary<Square, PieceInfo> piecesBySquare,
            int[,] directions,
            bool repeatKnightStep)
        {
            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int file = attacker.File + directions[i, 0];
                int rank = attacker.Rank + directions[i, 1];

                while (IsInsideBoard(file, rank))
                {
                    var current = new Square(file, rank);

                    if (current == target)
                    {
                        return true;
                    }

                    if (piecesBySquare.ContainsKey(current))
                    {
                        break;
                    }

                    file += directions[i, 0];
                    rank += directions[i, 1];

                    if (!repeatKnightStep && directions == KnightOffsets)
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private static bool IsInsideBoard(int file, int rank)
        {
            return file >= 0 && file < Square.BoardSize &&
                rank >= 0 && rank < Square.BoardSize;
        }

        private static int ChebyshevDistance(Square left, Square right)
        {
            return Math.Max(
                Math.Abs(left.File - right.File),
                Math.Abs(left.Rank - right.Rank));
        }
    }
}
