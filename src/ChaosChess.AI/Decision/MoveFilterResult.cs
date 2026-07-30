using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision
{
    public sealed class MoveFilterResult
    {
        private readonly ReadOnlyCollection<MoveRecommendation> _recommendations;
        private readonly ReadOnlyCollection<FilteredMoveCandidate> _filteredMoves;

        public MoveFilterResult(
            IEnumerable<MoveRecommendation> recommendations,
            IEnumerable<FilteredMoveCandidate> filteredMoves)
        {
            if (recommendations == null)
            {
                throw new ArgumentNullException(nameof(recommendations));
            }

            if (filteredMoves == null)
            {
                throw new ArgumentNullException(nameof(filteredMoves));
            }

            _recommendations = CopyRecommendations(recommendations);
            _filteredMoves = CopyFilteredMoves(filteredMoves);
        }

        public IReadOnlyList<MoveRecommendation> Recommendations => _recommendations;

        public IReadOnlyList<FilteredMoveCandidate> FilteredMoves => _filteredMoves;

        public bool HasRecommendations => _recommendations.Count > 0;

        private static ReadOnlyCollection<MoveRecommendation> CopyRecommendations(
            IEnumerable<MoveRecommendation> recommendations)
        {
            var copy = new List<MoveRecommendation>();

            foreach (MoveRecommendation recommendation in recommendations)
            {
                if (recommendation == null)
                {
                    throw new ArgumentException("Recommendation collection cannot contain null.", nameof(recommendations));
                }

                copy.Add(recommendation);
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<FilteredMoveCandidate> CopyFilteredMoves(
            IEnumerable<FilteredMoveCandidate> filteredMoves)
        {
            var copy = new List<FilteredMoveCandidate>();

            foreach (FilteredMoveCandidate filteredMove in filteredMoves)
            {
                if (filteredMove == null)
                {
                    throw new ArgumentException("Filtered move collection cannot contain null.", nameof(filteredMoves));
                }

                copy.Add(filteredMove);
            }

            return copy.AsReadOnly();
        }
    }
}
