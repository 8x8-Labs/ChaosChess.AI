using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Domain;

public sealed class DomainModelTests
{
    [Fact]
    public void PieceInfo_ExplicitKind_PreservesIdentityAndOverrideFenCode()
    {
        var piece = new PieceInfo(
            PieceKind.Pawn,
            PieceColor.White,
            Square.Parse("e4"),
            "T");

        Assert.Equal(PieceKind.Pawn, piece.Kind);
        Assert.Equal("t", piece.FenCode);
        Assert.Equal(PieceColor.White, piece.Color);
    }

    [Fact]
    public void PieceInfo_ExplicitKind_PreservesPromotionMetadata()
    {
        var startSquare = Square.Parse("e2");

        var piece = new PieceInfo(
            PieceKind.Queen,
            PieceColor.White,
            Square.Parse("e8"),
            "q",
            isPromotioned: true,
            startSquare);

        Assert.True(piece.IsPromotioned);
        Assert.Equal(startSquare, piece.StartSquare);
    }

    [Theory]
    [InlineData("p", PieceKind.Pawn)]
    [InlineData("S", PieceKind.Amazon)]
    [InlineData("y", PieceKind.Chancellor)]
    [InlineData("Z", PieceKind.KnightRider)]
    [InlineData("a", PieceKind.Wall)]
    [InlineData("t", PieceKind.Unknown)]
    public void PieceInfo_WithoutExplicitKind_InfersKnownFenCodes(string fenCode, PieceKind expected)
    {
        var piece = new PieceInfo(PieceColor.Black, Square.Parse("a1"), fenCode);

        Assert.Equal(expected, piece.Kind);
    }

    [Fact]
    public void BoardState_DuplicateSquare_Throws()
    {
        PieceInfo[] pieces =
        {
            new PieceInfo(PieceColor.White, Square.Parse("a1"), "r"),
            new PieceInfo(PieceColor.Black, Square.Parse("a1"), "q")
        };

        Assert.Throws<ArgumentException>(() => new BoardState(
            pieces,
            PieceColor.White,
            CastlingRights.None,
            null,
            0,
            1));
    }

    [Fact]
    public void GameState_CopiesInputCollections()
    {
        var boardState = new BoardState(
            Array.Empty<PieceInfo>(),
            PieceColor.White,
            CastlingRights.None,
            null,
            0,
            1);
        var cards = new List<CardInfo>
        {
            new CardInfo("card.fast-march", "Movement", 1)
        };
        var effects = new List<TileEffectInfo>
        {
            new TileEffectInfo("effect.mine.1", "Mine", Square.Parse("c3"), PieceColor.Black, 2)
        };
        var timeReversals = new List<TimeReversalState>
        {
            new TimeReversalState("time-reversal.1", PieceColor.White, 8, boardState)
        };

        var gameState = new GameState(boardState, cards, effects, timeReversals: timeReversals);
        cards.Clear();
        effects.Clear();
        timeReversals.Clear();

        Assert.Single(gameState.AvailableCards);
        Assert.Single(gameState.TileEffects);
        Assert.Single(gameState.TimeReversals);
        Assert.Empty(gameState.CapturedPieces.WhitePieces);
        Assert.Empty(gameState.CapturedPieces.BlackPieces);
    }

    [Fact]
    public void TimeReversalState_ValidatesAndTicks()
    {
        var boardState = new BoardState(
            Array.Empty<PieceInfo>(),
            PieceColor.White,
            CastlingRights.None,
            null,
            0,
            1);

        var timeReversal = new TimeReversalState("time-reversal.1", PieceColor.White, 8, boardState);
        TimeReversalState ticked = timeReversal.Tick();

        Assert.Equal("time-reversal.1", timeReversal.Id);
        Assert.Equal(PieceColor.White, timeReversal.Owner);
        Assert.Equal(7, ticked.RemainingTurns);
        Assert.Same(boardState, ticked.SavedBoardState);
        Assert.Throws<ArgumentException>(() => new TimeReversalState("", PieceColor.White, 1, boardState));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeReversalState("bad", PieceColor.White, -1, boardState));
        Assert.Throws<ArgumentNullException>(() => new TimeReversalState("bad", PieceColor.White, 1, null!));
    }

    [Fact]
    public void CapturedPieceState_CopiesInputCollections()
    {
        var white = new List<PieceKind> { PieceKind.Queen };
        var black = new List<PieceKind> { PieceKind.Rook };

        var capturedPieces = new CapturedPieceState(white, black);
        white.Clear();
        black.Clear();

        Assert.Equal(new[] { PieceKind.Queen }, capturedPieces.WhitePieces);
        Assert.Equal(new[] { PieceKind.Rook }, capturedPieces.GetPieces(PieceColor.Black));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CapturedPieceState(new[] { PieceKind.Unknown }, Array.Empty<PieceKind>()));
    }

    [Fact]
    public void TileEffectInfo_PreservesPersistentLifetime()
    {
        var effect = new TileEffectInfo(
            "portal-1",
            "Portal",
            Square.Parse("c3"),
            PieceColor.White,
            remainingTurns: -1,
            destinationSquare: Square.Parse("f6"),
            sharedRemainingUses: 2,
            TileEffectLifetimeKind.PersistentUntilTriggered);

        Assert.Equal(-1, effect.RemainingTurns);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, effect.LifetimeKind);
        Assert.Equal(Square.Parse("f6"), effect.DestinationSquare);
        Assert.Equal(2, effect.SharedRemainingUses);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TileEffectInfo(
                "invalid",
                "Fire",
                Square.Parse("d4"),
                PieceColor.White,
                remainingTurns: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TileEffectInfo(
                "invalid",
                "Fire",
                Square.Parse("d4"),
                PieceColor.White,
                remainingTurns: 1,
                lifetimeKind: (TileEffectLifetimeKind)99));
    }

    [Fact]
    public void MoveCandidate_RequiresExactlyOneScoreType()
    {
        Assert.Throws<ArgumentException>(() => new MoveCandidate("e2e4", null, null));
        Assert.Throws<ArgumentException>(() => new MoveCandidate("e2e4", 20, 3));

        var centipawnMove = new MoveCandidate("e2e4", 20, null);
        var mateMove = new MoveCandidate("e2e4", null, 3);

        Assert.Equal(20, centipawnMove.ScoreCentipawns);
        Assert.Equal(3, mateMove.MateIn);
    }
}
