using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Simulator
{
    public sealed class BatchSimulationOptions
    {
        private readonly ReadOnlyCollection<MatchupDefinition> _matchups;

        public BatchSimulationOptions(
            string batchId,
            int baseSeed,
            int gameCount,
            string scenarioId,
            string startingFen,
            IEnumerable<MatchupDefinition> matchups,
            HeadlessGameOptions? headlessGameOptions = null,
            string? engineSha256 = null,
            string? variantSha256 = null,
            int? depth = null)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                throw new ArgumentException("Batch ID cannot be empty.", nameof(batchId));
            }

            if (gameCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameCount), gameCount, "Game count must be positive.");
            }

            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                throw new ArgumentException("Scenario ID cannot be empty.", nameof(scenarioId));
            }

            if (string.IsNullOrWhiteSpace(startingFen))
            {
                throw new ArgumentException("Starting FEN cannot be empty.", nameof(startingFen));
            }

            if (depth.HasValue && depth.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth.Value, "Depth must be positive.");
            }

            BatchId = batchId;
            BaseSeed = baseSeed;
            GameCount = gameCount;
            ScenarioId = scenarioId;
            StartingFen = startingFen;
            _matchups = CopyMatchups(matchups);
            HeadlessGameOptions = headlessGameOptions ?? new HeadlessGameOptions();
            EngineSha256 = engineSha256;
            VariantSha256 = variantSha256;
            Depth = depth;
        }

        public string BatchId { get; }

        public int BaseSeed { get; }

        public int GameCount { get; }

        public string ScenarioId { get; }

        public string StartingFen { get; }

        public IReadOnlyList<MatchupDefinition> Matchups => _matchups;

        public HeadlessGameOptions HeadlessGameOptions { get; }

        public string? EngineSha256 { get; }

        public string? VariantSha256 { get; }

        public int? Depth { get; }

        private static ReadOnlyCollection<MatchupDefinition> CopyMatchups(IEnumerable<MatchupDefinition> matchups)
        {
            if (matchups == null)
            {
                throw new ArgumentNullException(nameof(matchups));
            }

            var copy = new List<MatchupDefinition>();

            foreach (MatchupDefinition matchup in matchups)
            {
                if (matchup == null)
                {
                    throw new ArgumentException("Matchup collection cannot contain null.", nameof(matchups));
                }

                copy.Add(matchup);
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException("At least one matchup is required.", nameof(matchups));
            }

            return copy.AsReadOnly();
        }
    }
}
