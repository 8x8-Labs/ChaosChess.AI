using System;

namespace ChaosChess.AI.Stockfish
{
    public sealed class UciBestMove
    {
        public UciBestMove(string? move, string? ponderMove)
        {
            if (move != null && string.IsNullOrWhiteSpace(move))
            {
                throw new ArgumentException("Best move cannot be empty.", nameof(move));
            }

            if (ponderMove != null && string.IsNullOrWhiteSpace(ponderMove))
            {
                throw new ArgumentException("Ponder move cannot be empty.", nameof(ponderMove));
            }

            Move = move;
            PonderMove = ponderMove;
        }

        public string? Move { get; }

        public string? PonderMove { get; }

        public bool IsNone => Move == null;
    }
}
