using System;

namespace ChaosChess.AI.Domain
{
    public readonly struct Square : IEquatable<Square>
    {
        public const int BoardSize = 8;

        public Square(int file, int rank)
        {
            if (file < 0 || file >= BoardSize)
            {
                throw new ArgumentOutOfRangeException(nameof(file), file, "File must be between 0 and 7.");
            }

            if (rank < 0 || rank >= BoardSize)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be between 0 and 7.");
            }

            File = file;
            Rank = rank;
        }

        public int File { get; }

        public int Rank { get; }

        public static Square Parse(string notation)
        {
            if (!TryParse(notation, out Square square))
            {
                throw new FormatException($"Invalid square notation: '{notation}'.");
            }

            return square;
        }

        public static bool TryParse(string? notation, out Square square)
        {
            square = default;

            if (notation == null || notation.Length != 2)
            {
                return false;
            }

            char file = char.ToLowerInvariant(notation[0]);
            char rank = notation[1];

            if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            {
                return false;
            }

            square = new Square(file - 'a', rank - '1');
            return true;
        }

        public bool Equals(Square other)
        {
            return File == other.File && Rank == other.Rank;
        }

        public override bool Equals(object? obj)
        {
            return obj is Square other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(File, Rank);
        }

        public override string ToString()
        {
            return string.Concat((char)('a' + File), (char)('1' + Rank));
        }

        public static bool operator ==(Square left, Square right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Square left, Square right)
        {
            return !left.Equals(right);
        }
    }
}
