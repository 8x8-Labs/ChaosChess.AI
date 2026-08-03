using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlannerResult
    {
        private readonly ReadOnlyCollection<TurnPlanCandidate> _candidates;

        public TurnPlannerResult(
            IEnumerable<TurnPlanCandidate> candidates,
            TurnPlannerTraceSummary traceSummary)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var copy = new List<TurnPlanCandidate>();

            foreach (TurnPlanCandidate candidate in candidates)
            {
                if (candidate == null)
                {
                    throw new ArgumentException(
                        "Candidate collection cannot contain null.",
                        nameof(candidates));
                }

                copy.Add(candidate);
            }

            copy.Sort(TurnPlanCandidate.CompareByRank);
            _candidates = copy.AsReadOnly();
            TraceSummary = traceSummary ?? throw new ArgumentNullException(nameof(traceSummary));
        }

        public IReadOnlyList<TurnPlanCandidate> Candidates => _candidates;

        public TurnPlannerTraceSummary TraceSummary { get; }

        public bool HasPlan
        {
            get
            {
                foreach (TurnPlanCandidate candidate in _candidates)
                {
                    if (candidate.HasPlan)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public TurnPlan? SelectedPlan
        {
            get
            {
                foreach (TurnPlanCandidate candidate in _candidates)
                {
                    if (candidate.HasPlan)
                    {
                        return candidate.Plan;
                    }
                }

                return null;
            }
        }
    }
}
