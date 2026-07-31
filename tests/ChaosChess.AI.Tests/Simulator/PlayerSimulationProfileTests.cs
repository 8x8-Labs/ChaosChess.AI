using System;
using ChaosChess.AI.Simulator;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class PlayerSimulationProfileTests
    {
        [Fact]
        public void Constructor_StoresEngineAndDecisionProfilesSeparately()
        {
            var profile = new PlayerSimulationProfile(
                profileId: "engine-1200-balanced",
                decisionProfileId: "balanced-card-user",
                maxCardsPerTurn: 1,
                useRandomTieBreak: true,
                cardScoreBias: 5,
                engineElo: 1200);

            Assert.Equal("engine-1200-balanced", profile.ProfileId);
            Assert.Equal("balanced-card-user", profile.DecisionProfileId);
            Assert.Equal(1200, profile.EngineElo);
            Assert.Equal(1, profile.MaxCardsPerTurn);
            Assert.True(profile.UseRandomTieBreak);
            Assert.Equal(5, profile.CardScoreBias);
        }

        [Fact]
        public void Constructor_AllowsNoEngineElo()
        {
            var profile = new PlayerSimulationProfile(
                profileId: "full-strength",
                decisionProfileId: "default",
                maxCardsPerTurn: 0,
                useRandomTieBreak: false);

            Assert.Null(profile.EngineElo);
        }

        [Fact]
        public void Constructor_InvalidValues_Throw()
        {
            Assert.Throws<ArgumentException>(
                () => new PlayerSimulationProfile("", "default", 1, false));
            Assert.Throws<ArgumentException>(
                () => new PlayerSimulationProfile("profile", "", 1, false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PlayerSimulationProfile("profile", "default", -1, false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PlayerSimulationProfile("profile", "default", 1, false, engineElo: 0));
        }

        [Fact]
        public void CreateColorSwap_SwapsProfilesAndMarksSwap()
        {
            var white = new PlayerSimulationProfile("white", "white-decision", 1, false);
            var black = new PlayerSimulationProfile("black", "black-decision", 2, true);
            var matchup = new MatchupDefinition("white-vs-black", white, black, colorSwap: false);

            MatchupDefinition swapped = matchup.CreateColorSwap();

            Assert.Equal("white-vs-black-swap", swapped.MatchupId);
            Assert.Same(black, swapped.WhiteProfile);
            Assert.Same(white, swapped.BlackProfile);
            Assert.True(swapped.ColorSwap);
        }

        [Fact]
        public void MatchupConstructor_InvalidValues_Throw()
        {
            var profile = new PlayerSimulationProfile("profile", "default", 1, false);

            Assert.Throws<ArgumentException>(
                () => new MatchupDefinition("", profile, profile, colorSwap: false));
            Assert.Throws<ArgumentNullException>(
                () => new MatchupDefinition("matchup", null!, profile, colorSwap: false));
            Assert.Throws<ArgumentNullException>(
                () => new MatchupDefinition("matchup", profile, null!, colorSwap: false));
        }
    }
}
