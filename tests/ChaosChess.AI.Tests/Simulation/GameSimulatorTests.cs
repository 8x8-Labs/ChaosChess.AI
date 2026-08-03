using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;
using Xunit;

namespace ChaosChess.AI.Tests.Simulation;

public sealed class GameSimulatorTests
{
    [Fact]
    public void SimulateFuture_HorizonZero_ReturnsInitialStateWithoutSteps()
    {
        GameState state = State(
            Piece(PieceKind.King, PieceColor.White, "e1", "k"),
            Piece(PieceKind.King, PieceColor.Black, "e8", "k"));
        GameSimulator simulator = Simulator(new StubChessEngine());

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 0));

        Assert.Same(state, result.InitialState);
        Assert.Same(state, result.FinalState);
        Assert.Empty(result.Steps);
        Assert.Equal(SimulationTerminationReason.HorizonReached, result.TerminationReason);
    }

    [Fact]
    public void SimulationOptions_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SimulationOptions(horizonPly: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SimulationOptions(horizonPly: 9));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SimulationOptions(variationCount: 0));
    }

    [Fact]
    public void SimulateFuture_TwoPly_AppliesMovesAndPreservesInputState()
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
        GameSimulator simulator = Simulator(new StubChessEngine(
            new[] { Move("e2e4", 13) },
            new[] { Move("e7e5", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 2, variationCount: 1));

        Assert.Equal(2, result.Steps.Count);
        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e4")));
        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e5")));
        Assert.Equal(PieceColor.White, result.FinalState.BoardState.SideToMove);
        Assert.Equal(2, result.FinalState.BoardState.FullmoveNumber);
        Assert.NotNull(state.BoardState.FindPiece(Square.Parse("e2")));
        Assert.NotNull(state.BoardState.FindPiece(Square.Parse("e7")));
    }

    [Fact]
    public void SimulateFuture_CaptureAndPromotion_UpdateCoarseBoard()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Pawn, PieceColor.White, "e7", "p"),
                Piece(PieceKind.Rook, PieceColor.Black, "d8", "r")
            },
            PieceColor.White);
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e7d8q", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        PieceInfo promoted = Assert.Single(result.FinalState.BoardState.Pieces, piece => piece.Square == Square.Parse("d8"));
        Assert.Equal(PieceKind.Queen, promoted.Kind);
        Assert.Equal(PieceColor.White, promoted.Color);
        Assert.Equal("q", promoted.FenCode);
        Assert.Equal(0, result.FinalState.BoardState.HalfmoveClock);
    }

    [Fact]
    public void SimulateFuture_Castling_MovesRookAndClearsRights()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.Rook, PieceColor.White, "h1", "r"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k")
            },
            PieceColor.White,
            CastlingRights.WhiteKingSide);
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e1g1", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("g1")));
        PieceInfo rook = Assert.Single(result.FinalState.BoardState.Pieces, piece => piece.Square == Square.Parse("f1"));
        Assert.Equal(PieceKind.Rook, rook.Kind);
        Assert.Equal(CastlingRights.None, result.FinalState.BoardState.CastlingRights);
    }

    [Fact]
    public void SimulateFuture_EnPassant_RemovesCapturedPawn()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Pawn, PieceColor.White, "e5", "p"),
                Piece(PieceKind.Pawn, PieceColor.Black, "d5", "p")
            },
            PieceColor.White,
            CastlingRights.None,
            Square.Parse("d6"));
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e5d6", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("d6")));
        Assert.Null(result.FinalState.BoardState.FindPiece(Square.Parse("d5")));
    }

    [Fact]
    public void SimulateFuture_MinePath_RemovesBlastPiecesAndMine()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Rook, PieceColor.White, "a1", "r"),
                Piece(PieceKind.Queen, PieceColor.Black, "b2", "q")
            },
            PieceColor.White,
            CastlingRights.None,
            null,
            Effect("mine-1", "Mine", "a3", PieceColor.Black, 3));
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("a1a4", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        Assert.Null(result.FinalState.BoardState.FindPiece(Square.Parse("a4")));
        Assert.Null(result.FinalState.BoardState.FindPiece(Square.Parse("b2")));
        Assert.DoesNotContain(result.FinalState.TileEffects, effect => effect.Id == "mine-1");
    }

    [Fact]
    public void SimulateFuture_PeaceCapture_IsBlockedAndConsumesEffect()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Rook, PieceColor.White, "e4", "r"),
                Piece(PieceKind.Pawn, PieceColor.Black, "e5", "p")
            },
            PieceColor.White,
            CastlingRights.None,
            null,
            Effect("peace-1", "Peace", "e5", PieceColor.Black, 3));
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e4e5", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        Assert.Equal(SimulationTerminationReason.MoveBlocked, result.TerminationReason);
        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e4")));
        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e5")));
        Assert.DoesNotContain(result.FinalState.TileEffects, effect => effect.Id == "peace-1");
    }

    [Fact]
    public void SimulateFuture_PortalEntry_TeleportsPieceAndUpdatesSharedUses()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")
            },
            PieceColor.White,
            CastlingRights.None,
            null,
            Portal("portal-a", "e4", "h6", 2),
            Portal("portal-b", "h6", "e4", 2));
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e2e4", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        Assert.Null(result.FinalState.BoardState.FindPiece(Square.Parse("e4")));
        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("h6")));
        Assert.All(result.FinalState.TileEffects, effect => Assert.Equal(1, effect.SharedRemainingUses));
    }

    [Fact]
    public void SimulateFuture_FireAndBlessing_RecordWarningsWithoutChangingEffect()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")
            },
            PieceColor.White,
            CastlingRights.None,
            null,
            Effect("fire-1", "Fire", "e4", PieceColor.Black, 3),
            Effect("blessing-1", "Blessing", "e4", PieceColor.White, 3));
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e2e4", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        Assert.Equal(SimulationTerminationReason.UnsupportedEffectEncountered, result.TerminationReason);
        Assert.Equal(2, result.Warnings.Count);
        Assert.Contains(result.FinalState.TileEffects, effect => effect.Id == "fire-1");
        Assert.Contains(result.FinalState.TileEffects, effect => effect.Id == "blessing-1");
    }

    [Fact]
    public void SimulateFuture_NoMoves_UsesCheckStateForTermination()
    {
        GameState state = State(
            Piece(PieceKind.King, PieceColor.White, "e1", "k"),
            Piece(PieceKind.King, PieceColor.Black, "e8", "k"));
        GameSimulator simulator = Simulator(new StubChessEngine(Array.Empty<MoveCandidate>()) { InCheck = true });

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1));

        Assert.Equal(SimulationTerminationReason.Checkmate, result.TerminationReason);
        Assert.Null(result.Steps[0].SelectedMove);
    }

    [Fact]
    public void SimulateFuture_KingRemoved_EndsSimulation()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Queen, PieceColor.White, "e7", "q")
            },
            PieceColor.White);
        GameSimulator simulator = Simulator(new StubChessEngine(new[] { Move("e7e8", 13) }));

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 2, variationCount: 1));

        Assert.Equal(SimulationTerminationReason.KingRemoved, result.TerminationReason);
        Assert.Single(result.Steps);
    }

    [Fact]
    public void SimulateFuture_SameSeedAndInput_ReproducesTwoPlyTrace()
    {
        GameState state = State(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                Piece(PieceKind.Pawn, PieceColor.White, "d2", "p"),
                Piece(PieceKind.Pawn, PieceColor.Black, "e7", "p"),
                Piece(PieceKind.Pawn, PieceColor.Black, "d7", "p")
            },
            PieceColor.White);
        var options = new SimulationOptions(horizonPly: 2, variationCount: 2, useRandomTieBreak: true, seed: 7);
        var firstSimulator = Simulator(new StubChessEngine(
            new[] { Move("e2e4", 13), Move("d2d4", 13) },
            new[] { Move("e7e5", 13), Move("d7d5", 13) }));
        var secondSimulator = Simulator(new StubChessEngine(
            new[] { Move("e2e4", 13), Move("d2d4", 13) },
            new[] { Move("e7e5", 13), Move("d7d5", 13) }));

        SimulationResult first = firstSimulator.SimulateFuture(state, PieceColor.White, options);
        SimulationResult second = secondSimulator.SimulateFuture(state, PieceColor.White, options);

        Assert.Equal(first.Seed, second.Seed);
        Assert.Equal(first.Steps[0].SelectedUciMove, second.Steps[0].SelectedUciMove);
        Assert.Equal(first.Steps[1].SelectedUciMove, second.Steps[1].SelectedUciMove);
        Assert.Equal(
            SortedSquares(first.FinalState.BoardState.Pieces),
            SortedSquares(second.FinalState.BoardState.Pieces));
    }

    [Fact]
    public void SimulateFuture_WithCardTargeting_RecordsSelectedPlanWithoutApplyingCard()
    {
        GameState state = new GameState(
            new BoardState(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")
                },
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo("agile", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
        GameSimulator simulator = Simulator(
            new StubChessEngine(new[] { Move("e2e4", 13) }),
            new CardTargetingModule());

        SimulationResult result = simulator.SimulateFuture(
            state,
            PieceColor.White,
            new SimulationOptions(horizonPly: 1, variationCount: 1));

        CardUseRecommendation recommendation = Assert.Single(result.Steps[0].CardDecision.Recommendations);
        Assert.NotNull(recommendation.Plan);
        Assert.Equal("agile", recommendation.Plan!.CardId);
        Assert.NotNull(recommendation.PlanScore);
        Assert.Equal(9, recommendation.PlanScore!.Total);
        Assert.NotNull(state.BoardState.FindPiece(Square.Parse("e2")));
        Assert.NotNull(result.FinalState.BoardState.FindPiece(Square.Parse("e4")));
    }

    private static GameSimulator Simulator(StubChessEngine engine)
    {
        return new GameSimulator(
            engine,
            new GameStateEvaluator(engine),
            new CardDecisionModule(new ConfiguredCardScorer()),
            new MoveFilter(engine));
    }

    private static GameSimulator Simulator(
        StubChessEngine engine,
        CardTargetingModule cardTargetingModule)
    {
        return new GameSimulator(
            engine,
            new GameStateEvaluator(engine),
            new CardDecisionModule(new ConfiguredCardScorer()),
            new MoveFilter(engine),
            random: null,
            cardTargetingModule);
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
        return State(pieces, sideToMove, CastlingRights.None);
    }

    private static GameState State(
        IEnumerable<PieceInfo> pieces,
        PieceColor sideToMove,
        CastlingRights castlingRights,
        Square? enPassantTarget = null,
        params TileEffectInfo[] effects)
    {
        return new GameState(
            new BoardState(
                pieces,
                sideToMove,
                castlingRights,
                enPassantTarget,
                0,
                1),
            Array.Empty<CardInfo>(),
            effects);
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        string square,
        string fenCode)
    {
        return new PieceInfo(
            kind,
            color,
            Square.Parse(square),
            fenCode);
    }

    private static TileEffectInfo Effect(
        string id,
        string effectType,
        string square,
        PieceColor? owner,
        int remainingTurns)
    {
        return new TileEffectInfo(
            id,
            effectType,
            Square.Parse(square),
            owner,
            remainingTurns);
    }

    private static TileEffectInfo Portal(
        string id,
        string square,
        string destination,
        int sharedUses)
    {
        return new TileEffectInfo(
            id,
            "Portal",
            Square.Parse(square),
            PieceColor.White,
            remainingTurns: 3,
            destinationSquare: Square.Parse(destination),
            sharedRemainingUses: sharedUses);
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
            if (_moveBatches.Count == 0)
            {
                return Array.Empty<MoveCandidate>();
            }

            return _moveBatches.Dequeue();
        }

        public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
        {
            return new PositionEvaluation(PieceColor.White, scoreCentipawns: 0, mateIn: null);
        }

        public bool IsInCheck(BoardState boardState)
        {
            return InCheck;
        }
    }
}
