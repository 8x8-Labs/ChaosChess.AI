using System;

namespace ChaosChess.AI.Simulator
{
    public static class SeedDerivation
    {
        public static int DeriveGameSeed(
            int baseSeed,
            int gameIndex,
            int matchupOrdinal,
            bool colorSwap)
        {
            if (gameIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameIndex), gameIndex, "Game index cannot be negative.");
            }

            if (matchupOrdinal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(matchupOrdinal), matchupOrdinal, "Matchup ordinal cannot be negative.");
            }

            unchecked
            {
                uint hash = 2166136261u;
                hash = MixInt(hash, baseSeed);
                hash = MixInt(hash, gameIndex);
                hash = MixInt(hash, matchupOrdinal);
                hash = MixInt(hash, colorSwap ? 1 : 0);
                return (int)(hash & 0x7fffffffu);
            }
        }

        private static uint MixInt(uint hash, int value)
        {
            unchecked
            {
                uint data = (uint)value;

                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (byte)(data >> shift);
                    hash *= 16777619u;
                }

                return hash;
            }
        }
    }
}
