using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Fen;
using ChaosChess.AI.Simulator.Balance;

namespace ChaosChess.AI.Simulator
{
    public sealed class BatchSimulationRunner
    {
        private readonly HeadlessGameRunner _gameRunner;

        public BatchSimulationRunner(HeadlessGameRunner gameRunner)
        {
            _gameRunner = gameRunner ?? throw new ArgumentNullException(nameof(gameRunner));
        }

        public BatchSimulationResult Run(BatchSimulationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var games = new List<BatchGameResult>();

            for (int gameIndex = 0; gameIndex < options.GameCount; gameIndex++)
            {
                for (int matchupOrdinal = 0; matchupOrdinal < options.Matchups.Count; matchupOrdinal++)
                {
                    MatchupDefinition matchup = options.Matchups[matchupOrdinal];
                    int gameSeed = SeedDerivation.DeriveGameSeed(
                        options.BaseSeed,
                        gameIndex,
                        matchupOrdinal,
                        matchup.ColorSwap);
                    string gameId = CreateGameId(
                        options.BatchId,
                        options.ScenarioId,
                        matchupOrdinal,
                        gameIndex,
                        gameSeed);
                    HeadlessGameOptions gameOptions = WithSeed(options.HeadlessGameOptions, gameSeed);
                    GameState initialState = CreateInitialState(options);
                    HeadlessGameResult result = _gameRunner.Run(
                        initialState,
                        GetPerspective(options),
                        gameOptions);

                    games.Add(new BatchGameResult(
                        gameIndex,
                        gameId,
                        gameSeed,
                        matchupOrdinal,
                        options.ScenarioId,
                        matchup.WhiteProfile.ProfileId,
                        matchup.BlackProfile.ProfileId,
                        result));
                }
            }

            return new BatchSimulationResult(options, games);
        }

        private static GameState CreateInitialState(BatchSimulationOptions options)
        {
            if (options.Scenario != null)
            {
                return BalanceScenarioGameStateFactory.Create(options.Scenario);
            }

            return new GameState(
                FenParser.Parse(options.StartingFen),
                Array.Empty<CardInfo>(),
                Array.Empty<TileEffectInfo>());
        }

        private static PieceColor GetPerspective(BatchSimulationOptions options)
        {
            return options.Scenario?.Actor ?? PieceColor.White;
        }

        private static HeadlessGameOptions WithSeed(HeadlessGameOptions options, int gameSeed)
        {
            return new HeadlessGameOptions(
                options.MaxPly,
                options.VariationCount,
                options.SimulationHorizonPly,
                options.UseRandomTieBreak,
                gameSeed);
        }

        private static string CreateGameId(
            string batchId,
            string scenarioId,
            int matchupOrdinal,
            int gameIndex,
            int gameSeed)
        {
            return batchId + ":" +
                scenarioId + ":" +
                "m" + matchupOrdinal + ":" +
                "g" + gameIndex + ":" +
                "s" + gameSeed;
        }
    }
}
