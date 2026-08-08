using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Domain;

public sealed class CardUsePlanTests
{
    [Fact]
    public void Constructor_StoresCardActorTargetAndDefaultEmptyParameters()
    {
        CardTargetSelection target = CardTargetSelection.BoardSquare(new Square(4, 3));

        var plan = new CardUsePlan("fire", PieceColor.Black, target);

        Assert.Equal("fire", plan.CardId);
        Assert.Equal(PieceColor.Black, plan.Actor);
        Assert.Same(target, plan.Target);
        Assert.Same(CardEffectParameters.Empty, plan.EffectParameters);
    }

    [Fact]
    public void Constructor_RejectsInvalidArguments()
    {
        CardTargetSelection target = CardTargetSelection.None();

        Assert.Throws<ArgumentException>(
            () => new CardUsePlan("", PieceColor.White, target));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardUsePlan("charge", (PieceColor)99, target));
        Assert.Throws<ArgumentNullException>(
            () => new CardUsePlan("charge", PieceColor.White, null!));
    }

    [Fact]
    public void NoneTarget_HasNoPieceOrSquares()
    {
        CardTargetSelection target = CardTargetSelection.None();

        Assert.Equal(CardTargetKind.None, target.Kind);
        Assert.Null(target.Piece);
        Assert.Empty(target.Squares);
    }

    [Fact]
    public void PieceAtSquareTarget_StoresPieceSnapshotAndSquare()
    {
        var startSquare = new Square(4, 1);
        var piece = new PieceTargetSnapshot(
            new Square(4, 7),
            PieceColor.White,
            PieceKind.Queen,
            isPromotioned: true,
            startSquare);

        CardTargetSelection target = CardTargetSelection.PieceAtSquare(piece);

        Assert.Equal(CardTargetKind.PieceAtSquare, target.Kind);
        Assert.Same(piece, target.Piece);
        Square square = Assert.Single(target.Squares);
        Assert.Equal(piece.Square, square);
        Assert.True(piece.IsPromotioned);
        Assert.Equal(startSquare, piece.StartSquare);
    }

    [Fact]
    public void PieceTargetSnapshot_RejectsUnknownPieceState()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PieceTargetSnapshot(
                new Square(0, 0),
                (PieceColor)99,
                PieceKind.Pawn));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PieceTargetSnapshot(
                new Square(0, 0),
                PieceColor.White,
                PieceKind.Unknown));
    }

    [Fact]
    public void BoardSquareTarget_StoresSingleSquare()
    {
        var square = new Square(2, 5);

        CardTargetSelection target = CardTargetSelection.BoardSquare(square);

        Assert.Equal(CardTargetKind.BoardSquare, target.Kind);
        Assert.Null(target.Piece);
        Assert.Equal(square, Assert.Single(target.Squares));
    }

    [Fact]
    public void OrderedSquaresTarget_PreservesInputOrder()
    {
        var first = new Square(0, 0);
        var second = new Square(7, 7);

        CardTargetSelection target = CardTargetSelection.OrderedSquares(
            new[] { first, second });

        Assert.Equal(CardTargetKind.OrderedSquares, target.Kind);
        Assert.Null(target.Piece);
        Assert.Equal(new[] { first, second }, target.Squares);
    }

    [Fact]
    public void OrderedSquaresTarget_DefensivelyCopiesInput()
    {
        var squares = new List<Square>
        {
            new Square(0, 0),
            new Square(1, 1)
        };

        CardTargetSelection target = CardTargetSelection.OrderedSquares(squares);
        squares[0] = new Square(7, 7);
        squares.Add(new Square(2, 2));

        Assert.Equal(2, target.Squares.Count);
        Assert.Equal(new Square(0, 0), target.Squares[0]);
        Assert.Equal(new Square(1, 1), target.Squares[1]);
    }

    [Fact]
    public void OrderedSquaresTarget_RejectsInvalidInput()
    {
        Assert.Throws<ArgumentNullException>(
            () => CardTargetSelection.OrderedSquares(null!));
        Assert.Throws<ArgumentException>(
            () => CardTargetSelection.OrderedSquares(Array.Empty<Square>()));
    }

    [Fact]
    public void PieceAtSquareTarget_RejectsNullSnapshot()
    {
        Assert.Throws<ArgumentNullException>(
            () => CardTargetSelection.PieceAtSquare(null!));
    }
}
