using System;

namespace ChaosChess.AI.Evaluation
{
    public sealed class EvaluationOptions
    {
        public EvaluationOptions(
            double materialWeight = 1.0,
            double threatWeight = 0.8,
            double advantageWeight = 0.6,
            double kingSafetyWeight = 0.5)
        {
            MaterialWeight = EnsureValidWeight(materialWeight, nameof(materialWeight));
            ThreatWeight = EnsureValidWeight(threatWeight, nameof(threatWeight));
            AdvantageWeight = EnsureValidWeight(advantageWeight, nameof(advantageWeight));
            KingSafetyWeight = EnsureValidWeight(kingSafetyWeight, nameof(kingSafetyWeight));
        }

        public double MaterialWeight { get; }

        public double ThreatWeight { get; }

        public double AdvantageWeight { get; }

        public double KingSafetyWeight { get; }

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
