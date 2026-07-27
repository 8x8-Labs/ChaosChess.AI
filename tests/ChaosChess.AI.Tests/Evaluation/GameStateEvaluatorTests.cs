using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using Xunit;

namespace ChaosChess.AI.Tests.Evaluation;

public sealed class GameStateEvaluatorTests
{
    [Fact]
    public void Evaluate_BalancedKingsOnly_ReturnsNeutralResult()
    {
        GameState state = CreateState(
            Piece(PieceKind.King, PieceColor.White, "a1"),
            Piece(PieceKind.King, PieceColor.Black, "h8"));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(0, result.Material);
        Assert.Equal(0, result.Threat);
        Assert.Equal(0, result.Advantage);
        Assert.Equal(0, result.KingSafety);
        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void Evaluate_MaterialAdvantage_IsNormalizedAndPerspectiveRelative()
    {
        GameState state = CreateState(
            Piece(PieceKind.King, PieceColor.White, "a1"),
            Piece(PieceKind.Queen, PieceColor.White, "d3"),
            Piece(PieceKind.King, PieceColor.Black, "h8"));
        var evaluator = new GameStateEvaluator(new EvaluationOptions(1, 0, 0, 0));

        EvaluationResult white = evaluator.Evaluate(state, PieceColor.White);
        EvaluationResult black = evaluator.Evaluate(state, PieceColor.Black);

        Assert.Equal(69, white.Material);
        Assert.Equal(69, white.TotalScore);
        Assert.Equal(-69, black.Material);
        Assert.Equal(-69, black.TotalScore);
    }

    [Theory]
    [InlineData(PieceKind.Pawn, 8)]
    [InlineData(PieceKind.Knight, 25)]
    [InlineData(PieceKind.Bishop, 25)]
    [InlineData(PieceKind.Rook, 38)]
    [InlineData(PieceKind.Queen, 69)]
    [InlineData(PieceKind.Amazon, 100)]
    [InlineData(PieceKind.Chancellor, 69)]
    [InlineData(PieceKind.KnightRider, 54)]
    [InlineData(PieceKind.Wall, 0)]
    public void Evaluate_UsesConfiguredPieceValues(PieceKind kind, int expectedMaterial)
    {
        GameState state = CreateState(
            Piece(PieceKind.King, PieceColor.White, "a1"),
            Piece(kind, PieceColor.White, "d4"),
            Piece(PieceKind.King, PieceColor.Black, "h8"));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(expectedMaterial, result.Material);
    }

    [Fact]
    public void Evaluate_MissingKing_ReturnsTerminalScore()
    {
        GameState whiteWins = CreateState(
            Piece(PieceKind.King, PieceColor.White, "a1"));
        GameState whiteLoses = CreateState(
            Piece(PieceKind.King, PieceColor.Black, "h8"));
        var evaluator = new GameStateEvaluator();

        Assert.Equal(100, evaluator.Evaluate(whiteWins, PieceColor.White).TotalScore);
        Assert.Equal(-100, evaluator.Evaluate(whiteLoses, PieceColor.White).TotalScore);
    }

    [Fact]
    public void Evaluate_MineThreat_UsesOwnerAndThreatenedMaterial()
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.Queen, PieceColor.White, "d3"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("mine-1", "Mine", Square.Parse("e4"), PieceColor.Black, 1));
        var evaluator = new GameStateEvaluator(new EvaluationOptions(0, 1, 0, 0));

        EvaluationResult result = evaluator.Evaluate(state, PieceColor.White);

        Assert.Equal(-69, result.Threat);
        Assert.Equal(-69, result.TotalScore);
    }

    [Fact]
    public void Evaluate_FireThreat_AppliesReducedWeight()
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.Queen, PieceColor.White, "d3"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("fire-1", "Fire", Square.Parse("e4"), PieceColor.Black, 1));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(-55, result.Threat);
    }

    [Fact]
    public void Evaluate_UnknownAndUnownedEffects_DoNotAffectScores()
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("unknown-1", "Unknown", Square.Parse("d4"), PieceColor.White, 1),
            new TileEffectInfo("mine-1", "Mine", Square.Parse("a1"), null, 1));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(0, result.Threat);
        Assert.Equal(0, result.Advantage);
    }

    [Fact]
    public void Evaluate_AdvantageEffects_AreAddedRelativeToPerspective()
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("blessing-1", "Blessing", Square.Parse("b2"), PieceColor.White, 2),
            new TileEffectInfo("portal-1", "Portal", Square.Parse("c3"), PieceColor.White, 1),
            new TileEffectInfo("peace-1", "Peace", Square.Parse("g7"), PieceColor.Black, 3));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(25, result.Advantage);
        Assert.Equal(15, result.TotalScore);
    }

    [Fact]
    public void Evaluate_DirectKingAttackAndRingAttack_AreCounted()
    {
        GameState state = CreateState(
            Piece(PieceKind.King, PieceColor.White, "e1"),
            Piece(PieceKind.Rook, PieceColor.Black, "e8"),
            Piece(PieceKind.King, PieceColor.Black, "a8"));
        var evaluator = new GameStateEvaluator(new EvaluationOptions(0, 0, 0, 1));

        EvaluationResult result = evaluator.Evaluate(state, PieceColor.White);

        Assert.Equal(-56, result.KingSafety);
        Assert.Equal(-56, result.TotalScore);
    }

    [Fact]
    public void Evaluate_WallBlocksSlidingAttack()
    {
        GameState state = CreateState(
            Piece(PieceKind.King, PieceColor.White, "e1"),
            Piece(PieceKind.Wall, PieceColor.White, "e4"),
            Piece(PieceKind.Rook, PieceColor.Black, "e8"),
            Piece(PieceKind.King, PieceColor.Black, "a8"));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(0, result.KingSafety);
    }

    [Theory]
    [InlineData(PieceKind.Amazon, "d3", -80)]
    [InlineData(PieceKind.KnightRider, "c2", -50)]
    public void Evaluate_ChaosPieceAttacks_AffectKingSafety(
        PieceKind kind,
        string attackerSquare,
        int expectedKingSafety)
    {
        GameState state = CreateState(
            Piece(PieceKind.King, PieceColor.White, "e1"),
            Piece(kind, PieceColor.Black, attackerSquare),
            Piece(PieceKind.King, PieceColor.Black, "h8"));

        EvaluationResult result = new GameStateEvaluator().Evaluate(state, PieceColor.White);

        Assert.Equal(expectedKingSafety, result.KingSafety);
    }

    [Fact]
    public void Evaluate_SameInput_ReturnsSameResult()
    {
        GameState state = CreateState(
            new[]
            {
                Piece(PieceKind.King, PieceColor.White, "a1"),
                Piece(PieceKind.Rook, PieceColor.White, "d4"),
                Piece(PieceKind.King, PieceColor.Black, "h8")
            },
            new TileEffectInfo("mine-1", "Mine", Square.Parse("e5"), PieceColor.Black, 1));
        var evaluator = new GameStateEvaluator();

        EvaluationResult first = evaluator.Evaluate(state, PieceColor.White);
        EvaluationResult second = evaluator.Evaluate(state, PieceColor.White);

        Assert.Equal(first.Material, second.Material);
        Assert.Equal(first.Threat, second.Threat);
        Assert.Equal(first.Advantage, second.Advantage);
        Assert.Equal(first.KingSafety, second.KingSafety);
        Assert.Equal(first.TotalScore, second.TotalScore);
    }

    [Fact]
    public void Constructor_InvalidWeight_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EvaluationOptions(threatWeight: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EvaluationOptions(kingSafetyWeight: -1));
    }

    private static GameState CreateState(params PieceInfo[] pieces)
    {
        return CreateState(pieces, Array.Empty<TileEffectInfo>());
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
        return new PieceInfo(kind, color, Square.Parse(square), FenCodeFor(kind));
    }

    private static string FenCodeFor(PieceKind kind)
    {
        return kind switch
        {
            PieceKind.Pawn => "p",
            PieceKind.Knight => "n",
            PieceKind.Bishop => "b",
            PieceKind.Rook => "r",
            PieceKind.Queen => "q",
            PieceKind.King => "k",
            PieceKind.Wall => "a",
            PieceKind.Amazon => "s",
            PieceKind.Chancellor => "y",
            PieceKind.KnightRider => "z",
            _ => "x"
        };
    }
}
