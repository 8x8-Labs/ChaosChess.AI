using System;
using System.Linq;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class CardTargetingModuleTests
{
    [Fact]
    public void DefaultCatalog_ContainsSupportedStrategies()
    {
        CardTargetStrategyRegistry registry = DefaultCardTargetStrategyCatalog.CreateRegistry();

        Assert.True(registry.TryGetStrategy("agile", out _));
        Assert.True(registry.TryGetStrategy("aim", out _));
        Assert.True(registry.TryGetStrategy("caterpillar", out _));
        Assert.True(registry.TryGetStrategy("charge", out _));
        Assert.True(registry.TryGetStrategy("concentration", out _));
        Assert.True(registry.TryGetStrategy("fast_march", out _));
        Assert.True(registry.TryGetStrategy("fire", out _));
        Assert.True(registry.TryGetStrategy("limitless", out _));
        Assert.True(registry.TryGetStrategy("peace_zone", out _));
        Assert.True(registry.TryGetStrategy("portal", out _));
        Assert.True(registry.TryGetStrategy("sneak_pawn", out _));
        Assert.True(registry.TryGetStrategy("thunderclap_flash", out _));
        Assert.Equal(12, registry.Strategies.Count);
    }

    [Theory]
    [InlineData("agile")]
    [InlineData("aim")]
    [InlineData("caterpillar")]
    [InlineData("charge")]
    [InlineData("concentration")]
    [InlineData("fast_march")]
    [InlineData("fire")]
    [InlineData("limitless")]
    [InlineData("peace_zone")]
    [InlineData("portal")]
    [InlineData("sneak_pawn")]
    [InlineData("thunderclap_flash")]
    public void DecideBestPlan_DefaultStrategiesReturnPlanForSupportedCards(
        string cardId)
    {
        GameState state = State(
            PieceColor.White,
            Card(cardId),
            pieces: SupportedCardTargetPieces());
        var module = new CardTargetingModule();

        CardPlanDecisionResult result = module.DecideBestPlan(
            state,
            state.AvailableCards[0],
            PieceColor.White);

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(cardId, result.SelectedCandidate!.Plan.CardId);
    }

    [Fact]
    public void DecideBestPlan_ForwardsEngineTopMovesToStrategy()
    {
        var enemyKnight = Piece(PieceKind.Knight, PieceColor.Black, new Square(6, 5), "n");
        GameState state = State(
            PieceColor.White,
            Card("fire"),
            pieces: new[] { enemyKnight });
        var module = new CardTargetingModule();

        CardPlanDecisionResult result = module.DecideBestPlan(
            state,
            state.AvailableCards[0],
            PieceColor.White,
            engineTopMoves: new[] { Move("g6e5") });

        Assert.True(result.HasSelection);
        Assert.Equal(new Square(4, 4), result.SelectedCandidate!.Plan.Target.Squares[0]);
    }

    [Fact]
    public void DecideBestPlan_UnsupportedCardReturnsUnsupportedSkipCode()
    {
        GameState state = State(PieceColor.White, Card("unknown"));
        var module = new CardTargetingModule();

        CardPlanDecisionResult result = module.DecideBestPlan(
            state,
            state.AvailableCards[0],
            PieceColor.White);

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void DecideBestPlan_MissingStrategyStillReturnsMissingStrategySkipCode()
    {
        var registry = new CardTargetStrategyRegistry();
        var module = new CardTargetingModule(registry);
        GameState state = State(PieceColor.White, Card("fire"));

        CardPlanDecisionResult result = module.DecideBestPlan(
            state,
            state.AvailableCards[0],
            PieceColor.White);

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.MissingStrategy, result.SkipCode);
    }

    [Fact]
    public void DecideBestPlan_SameInputReturnsSamePlan()
    {
        GameState state = State(
            PieceColor.White,
            Card("portal"),
            pieces: new[] { Pawn(PieceColor.White, new Square(4, 1)) });
        var module = new CardTargetingModule();

        CardPlanDecisionResult first = module.DecideBestPlan(
            state,
            state.AvailableCards[0],
            PieceColor.White,
            engineTopMoves: new[] { Move("e2e4") });
        CardPlanDecisionResult second = module.DecideBestPlan(
            state,
            state.AvailableCards[0],
            PieceColor.White,
            engineTopMoves: new[] { Move("e2e4") });

        Assert.True(first.HasSelection);
        Assert.True(second.HasSelection);
        Assert.Equal(first.SelectedCandidate!.Plan.Target.Squares, second.SelectedCandidate!.Plan.Target.Squares);
        Assert.Equal(first.SelectedCandidate.Score.Total, second.SelectedCandidate.Score.Total);
    }

    [Fact]
    public void DecideBestPlan_InvalidArguments_Throw()
    {
        GameState state = State(PieceColor.White, Card("charge"));
        var module = new CardTargetingModule();

        Assert.Throws<ArgumentNullException>(
            () => module.DecideBestPlan(null!, state.AvailableCards[0], PieceColor.White));
        Assert.Throws<ArgumentNullException>(
            () => module.DecideBestPlan(state, null!, PieceColor.White));
    }

    private static GameState State(
        PieceColor sideToMove,
        CardInfo card,
        PieceInfo[]? pieces = null)
    {
        return new GameState(
            new BoardState(
                pieces ?? Array.Empty<PieceInfo>(),
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { card },
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "Mobility", 1);
    }

    private static PieceInfo Pawn(PieceColor color, Square square)
    {
        return Piece(PieceKind.Pawn, color, square, "p");
    }

    private static PieceInfo[] SupportedCardTargetPieces()
    {
        return new[]
        {
            Pawn(PieceColor.White, new Square(4, 1)),
            Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n"),
            Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r"),
            Piece(PieceKind.Queen, PieceColor.White, new Square(3, 0), "q")
        };
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode)
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
