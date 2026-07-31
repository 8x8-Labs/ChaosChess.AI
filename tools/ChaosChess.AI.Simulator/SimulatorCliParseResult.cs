using System;

namespace ChaosChess.AI.Simulator
{
    public sealed class SimulatorCliParseResult
    {
        private SimulatorCliParseResult(SimulatorCliOptions? options, string? errorMessage)
        {
            Options = options;
            ErrorMessage = errorMessage;
        }

        public SimulatorCliOptions? Options { get; }

        public string? ErrorMessage { get; }

        public bool Success => Options != null;

        public static SimulatorCliParseResult Ok(SimulatorCliOptions options)
        {
            return new SimulatorCliParseResult(options ?? throw new ArgumentNullException(nameof(options)), null);
        }

        public static SimulatorCliParseResult Error(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be empty.", nameof(errorMessage));
            }

            return new SimulatorCliParseResult(null, errorMessage);
        }
    }
}
