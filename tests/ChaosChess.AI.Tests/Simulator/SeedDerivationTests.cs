using System;
using ChaosChess.AI.Simulator;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class SeedDerivationTests
    {
        [Theory]
        [InlineData(12345, 0, 0, false, 1805374444)]
        [InlineData(12345, 1, 0, false, 1059603485)]
        [InlineData(12345, 0, 1, false, 1224553229)]
        [InlineData(12345, 0, 0, true, 1002967805)]
        [InlineData(-1, 0, 0, false, 367161569)]
        public void DeriveGameSeed_UsesStableTestVectors(
            int baseSeed,
            int gameIndex,
            int matchupOrdinal,
            bool colorSwap,
            int expected)
        {
            Assert.Equal(
                expected,
                SeedDerivation.DeriveGameSeed(baseSeed, gameIndex, matchupOrdinal, colorSwap));
        }

        [Fact]
        public void DeriveGameSeed_SameInput_ReturnsSameSeed()
        {
            int first = SeedDerivation.DeriveGameSeed(7, 3, 2, colorSwap: true);
            int second = SeedDerivation.DeriveGameSeed(7, 3, 2, colorSwap: true);

            Assert.Equal(first, second);
        }

        [Fact]
        public void DeriveGameSeed_InputChanges_ChangeSeed()
        {
            int baseline = SeedDerivation.DeriveGameSeed(12345, 0, 0, colorSwap: false);

            Assert.NotEqual(baseline, SeedDerivation.DeriveGameSeed(12345, 1, 0, colorSwap: false));
            Assert.NotEqual(baseline, SeedDerivation.DeriveGameSeed(12345, 0, 1, colorSwap: false));
            Assert.NotEqual(baseline, SeedDerivation.DeriveGameSeed(12345, 0, 0, colorSwap: true));
        }

        [Fact]
        public void DeriveGameSeed_InvalidIndexes_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SeedDerivation.DeriveGameSeed(0, -1, 0, colorSwap: false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SeedDerivation.DeriveGameSeed(0, 0, -1, colorSwap: false));
        }

        [Fact]
        public void DeriveGameSeed_ReturnsNonNegativeInt()
        {
            int seed = SeedDerivation.DeriveGameSeed(int.MinValue, 100, 20, colorSwap: true);

            Assert.True(seed >= 0);
        }
    }
}
