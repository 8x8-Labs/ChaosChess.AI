using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Fen;
using ChaosChess.AI.Simulator;
using ChaosChess.AI.Simulator.Csv;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class SimulationCsvExporterTests
    {
        private const string StartingFen = "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1";

        [Fact]
        public void Export_WritesStableHeaderAndRowOrder()
        {
            BatchSimulationResult result = Result(
                Game(0, "game-0", 11, "white", "black", GameResult.WhiteWin),
                Game(1, "game-1", 22, "white", "black", GameResult.Draw));

            string csv = SimulationCsvExporter.Export(result);
            string[] lines = csv.Split('\n');

            Assert.Equal(HeaderLine(), lines[0]);
            Assert.Contains(",0,game-0,12345,11,", lines[1]);
            Assert.Contains(",1,game-1,12345,22,", lines[2]);
        }

        [Fact]
        public void Export_EscapesCommaQuoteAndNewline()
        {
            BatchSimulationResult result = Result(
                Game(0, "game,\"0\"\nnext", 11, "white,profile", "black\"profile", GameResult.Invalid));

            string csv = SimulationCsvExporter.Export(result);

            Assert.Contains("\"game,\"\"0\"\"\nnext\"", csv);
            Assert.Contains("\"white,profile\"", csv);
            Assert.Contains("\"black\"\"profile\"", csv);
        }

        [Fact]
        public void Export_UsesInvariantCultureAndStableFenHash()
        {
            BatchSimulationResult result = Result(
                Game(0, "game-0", 1805374444, "white", "black", GameResult.Aborted));

            string csv = SimulationCsvExporter.Export(result);

            Assert.Contains(",12345,1805374444,", csv);
            Assert.Contains(",bdea6a063486098eb5025b03dfb5698fbde3743f62e5c6825bbdd6ce3cf3995c,", csv);
        }

        [Fact]
        public void Export_RecordsEngineProvenance()
        {
            BatchSimulationResult result = ResultWithOptions(
                new[] { Game(0, "game-0", 11, "white", "black", GameResult.Aborted) },
                engineSha256: "engine-hash",
                variantSha256: "variant-hash",
                depth: 8);

            string csv = SimulationCsvExporter.Export(result);

            Assert.Contains(",engine-hash,variant-hash,8,3,200,", csv);
        }

        [Fact]
        public void Export_SameBatchResult_ReturnsByteEquivalentText()
        {
            BatchSimulationResult first = Result(
                Game(0, "game-0", 11, "white", "black", GameResult.WhiteWin));
            BatchSimulationResult second = Result(
                Game(0, "game-0", 11, "white", "black", GameResult.WhiteWin));

            Assert.Equal(
                SimulationCsvExporter.Export(first),
                SimulationCsvExporter.Export(second));
        }

        [Fact]
        public void Export_RecordsCardRecommendationAndSkippedReason()
        {
            BatchSimulationResult result = Result(
                Game(
                    0,
                    "game-0",
                    11,
                    "white",
                    "black",
                    GameResult.Aborted,
                    cardsRecommended: 2,
                    cardsApplied: 0,
                    cardsSkippedReason: "not_applied_contract_missing"));

            string csv = SimulationCsvExporter.Export(result);

            Assert.Contains(",2,0,not_applied_contract_missing,", csv);
        }

        [Theory]
        [InlineData(GameResult.WhiteWin, "valid")]
        [InlineData(GameResult.BlackWin, "valid")]
        [InlineData(GameResult.Draw, "valid")]
        [InlineData(GameResult.Invalid, "invalid")]
        [InlineData(GameResult.Aborted, "aborted")]
        public void Export_MapsValidity(GameResult gameResult, string validity)
        {
            BatchSimulationResult result = Result(
                Game(0, "game-0", 11, "white", "black", gameResult));

            string csv = SimulationCsvExporter.Export(result);

            Assert.Contains("," + validity + ",", csv);
        }

        [Fact]
        public void Export_PreservesInvalidAndAbortedRows()
        {
            BatchSimulationResult result = Result(
                Game(0, "invalid", 11, "white", "black", GameResult.Invalid),
                Game(1, "aborted", 22, "white", "black", GameResult.Aborted));

            string csv = SimulationCsvExporter.Export(result);
            string[] lines = csv.Split('\n');

            Assert.Equal(3, lines.Length);
            Assert.Contains(",invalid,", lines[1]);
            Assert.Contains(",aborted,", lines[2]);
        }

        private static string HeaderLine()
        {
            return string.Join(",", SimulationCsvSchema.Header);
        }

        private static BatchSimulationResult Result(params BatchGameResult[] games)
        {
            return ResultWithOptions(games);
        }

        private static BatchSimulationResult ResultWithOptions(
            BatchGameResult[] games,
            string? engineSha256 = null,
            string? variantSha256 = null,
            int? depth = null)
        {
            var matchup = new MatchupDefinition(
                "white-vs-black",
                Profile("white"),
                Profile("black"),
                colorSwap: false);
            var options = new BatchSimulationOptions(
                "batch-1",
                baseSeed: 12345,
                gameCount: Math.Max(1, games.Length),
                "opening",
                StartingFen,
                new[] { matchup },
                new HeadlessGameOptions(maxPly: 200, variationCount: 3),
                engineSha256,
                variantSha256,
                depth);

            return new BatchSimulationResult(options, games);
        }

        private static BatchGameResult Game(
            int gameIndex,
            string gameId,
            int gameSeed,
            string whiteProfileId,
            string blackProfileId,
            GameResult gameResult,
            int cardsRecommended = 0,
            int cardsApplied = 0,
            string? cardsSkippedReason = null)
        {
            GameState state = new GameState(
                FenParser.Parse(StartingFen),
                Array.Empty<CardInfo>(),
                Array.Empty<TileEffectInfo>());
            var result = new HeadlessGameResult(
                state,
                state,
                plyCount: gameResult == GameResult.Draw ? 0 : 1,
                gameResult,
                Winner(gameResult),
                Termination(gameResult),
                cardsRecommended,
                cardsApplied,
                cardsSkippedReason,
                Array.Empty<string>(),
                ErrorCode(gameResult));

            return new BatchGameResult(
                gameIndex,
                gameId,
                gameSeed,
                matchupOrdinal: 0,
                "opening",
                whiteProfileId,
                blackProfileId,
                result);
        }

        private static PlayerSimulationProfile Profile(string profileId)
        {
            return new PlayerSimulationProfile(profileId, "default", maxCardsPerTurn: 1, useRandomTieBreak: false);
        }

        private static PieceColor? Winner(GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                    return PieceColor.White;
                case GameResult.BlackWin:
                    return PieceColor.Black;
                default:
                    return null;
            }
        }

        private static GameTerminationReason Termination(GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                case GameResult.BlackWin:
                    return GameTerminationReason.Checkmate;
                case GameResult.Draw:
                    return GameTerminationReason.Stalemate;
                case GameResult.Invalid:
                    return GameTerminationReason.NoRecommendations;
                case GameResult.Aborted:
                    return GameTerminationReason.MaxPly;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown game result.");
            }
        }

        private static string? ErrorCode(GameResult result)
        {
            switch (result)
            {
                case GameResult.Invalid:
                    return "no_recommendations";
                case GameResult.Aborted:
                    return "max_ply";
                default:
                    return null;
            }
        }
    }
}
