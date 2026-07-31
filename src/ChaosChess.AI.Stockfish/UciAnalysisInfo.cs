using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Stockfish
{
    public enum UciScoreBound
    {
        Exact,
        Lower,
        Upper
    }

    public sealed class UciAnalysisInfo
    {
        private readonly ReadOnlyCollection<string> _principalVariation;

        public UciAnalysisInfo(
            int depth,
            int multipv,
            int? scoreCentipawns,
            int? mateIn,
            UciScoreBound bound,
            IEnumerable<string> principalVariation)
        {
            if (depth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth cannot be negative.");
            }

            if (multipv <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(multipv), multipv, "MultiPV index must be positive.");
            }

            if (scoreCentipawns.HasValue == mateIn.HasValue)
            {
                throw new ArgumentException("Exactly one of centipawn score or mate distance must be supplied.");
            }

            Depth = depth;
            Multipv = multipv;
            ScoreCentipawns = scoreCentipawns;
            MateIn = mateIn;
            Bound = bound;
            _principalVariation = CopyPrincipalVariation(principalVariation);
        }

        public int Depth { get; }

        public int Multipv { get; }

        public int? ScoreCentipawns { get; }

        public int? MateIn { get; }

        public UciScoreBound Bound { get; }

        public IReadOnlyList<string> PrincipalVariation => _principalVariation;

        private static ReadOnlyCollection<string> CopyPrincipalVariation(IEnumerable<string> principalVariation)
        {
            if (principalVariation == null)
            {
                throw new ArgumentNullException(nameof(principalVariation));
            }

            var copy = new List<string>();

            foreach (string move in principalVariation)
            {
                if (string.IsNullOrWhiteSpace(move))
                {
                    throw new ArgumentException("Principal variation cannot contain empty moves.", nameof(principalVariation));
                }

                copy.Add(move);
            }

            return copy.AsReadOnly();
        }
    }
}
