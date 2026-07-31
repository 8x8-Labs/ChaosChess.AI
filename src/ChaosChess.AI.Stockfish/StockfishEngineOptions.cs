using System;

namespace ChaosChess.AI.Stockfish
{
    public sealed class StockfishEngineOptions
    {
        public const int DefaultDepth = 8;
        public const int DefaultVariationCount = 3;
        public const int DefaultTimeoutMilliseconds = 5000;
        public const int DefaultHashMegabytes = 16;
        public const int DefaultThreads = 1;
        public const string DefaultVariantName = "chaoschess";

        public StockfishEngineOptions(
            string enginePath,
            string variantConfigPath,
            int depth = DefaultDepth,
            int variationCount = DefaultVariationCount,
            int timeoutMilliseconds = DefaultTimeoutMilliseconds,
            int hashMegabytes = DefaultHashMegabytes,
            int threads = DefaultThreads,
            string variantName = DefaultVariantName,
            bool ponder = false,
            bool clearHashBetweenGames = true)
        {
            if (string.IsNullOrWhiteSpace(enginePath))
            {
                throw new ArgumentException("Engine path cannot be empty.", nameof(enginePath));
            }

            if (string.IsNullOrWhiteSpace(variantConfigPath))
            {
                throw new ArgumentException("Variant config path cannot be empty.", nameof(variantConfigPath));
            }

            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be positive.");
            }

            if (variationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(variationCount), variationCount, "Variation count must be positive.");
            }

            if (timeoutMilliseconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), timeoutMilliseconds, "Timeout must be positive.");
            }

            if (hashMegabytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hashMegabytes), hashMegabytes, "Hash size must be positive.");
            }

            if (threads <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threads), threads, "Thread count must be positive.");
            }

            if (string.IsNullOrWhiteSpace(variantName))
            {
                throw new ArgumentException("Variant name cannot be empty.", nameof(variantName));
            }

            EnginePath = enginePath;
            VariantConfigPath = variantConfigPath;
            Depth = depth;
            VariationCount = variationCount;
            TimeoutMilliseconds = timeoutMilliseconds;
            HashMegabytes = hashMegabytes;
            Threads = threads;
            VariantName = variantName;
            Ponder = ponder;
            ClearHashBetweenGames = clearHashBetweenGames;
        }

        public string EnginePath { get; }

        public string VariantConfigPath { get; }

        public int Depth { get; }

        public int VariationCount { get; }

        public int TimeoutMilliseconds { get; }

        public int HashMegabytes { get; }

        public int Threads { get; }

        public string VariantName { get; }

        public bool Ponder { get; }

        public bool ClearHashBetweenGames { get; }
    }
}
