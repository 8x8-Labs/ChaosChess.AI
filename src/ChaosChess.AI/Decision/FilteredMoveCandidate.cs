using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision
{
    public sealed class FilteredMoveCandidate
    {
        public FilteredMoveCandidate(
            MoveCandidate? candidate,
            int originalIndex,
            string reason)
        {
            if (originalIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(originalIndex), originalIndex, "Original index cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Filter reason cannot be empty.", nameof(reason));
            }

            Candidate = candidate;
            OriginalIndex = originalIndex;
            Reason = reason;
        }

        public MoveCandidate? Candidate { get; }

        public string? UciMove => Candidate?.UciMove;

        public int OriginalIndex { get; }

        public string Reason { get; }
    }
}
