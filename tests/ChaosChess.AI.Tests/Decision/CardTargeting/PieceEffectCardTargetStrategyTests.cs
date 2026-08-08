using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class PieceEffectCardTargetStrategyTests
{
    private readonly PieceEffectCardTargetStrategy strategy = new("dimension_instability", "Dimension Instability");

    [Fact]
    public void Decide_SelectsEngineSourcePiece()
    {
        var knight = Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n");
        GameState state = State(
            PieceColor.White,
            Card("dimension_instability"),
            pieces: new[] { knight });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("b1c3") }));

        Assert.True(result.HasSelection);
        Assert.Equal(knight.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        CardPlanScoreComponent engineSource = Component(
            result.SelectedCandidate.Score,
            "dimension_instability.engine_source");
        Assert.Equal(1, engineSource.RawValue);
        Assert.Equal(8, engineSource.Weight);
        Assert.Equal(8, engineSource.Contribution);
    }

    [Fact]
    public void Decide_UsesPieceValueAndCenterPressureWithoutEngineMove()
    {
        var edgePawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(0, 1));
        var centerBishop = Piece(PieceKind.Bishop, PieceColor.White, new Square(3, 3), "b");
        GameState state = State(
            PieceColor.White,
            Card("giant"),
            pieces: new[] { edgePawn, centerBishop });
        var giantStrategy = new PieceEffectCardTargetStrategy("giant", "Giant");

        CardPlanDecisionResult result = giantStrategy.Decide(Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(centerBishop.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Equal(3, ComponentValue(result.SelectedCandidate.Score, "giant.target_piece_value"));
    }

    [Fact]
    public void Decide_CaptureSetupProfileRewardsEngineCapture()
    {
        var pawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 3));
        var target = Piece(PieceKind.Rook, PieceColor.Black, new Square(3, 4), "r");
        GameState state = State(
            PieceColor.White,
            Card("sunset_blade"),
            pieces: new[] { pawn, target });
        var sunsetStrategy = new PieceEffectCardTargetStrategy(
            "sunset_blade",
            "Sunset Blade",
            PieceEffectTargetProfile.CaptureSetup);

        CardPlanDecisionResult result = sunsetStrategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("e4d5") }));

        Assert.True(result.HasSelection);
        Assert.Equal(pawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Equal(6, ComponentValue(result.SelectedCandidate.Score, "sunset_blade.engine_capture"));
    }

    [Theory]
    [InlineData("chaotic_knight")]
    [InlineData("desperado")]
    [InlineData("dimension_instability")]
    [InlineData("father_enemy")]
    [InlineData("giant")]
    [InlineData("sunset_blade")]
    public void Decide_GenericPieceEffectCardsSelectLegalActorPiece(string cardId)
    {
        PieceInfo piece = cardId == "sunset_blade" || cardId == "father_enemy" || cardId == "desperado"
            ? Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 1))
            : Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n");
        PieceEffectTargetProfile profile = cardId == "sunset_blade"
            ? PieceEffectTargetProfile.CaptureSetup
            : PieceEffectTargetProfile.General;
        var genericStrategy = new PieceEffectCardTargetStrategy(cardId, cardId, profile);
        GameState state = State(PieceColor.White, Card(cardId), pieces: new[] { piece });

        CardPlanDecisionResult result = genericStrategy.Decide(Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(cardId, result.SelectedCandidate!.Plan.CardId);
        Assert.Equal(CardTargetKind.PieceAtSquare, result.SelectedCandidate.Plan.Target.Kind);
    }

    [Fact]
    public void Decide_NoLegalPiece_ReturnsNoLegalCandidate()
    {
        GameState state = State(PieceColor.White, Card("dimension_instability"));

        CardPlanDecisionResult result = strategy.Decide(Context(state, PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_UnsupportedCard_ReturnsUnsupported()
    {
        GameState state = State(PieceColor.White, Card("giant"));

        CardPlanDecisionResult result = strategy.Decide(Context(state, PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Decide_ActorMismatch_ReturnsInvalidActor()
    {
        GameState state = State(
            PieceColor.White,
            Card("dimension_instability"),
            pieces: new[] { Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n") });

        CardPlanDecisionResult result = strategy.Decide(Context(state, PieceColor.Black));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentException>(() => new PieceEffectCardTargetStrategy(string.Empty, "Piece Effect"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PieceEffectCardTargetStrategy("giant", "Giant", (PieceEffectTargetProfile)99));
        Assert.Throws<ArgumentNullException>(
            () => new PieceEffectCardTargetStrategy("giant", "Giant", PieceEffectTargetProfile.General, null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        PieceColor actor,
        MoveCandidate[]? moves = null)
    {
        return new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            actor,
            engineTopMoves: moves);
    }

    private static int ComponentValue(CardPlanScore score, string code)
    {
        return Component(score, code).Value;
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

        throw new InvalidOperationException("Component was not found.");
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

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode = "p")
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
