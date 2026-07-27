using System;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Domain;

public sealed class SquareTests
{
    [Theory]
    [InlineData("a1", 0, 0)]
    [InlineData("e4", 4, 3)]
    [InlineData("H8", 7, 7)]
    public void Parse_ValidNotation_UsesZeroBasedCoordinates(string notation, int file, int rank)
    {
        Square square = Square.Parse(notation);

        Assert.Equal(file, square.File);
        Assert.Equal(rank, square.Rank);
        Assert.Equal(notation.ToLowerInvariant(), square.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("a0")]
    [InlineData("i1")]
    [InlineData("a10")]
    [InlineData("11")]
    public void TryParse_InvalidNotation_ReturnsFalse(string notation)
    {
        Assert.False(Square.TryParse(notation, out _));
    }

    [Fact]
    public void Constructor_OutOfBounds_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Square(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Square(0, 8));
    }
}
