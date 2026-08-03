using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;

namespace ChaosChess.AI.Decision
{
    public static class CardBalanceProfileCatalog
    {
        public const string P10BaselineProfileId = "p10-v0.3.0-baseline";
        public const int CurrentSchemaVersion = 1;

        public static CardBalanceProfile CreateP10Baseline()
        {
            return new CardBalanceProfile(
                P10BaselineProfileId,
                CurrentSchemaVersion,
                CreateP10BaselineCategoryScores(),
                cardScores: null,
                minimumScoreGain: 1,
                maximumCardsPerTurn: 1,
                CardTargetingProfile.CreateP10Baseline());
        }

        public static IReadOnlyDictionary<string, int> CreateP10BaselineCategoryScores()
        {
            return new Dictionary<string, int>
            {
                ["Tactical"] = 10,
                ["Defensive"] = 8,
                ["Mobility"] = 8,
                ["BoardControl"] = 10,
                ["Summon"] = 7,
                ["Transformation"] = 7,
                ["Utility"] = 5
            };
        }
    }
}
