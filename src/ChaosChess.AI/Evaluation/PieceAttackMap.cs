using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Evaluation
{
    internal static class PieceAttackMap
    {
        private static readonly (int File, int Rank)[] KnightDirections =
        {
            (1, 2),
            (2, 1),
            (2, -1),
            (1, -2),
            (-1, -2),
            (-2, -1),
            (-2, 1),
            (-1, 2)
        };

        private static readonly (int File, int Rank)[] DiagonalDirections =
        {
            (1, 1),
            (1, -1),
            (-1, -1),
            (-1, 1)
        };

        private static readonly (int File, int Rank)[] OrthogonalDirections =
        {
            (1, 0),
            (0, -1),
            (-1, 0),
            (0, 1)
        };

        public static HashSet<Square> Create(BoardState boardState, PieceColor attackerColor)
        {
            var occupiedSquares = new HashSet<Square>();

            foreach (PieceInfo piece in boardState.Pieces)
            {
                occupiedSquares.Add(piece.Square);
            }

            var attackedSquares = new HashSet<Square>();

            foreach (PieceInfo piece in boardState.Pieces)
            {
                if (piece.Color != attackerColor)
                {
                    continue;
                }

                AddPieceAttacks(piece, occupiedSquares, attackedSquares);
            }

            return attackedSquares;
        }

        private static void AddPieceAttacks(
            PieceInfo piece,
            HashSet<Square> occupiedSquares,
            HashSet<Square> attackedSquares)
        {
            switch (piece.Kind)
            {
                case PieceKind.Pawn:
                    AddPawnAttacks(piece, attackedSquares);
                    break;
                case PieceKind.Knight:
                    AddStepAttacks(piece.Square, KnightDirections, attackedSquares);
                    break;
                case PieceKind.Bishop:
                    AddSlidingAttacks(piece.Square, DiagonalDirections, occupiedSquares, attackedSquares);
                    break;
                case PieceKind.Rook:
                    AddSlidingAttacks(piece.Square, OrthogonalDirections, occupiedSquares, attackedSquares);
                    break;
                case PieceKind.Queen:
                    AddSlidingAttacks(piece.Square, DiagonalDirections, occupiedSquares, attackedSquares);
                    AddSlidingAttacks(piece.Square, OrthogonalDirections, occupiedSquares, attackedSquares);
                    break;
                case PieceKind.King:
                    AddStepAttacks(piece.Square, DiagonalDirections, attackedSquares);
                    AddStepAttacks(piece.Square, OrthogonalDirections, attackedSquares);
                    break;
                case PieceKind.Amazon:
                    AddSlidingAttacks(piece.Square, DiagonalDirections, occupiedSquares, attackedSquares);
                    AddSlidingAttacks(piece.Square, OrthogonalDirections, occupiedSquares, attackedSquares);
                    AddStepAttacks(piece.Square, KnightDirections, attackedSquares);
                    break;
                case PieceKind.Chancellor:
                    AddSlidingAttacks(piece.Square, OrthogonalDirections, occupiedSquares, attackedSquares);
                    AddStepAttacks(piece.Square, KnightDirections, attackedSquares);
                    break;
                case PieceKind.KnightRider:
                    AddKnightRiderAttacks(piece.Square, occupiedSquares, attackedSquares);
                    break;
            }
        }

        private static void AddPawnAttacks(PieceInfo piece, HashSet<Square> attackedSquares)
        {
            int rankDirection = piece.Color == PieceColor.White ? 1 : -1;
            AddIfOnBoard(piece.Square.File - 1, piece.Square.Rank + rankDirection, attackedSquares);
            AddIfOnBoard(piece.Square.File + 1, piece.Square.Rank + rankDirection, attackedSquares);
        }

        private static void AddStepAttacks(
            Square origin,
            (int File, int Rank)[] directions,
            HashSet<Square> attackedSquares)
        {
            foreach ((int file, int rank) in directions)
            {
                AddIfOnBoard(origin.File + file, origin.Rank + rank, attackedSquares);
            }
        }

        private static void AddSlidingAttacks(
            Square origin,
            (int File, int Rank)[] directions,
            HashSet<Square> occupiedSquares,
            HashSet<Square> attackedSquares)
        {
            foreach ((int fileDirection, int rankDirection) in directions)
            {
                int file = origin.File + fileDirection;
                int rank = origin.Rank + rankDirection;

                while (IsOnBoard(file, rank))
                {
                    var square = new Square(file, rank);
                    attackedSquares.Add(square);

                    if (occupiedSquares.Contains(square))
                    {
                        break;
                    }

                    file += fileDirection;
                    rank += rankDirection;
                }
            }
        }

        private static void AddKnightRiderAttacks(
            Square origin,
            HashSet<Square> occupiedSquares,
            HashSet<Square> attackedSquares)
        {
            foreach ((int fileDirection, int rankDirection) in KnightDirections)
            {
                int file = origin.File + fileDirection;
                int rank = origin.Rank + rankDirection;

                while (IsOnBoard(file, rank))
                {
                    var square = new Square(file, rank);
                    attackedSquares.Add(square);

                    if (occupiedSquares.Contains(square))
                    {
                        break;
                    }

                    file += fileDirection;
                    rank += rankDirection;
                }
            }
        }

        private static void AddIfOnBoard(int file, int rank, HashSet<Square> attackedSquares)
        {
            if (IsOnBoard(file, rank))
            {
                attackedSquares.Add(new Square(file, rank));
            }
        }

        private static bool IsOnBoard(int file, int rank)
        {
            return file >= 0 &&
                   file < Square.BoardSize &&
                   rank >= 0 &&
                   rank < Square.BoardSize;
        }
    }
}
