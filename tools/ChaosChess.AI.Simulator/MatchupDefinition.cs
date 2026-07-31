using System;

namespace ChaosChess.AI.Simulator
{
    public sealed class MatchupDefinition
    {
        public MatchupDefinition(
            string matchupId,
            PlayerSimulationProfile whiteProfile,
            PlayerSimulationProfile blackProfile,
            bool colorSwap)
        {
            if (string.IsNullOrWhiteSpace(matchupId))
            {
                throw new ArgumentException("Matchup ID cannot be empty.", nameof(matchupId));
            }

            MatchupId = matchupId;
            WhiteProfile = whiteProfile ?? throw new ArgumentNullException(nameof(whiteProfile));
            BlackProfile = blackProfile ?? throw new ArgumentNullException(nameof(blackProfile));
            ColorSwap = colorSwap;
        }

        public string MatchupId { get; }

        public PlayerSimulationProfile WhiteProfile { get; }

        public PlayerSimulationProfile BlackProfile { get; }

        public bool ColorSwap { get; }

        public MatchupDefinition CreateColorSwap()
        {
            return new MatchupDefinition(
                MatchupId + "-swap",
                BlackProfile,
                WhiteProfile,
                colorSwap: true);
        }
    }
}
