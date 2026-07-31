using System;

namespace ChaosChess.AI.Evaluation
{
    public sealed class EvaluationOptions
    {
        public EvaluationOptions(
            int searchDepth = 12,
            double boardScoreWeight = 1.0,
            double threatWeight = 0.8,
            double advantageWeight = 0.6)
        {
            if (searchDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(searchDepth), searchDepth, "Search depth must be positive.");
            }

            SearchDepth = searchDepth;
            BoardScoreWeight = EnsureValidWeight(boardScoreWeight, nameof(boardScoreWeight));
            ThreatWeight = EnsureValidWeight(threatWeight, nameof(threatWeight));
            AdvantageWeight = EnsureValidWeight(advantageWeight, nameof(advantageWeight));
        }

        public int SearchDepth { get; }

        public double BoardScoreWeight { get; }

        public double ThreatWeight { get; }

        public double AdvantageWeight { get; }

        private static double EnsureValidWeight(double weight, string parameterName)
        {
            if (double.IsNaN(weight) || double.IsInfinity(weight) || weight < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, weight, "Evaluation weight must be finite and non-negative.");
            }

            return weight;
        }
    }
}
