using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceEngineObservation
    {
        private readonly ReadOnlyCollection<MoveCandidate> _moves;

        public BalanceEngineObservation(IEnumerable<MoveCandidate>? moves = null)
        {
            _moves = CopyMoves(moves);
        }

        public IReadOnlyList<MoveCandidate> Moves => _moves;

        private static ReadOnlyCollection<MoveCandidate> CopyMoves(
            IEnumerable<MoveCandidate>? moves)
        {
            var copy = new List<MoveCandidate>();

            if (moves == null)
            {
                return copy.AsReadOnly();
            }

            foreach (MoveCandidate move in moves)
            {
                if (move == null)
                {
                    throw new ArgumentException("Move collection cannot contain null.", nameof(moves));
                }

                copy.Add(move);
            }

            return copy.AsReadOnly();
        }
    }
}
