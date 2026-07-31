using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;
using ChaosChess.AI.Simulator;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class HeadlessGameRunnerTests
    {
        [Fact]
        public void Run_ConnectsOnePlyResultsUntilMaxPly()
        {
            GameState state = State(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.Black, "e7", "p")
                },
                PieceColor.White);
            var runner = Runner(new StubChessEngine(
                new[] { Move("e2e4", 10) },
                new[] { Move("e7e5", 10) }));

            HeadlessGameResult result = runner.Run(
                state,
                PieceColor.White,
                new HeadlessGameOptions(maxPly: 2, variationCount: 1));

            Assert.Equal(GameResult.Aborted, result.Result);
            Assert.Equal(GameTerminationReason.MaxPly, result.TerminationReason);
            Assert.Equal(2, result.PlyCount);
            Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e4")));
            Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e5")));
        }

        [Fact]
        public void Run_KingRemoved_DeclaresSurvivingKingWinner()
        {
            GameState state = State(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Queen, PieceColor.White, "e7", "q")
                },
                PieceColor.White);
            var runner = Runner(new StubChessEngine(new[] { Move("e7e8", 10) }));

            HeadlessGameResult result = runner.Run(
                state,
                PieceColor.White,
                new HeadlessGameOptions(maxPly: 10, variationCount: 1));

            Assert.Equal(GameResult.WhiteWin, result.Result);
            Assert.Equal(PieceColor.White, result.Winner);
            Assert.Equal(GameTerminationReason.KingRemoved, result.TerminationReason);
            Assert.Equal(1, result.PlyCount);
        }

        [Theory]
        [InlineData(true, GameResult.BlackWin, GameTerminationReason.Checkmate)]
        [InlineData(false, GameResult.Draw, GameTerminationReason.Stalemate)]
        public void Run_NoMoves_MapsCheckmateAndStalemate(
            bool inCheck,
            GameResult expectedResult,
            GameTerminationReason expectedTermination)
        {
            GameState state = State(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"));
            var runner = Runner(new StubChessEngine(Array.Empty<MoveCandidate>()) { InCheck = inCheck });

            HeadlessGameResult result = runner.Run(
                state,
                PieceColor.White,
                new HeadlessGameOptions(maxPly: 10));

            Assert.Equal(expectedResult, result.Result);
            Assert.Equal(expectedTermination, result.TerminationReason);
        }

        [Fact]
        public void Run_UnsupportedEffect_IsInvalid()
        {
            GameState state = State(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")
                },
                PieceColor.White,
                Effect("fire-1", "Fire", "e4", PieceColor.Black));
            var runner = Runner(new StubChessEngine(new[] { Move("e2e4", 10) }));

            HeadlessGameResult result = runner.Run(
                state,
                PieceColor.White,
                new HeadlessGameOptions(maxPly: 10, variationCount: 1));

            Assert.Equal(GameResult.Invalid, result.Result);
            Assert.Equal(GameTerminationReason.UnsupportedEffect, result.TerminationReason);
            Assert.Equal("unsupported_effect", result.ErrorCode);
            Assert.NotEmpty(result.Warnings);
        }

        [Fact]
        public void Run_CardRecommendationsAreCountedButNotApplied()
        {
            GameState state = State(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")
                },
                PieceColor.White,
                Array.Empty<TileEffectInfo>(),
                new CardInfo("boost", "MaterialSwing", remainingUses: 1));
            var runner = Runner(new StubChessEngine(new[] { Move("e2e4", 10) }));

            HeadlessGameResult result = runner.Run(
                state,
                PieceColor.White,
                new HeadlessGameOptions(maxPly: 1, variationCount: 1));

            Assert.Equal(1, result.CardsRecommended);
            Assert.Equal(0, result.CardsApplied);
            Assert.Equal("not_applied_contract_missing", result.CardsSkippedReason);
        }

        [Fact]
        public void Run_SameSeedAndInput_ReproducesFinalState()
        {
            GameState state = State(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                PieceColor.White);
            var options = new HeadlessGameOptions(maxPly: 1, variationCount: 2, useRandomTieBreak: true, seed: 7);
            HeadlessGameResult first = Runner(new StubChessEngine(new[] { Move("e2e4", 10), Move("d2d4", 10) }))
                .Run(state, PieceColor.White, options);
            HeadlessGameResult second = Runner(new StubChessEngine(new[] { Move("e2e4", 10), Move("d2d4", 10) }))
                .Run(state, PieceColor.White, options);

            Assert.Equal(first.PlyCount, second.PlyCount);
            Assert.Equal(SortedSquares(first.FinalState.BoardState.Pieces), SortedSquares(second.FinalState.BoardState.Pieces));
        }

        private static HeadlessGameRunner Runner(StubChessEngine engine)
        {
            return new HeadlessGameRunner(new GameSimulator(
                engine,
                new GameStateEvaluator(engine),
                new CardDecisionModule(new ConfiguredCardScorer(
                    cardScores: new Dictionary<string, int> { { "boost", 100 } })),
                new MoveFilter(engine)));
        }

        private static MoveCandidate Move(string uciMove, int scoreCentipawns)
        {
            return new MoveCandidate(uciMove, scoreCentipawns, mateIn: null);
        }

        private static GameState State(params PieceInfo[] pieces)
        {
            return State(pieces, PieceColor.White);
        }

        private static GameState State(IEnumerable<PieceInfo> pieces, PieceColor sideToMove)
        {
            return State(pieces, sideToMove, Array.Empty<TileEffectInfo>());
        }

        private static GameState State(
            IEnumerable<PieceInfo> pieces,
            PieceColor sideToMove,
            params TileEffectInfo[] effects)
        {
            return State(pieces, sideToMove, effects, Array.Empty<CardInfo>());
        }

        private static GameState State(
            IEnumerable<PieceInfo> pieces,
            PieceColor sideToMove,
            IEnumerable<TileEffectInfo> effects,
            params CardInfo[] cards)
        {
            return new GameState(
                new BoardState(
                    pieces,
                    sideToMove,
                    CastlingRights.None,
                    enPassantTarget: null,
                    halfmoveClock: 0,
                    fullmoveNumber: 1),
                cards,
                effects);
        }

        private static PieceInfo Piece(
            PieceKind kind,
            PieceColor color,
            string square,
            string fenCode)
        {
            return new PieceInfo(kind, color, Square.Parse(square), fenCode);
        }

        private static TileEffectInfo Effect(
            string id,
            string effectType,
            string square,
            PieceColor? owner)
        {
            return new TileEffectInfo(id, effectType, Square.Parse(square), owner, remainingTurns: 3);
        }

        private static string SortedSquares(IEnumerable<PieceInfo> pieces)
        {
            var values = new List<string>();

            foreach (PieceInfo piece in pieces)
            {
                values.Add(piece.Color + ":" + piece.Kind + ":" + piece.Square);
            }

            values.Sort(StringComparer.Ordinal);
            return string.Join("|", values);
        }

        private sealed class StubChessEngine : IChessEngine
        {
            private readonly Queue<IReadOnlyList<MoveCandidate>> _moveBatches = new Queue<IReadOnlyList<MoveCandidate>>();

            public StubChessEngine(params IReadOnlyList<MoveCandidate>[] moveBatches)
            {
                foreach (IReadOnlyList<MoveCandidate> batch in moveBatches)
                {
                    _moveBatches.Enqueue(batch);
                }
            }

            public bool InCheck { get; set; }

            public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
            {
                return _moveBatches.Count == 0
                    ? Array.Empty<MoveCandidate>()
                    : _moveBatches.Dequeue();
            }

            public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
            {
                return new PositionEvaluation(boardState.SideToMove, scoreCentipawns: 0, mateIn: null);
            }

            public bool IsInCheck(BoardState boardState)
            {
                return InCheck;
            }
        }
    }
}
