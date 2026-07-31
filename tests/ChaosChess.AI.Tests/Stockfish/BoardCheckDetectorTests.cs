using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Stockfish;
using Xunit;

namespace ChaosChess.AI.Tests.Stockfish
{
    public sealed class BoardCheckDetectorTests
    {
        [Theory]
        [InlineData(PieceKind.Rook, "e8", "r")]
        [InlineData(PieceKind.Bishop, "h4", "b")]
        [InlineData(PieceKind.Queen, "e8", "q")]
        [InlineData(PieceKind.Queen, "h4", "q")]
        [InlineData(PieceKind.Knight, "f3", "n")]
        [InlineData(PieceKind.King, "e2", "k")]
        public void IsInCheck_StandardAttackers_ReturnsTrue(
            PieceKind attackerKind,
            string attackerSquare,
            string fenCode)
        {
            BoardState board = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(attackerKind, PieceColor.Black, attackerSquare, fenCode));

            Assert.True(BoardCheckDetector.IsInCheck(board));
        }

        [Fact]
        public void IsInCheck_PawnAttack_UsesAttackerColorDirection()
        {
            BoardState whiteInCheck = Board(
                Piece(PieceKind.King, PieceColor.White, "e4", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Pawn, PieceColor.Black, "d5", "p"));
            BoardState notInCheck = Board(
                Piece(PieceKind.King, PieceColor.White, "e4", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Pawn, PieceColor.Black, "d3", "p"));

            Assert.True(BoardCheckDetector.IsInCheck(whiteInCheck));
            Assert.False(BoardCheckDetector.IsInCheck(notInCheck));
        }

        [Fact]
        public void IsInCheck_BlockerStopsSlidingAttack()
        {
            BoardState board = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Rook, PieceColor.Black, "e8", "r"),
                Piece(PieceKind.Pawn, PieceColor.White, "e4", "p"));

            Assert.False(BoardCheckDetector.IsInCheck(board));
        }

        [Fact]
        public void IsInCheck_ChecksOnlySideToMoveKing()
        {
            BoardState board = Board(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                    Piece(PieceKind.King, PieceColor.Black, "e8", "k"),
                    Piece(PieceKind.Rook, PieceColor.White, "e2", "r")
                },
                PieceColor.White);

            Assert.False(BoardCheckDetector.IsInCheck(board));
        }

        [Fact]
        public void IsInCheck_AmazonAttacksAsQueenAndKnight()
        {
            BoardState queenLike = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Amazon, PieceColor.Black, "e8", "s"));
            BoardState knightLike = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Amazon, PieceColor.Black, "f3", "s"));

            Assert.True(BoardCheckDetector.IsInCheck(queenLike));
            Assert.True(BoardCheckDetector.IsInCheck(knightLike));
        }

        [Fact]
        public void IsInCheck_ChancellorAttacksAsRookAndKnight()
        {
            BoardState rookLike = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Chancellor, PieceColor.Black, "e8", "y"));
            BoardState knightLike = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Chancellor, PieceColor.Black, "f3", "y"));
            BoardState bishopLike = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Chancellor, PieceColor.Black, "h4", "y"));

            Assert.True(BoardCheckDetector.IsInCheck(rookLike));
            Assert.True(BoardCheckDetector.IsInCheck(knightLike));
            Assert.False(BoardCheckDetector.IsInCheck(bishopLike));
        }

        [Fact]
        public void IsInCheck_KnightRiderAttacksAlongRepeatedKnightStep()
        {
            BoardState board = Board(
                Piece(PieceKind.King, PieceColor.White, "c5", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.KnightRider, PieceColor.Black, "a1", "z"));

            Assert.True(BoardCheckDetector.IsInCheck(board));
        }

        [Fact]
        public void IsInCheck_KnightRiderIsBlockedByIntermediatePiece()
        {
            BoardState board = Board(
                Piece(PieceKind.King, PieceColor.White, "c5", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.KnightRider, PieceColor.Black, "a1", "z"),
                Piece(PieceKind.Pawn, PieceColor.White, "b3", "p"));

            Assert.False(BoardCheckDetector.IsInCheck(board));
        }

        [Fact]
        public void IsInCheck_WallBlocksAndDoesNotAttack()
        {
            BoardState blocked = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Rook, PieceColor.Black, "e8", "r"),
                Piece(PieceKind.Wall, PieceColor.Black, "e4", "a"));
            BoardState wallOnly = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Wall, PieceColor.Black, "e2", "a"));

            Assert.False(BoardCheckDetector.IsInCheck(blocked));
            Assert.False(BoardCheckDetector.IsInCheck(wallOnly));
        }

        [Fact]
        public void IsInCheck_MissingSideToMoveKing_ReturnsFalse()
        {
            BoardState board = Board(
                new[]
                {
                    Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                    Piece(PieceKind.Rook, PieceColor.Black, "e8", "r")
                },
                PieceColor.White);

            Assert.False(BoardCheckDetector.IsInCheck(board));
        }

        [Fact]
        public void StockfishProcessEngine_IsInCheck_UsesPureDetector()
        {
            var engine = new StockfishProcessEngine(
                new StockfishEngineOptions("engine.exe", "variants.ini"),
                _ => throw new InvalidOperationException("Process should not start for check detection."));
            BoardState board = Board(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.King, PieceColor.Black, "a8", "k"),
                Piece(PieceKind.Rook, PieceColor.Black, "e8", "r"));

            Assert.True(engine.IsInCheck(board));
        }

        private static BoardState Board(params PieceInfo[] pieces)
        {
            return Board(pieces, PieceColor.White);
        }

        private static BoardState Board(IEnumerable<PieceInfo> pieces, PieceColor sideToMove)
        {
            return new BoardState(
                pieces,
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1);
        }

        private static PieceInfo Piece(
            PieceKind kind,
            PieceColor color,
            string square,
            string fenCode)
        {
            return new PieceInfo(
                kind,
                color,
                Square.Parse(square),
                fenCode);
        }
    }
}
