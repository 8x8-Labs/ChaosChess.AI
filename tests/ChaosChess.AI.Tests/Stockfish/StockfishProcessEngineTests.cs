using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Stockfish;
using Xunit;

namespace ChaosChess.AI.Tests.Stockfish
{
    public sealed class StockfishProcessEngineTests
    {
        [Fact]
        public void GetTopMoves_InitializesEngineAndSendsAnalysisCommands()
        {
            var process = new FakeStockfishProcess();
            process.EnqueueAnalysis(
                "info depth 8 multipv 1 score cp 31 pv e2e4 e7e5",
                "bestmove e2e4");
            var engine = new StockfishProcessEngine(Options(), _ => process);

            IReadOnlyList<MoveCandidate> moves = engine.GetTopMoves(Board(), variationCount: 1);

            Assert.Single(moves);
            Assert.Equal(
                new[]
                {
                    "uci",
                    "setoption name VariantPath value C:\\variant",
                    "setoption name UCI_Variant value chaoschess",
                    "setoption name Threads value 1",
                    "setoption name Hash value 16",
                    "setoption name MultiPV value 3",
                    "setoption name Ponder value false",
                    "isready",
                    "ucinewgame",
                    "setoption name Clear Hash",
                    "isready",
                    "setoption name MultiPV value 1",
                    "isready",
                    "position fen 4k3/8/8/8/8/8/4P3/4K3 w - - 0 1",
                    "go depth 8"
                },
                process.Commands);
        }

        [Fact]
        public void GetTopMoves_ConvertsAnalysisInfosToMoveCandidates()
        {
            var process = new FakeStockfishProcess();
            process.EnqueueAnalysis(
                "info depth 8 multipv 1 score cp 31 pv e2e4 e7e5",
                "info depth 8 multipv 2 score mate -2 pv d2d4 d7d5",
                "bestmove e2e4");
            var engine = new StockfishProcessEngine(Options(), _ => process);

            IReadOnlyList<MoveCandidate> moves = engine.GetTopMoves(Board(), variationCount: 2);

            Assert.Equal(2, moves.Count);
            Assert.Equal("e2e4", moves[0].UciMove);
            Assert.Equal(31, moves[0].ScoreCentipawns);
            Assert.Null(moves[0].MateIn);
            Assert.Equal("d2d4", moves[1].UciMove);
            Assert.Null(moves[1].ScoreCentipawns);
            Assert.Equal(-2, moves[1].MateIn);
        }

        [Fact]
        public void GetTopMoves_BestMoveNone_ReturnsEmptyCandidates()
        {
            var process = new FakeStockfishProcess();
            process.EnqueueAnalysis("bestmove none");
            var engine = new StockfishProcessEngine(Options(), _ => process);

            IReadOnlyList<MoveCandidate> moves = engine.GetTopMoves(Board(), variationCount: 3);

            Assert.Empty(moves);
        }

        [Fact]
        public void EvaluatePosition_UsesRequestedDepthAndReturnsLatestEvaluation()
        {
            var process = new FakeStockfishProcess();
            process.EnqueueAnalysis(
                "info depth 4 multipv 1 score cp 11 pv e2e4",
                "info depth 5 multipv 1 score cp 21 pv e2e4",
                "bestmove e2e4");
            var engine = new StockfishProcessEngine(Options(), _ => process);

            PositionEvaluation evaluation = engine.EvaluatePosition(Board(), depth: 5);

            Assert.Equal(PieceColor.White, evaluation.Perspective);
            Assert.Equal(21, evaluation.ScoreCentipawns);
            Assert.Null(evaluation.MateIn);
            Assert.Contains("go depth 5", process.Commands);
        }

        [Fact]
        public void HandshakeTimeout_KillsProcessAndThrows()
        {
            var process = new FakeStockfishProcess
            {
                RespondToUci = false
            };
            var engine = new StockfishProcessEngine(Options(), _ => process);

            StockfishEngineException exception = Assert.Throws<StockfishEngineException>(
                () => engine.GetTopMoves(Board(), variationCount: 1));

            Assert.Equal(StockfishEngineErrorCode.HandshakeFailed, exception.ErrorCode);
            Assert.True(process.Killed);
        }

        [Fact]
        public void AnalysisTimeout_StopsAndKillsProcess()
        {
            var process = new FakeStockfishProcess();
            var engine = new StockfishProcessEngine(Options(), _ => process);

            StockfishEngineException exception = Assert.Throws<StockfishEngineException>(
                () => engine.GetTopMoves(Board(), variationCount: 1));

            Assert.Equal(StockfishEngineErrorCode.Timeout, exception.ErrorCode);
            Assert.Contains("stop", process.Commands);
            Assert.True(process.Killed);
        }

        [Fact]
        public void Dispose_SendsQuit()
        {
            var process = new FakeStockfishProcess();
            process.EnqueueAnalysis("info depth 8 score cp 1 pv e2e4", "bestmove e2e4");
            var engine = new StockfishProcessEngine(Options(), _ => process);
            engine.GetTopMoves(Board(), variationCount: 1);

            engine.Dispose();

            Assert.Contains("quit", process.Commands);
            Assert.True(process.Disposed);
        }

        private static StockfishEngineOptions Options()
        {
            return new StockfishEngineOptions(
                enginePath: "C:\\engine\\fairy-stockfish.exe",
                variantConfigPath: "C:\\variant\\variants.ini",
                depth: 8,
                variationCount: 3,
                timeoutMilliseconds: 1,
                hashMegabytes: 16);
        }

        private static BoardState Board()
        {
            return new BoardState(
                new[]
                {
                    new PieceInfo(PieceKind.King, PieceColor.White, Square.Parse("e1"), "k"),
                    new PieceInfo(PieceKind.King, PieceColor.Black, Square.Parse("e8"), "k"),
                    new PieceInfo(PieceKind.Pawn, PieceColor.White, Square.Parse("e2"), "p")
                },
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1);
        }

        private sealed class FakeStockfishProcess : IStockfishProcess
        {
            private readonly Queue<string> _output = new Queue<string>();
            private readonly Queue<string[]> _analysisBatches = new Queue<string[]>();

            public bool RespondToUci { get; set; } = true;

            public bool HasExited { get; private set; }

            public bool Killed { get; private set; }

            public bool Disposed { get; private set; }

            public List<string> Commands { get; } = new List<string>();

            public void EnqueueAnalysis(params string[] lines)
            {
                _analysisBatches.Enqueue(lines);
            }

            public void Start()
            {
            }

            public void WriteLine(string command)
            {
                Commands.Add(command);

                if (command == "uci" && RespondToUci)
                {
                    _output.Enqueue("uciok");
                }
                else if (command == "isready")
                {
                    _output.Enqueue("readyok");
                }
                else if (command.StartsWith("go depth ", StringComparison.Ordinal) && _analysisBatches.Count > 0)
                {
                    foreach (string line in _analysisBatches.Dequeue())
                    {
                        _output.Enqueue(line);
                    }
                }
            }

            public string? ReadLine(TimeSpan timeout)
            {
                return _output.Count == 0 ? null : _output.Dequeue();
            }

            public void ClearOutput()
            {
                _output.Clear();
            }

            public void Kill()
            {
                Killed = true;
                HasExited = true;
            }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
