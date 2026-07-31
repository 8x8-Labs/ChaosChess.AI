using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulator.Csv
{
    public static class SimulationCsvSchema
    {
        public const string SchemaVersion = "p7.logical.v1";

        public static readonly IReadOnlyList<string> Header = new[]
        {
            "schema_version",
            "batch_id",
            "game_index",
            "game_id",
            "base_seed",
            "game_seed",
            "scenario_id",
            "starting_fen_hash",
            "white_profile_id",
            "black_profile_id",
            "engine_sha256",
            "variant_sha256",
            "depth",
            "multipv",
            "max_ply",
            "result",
            "winner",
            "termination_reason",
            "validity",
            "ply_count",
            "final_score_cp",
            "final_mate_in",
            "cards_recommended",
            "cards_applied",
            "cards_skipped_reason",
            "filtered_move_count",
            "engine_timeout_count",
            "engine_restart_count",
            "error_code"
        };

        public static IReadOnlyList<string?> ToRow(BatchSimulationOptions options, BatchGameResult game)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return new[]
            {
                SchemaVersion,
                options.BatchId,
                CsvWriter.FormatInvariant(game.GameIndex),
                game.GameId,
                CsvWriter.FormatInvariant(options.BaseSeed),
                CsvWriter.FormatInvariant(game.GameSeed),
                game.ScenarioId,
                ComputeSha256Hex(options.StartingFen),
                game.WhiteProfileId,
                game.BlackProfileId,
                options.EngineSha256 ?? string.Empty,
                options.VariantSha256 ?? string.Empty,
                options.Depth.HasValue ? CsvWriter.FormatInvariant(options.Depth.Value) : string.Empty,
                CsvWriter.FormatInvariant(options.HeadlessGameOptions.VariationCount),
                CsvWriter.FormatInvariant(options.HeadlessGameOptions.MaxPly),
                game.GameResult.Result.ToString(),
                FormatWinner(game.GameResult.Winner),
                game.GameResult.TerminationReason.ToString(),
                FormatValidity(game.GameResult.Result),
                CsvWriter.FormatInvariant(game.GameResult.PlyCount),
                string.Empty,
                string.Empty,
                CsvWriter.FormatInvariant(game.GameResult.CardsRecommended),
                CsvWriter.FormatInvariant(game.GameResult.CardsApplied),
                game.GameResult.CardsSkippedReason ?? string.Empty,
                string.Empty,
                "0",
                "0",
                game.GameResult.ErrorCode ?? string.Empty
            };
        }

        private static string FormatWinner(PieceColor? winner)
        {
            return winner.HasValue ? winner.Value.ToString() : string.Empty;
        }

        private static string FormatValidity(GameResult result)
        {
            switch (result)
            {
                case GameResult.WhiteWin:
                case GameResult.BlackWin:
                case GameResult.Draw:
                    return "valid";
                case GameResult.Invalid:
                    return "invalid";
                case GameResult.Aborted:
                    return "aborted";
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown game result.");
            }
        }

        private static string ComputeSha256Hex(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            byte[] hash = SHA256.HashData(bytes);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (byte data in hash)
            {
                builder.Append(data.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
