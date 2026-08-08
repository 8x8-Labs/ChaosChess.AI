using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class GaslightingCardTargetStrategyTests
{
    private readonly GaslightingCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_AveragesMaterialSwingAcrossRandomConversionCandidates()
    {
        GameState state = State(
            Piece(PieceKind.Pawn, PieceColor.Black, "a7", "p"),
            Piece(PieceKind.Bishop, PieceColor.Black, "c5", "b"),
            Piece(PieceKind.Rook, PieceColor.Black, "h8", "r"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        CardPlanScoreComponent component = Component(result.SelectedCandidate!.Score, "gaslighting.expected_material_swing");
        Assert.Equal(4, component.RawValue);
        Assert.Equal(2, component.Weight);
    }

    [Fact]
    public void Decide_WithoutRandomCandidates_ReturnsNoBenefit()
    {
        GameState state = State(
            Piece(PieceKind.Rook, PieceColor.Black, "a8", "r"),
            Piece(PieceKind.Queen, PieceColor.Black, "d8", "q"));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new GaslightingCardTargetStrategy(null!));
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
            new[] { new CardInfo("gaslighting", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square, string fenCode)
    {
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }
}
