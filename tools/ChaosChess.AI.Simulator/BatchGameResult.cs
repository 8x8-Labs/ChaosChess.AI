using System;

namespace ChaosChess.AI.Simulator
{
    public sealed class BatchGameResult
    {
        public BatchGameResult(
            int gameIndex,
            string gameId,
            int gameSeed,
            int matchupOrdinal,
            string scenarioId,
            string whiteProfileId,
            string blackProfileId,
            HeadlessGameResult gameResult)
        {
            if (gameIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameIndex), gameIndex, "Game index cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("Game ID cannot be empty.", nameof(gameId));
            }

            if (matchupOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchupOrdinal), matchupOrdinal, "Matchup ordinal cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                throw new ArgumentException("Scenario ID cannot be empty.", nameof(scenarioId));
            }

            if (string.IsNullOrWhiteSpace(whiteProfileId))
            {
                throw new ArgumentException("White profile ID cannot be empty.", nameof(whiteProfileId));
            }

            if (string.IsNullOrWhiteSpace(blackProfileId))
            {
                throw new ArgumentException("Black profile ID cannot be empty.", nameof(blackProfileId));
            }

            GameIndex = gameIndex;
            GameId = gameId;
            GameSeed = gameSeed;
            MatchupOrdinal = matchupOrdinal;
            ScenarioId = scenarioId;
            WhiteProfileId = whiteProfileId;
            BlackProfileId = blackProfileId;
            GameResult = gameResult ?? throw new ArgumentNullException(nameof(gameResult));
        }

        public int GameIndex { get; }

        public string GameId { get; }

        public int GameSeed { get; }

        public int MatchupOrdinal { get; }

        public string ScenarioId { get; }

        public string WhiteProfileId { get; }

        public string BlackProfileId { get; }

        public HeadlessGameResult GameResult { get; }
    }
}
