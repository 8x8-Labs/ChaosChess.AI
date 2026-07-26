using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Fen
{
    public static class FenParser
    {
        public static BoardState Parse(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new FormatException("FEN cannot be empty.");
            }

            string[] fields = fen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length != 6)
            {
                throw new FormatException("FEN must contain exactly six fields.");
            }

            IReadOnlyList<PieceInfo> pieces = ParsePlacement(fields[0]);
            PieceColor sideToMove = ParseSideToMove(fields[1]);
            CastlingRights castlingRights = ParseCastlingRights(fields[2]);
            Square? enPassantTarget = ParseEnPassantTarget(fields[3]);
            int halfmoveClock = ParseHalfmoveClock(fields[4]);
            int fullmoveNumber = ParseFullmoveNumber(fields[5]);

            return new BoardState(
                pieces,
                sideToMove,
                castlingRights,
                enPassantTarget,
                halfmoveClock,
                fullmoveNumber);
        }

        public static bool TryParse(string? fen, out BoardState? boardState)
        {
            try
            {
                boardState = Parse(fen ?? string.Empty);
                return true;
            }
            catch (FormatException)
            {
                boardState = null;
                return false;
            }
            catch (ArgumentException)
            {
                boardState = null;
                return false;
            }
            catch (OverflowException)
            {
                boardState = null;
                return false;
            }
        }

        public static string Serialize(BoardState boardState)
        {
            if (boardState == null)
            {
                throw new ArgumentNullException(nameof(boardState));
            }

            var piecesBySquare = new Dictionary<Square, PieceInfo>();

            foreach (PieceInfo piece in boardState.Pieces)
            {
                piecesBySquare.Add(piece.Square, piece);
            }

            var builder = new StringBuilder();

            for (int rank = Square.BoardSize - 1; rank >= 0; rank--)
            {
                int emptyCount = 0;

                for (int file = 0; file < Square.BoardSize; file++)
                {
                    var square = new Square(file, rank);

                    if (!piecesBySquare.TryGetValue(square, out PieceInfo? piece))
                    {
                        emptyCount++;
                        continue;
                    }

                    if (emptyCount > 0)
                    {
                        builder.Append(emptyCount);
                        emptyCount = 0;
                    }

                    char symbol = piece.FenCode[0];
                    builder.Append(piece.Color == PieceColor.White
                        ? char.ToUpperInvariant(symbol)
                        : char.ToLowerInvariant(symbol));
                }

                if (emptyCount > 0)
                {
                    builder.Append(emptyCount);
                }

                if (rank > 0)
                {
                    builder.Append('/');
                }
            }

            builder.Append(' ');
            builder.Append(boardState.SideToMove == PieceColor.White ? 'w' : 'b');
            builder.Append(' ');
            builder.Append(boardState.CastlingRights.ToFen());
            builder.Append(' ');
            builder.Append(boardState.EnPassantTarget?.ToString() ?? "-");
            builder.Append(' ');
            builder.Append(boardState.HalfmoveClock.ToString(CultureInfo.InvariantCulture));
            builder.Append(' ');
            builder.Append(boardState.FullmoveNumber.ToString(CultureInfo.InvariantCulture));

            return builder.ToString();
        }

        private static IReadOnlyList<PieceInfo> ParsePlacement(string placement)
        {
            string[] ranks = placement.Split('/');

            if (ranks.Length != Square.BoardSize)
            {
                throw new FormatException("Piece placement must contain eight ranks.");
            }

            var pieces = new List<PieceInfo>();

            for (int rankIndex = 0; rankIndex < ranks.Length; rankIndex++)
            {
                int file = 0;
                string rankText = ranks[rankIndex];

                foreach (char token in rankText)
                {
                    if (token >= '1' && token <= '8')
                    {
                        file += token - '0';
                    }
                    else
                    {
                        if (!IsAsciiLetter(token))
                        {
                            throw new FormatException($"Invalid piece symbol: '{token}'.");
                        }

                        if (file >= Square.BoardSize)
                        {
                            throw new FormatException("A rank contains more than eight squares.");
                        }

                        PieceColor color = char.IsUpper(token) ? PieceColor.White : PieceColor.Black;
                        var square = new Square(file, Square.BoardSize - 1 - rankIndex);
                        pieces.Add(new PieceInfo(color, square, token.ToString()));
                        file++;
                    }

                    if (file > Square.BoardSize)
                    {
                        throw new FormatException("A rank contains more than eight squares.");
                    }
                }

                if (file != Square.BoardSize)
                {
                    throw new FormatException("Every rank must contain exactly eight squares.");
                }
            }

            return pieces;
        }

        private static PieceColor ParseSideToMove(string value)
        {
            if (value == "w")
            {
                return PieceColor.White;
            }

            if (value == "b")
            {
                return PieceColor.Black;
            }

            throw new FormatException("Side to move must be 'w' or 'b'.");
        }

        private static CastlingRights ParseCastlingRights(string value)
        {
            if (value == "-")
            {
                return CastlingRights.None;
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new FormatException("Castling rights cannot be empty.");
            }

            CastlingRights result = CastlingRights.None;

            foreach (char token in value)
            {
                CastlingRights right;

                switch (token)
                {
                    case 'K':
                        right = CastlingRights.WhiteKingSide;
                        break;
                    case 'Q':
                        right = CastlingRights.WhiteQueenSide;
                        break;
                    case 'k':
                        right = CastlingRights.BlackKingSide;
                        break;
                    case 'q':
                        right = CastlingRights.BlackQueenSide;
                        break;
                    default:
                        throw new FormatException($"Invalid castling right: '{token}'.");
                }

                if ((result & right) != 0)
                {
                    throw new FormatException($"Duplicate castling right: '{token}'.");
                }

                result |= right;
            }

            return result;
        }

        private static Square? ParseEnPassantTarget(string value)
        {
            if (value == "-")
            {
                return null;
            }

            if (!Square.TryParse(value, out Square square) || (square.Rank != 2 && square.Rank != 5))
            {
                throw new FormatException("En passant target must be '-' or a square on rank 3 or 6.");
            }

            return square;
        }

        private static int ParseHalfmoveClock(string value)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < 0)
            {
                throw new FormatException("Halfmove clock must be a non-negative integer.");
            }

            return result;
        }

        private static int ParseFullmoveNumber(string value)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < 1)
            {
                throw new FormatException("Fullmove number must be a positive integer.");
            }

            return result;
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z');
        }
    }
}
