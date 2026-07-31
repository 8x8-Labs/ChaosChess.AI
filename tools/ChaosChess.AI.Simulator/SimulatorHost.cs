using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;
using ChaosChess.AI.Simulator.Csv;
using ChaosChess.AI.Stockfish;

namespace ChaosChess.AI.Simulator
{
    public sealed class SimulatorHost
    {
        private const string DefaultStartingFen = "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1";
        private readonly Func<SimulatorCliOptions, IChessEngine>? _engineFactory;

        public SimulatorHost()
        {
        }

        public SimulatorHost(Func<SimulatorCliOptions, IChessEngine> engineFactory)
        {
            _engineFactory = engineFactory ?? throw new ArgumentNullException(nameof(engineFactory));
        }

        public int Run(string[] args, TextWriter stdout, TextWriter stderr)
        {
            if (stdout == null)
            {
                throw new ArgumentNullException(nameof(stdout));
            }

            if (stderr == null)
            {
                throw new ArgumentNullException(nameof(stderr));
            }

            SimulatorCliParseResult parse = SimulatorCliParser.Parse(args);

            if (!parse.Success)
            {
                stderr.WriteLine(parse.ErrorMessage);
                stderr.WriteLine("Use --help for usage.");
                return 2;
            }

            SimulatorCliOptions options = parse.Options ?? throw new InvalidOperationException("Parser succeeded without options.");

            if (options.ShowHelp)
            {
                WriteUsage(stdout);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                stderr.WriteLine("--output is required.");
                return 2;
            }

            if ((options.EnginePath == null) != (options.VariantConfigPath == null))
            {
                stderr.WriteLine("--engine and --variant-config must be supplied together.");
                return 2;
            }

            if (File.Exists(options.OutputPath) && !options.Overwrite)
            {
                stderr.WriteLine("Output file already exists. Pass --overwrite to replace it.");
                return 3;
            }

            try
            {
                string? engineSha256 = null;
                string? variantSha256 = null;

                if (options.IsEngineMode)
                {
                    if (!File.Exists(options.EnginePath))
                    {
                        stderr.WriteLine("Engine file does not exist: " + options.EnginePath);
                        return 2;
                    }

                    if (!File.Exists(options.VariantConfigPath))
                    {
                        stderr.WriteLine("Variant config file does not exist: " + options.VariantConfigPath);
                        return 2;
                    }

                    engineSha256 = ComputeFileSha256Hex(options.EnginePath);
                    variantSha256 = ComputeFileSha256Hex(options.VariantConfigPath);
                }

                string? directory = Path.GetDirectoryName(options.OutputPath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using EngineLease engineLease = CreateEngine(options);
                BatchSimulationResult result = CreateBatch(options, engineLease.Engine, engineSha256, variantSha256);
                string csv = SimulationCsvExporter.Export(result);
                File.WriteAllText(options.OutputPath, csv);
                stderr.WriteLine("Wrote " + result.Games.Count + " game row(s) to " + options.OutputPath);
                return 0;
            }
            catch (StockfishEngineException exception)
            {
                stderr.WriteLine("Engine failed: " + exception.ErrorCode + ": " + exception.Message);
                return 4;
            }
            catch (OperationCanceledException)
            {
                stderr.WriteLine("Simulation cancelled.");
                return 130;
            }
            catch (Exception exception)
            {
                stderr.WriteLine(exception.Message);
                return 1;
            }
        }

        private static BatchSimulationResult CreateBatch(
            SimulatorCliOptions options,
            IChessEngine engine,
            string? engineSha256,
            string? variantSha256)
        {
            var simulator = new GameSimulator(
                engine,
                new GameStateEvaluator(engine),
                new CardDecisionModule(new ConfiguredCardScorer()),
                new MoveFilter(engine));
            var runner = new BatchSimulationRunner(new HeadlessGameRunner(simulator));
            var matchup = new MatchupDefinition(
                "fake-white-vs-fake-black",
                new PlayerSimulationProfile("fake-white", "default", maxCardsPerTurn: 0, useRandomTieBreak: false),
                new PlayerSimulationProfile("fake-black", "default", maxCardsPerTurn: 0, useRandomTieBreak: false),
                colorSwap: false);
            var batchOptions = new BatchSimulationOptions(
                "cli-fake",
                options.Seed,
                options.Games,
                "default-fake",
                DefaultStartingFen,
                new[] { matchup },
                new HeadlessGameOptions(
                    options.MaxPly,
                    options.MultiPv,
                    simulationHorizonPly: 1,
                    useRandomTieBreak: false),
                engineSha256,
                variantSha256,
                options.IsEngineMode ? options.Depth : null);

            return runner.Run(batchOptions);
        }

        private EngineLease CreateEngine(SimulatorCliOptions options)
        {
            if (!options.IsEngineMode)
            {
                return new EngineLease(new FakeCliChessEngine());
            }

            IChessEngine engine = _engineFactory != null
                ? _engineFactory(options)
                : new StockfishProcessEngine(new StockfishEngineOptions(
                    options.EnginePath ?? throw new InvalidOperationException("Engine path is required."),
                    options.VariantConfigPath ?? throw new InvalidOperationException("Variant config path is required."),
                    options.Depth,
                    options.MultiPv));

            return new EngineLease(engine);
        }

        private static void WriteUsage(TextWriter output)
        {
            output.WriteLine("ChaosChess.AI.Simulator");
            output.WriteLine("Usage:");
            output.WriteLine("  --games <N> --seed <S> --max-ply <N> --multipv <N> --output <path> [--overwrite]");
            output.WriteLine("  [--engine <path> --variant-config <path> --depth <N>]");
        }

        private static string ComputeFileSha256Hex(string path)
        {
            using Stream stream = File.OpenRead(path);
            byte[] hash = SHA256.HashData(stream);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (byte data in hash)
            {
                builder.Append(data.ToString("x2"));
            }

            return builder.ToString();
        }

        private sealed class EngineLease : IDisposable
        {
            public EngineLease(IChessEngine engine)
            {
                Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            }

            public IChessEngine Engine { get; }

            public void Dispose()
            {
                if (Engine is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private sealed class FakeCliChessEngine : IChessEngine
        {
            public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
            {
                if (boardState.FindPiece(Square.Parse("e2")) != null)
                {
                    return new[] { new MoveCandidate("e2e4", scoreCentipawns: 0, mateIn: null) };
                }

                return Array.Empty<MoveCandidate>();
            }

            public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
            {
                return new PositionEvaluation(boardState.SideToMove, scoreCentipawns: 0, mateIn: null);
            }

            public bool IsInCheck(BoardState boardState)
            {
                return false;
            }
        }
    }
}
