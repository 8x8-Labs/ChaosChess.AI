using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision
{
    public sealed class MoveRecommendation
    {
        private readonly ReadOnlyCollection<string> _reasons;

        public MoveRecommendation(
            MoveCandidate candidate,
            int originalIndex,
            int engineScore,
            int adjustmentScore,
            int adjustedScore,
            IEnumerable<string> reasons)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));

            if (originalIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(originalIndex), originalIndex, "Original index cannot be negative.");
            }

            if (reasons == null)
            {
                throw new ArgumentNullException(nameof(reasons));
            }

            var copy = new List<string>();

            foreach (string reason in reasons)
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentException("Reason collection cannot contain empty values.", nameof(reasons));
                }

                copy.Add(reason);
            }

            OriginalIndex = originalIndex;
            EngineScore = engineScore;
            AdjustmentScore = adjustmentScore;
            AdjustedScore = adjustedScore;
            _reasons = copy.AsReadOnly();
        }

        public MoveCandidate Candidate { get; }

        public string UciMove => Candidate.UciMove;

        public int OriginalIndex { get; }

        public int EngineScore { get; }

        public int AdjustmentScore { get; }

        public int AdjustedScore { get; }

        public IReadOnlyList<string> Reasons => _reasons;
    }
}
