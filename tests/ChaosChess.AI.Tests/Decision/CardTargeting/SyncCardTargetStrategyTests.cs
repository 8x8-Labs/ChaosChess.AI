using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class SyncCardTargetStrategyTests
{
    private readonly SyncCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsEmptySquareMirroredFromActorPiece()
    {
        GameState state = State(
            new PieceInfo(PieceKind.Rook, PieceColor.White, new Square(5, 2), "r"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(new Square(2, 2), result.SelectedCandidate!.Plan.Target.Squares[0]);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "sync.linked_piece_present");
    }

    [Fact]
    public void Decide_WithoutMirroredActorPiece_ReturnsNoBenefit()
    {
        GameState state = State(
            new PieceInfo(PieceKind.Rook, PieceColor.Black, new Square(5, 2), "r"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new SyncCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(GameState state)
    {
        return new CardTargetStrategyContext(state, state.AvailableCards[0], PieceColor.White);
    }

    private static GameState State(params PieceInfo[] pieces)
    {
        return new GameState(
            new BoardState(
                pieces,
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo("sync", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }
}
