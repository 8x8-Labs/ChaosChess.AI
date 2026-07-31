using System;

namespace ChaosChess.AI.Simulator
{
    public sealed class HeadlessGameOptions
    {
        public const int DefaultMaxPly = 200;
        public const int DefaultVariationCount = 3;
        public const int DefaultSimulationHorizonPly = 1;

        public HeadlessGameOptions(
            int maxPly = DefaultMaxPly,
            int variationCount = DefaultVariationCount,
            int simulationHorizonPly = DefaultSimulationHorizonPly,
            bool useRandomTieBreak = false,
            int? seed = null)
        {
            if (maxPly <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPly), maxPly, "Max ply must be positive.");
            }

            if (variationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(variationCount), variationCount, "Variation count must be positive.");
            }

            if (simulationHorizonPly <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(simulationHorizonPly), simulationHorizonPly, "Simulation horizon must be positive.");
            }

            MaxPly = maxPly;
            VariationCount = variationCount;
            SimulationHorizonPly = simulationHorizonPly;
            UseRandomTieBreak = useRandomTieBreak;
            Seed = seed;
        }

        public int MaxPly { get; }

        public int VariationCount { get; }

        public int SimulationHorizonPly { get; }

        public bool UseRandomTieBreak { get; }

        public int? Seed { get; }
    }
}
