using System;

namespace ChaosChess.AI.Simulator
{
    public sealed class SimulatorCliOptions
    {
        public SimulatorCliOptions(
            bool showHelp,
            int games,
            int seed,
            int maxPly,
            int multipv,
            string? outputPath,
            bool overwrite,
            string? enginePath,
            string? variantConfigPath,
            int depth)
        {
            if (games <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(games), games, "Game count must be positive.");
            }

            if (maxPly <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPly), maxPly, "Max ply must be positive.");
            }

            if (multipv <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(multipv), multipv, "MultiPV must be positive.");
            }

            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be positive.");
            }

            ShowHelp = showHelp;
            Games = games;
            Seed = seed;
            MaxPly = maxPly;
            MultiPv = multipv;
            OutputPath = outputPath;
            Overwrite = overwrite;
            EnginePath = enginePath;
            VariantConfigPath = variantConfigPath;
            Depth = depth;
        }

        public bool ShowHelp { get; }

        public int Games { get; }

        public int Seed { get; }

        public int MaxPly { get; }

        public int MultiPv { get; }

        public string? OutputPath { get; }

        public bool Overwrite { get; }

        public string? EnginePath { get; }

        public string? VariantConfigPath { get; }

        public int Depth { get; }

        public bool IsEngineMode => EnginePath != null || VariantConfigPath != null;
    }
}
