using System;
using System.Collections.Generic;
using System.IO;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Fen;

namespace ChaosChess.AI.Stockfish
{
    public sealed class StockfishProcessEngine : IChessEngine, IDisposable
    {
        private readonly StockfishEngineOptions _options;
        private readonly Func<StockfishEngineOptions, IStockfishProcess> _processFactory;
        private IStockfishProcess? _process;
        private bool _isReady;

        public StockfishProcessEngine(StockfishEngineOptions options)
            : this(options, static engineOptions => new StockfishProcess(engineOptions))
        {
        }

        public StockfishProcessEngine(
            StockfishEngineOptions options,
            Func<StockfishEngineOptions, IStockfishProcess> processFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
        }

        public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
        {
            if (boardState == null)
            {
                throw new ArgumentNullException(nameof(boardState));
            }

            if (variationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(variationCount), variationCount, "Variation count must be positive.");
            }

            AnalysisResult result = Analyze(boardState, _options.Depth, variationCount);

            if (result.BestMove.IsNone)
            {
                return Array.Empty<MoveCandidate>();
            }

            IReadOnlyList<MoveCandidate> candidates = UciAnalysisParser.ToMoveCandidates(
                result.AnalysisInfos,
                variationCount);

            if (candidates.Count == 0)
            {
                throw new StockfishEngineException(
                    StockfishEngineErrorCode.InvalidOutput,
                    "Stockfish returned a bestmove but no usable analysis candidate.");
            }

            return candidates;
        }

        public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
        {
            if (boardState == null)
            {
                throw new ArgumentNullException(nameof(boardState));
            }

            if (depth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Depth must be positive.");
            }

            AnalysisResult result = Analyze(boardState, depth, variationCount: 1);
            UciAnalysisInfo? latest = SelectLatestAnalysis(result.AnalysisInfos, multipv: 1);

            if (latest == null)
            {
                throw new StockfishEngineException(
                    StockfishEngineErrorCode.InvalidOutput,
                    "Stockfish returned no position evaluation info.");
            }

            return new PositionEvaluation(
                boardState.SideToMove,
                latest.ScoreCentipawns,
                latest.MateIn);
        }

        public bool IsInCheck(BoardState boardState)
        {
            if (boardState == null)
            {
                throw new ArgumentNullException(nameof(boardState));
            }

            return BoardCheckDetector.IsInCheck(boardState);
        }

        public void Dispose()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.WriteLine("quit");
                }
            }
            catch (StockfishEngineException)
            {
            }

            _process.Dispose();
            _process = null;
            _isReady = false;
        }

        private AnalysisResult Analyze(BoardState boardState, int depth, int variationCount)
        {
            IStockfishProcess process = EnsureReady();

            process.ClearOutput();
            WriteReadinessScopedOption(process, "MultiPV", variationCount.ToString());
            process.ClearOutput();
            process.WriteLine("position fen " + FenParser.Serialize(boardState));
            process.WriteLine("go depth " + depth);

            var infos = new List<UciAnalysisInfo>();

            while (true)
            {
                ThrowIfExited(process);

                string? line = process.ReadLine(TimeSpan.FromMilliseconds(_options.TimeoutMilliseconds));

                if (line == null)
                {
                    StopAndKill(process);
                    throw new StockfishEngineException(
                        StockfishEngineErrorCode.Timeout,
                        "Timed out waiting for Stockfish bestmove.");
                }

                if (UciAnalysisParser.TryParseInfoLine(line, out UciAnalysisInfo? info))
                {
                    infos.Add(info ?? throw new StockfishEngineException(
                        StockfishEngineErrorCode.InvalidOutput,
                        "Stockfish info parser returned a null result."));
                    continue;
                }

                if (UciAnalysisParser.TryParseBestMoveLine(line, out UciBestMove? bestMove))
                {
                    return new AnalysisResult(
                        infos,
                        bestMove ?? throw new StockfishEngineException(
                            StockfishEngineErrorCode.InvalidOutput,
                            "Stockfish bestmove parser returned a null result."));
                }
            }
        }

        private IStockfishProcess EnsureReady()
        {
            if (_isReady && _process != null)
            {
                return _process;
            }

            _process = _processFactory(_options);
            _process.Start();
            _process.WriteLine("uci");
            WaitForToken(_process, "uciok", StockfishEngineErrorCode.HandshakeFailed);
            _process.WriteLine("setoption name VariantPath value " + GetVariantDirectory(_options.VariantConfigPath));
            _process.WriteLine("setoption name UCI_Variant value " + _options.VariantName);
            _process.WriteLine("setoption name Threads value " + _options.Threads);
            _process.WriteLine("setoption name Hash value " + _options.HashMegabytes);
            _process.WriteLine("setoption name MultiPV value " + _options.VariationCount);
            _process.WriteLine("setoption name Ponder value " + (_options.Ponder ? "true" : "false"));
            WaitUntilReady(_process);
            _process.WriteLine("ucinewgame");

            if (_options.ClearHashBetweenGames)
            {
                _process.WriteLine("setoption name Clear Hash");
            }

            WaitUntilReady(_process);
            _isReady = true;
            return _process;
        }

        private void WriteReadinessScopedOption(IStockfishProcess process, string name, string value)
        {
            process.WriteLine("setoption name " + name + " value " + value);
            WaitUntilReady(process);
        }

        private void WaitUntilReady(IStockfishProcess process)
        {
            process.WriteLine("isready");
            WaitForToken(process, "readyok", StockfishEngineErrorCode.HandshakeFailed);
        }

        private void WaitForToken(
            IStockfishProcess process,
            string expectedToken,
            StockfishEngineErrorCode failureCode)
        {
            while (true)
            {
                ThrowIfExited(process);

                string? line = process.ReadLine(TimeSpan.FromMilliseconds(_options.TimeoutMilliseconds));

                if (line == null)
                {
                    StopAndKill(process);
                    throw new StockfishEngineException(
                        failureCode,
                        "Timed out waiting for Stockfish token '" + expectedToken + "'.");
                }

                if (string.Equals(line, expectedToken, StringComparison.Ordinal))
                {
                    return;
                }
            }
        }

        private static UciAnalysisInfo? SelectLatestAnalysis(
            IEnumerable<UciAnalysisInfo> infos,
            int multipv)
        {
            UciAnalysisInfo? selected = null;

            foreach (UciAnalysisInfo info in infos)
            {
                if (info.Multipv != multipv)
                {
                    continue;
                }

                if (selected == null || info.Depth >= selected.Depth)
                {
                    selected = info;
                }
            }

            return selected;
        }

        private static string GetVariantDirectory(string variantConfigPath)
        {
            int separatorIndex = variantConfigPath.LastIndexOfAny(new[] { '\\', '/' });

            if (separatorIndex < 0)
            {
                return ".";
            }

            if (separatorIndex == 0)
            {
                return variantConfigPath.Substring(0, 1);
            }

            if (separatorIndex == 2 && variantConfigPath.Length > 1 && variantConfigPath[1] == ':')
            {
                return variantConfigPath.Substring(0, 3);
            }

            return variantConfigPath.Substring(0, separatorIndex);
        }

        private static void ThrowIfExited(IStockfishProcess process)
        {
            if (process.HasExited)
            {
                throw new StockfishEngineException(
                    StockfishEngineErrorCode.ProcessExited,
                    "Stockfish process exited unexpectedly.");
            }
        }

        private static void StopAndKill(IStockfishProcess process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.WriteLine("stop");
                }
            }
            finally
            {
                process.Kill();
            }
        }

        private sealed class AnalysisResult
        {
            public AnalysisResult(
                IReadOnlyList<UciAnalysisInfo> analysisInfos,
                UciBestMove bestMove)
            {
                AnalysisInfos = analysisInfos ?? throw new ArgumentNullException(nameof(analysisInfos));
                BestMove = bestMove ?? throw new ArgumentNullException(nameof(bestMove));
            }

            public IReadOnlyList<UciAnalysisInfo> AnalysisInfos { get; }

            public UciBestMove BestMove { get; }
        }
    }
}
