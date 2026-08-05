using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class PawnMovementOverrideCardTargetStrategyTests
{
    [Theory]
    [InlineData("aim")]
    [InlineData("fast_march")]
    public void Decide_SelectsActorPawnUsedByEngineTopMove(string cardId)
    {
        var firstPawn = Pawn(PieceColor.White, new Square(0, 1));
        var enginePawn = Pawn(PieceColor.White, new Square(4, 1));
        GameState state = State(
            PieceColor.White,
            cardId,
            new[] { firstPawn, enginePawn });
        var strategy = new PawnMovementOverrideCardTargetStrategy(cardId, cardId);

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                state.AvailableCards[0],
                PieceColor.White,
                new[] { Move("e2e4") }));

        Assert.True(result.HasSelection, result.Reason);
        CardPlanCandidate selected = result.SelectedCandidate!;
        Assert.Equal(cardId, selected.Plan.CardId);
        Assert.Equal(enginePawn.Square, selected.Plan.Target.Piece!.Square);
        Assert.Equal(13, selected.Score.Total);
        Assert.Contains(
            selected.Score.Components,
            component => component.Code == cardId + ".engine_source" && component.Value == 8);
        Assert.Contains(
            selected.Score.Components,
            component => component.Code == cardId + ".engine_destination_relation" && component.Value == 4);
    }

    [Fact]
    public void Decide_ScoresPromotionPressureAsTieBreaker()
    {
        var farPawn = Pawn(PieceColor.White, new Square(0, 1));
        var nearPromotionPawn = Pawn(PieceColor.White, new Square(1, 6));
        GameState state = State(
            PieceColor.White,
            "aim",
            new[] { farPawn, nearPromotionPawn });
        var strategy = new PawnMovementOverrideCardTargetStrategy("aim", "Aim");

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(nearPromotionPawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Equal(3, result.SelectedCandidate.Score.Total);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "aim.promotion_pressure" && component.Value == 2);
    }

    [Fact]
    public void Decide_WrongCardIdReturnsUnsupported()
    {
        GameState state = State(
            PieceColor.White,
            "fast_march",
            new[] { Pawn(PieceColor.White, new Square(4, 1)) });
        var strategy = new PawnMovementOverrideCardTargetStrategy("aim", "Aim");

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Decide_NoActorPawnReturnsNoLegalCandidate()
    {
        GameState state = State(
            PieceColor.White,
            "aim",
            new[] { Pawn(PieceColor.Black, new Square(0, 6)) });
        var strategy = new PawnMovementOverrideCardTargetStrategy("aim", "Aim");

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Constructor_InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new PawnMovementOverrideCardTargetStrategy(string.Empty, "Aim"));
        Assert.Throws<ArgumentNullException>(
            () => new PawnMovementOverrideCardTargetStrategy("aim", "Aim", null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        CardInfo card,
        PieceColor actor,
        IEnumerable<MoveCandidate>? moves = null)
    {
        return new CardTargetStrategyContext(
            state,
            card,
            actor,
            engineTopMoves: moves);
    }

    private static GameState State(
        PieceColor sideToMove,
        string cardId,
        PieceInfo[] pieces)
    {
        return new GameState(
            new BoardState(
                pieces,
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo(cardId, "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Pawn(PieceColor color, Square square)
    {
        return new PieceInfo(PieceKind.Pawn, color, square, "p");
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
