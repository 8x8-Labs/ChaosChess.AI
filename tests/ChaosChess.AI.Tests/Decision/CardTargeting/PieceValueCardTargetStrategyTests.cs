using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class PieceValueCardTargetStrategyTests
{
    [Fact]
    public void Decide_SelectsHighestValueLegalPiece()
    {
        var opponentKnight = Piece(PieceKind.Knight, PieceColor.Black, new Square(1, 7), "n");
        var opponentRook = Piece(PieceKind.Rook, PieceColor.Black, new Square(0, 7), "r");
        var opponentAmazon = Piece(PieceKind.Amazon, PieceColor.Black, new Square(3, 7), "s");
        var actorRook = Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r");
        GameState state = State(
            PieceColor.White,
            "missing_promotion",
            new[] { opponentKnight, opponentRook, opponentAmazon, actorRook });
        var strategy = new PieceValueCardTargetStrategy("missing_promotion", "Missing Promotion");

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        CardPlanCandidate selected = result.SelectedCandidate!;
        Assert.Equal("missing_promotion", selected.Plan.CardId);
        Assert.Equal(opponentAmazon.Square, selected.Plan.Target.Piece!.Square);
        Assert.Equal(PieceColor.Black, selected.Plan.Target.Piece.ExpectedColor);
        Assert.Equal(13, selected.Score.Total);
        Assert.Contains(
            selected.Score.Components,
            component => component.Code == "missing_promotion.target_piece_value" && component.Value == 13);
    }

    [Fact]
    public void Decide_UsesPlanningCatalogToExcludeActorPiecesAndUnsupportedKinds()
    {
        var actorRook = Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r");
        var opponentQueen = Piece(PieceKind.Queen, PieceColor.Black, new Square(3, 7), "q");
        var opponentKing = Piece(PieceKind.King, PieceColor.Black, new Square(4, 7), "k");
        GameState state = State(
            PieceColor.White,
            "missing_promotion",
            new[] { actorRook, opponentQueen, opponentKing });
        var strategy = new PieceValueCardTargetStrategy("missing_promotion", "Missing Promotion");

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_WrongCardIdReturnsUnsupported()
    {
        GameState state = State(
            PieceColor.White,
            "fire",
            new[] { Piece(PieceKind.Rook, PieceColor.Black, new Square(0, 7), "r") });
        var strategy = new PieceValueCardTargetStrategy("missing_promotion", "Missing Promotion");

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, state.AvailableCards[0], PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Constructor_InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentException>(
            () => new PieceValueCardTargetStrategy(string.Empty, "Missing Promotion"));
        Assert.Throws<ArgumentNullException>(
            () => new PieceValueCardTargetStrategy("missing_promotion", "Missing Promotion", null!));
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
            new[] { new CardInfo(cardId, "Transformation", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode)
    {
        return new PieceInfo(kind, color, square, fenCode);
    }
}
