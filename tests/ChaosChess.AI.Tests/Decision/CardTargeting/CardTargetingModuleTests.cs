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
        Assert.True(registry.TryGetStrategy("arena", out _));
        Assert.True(registry.TryGetStrategy("at_mine", out _));
        Assert.True(registry.TryGetStrategy("blessing", out _));
        Assert.True(registry.TryGetStrategy("castle_knight", out _));
        Assert.True(registry.TryGetStrategy("caterpillar", out _));
        Assert.True(registry.TryGetStrategy("chaotic_knight", out _));
        Assert.True(registry.TryGetStrategy("charge", out _));
        Assert.True(registry.TryGetStrategy("checkmate_declaration", out _));
        Assert.True(registry.TryGetStrategy("cobweb", out _));
        Assert.True(registry.TryGetStrategy("concentration", out _));
        Assert.True(registry.TryGetStrategy("dark_hand", out _));
        Assert.True(registry.TryGetStrategy("democracy", out _));
        Assert.True(registry.TryGetStrategy("desperado", out _));
        Assert.True(registry.TryGetStrategy("destroyer_tank_cards", out _));
        Assert.True(registry.TryGetStrategy("dimension_disturbance", out _));
        Assert.True(registry.TryGetStrategy("dimension_instability", out _));
        Assert.True(registry.TryGetStrategy("father_enemy", out _));
        Assert.True(registry.TryGetStrategy("fast_march", out _));
        Assert.True(registry.TryGetStrategy("fire", out _));
        Assert.True(registry.TryGetStrategy("gaslighting", out _));
        Assert.True(registry.TryGetStrategy("giant", out _));
        Assert.True(registry.TryGetStrategy("gods_move", out _));
        Assert.True(registry.TryGetStrategy("honey_trap", out _));
        Assert.True(registry.TryGetStrategy("jumping_platform", out _));
        Assert.True(registry.TryGetStrategy("limitless", out _));
        Assert.True(registry.TryGetStrategy("magnet", out _));
        Assert.True(registry.TryGetStrategy("missing_promotion", out _));
        Assert.True(registry.TryGetStrategy("mutiny", out _));
        Assert.True(registry.TryGetStrategy("obey_order", out _));
        Assert.True(registry.TryGetStrategy("overbearing", out _));
        Assert.True(registry.TryGetStrategy("peace_zone", out _));
        Assert.True(registry.TryGetStrategy("portal", out _));
        Assert.True(registry.TryGetStrategy("position_swap", out _));
        Assert.True(registry.TryGetStrategy("psilocybin_mushroom", out _));
        Assert.True(registry.TryGetStrategy("rampart", out _));
        Assert.True(registry.TryGetStrategy("revive", out _));
        Assert.True(registry.TryGetStrategy("shuffle_board", out _));
        Assert.True(registry.TryGetStrategy("sneak_pawn", out _));
        Assert.True(registry.TryGetStrategy("stag_fight", out _));
        Assert.True(registry.TryGetStrategy("sunset_blade", out _));
        Assert.True(registry.TryGetStrategy("sync", out _));
        Assert.True(registry.TryGetStrategy("teleport", out _));
        Assert.True(registry.TryGetStrategy("time_bomb", out _));
        Assert.True(registry.TryGetStrategy("time_reversal", out _));
        Assert.True(registry.TryGetStrategy("thunderclap_flash", out _));
        Assert.True(registry.TryGetStrategy("transmigration", out _));
        Assert.True(registry.TryGetStrategy("weird_castling", out _));
        Assert.True(registry.TryGetStrategy("windmill", out _));
        Assert.Equal(50, registry.Strategies.Count);
    }

    [Theory]
    [InlineData("agile")]
    [InlineData("aim")]
    [InlineData("arena")]
    [InlineData("at_mine")]
    [InlineData("blessing")]
    [InlineData("castle_knight")]
    [InlineData("caterpillar")]
    [InlineData("chaotic_knight")]
    [InlineData("charge")]
    [InlineData("checkmate_declaration")]
    [InlineData("cobweb")]
    [InlineData("concentration")]
    [InlineData("dark_hand")]
    [InlineData("democracy")]
    [InlineData("desperado")]
    [InlineData("destroyer_tank_cards")]
    [InlineData("dimension_disturbance")]
    [InlineData("dimension_instability")]
    [InlineData("father_enemy")]
    [InlineData("fast_march")]
    [InlineData("fire")]
    [InlineData("gaslighting")]
    [InlineData("giant")]
    [InlineData("gods_move")]
    [InlineData("honey_trap")]
    [InlineData("jumping_platform")]
    [InlineData("limitless")]
    [InlineData("magnet")]
    [InlineData("missing_promotion")]
    [InlineData("mutiny")]
    [InlineData("obey_order")]
    [InlineData("overbearing")]
    [InlineData("peace_zone")]
    [InlineData("portal")]
    [InlineData("position_swap")]
    [InlineData("psilocybin_mushroom")]
    [InlineData("rampart")]
    [InlineData("revive")]
    [InlineData("shuffle_board")]
    [InlineData("sneak_pawn")]
    [InlineData("stag_fight")]
    [InlineData("sunset_blade")]
    [InlineData("sync")]
    [InlineData("teleport")]
    [InlineData("time_bomb")]
    [InlineData("time_reversal")]
    [InlineData("thunderclap_flash")]
    [InlineData("transmigration")]
    [InlineData("weird_castling")]
    [InlineData("windmill")]
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
            Piece(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
            Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n"),
            Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r"),
            Piece(PieceKind.Queen, PieceColor.White, new Square(3, 0), "q"),
            Piece(PieceKind.King, PieceColor.Black, new Square(4, 7), "k"),
            Piece(PieceKind.Rook, PieceColor.Black, new Square(0, 6), "r"),
            Piece(PieceKind.Queen, PieceColor.Black, new Square(6, 6), "q"),
            Piece(PieceKind.Amazon, PieceColor.Black, new Square(7, 6), "s"),
            Piece(PieceKind.Chancellor, PieceColor.Black, new Square(5, 6), "y", true, new Square(5, 1))
        };
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode,
        bool isPromotioned = false,
        Square? startSquare = null)
    {
        return new PieceInfo(kind, color, square, fenCode, isPromotioned, startSquare);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
