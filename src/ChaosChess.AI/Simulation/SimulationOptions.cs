using System;

namespace ChaosChess.AI.Simulation
{
    public sealed class SimulationOptions
    {
        public const int DefaultHorizonPly = 2;
        public const int MaximumHorizonPly = 8;
        public const int DefaultVariationCount = 3;

        public SimulationOptions(
            int horizonPly = DefaultHorizonPly,
            int variationCount = DefaultVariationCount,
            bool useRandomTieBreak = false,
            int? seed = null)
        {
            if (horizonPly < 0 || horizonPly > MaximumHorizonPly)
            {
                throw new ArgumentOutOfRangeException(nameof(horizonPly), horizonPly, "Horizon ply must be between 0 and 8.");
            }

            if (variationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(variationCount), variationCount, "Variation count must be positive.");
            }

            HorizonPly = horizonPly;
            VariationCount = variationCount;
            UseRandomTieBreak = useRandomTieBreak;
            Seed = seed;
        }

        public int HorizonPly { get; }

        public int VariationCount { get; }

        public bool UseRandomTieBreak { get; }

        public int? Seed { get; }
    }
}
