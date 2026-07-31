using System;
using System.Text;

namespace ChaosChess.AI.Domain
{
    [Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteKingSide = 1,
        WhiteQueenSide = 2,
        BlackKingSide = 4,
        BlackQueenSide = 8
    }

    public static class CastlingRightsExtensions
    {
        private const CastlingRights All =
            CastlingRights.WhiteKingSide |
            CastlingRights.WhiteQueenSide |
            CastlingRights.BlackKingSide |
            CastlingRights.BlackQueenSide;

        public static string ToFen(this CastlingRights rights)
        {
            EnsureValid(rights);

            if (rights == CastlingRights.None)
            {
                return "-";
            }

            var builder = new StringBuilder(4);

            if ((rights & CastlingRights.WhiteKingSide) != 0)
            {
                builder.Append('K');
            }

            if ((rights & CastlingRights.WhiteQueenSide) != 0)
            {
                builder.Append('Q');
            }

            if ((rights & CastlingRights.BlackKingSide) != 0)
            {
                builder.Append('k');
            }

            if ((rights & CastlingRights.BlackQueenSide) != 0)
            {
                builder.Append('q');
            }

            return builder.ToString();
        }

        internal static void EnsureValid(CastlingRights rights)
        {
            if ((rights & ~All) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rights), rights, "Unknown castling rights.");
            }
        }
    }
}
