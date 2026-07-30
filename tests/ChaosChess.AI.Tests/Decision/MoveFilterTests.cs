using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision;

public sealed class MoveFilterTests
{
    [Fact]
    public void GetFilteredMoves_PassesVariationCountToEngine()
    {
        var engine = new StubChessEngine(Move("e2e4", 13));
        var filter = new MoveFilter(engine);

        MoveFilterResult result = filter.GetFilteredMoves(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")),
            variationCount: 7);

        MoveRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Equal("e2e4", recommendation.UciMove);
        Assert.Equal(7, engine.LastVariationCount);
    }

    [Fact]
    public void GetFilteredMoves_EmptyEngineCandidates_ReturnsEmptyResult()
    {
        var filter = new MoveFilter(new StubChessEngine());

        MoveFilterResult result = filter.GetFilteredMoves(
            State(Piece(PieceKind.King, PieceColor.White, "e1", "k")),
            variationCount: 3);

        Assert.False(result.HasRecommendations);
        Assert.Empty(result.Recommendations);
        Assert.Empty(result.FilteredMoves);
    }

    [Fact]
    public void GetFilteredMoves_ParsesPromotionUci()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("e7e8q", 26)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e7", "p")),
            variationCount: 1);

        MoveRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Equal("e7e8q", recommendation.UciMove);
        Assert.Equal(2, recommendation.EngineScore);
    }

    [Fact]
    public void GetFilteredMoves_InvalidUci_IsFiltered()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("e2e9", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")),
            variationCount: 1);

        Assert.Empty(result.Recommendations);
        FilteredMoveCandidate filtered = Assert.Single(result.FilteredMoves);
        Assert.Equal("e2e9", filtered.UciMove);
        Assert.Equal("Invalid UCI move.", filtered.Reason);
    }

    [Fact]
    public void GetFilteredMoves_MissingSourcePiece_IsFiltered()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("e2e4", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(Piece(PieceKind.King, PieceColor.White, "e1", "k")),
            variationCount: 1);

        Assert.Empty(result.Recommendations);
        Assert.Equal(
            "No piece exists on the move source square.",
            Assert.Single(result.FilteredMoves).Reason);
    }

    [Fact]
    public void GetFilteredMoves_WrongColorSourcePiece_IsFiltered()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("e7e5", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(Piece(PieceKind.Pawn, PieceColor.Black, "e7", "p")),
            variationCount: 1);

        Assert.Empty(result.Recommendations);
        Assert.Equal(
            "Move source piece does not match the side to move.",
            Assert.Single(result.FilteredMoves).Reason);
    }

    [Fact]
    public void GetFilteredMoves_PeaceCapture_IsHardFiltered()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("e4e5", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[]
                {
                    Piece(PieceKind.Rook, PieceColor.White, "e4", "r"),
                    Piece(PieceKind.Pawn, PieceColor.Black, "e5", "p")
                },
                Effect("peace-1", "Peace", "e5", PieceColor.Black)),
            variationCount: 1);

        Assert.Empty(result.Recommendations);
        Assert.Equal(
            "Peace tile cancels capture on the destination square.",
            Assert.Single(result.FilteredMoves).Reason);
    }

    [Fact]
    public void GetFilteredMoves_MinePath_AdjustsMaterialBalance()
    {
        var filter = new MoveFilter(new StubChessEngine(
            Move("a1a4", 0),
            Move("h1h2", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[]
                {
                    Piece(PieceKind.Rook, PieceColor.White, "a1", "r"),
                    Piece(PieceKind.Queen, PieceColor.Black, "b2", "q")
                },
                Effect("mine-1", "Mine", "a3", PieceColor.Black)),
            variationCount: 2);

        Assert.Equal("a1a4", result.Recommendations[0].UciMove);
        Assert.Equal(31, result.Recommendations[0].AdjustmentScore);
        Assert.Contains("Mine path explosion adjusted material balance.", result.Recommendations[0].Reasons);
    }

    [Fact]
    public void GetFilteredMoves_FireEntry_AppliesRiskPenalty()
    {
        var filter = new MoveFilter(new StubChessEngine(
            Move("e2e4", 20),
            Move("d2d4", 0)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[]
                {
                    Piece(PieceKind.Rook, PieceColor.White, "e2", "r"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                Effect("fire-1", "Fire", "e4", PieceColor.Black)),
            variationCount: 2);

        Assert.Equal("d2d4", result.Recommendations[0].UciMove);
        Assert.Equal(-31, result.Recommendations[1].AdjustmentScore);
        Assert.Contains("Fire tile entry risk applied.", result.Recommendations[1].Reasons);
    }

    [Fact]
    public void GetFilteredMoves_BlessingEntry_AppliesPromotionGain()
    {
        var filter = new MoveFilter(new StubChessEngine(
            Move("e2e4", 0),
            Move("d2d4", 1)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                Effect("blessing-1", "Blessing", "e4", PieceColor.White)),
            variationCount: 2);

        Assert.Equal("e2e4", result.Recommendations[0].UciMove);
        Assert.Equal(17, result.Recommendations[0].AdjustmentScore);
        Assert.Contains("Blessing tile promotion gain applied.", result.Recommendations[0].Reasons);
    }

    [Fact]
    public void GetFilteredMoves_EmptyPeaceEntry_AppliesDefenseBonus()
    {
        var filter = new MoveFilter(new StubChessEngine(
            Move("e2e4", 0),
            Move("d2d4", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                Effect("peace-1", "Peace", "e4", PieceColor.White)),
            variationCount: 2);

        Assert.Equal("e2e4", result.Recommendations[0].UciMove);
        Assert.Equal(20, result.Recommendations[0].AdjustmentScore);
        Assert.Contains("Peace tile entry bonus applied.", result.Recommendations[0].Reasons);
    }

    [Fact]
    public void GetFilteredMoves_PortalWithDestination_AdjustsDestinationValue()
    {
        var filter = new MoveFilter(new StubChessEngine(
            Move("e2e4", 0),
            Move("d2d4", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p"),
                    Piece(PieceKind.Rook, PieceColor.Black, "h8", "r")
                },
                new TileEffectInfo(
                    "portal-1",
                    "Portal",
                    Square.Parse("e4"),
                    PieceColor.White,
                    2,
                    destinationSquare: Square.Parse("h8"),
                    sharedRemainingUses: 1)),
            variationCount: 2);

        Assert.Equal("e2e4", result.Recommendations[0].UciMove);
        Assert.Equal(53, result.Recommendations[0].AdjustmentScore);
        Assert.Contains("Portal destination adjustment applied.", result.Recommendations[0].Reasons);
    }

    [Fact]
    public void GetFilteredMoves_WallPiece_IsNotFilteredByMoveFilter()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("a1a4", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                Piece(PieceKind.Rook, PieceColor.White, "a1", "r"),
                Piece(PieceKind.Wall, PieceColor.Black, "a3", "a")),
            variationCount: 1);

        Assert.Empty(result.FilteredMoves);
        Assert.Equal("a1a4", Assert.Single(result.Recommendations).UciMove);
    }

    [Fact]
    public void GetFilteredMoves_UnknownAndUnownedEffects_AreIgnored()
    {
        var filter = new MoveFilter(new StubChessEngine(Move("e2e4", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                Effect("unknown-1", "Unknown", "e4", PieceColor.White),
                Effect("portal-1", "Portal", "e4", null)),
            variationCount: 1);

        MoveRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Equal(0, recommendation.AdjustmentScore);
        Assert.Empty(recommendation.Reasons);
    }

    [Fact]
    public void GetFilteredMoves_TiesPreserveEngineOrder()
    {
        var filter = new MoveFilter(new StubChessEngine(
            Move("e2e4", 13),
            Move("d2d4", 13)));

        MoveFilterResult result = filter.GetFilteredMoves(
            State(
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")),
            variationCount: 2);

        Assert.Equal("e2e4", result.Recommendations[0].UciMove);
        Assert.Equal("d2d4", result.Recommendations[1].UciMove);
    }

    [Fact]
    public void ConstructorAndGetFilteredMoves_InvalidArguments_Throw()
    {
        var engine = new StubChessEngine();
        var filter = new MoveFilter(engine);

        Assert.Throws<ArgumentNullException>(
            () => new MoveFilter(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MoveFilterOptions(scoreNormalizationDivisor: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new MoveFilterOptions(fireRiskWeight: -1));
        Assert.Throws<ArgumentNullException>(
            () => filter.GetFilteredMoves(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => filter.GetFilteredMoves(State(Piece(PieceKind.King, PieceColor.White, "e1", "k")), 0));
        Assert.Throws<InvalidOperationException>(
            () => new MoveFilter(new StubChessEngine(returnNullCandidates: true))
                .GetFilteredMoves(State(Piece(PieceKind.King, PieceColor.White, "e1", "k")), 1));
    }

    private static MoveCandidate Move(string uciMove, int scoreCentipawns)
    {
        return new MoveCandidate(uciMove, scoreCentipawns, mateIn: null);
    }

    private static GameState State(params PieceInfo[] pieces)
    {
        return State(pieces, Array.Empty<TileEffectInfo>());
    }

    private static GameState State(
        PieceInfo piece,
        TileEffectInfo effect)
    {
        return State(new[] { piece }, effect);
    }

    private static GameState State(
        PieceInfo first,
        PieceInfo second)
    {
        return State(new[] { first, second }, Array.Empty<TileEffectInfo>());
    }

    private static GameState State(
        PieceInfo first,
        PieceInfo second,
        TileEffectInfo effect)
    {
        return State(new[] { first, second }, effect);
    }

    private static GameState State(
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

        return new GameState(
            boardState,
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
        PieceColor? owner)
    {
        return new TileEffectInfo(
            id,
            effectType,
            Square.Parse(square),
            owner,
            remainingTurns: 1);
    }

    private sealed class StubChessEngine : IChessEngine
    {
        private readonly IReadOnlyList<MoveCandidate>? _moves;

        public StubChessEngine(params MoveCandidate[] moves)
            : this(false, moves)
        {
        }

        public StubChessEngine(bool returnNullCandidates, params MoveCandidate[] moves)
        {
            _moves = returnNullCandidates ? null : moves;
        }

        public int LastVariationCount { get; private set; }

        public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
        {
            LastVariationCount = variationCount;
            return _moves!;
        }

        public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
        {
            throw new NotSupportedException();
        }

        public bool IsInCheck(BoardState boardState)
        {
            throw new NotSupportedException();
        }
    }
}
