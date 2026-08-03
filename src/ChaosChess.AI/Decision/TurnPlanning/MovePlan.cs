using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class MovePlan
    {
        private readonly ReadOnlyCollection<string> _filterReasons;

        public MovePlan(
            MoveCandidate candidate,
            int originalIndex,
            int engineScore,
            int adjustmentScore,
            int adjustedScore,
            IEnumerable<string>? filterReasons = null)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));

            if (originalIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(originalIndex),
                    originalIndex,
                    "Original index cannot be negative.");
            }

            if (!TryParseUciMove(candidate.UciMove, out Square source, out Square destination, out char? promotion))
            {
                throw new ArgumentException("Move candidate UCI cannot be parsed.", nameof(candidate));
            }

            OriginalIndex = originalIndex;
            EngineScore = engineScore;
            AdjustmentScore = adjustmentScore;
            AdjustedScore = adjustedScore;
            Source = source;
            Destination = destination;
            Promotion = promotion;
            _filterReasons = CopyReasons(filterReasons);
        }

        public MoveCandidate Candidate { get; }

        public string UciMove => Candidate.UciMove;

        public Square Source { get; }

        public Square Destination { get; }

        public char? Promotion { get; }

        public int OriginalIndex { get; }

        public int EngineScore { get; }

        public int AdjustmentScore { get; }

        public int AdjustedScore { get; }

        public IReadOnlyList<string> FilterReasons => _filterReasons;

        public static MovePlan FromRecommendation(MoveRecommendation recommendation)
        {
            if (recommendation == null)
            {
                throw new ArgumentNullException(nameof(recommendation));
            }

            return new MovePlan(
                recommendation.Candidate,
                recommendation.OriginalIndex,
                recommendation.EngineScore,
                recommendation.AdjustmentScore,
                recommendation.AdjustedScore,
                recommendation.Reasons);
        }

        private static bool TryParseUciMove(
            string uciMove,
            out Square source,
            out Square destination,
            out char? promotion)
        {
            source = default;
            destination = default;
            promotion = null;

            if (uciMove == null || (uciMove.Length != 4 && uciMove.Length != 5))
            {
                return false;
            }

            if (!Square.TryParse(uciMove.Substring(0, 2), out source) ||
                !Square.TryParse(uciMove.Substring(2, 2), out destination))
            {
                return false;
            }

            if (uciMove.Length == 5)
            {
                char value = char.ToLowerInvariant(uciMove[4]);
                if (!IsAsciiLetter(value))
                {
                    return false;
                }

                promotion = value;
            }

            return true;
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'a' && value <= 'z') ||
                (value >= 'A' && value <= 'Z');
        }

        private static ReadOnlyCollection<string> CopyReasons(
            IEnumerable<string>? filterReasons)
        {
            var copy = new List<string>();

            if (filterReasons == null)
            {
                return copy.AsReadOnly();
            }

            foreach (string reason in filterReasons)
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException(
                        "Filter reason collection cannot contain empty values.",
                        nameof(filterReasons));
                }

                copy.Add(reason);
            }

            return copy.AsReadOnly();
        }
    }
}
