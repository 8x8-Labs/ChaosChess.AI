using System;

namespace ChaosChess.AI.Decision
{
    public sealed class MoveFilterOptions
    {
        public MoveFilterOptions(
            double scoreNormalizationDivisor = 13.0,
            double fireRiskWeight = 0.8,
            int peaceEntryBonus = 20,
            int portalEntryBonus = 15)
        {
            if (double.IsNaN(scoreNormalizationDivisor) ||
                double.IsInfinity(scoreNormalizationDivisor) ||
                scoreNormalizationDivisor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scoreNormalizationDivisor), scoreNormalizationDivisor, "Score normalization divisor must be positive.");
            }

            if (double.IsNaN(fireRiskWeight) ||
                double.IsInfinity(fireRiskWeight) ||
                fireRiskWeight < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fireRiskWeight), fireRiskWeight, "Fire risk weight cannot be negative.");
            }

            ScoreNormalizationDivisor = scoreNormalizationDivisor;
            FireRiskWeight = fireRiskWeight;
            PeaceEntryBonus = peaceEntryBonus;
            PortalEntryBonus = portalEntryBonus;
        }

        public double ScoreNormalizationDivisor { get; }

        public double FireRiskWeight { get; }

        public int PeaceEntryBonus { get; }

        public int PortalEntryBonus { get; }
    }
}
