using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class MagnetCardTargetStrategyTests
{
    private readonly MagnetCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsEmptySquareWithBestAverageAdjacentPull()
    {
        GameState state = State(
            Piece(PieceKind.Rook, PieceColor.Black, "d4", "r"),
            Piece(PieceKind.Pawn, PieceColor.Black, "e4", "p"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Contains(
            result.SelectedCandidate!.Score.Components,
            component => component.Code == "magnet.expected_enemy_pull");
    }

    [Fact]
    public void Decide_WithoutAdjacentPieces_ReturnsNoBenefit()
    {
        GameState state = State();

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new MagnetCardTargetStrategy(null!));
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
            new[] { new CardInfo("magnet", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square, string fenCode)
    {
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }
}
