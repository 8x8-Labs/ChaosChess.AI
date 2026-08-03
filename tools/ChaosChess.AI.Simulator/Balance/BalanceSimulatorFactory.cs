using System;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;

namespace ChaosChess.AI.Simulator.Balance
{
    public static class BalanceSimulatorFactory
    {
        public static GameSimulator CreateBaselineSimulator(IChessEngine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            CardBalanceProfile profile = CardBalanceProfileCatalog.CreateP10Baseline();

            return new GameSimulator(
                engine,
                new GameStateEvaluator(engine),
                new CardDecisionModule(
                    new ConfiguredCardScorer(profile.CategoryScores, profile.CardScores),
                    new EloCardProfile(
                        profile.MinimumScoreGain,
                        profile.MaximumCardsPerTurn)),
                new MoveFilter(engine),
                cardTargetingModule: new CardTargetingModule());
        }
    }
}
