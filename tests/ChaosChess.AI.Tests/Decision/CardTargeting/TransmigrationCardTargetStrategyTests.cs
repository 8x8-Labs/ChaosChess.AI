using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class TransmigrationCardTargetStrategyTests
{
    [Fact]
    public void Decide_SelectsHighestValuePromotedOpponentPieceWithEmptyStartSquare()
    {
        var promotedKnight = Piece(
            PieceKind.Knight,
            PieceColor.Black,
            new Square(2, 5),
            "n",
            isPromotioned: true,
            new Square(2, 1));
        var promotedChancellor = Piece(
            PieceKind.Chancellor,
            PieceColor.Black,
            new Square(4, 5),
            "y",
            isPromotioned: true,
            new Square(4, 1));
        GameState state = State(
            PieceColor.White,
            new[] { promotedKnight, promotedChancellor });
        var strategy = new TransmigrationCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        CardPlanCandidate selected = result.SelectedCandidate!;
        Assert.Equal("transmigration", selected.Plan.CardId);
        Assert.Equal(promotedChancellor.Square, selected.Plan.Target.Piece!.Square);
        Assert.True(selected.Plan.Target.Piece.IsPromotioned);
        Assert.Equal(promotedChancellor.StartSquare, selected.Plan.Target.Piece.StartSquare);
        Assert.Contains(
            selected.Score.Components,
            component => component.Code == "transmigration.material_reversion");
    }

    [Fact]
    public void Decide_SkipsTargetsWithoutPromotionMetadata()
    {
        GameState state = State(
            PieceColor.White,
            new[] { Piece(PieceKind.Chancellor, PieceColor.Black, new Square(4, 5), "y") });
        var strategy = new TransmigrationCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_SkipsTargetsWithOccupiedStartSquare()
    {
        var startSquare = new Square(4, 1);
        GameState state = State(
            PieceColor.White,
            new[]
            {
                Piece(PieceKind.Chancellor, PieceColor.Black, new Square(4, 5), "y", true, startSquare),
                Piece(PieceKind.Pawn, PieceColor.White, startSquare, "p")
            });
        var strategy = new TransmigrationCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_WrongCardIdReturnsUnsupported()
    {
        GameState state = State(PieceColor.White, Array.Empty<PieceInfo>(), "fire");
        var strategy = new TransmigrationCardTargetStrategy();

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Constructor_InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TransmigrationCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        CardInfo card,
        PieceColor actor)
    {
        return new CardTargetStrategyContext(state, card, actor);
    }

    private static GameState State(
        PieceColor sideToMove,
        PieceInfo[] pieces,
        string cardId = "transmigration")
    {
        return new GameState(
            new BoardState(
                pieces,
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo(cardId, "Transformation", 1) },
            Array.Empty<TileEffectInfo>());
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
}
