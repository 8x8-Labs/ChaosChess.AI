using System;

namespace ChaosChess.AI.Domain
{
    public sealed class MoveCandidate
    {
        public MoveCandidate(string uciMove, int? scoreCentipawns, int? mateIn)
        {
            if (string.IsNullOrWhiteSpace(uciMove))
            {
                throw new ArgumentException("UCI move cannot be empty.", nameof(uciMove));
            }

            if (scoreCentipawns.HasValue == mateIn.HasValue)
            {
                throw new ArgumentException("Exactly one of centipawn score or mate distance must be supplied.");
            }

            UciMove = uciMove;
            ScoreCentipawns = scoreCentipawns;
            MateIn = mateIn;
        }

        public string UciMove { get; }

        public int? ScoreCentipawns { get; }

        public int? MateIn { get; }
    }
}
