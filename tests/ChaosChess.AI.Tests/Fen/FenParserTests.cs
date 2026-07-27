using ChaosChess.AI.Domain;
using ChaosChess.AI.Fen;
using Xunit;

namespace ChaosChess.AI.Tests.Fen;

public sealed class FenParserTests
{
    [Fact]
    public void Parse_StartingPosition_RoundTripsWithoutLoss()
    {
        const string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        BoardState boardState = FenParser.Parse(fen);

        Assert.Equal(32, boardState.Pieces.Count);
        Assert.Equal(PieceColor.White, boardState.SideToMove);
        Assert.Equal(
            CastlingRights.WhiteKingSide |
            CastlingRights.WhiteQueenSide |
            CastlingRights.BlackKingSide |
            CastlingRights.BlackQueenSide,
            boardState.CastlingRights);
        Assert.Null(boardState.EnPassantTarget);
        Assert.Equal(fen, FenParser.Serialize(boardState));
    }

    [Fact]
    public void Parse_CustomAndOverrideSymbols_RoundTripsWithoutLoss()
    {
        const string fen = "SYZA4/8/8/3T4/8/8/8/4azys b - - 12 34";

        BoardState boardState = FenParser.Parse(fen);
        PieceInfo overridePiece = Assert.Single(
            boardState.Pieces,
            piece => piece.Square == Square.Parse("d5"));

        Assert.Equal(PieceKind.Unknown, overridePiece.Kind);
        Assert.Equal("t", overridePiece.FenCode);
        Assert.Equal(PieceColor.White, overridePiece.Color);
        Assert.Contains(boardState.Pieces, piece => piece.Kind == PieceKind.Amazon);
        Assert.Contains(boardState.Pieces, piece => piece.Kind == PieceKind.Chancellor);
        Assert.Contains(boardState.Pieces, piece => piece.Kind == PieceKind.KnightRider);
        Assert.Contains(boardState.Pieces, piece => piece.Kind == PieceKind.Wall);
        Assert.Equal(fen, FenParser.Serialize(boardState));
    }

    [Fact]
    public void Parse_EnPassantAndClocks_PreservesFields()
    {
        const string fen = "8/8/8/3pP3/8/8/8/8 w - d6 7 19";

        BoardState boardState = FenParser.Parse(fen);

        Assert.Equal(Square.Parse("d6"), boardState.EnPassantTarget);
        Assert.Equal(7, boardState.HalfmoveClock);
        Assert.Equal(19, boardState.FullmoveNumber);
        Assert.Equal(fen, FenParser.Serialize(boardState));
    }

    [Theory]
    [InlineData("")]
    [InlineData("8/8/8/8/8/8/8 w - - 0 1")]
    [InlineData("9/8/8/8/8/8/8/8 w - - 0 1")]
    [InlineData("7@/8/8/8/8/8/8/8 w - - 0 1")]
    [InlineData("8/8/8/8/8/8/8/8 x - - 0 1")]
    [InlineData("8/8/8/8/8/8/8/8 w KK - 0 1")]
    [InlineData("8/8/8/8/8/8/8/8 w - e4 0 1")]
    [InlineData("8/8/8/8/8/8/8/8 w - - -1 1")]
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 0")]
    [InlineData("8/8/8/8/8/8/8/8 w - - 0 1 extra")]
    public void TryParse_InvalidFen_ReturnsFalse(string fen)
    {
        bool result = FenParser.TryParse(fen, out BoardState? boardState);

        Assert.False(result);
        Assert.Null(boardState);
    }
}
