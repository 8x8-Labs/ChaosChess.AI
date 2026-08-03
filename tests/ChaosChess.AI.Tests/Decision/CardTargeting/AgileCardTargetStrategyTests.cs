using System;
using System.Collections.Generic;
using System.Linq;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class AgileCardTargetStrategyTests
{
    private readonly AgileCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsPawnUsedByEngineTopMove()
    {
        var firstPawn = Piece(PieceColor.White, new Square(0, 1));
        var enginePawn = Piece(PieceColor.White, new Square(4, 1));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { firstPawn, enginePawn });
        CardInfo card = state.AvailableCards[0];

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                card,
                PieceColor.White,
                new[] { Move("e2e4") }));

        Assert.True(result.HasSelection);
        CardPlanCandidate selected = result.SelectedCandidate!;
        Assert.Equal(enginePawn.Square, selected.Plan.Target.Piece!.Square);
        Assert.Equal(9, selected.Score.Total);
        CardPlanScoreComponent engineSource = Component(selected.Score, "agile.engine_source");
        Assert.Equal(1, engineSource.RawValue);
        Assert.Equal(8, engineSource.Weight);
        Assert.Equal(8, engineSource.Contribution);
        Assert.Equal(engineSource.Contribution, engineSource.Value);
    }

    [Fact]
    public void Decide_UsesPromotionPressureAsSmallTieBreakerWhenEngineMoveIsUnavailable()
    {
        var farPawn = Piece(PieceColor.White, new Square(0, 1));
        var nearPromotionPawn = Piece(PieceColor.White, new Square(1, 6));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { farPawn, nearPromotionPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(nearPromotionPawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Equal(3, result.SelectedCandidate.Score.Total);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "agile.promotion_pressure" && component.Value == 2);
    }

    [Fact]
    public void Decide_ScoresEngineDestinationRelation()
    {
        var relatedPawn = Piece(PieceColor.White, new Square(3, 3));
        var unrelatedPawn = Piece(PieceColor.White, new Square(0, 1));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { unrelatedPawn, relatedPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                state.AvailableCards[0],
                PieceColor.White,
                new[] { Move("a1e5") }));

        Assert.True(result.HasSelection);
        Assert.Equal(relatedPawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "agile.engine_destination_relation" && component.Value == 4);
    }

    [Fact]
    public void Decide_TiedScoreUsesEnumerationIndex()
    {
        var firstPawn = Piece(PieceColor.White, new Square(0, 1));
        var secondPawn = Piece(PieceColor.White, new Square(1, 1));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { firstPawn, secondPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(firstPawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Equal(0, result.SelectedCandidate.EnumerationIndex);
    }

    [Fact]
    public void Decide_BlackPromotionPressureMirrorsWhite()
    {
        var farPawn = Piece(PieceColor.Black, new Square(0, 6));
        var nearPromotionPawn = Piece(PieceColor.Black, new Square(1, 1));
        GameState state = State(
            PieceColor.Black,
            pieces: new[] { farPawn, nearPromotionPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.Black));

        Assert.True(result.HasSelection);
        Assert.Equal(nearPromotionPawn.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Equal(3, result.SelectedCandidate.Score.Total);
    }

    [Fact]
    public void Decide_NoActorPawn_ReturnsNoLegalCandidate()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Piece(PieceColor.Black, new Square(0, 6)) });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_ActorMismatch_ReturnsInvalidActor()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Piece(PieceColor.White, new Square(0, 1)) });

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.Black));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, result.SkipCode);
    }

    [Fact]
    public void Decide_ThresholdAboveBestScore_ReturnsNoBenefit()
    {
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Piece(PieceColor.White, new Square(0, 1)) });

        CardPlanDecisionResult result = strategy.Decide(
            new CardTargetStrategyContext(
                state,
                state.AvailableCards[0],
                PieceColor.White,
                new CardTargetingOptions(activationThreshold: 2)));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Context_CopiesEngineTopMoves()
    {
        var moves = new[] { Move("e2e4") }.ToList();
        GameState state = State(
            PieceColor.White,
            pieces: new[] { Piece(PieceColor.White, new Square(4, 1)) });

        CardTargetStrategyContext context = Context(
            state,
            state.AvailableCards[0],
            PieceColor.White,
            moves);
        moves.Add(Move("a2a4"));

        MoveCandidate move = Assert.Single(context.EngineTopMoves);
        Assert.Equal("e2e4", move.UciMove);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
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
            new[] { new CardInfo("agile", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(PieceColor color, Square square)
    {
        return new PieceInfo(PieceKind.Pawn, color, square, "p");
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
