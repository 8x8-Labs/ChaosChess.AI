using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class HoneyTrapCardTargetStrategyTests
{
    private readonly HoneyTrapCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_AveragesRandomQueenOutcomes()
    {
        GameState state = State(
            Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
            Piece(PieceKind.Queen, PieceColor.White, "e5", "q"),
            Piece(PieceKind.Queen, PieceColor.White, "a8", "q"),
            Piece(PieceKind.Rook, PieceColor.Black, "e7", "r"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        CardPlanScoreComponent capture = Component(result.SelectedCandidate!.Score, "honey_trap.expected_capture_gain");
        Assert.Equal(2, capture.RawValue);
        Assert.Equal(3, capture.Weight);
    }

    [Fact]
    public void Decide_WithoutActorQueen_ReturnsNoBenefit()
    {
        GameState state = State(Piece(PieceKind.King, PieceColor.Black, "e8", "k"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new HoneyTrapCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(GameState state)
    {
        return new CardTargetStrategyContext(state, state.AvailableCards[0], PieceColor.White);
    }

    private static CardPlanScoreComponent Component(CardPlanScore score, string code)
    {
        foreach (CardPlanScoreComponent component in score.Components)
        {
            if (component.Code == code)
            {
                return component;
            }
        }

        throw new InvalidOperationException("Component not found: " + code);
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
            new[] { new CardInfo("honey_trap", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square, string fenCode)
    {
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }
}
