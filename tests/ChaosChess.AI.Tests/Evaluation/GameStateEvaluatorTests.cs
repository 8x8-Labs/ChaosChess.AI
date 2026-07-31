using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using Xunit;

namespace ChaosChess.AI.Tests.Evaluation;

public sealed class GameStateEvaluatorTests
{
    [Theory]
    [InlineData(130, PieceColor.White, PieceColor.White, 10)]
    [InlineData(130, PieceColor.White, PieceColor.Black, -10)]
    [InlineData(-260, PieceColor.Black, PieceColor.Black, -20)]
    [InlineData(-260, PieceColor.Black, PieceColor.White, 20)]
    public void Evaluate_CentipawnScore_IsNormalizedForRequestedPerspective(
        int centipawns,
        PieceColor enginePerspective,
        PieceColor requestedPerspective,
        int expectedBoardScore)
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(enginePerspective, centipawns, null));
        var evaluator = new GameStateEvaluator(engine);

        EvaluationResult result = evaluator.Evaluate(
            CreateState(DefaultPieces()),
            requestedPerspective);

        Assert.Equal(expectedBoardScore, result.BoardScore);
        Assert.Null(result.MateIn);
        Assert.Equal(expectedBoardScore, result.TotalScore);
    }

    [Theory]
    [InlineData(2, PieceColor.White, PieceColor.White, 90, 2)]
    [InlineData(2, PieceColor.White, PieceColor.Black, -90, -2)]
    [InlineData(-1, PieceColor.Black, PieceColor.Black, -90, -1)]
    [InlineData(-1, PieceColor.Black, PieceColor.White, 90, 1)]
    public void Evaluate_PredictedMate_IsUrgentButNotTerminal(
        int mateIn,
        PieceColor enginePerspective,
        PieceColor requestedPerspective,
        int expectedBoardScore,
        int expectedMateIn)
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(enginePerspective, null, mateIn));
        var evaluator = new GameStateEvaluator(engine);

        EvaluationResult result = evaluator.Evaluate(
            CreateState(DefaultPieces()),
            requestedPerspective);

        Assert.Equal(expectedBoardScore, result.BoardScore);
        Assert.Equal(expectedMateIn, result.MateIn);
        Assert.Equal(expectedBoardScore, result.TotalScore);
        Assert.NotEqual(100, Math.Abs(result.TotalScore));
    }

    [Fact]
    public void Evaluate_ReevaluatedCardState_CanReversePredictedMate()
    {
        GameState state = CreateState(DefaultPieces());
        var beforeCard = new GameStateEvaluator(
            new FakeChessEngine(new PositionEvaluation(PieceColor.White, null, -1)));
        var afterCard = new GameStateEvaluator(
            new FakeChessEngine(new PositionEvaluation(PieceColor.White, 390, null)));

        EvaluationResult threatened = beforeCard.Evaluate(state, PieceColor.White);
        EvaluationResult rescued = afterCard.Evaluate(state, PieceColor.White);

        Assert.Equal(-90, threatened.BoardScore);
        Assert.Equal(-1, threatened.MateIn);
        Assert.Equal(30, rescued.BoardScore);
        Assert.Null(rescued.MateIn);
        Assert.True(rescued.TotalScore > threatened.TotalScore);
    }

    [Fact]
    public void Evaluate_CombinesBoardScoreAndChaosAdjustments()
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.Queen, PieceColor.White, "d3"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("mine-1", "Mine", Square.Parse("e4"), PieceColor.Black, 1),
            new TileEffectInfo("blessing-1", "Blessing", Square.Parse("b2"), PieceColor.White, 2),
            new TileEffectInfo("portal-1", "Portal", Square.Parse("c3"), PieceColor.White, 1),
            new TileEffectInfo("peace-1", "Peace", Square.Parse("g7"), PieceColor.Black, 3));
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 130, null));

        EvaluationResult result = new GameStateEvaluator(engine).Evaluate(
            state,
            PieceColor.White);

        Assert.Equal(10, result.BoardScore);
        Assert.Equal(-69, result.Threat);
        Assert.Equal(25, result.Advantage);
        Assert.Equal(-30, result.TotalScore);
    }

    [Fact]
    public void Evaluate_UsesFixedConfiguredSearchDepthAndWeights()
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 260, null));
        var options = new EvaluationOptions(
            searchDepth: 8,
            boardScoreWeight: 0.5,
            threatWeight: 0,
            advantageWeight: 0);
        var evaluator = new GameStateEvaluator(engine, options);

        EvaluationResult result = evaluator.Evaluate(
            CreateState(DefaultPieces()),
            PieceColor.White);

        Assert.Equal(8, engine.LastDepth);
        Assert.Equal(1, engine.EvaluateCallCount);
        Assert.Equal(10, result.TotalScore);
    }

    [Fact]
    public void Evaluate_DefaultSearchDepth_IsTwelve()
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 0, null));

        new GameStateEvaluator(engine).Evaluate(
            CreateState(DefaultPieces()),
            PieceColor.White);

        Assert.Equal(12, engine.LastDepth);
    }

    [Theory]
    [InlineData("Mine", -69)]
    [InlineData("Fire", -55)]
    public void Evaluate_ThreatEffects_UseThreatenedMaterial(
        string effectType,
        int expectedThreat)
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.Queen, PieceColor.White, "d3"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("threat-1", effectType, Square.Parse("e4"), PieceColor.Black, 1));
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 0, null));

        EvaluationResult result = new GameStateEvaluator(engine).Evaluate(
            state,
            PieceColor.White);

        Assert.Equal(expectedThreat, result.Threat);
    }

    [Fact]
    public void Evaluate_UnknownAndUnownedEffects_DoNotAffectScores()
    {
        GameState state = CreateState(
            DefaultPieces(),
            new TileEffectInfo("unknown-1", "Unknown", Square.Parse("d4"), PieceColor.White, 1),
            new TileEffectInfo("mine-1", "Mine", Square.Parse("a1"), null, 1));
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 0, null));

        EvaluationResult result = new GameStateEvaluator(engine).Evaluate(
            state,
            PieceColor.White);

        Assert.Equal(0, result.Threat);
        Assert.Equal(0, result.Advantage);
        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void Evaluate_LargeCentipawnScore_RemainsBelowPredictedMate()
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 5000, null));

        EvaluationResult result = new GameStateEvaluator(engine).Evaluate(
            CreateState(DefaultPieces()),
            PieceColor.White);

        Assert.Equal(89, result.BoardScore);
        Assert.Equal(89, result.TotalScore);
    }

    [Fact]
    public void Evaluate_NonTerminalTotalScore_DoesNotUseTerminalValue()
    {
        GameState state = CreateState(
            DefaultPieces(),
            new TileEffectInfo("blessing-1", "Blessing", Square.Parse("b2"), PieceColor.White, 1),
            new TileEffectInfo("portal-1", "Portal", Square.Parse("c3"), PieceColor.White, 1));
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, null, 1));

        EvaluationResult result = new GameStateEvaluator(engine).Evaluate(
            state,
            PieceColor.White);

        Assert.Equal(90, result.BoardScore);
        Assert.Equal(99, result.TotalScore);
        Assert.NotEqual(100, result.TotalScore);
    }

    [Fact]
    public void Evaluate_SameEngineEvaluation_ReturnsSameResult()
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 130, null));
        var evaluator = new GameStateEvaluator(engine);
        GameState state = CreateState(DefaultPieces());

        EvaluationResult first = evaluator.Evaluate(state, PieceColor.White);
        EvaluationResult second = evaluator.Evaluate(state, PieceColor.White);

        Assert.Equal(first.BoardScore, second.BoardScore);
        Assert.Equal(first.MateIn, second.MateIn);
        Assert.Equal(first.Threat, second.Threat);
        Assert.Equal(first.Advantage, second.Advantage);
        Assert.Equal(first.TotalScore, second.TotalScore);
    }

    [Fact]
    public void PositionEvaluation_RequiresExactlyOneValidScore()
    {
        Assert.Throws<ArgumentException>(
            () => new PositionEvaluation(PieceColor.White, null, null));
        Assert.Throws<ArgumentException>(
            () => new PositionEvaluation(PieceColor.White, 10, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PositionEvaluation(PieceColor.White, null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PositionEvaluation((PieceColor)99, 10, null));
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => new GameStateEvaluator(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EvaluationOptions(searchDepth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EvaluationOptions(boardScoreWeight: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EvaluationOptions(advantageWeight: -1));
    }

    [Fact]
    public void Evaluate_InvalidArguments_Throw()
    {
        var engine = new FakeChessEngine(
            new PositionEvaluation(PieceColor.White, 0, null));
        var evaluator = new GameStateEvaluator(engine);

        Assert.Throws<ArgumentNullException>(
            () => evaluator.Evaluate(null!, PieceColor.White));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => evaluator.Evaluate(CreateState(DefaultPieces()), (PieceColor)99));
    }

    private static PieceInfo[] DefaultPieces()
    {
        return
        [
            Piece(PieceKind.King, PieceColor.White, "a1"),
            Piece(PieceKind.King, PieceColor.Black, "h8")
        ];
    }

    private static GameState CreateState(
        IEnumerable<PieceInfo> pieces,
        params TileEffectInfo[] effects)
    {
        var boardState = new BoardState(
            pieces,
            PieceColor.White,
            CastlingRights.None,
            null,
            0,
            1);

        return new GameState(boardState, Array.Empty<CardInfo>(), effects);
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square)
    {
        string fenCode = kind == PieceKind.King ? "k" : "q";
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }

    private sealed class FakeChessEngine : IChessEngine
    {
        private readonly PositionEvaluation _evaluation;

        public FakeChessEngine(PositionEvaluation evaluation)
        {
            _evaluation = evaluation;
        }

        public int EvaluateCallCount { get; private set; }

        public int LastDepth { get; private set; }

        public IReadOnlyList<MoveCandidate> GetTopMoves(
            BoardState boardState,
            int variationCount)
        {
            return Array.Empty<MoveCandidate>();
        }

        public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
        {
            EvaluateCallCount++;
            LastDepth = depth;
            return _evaluation;
        }

        public bool IsInCheck(BoardState boardState)
        {
            return false;
        }
    }
}
