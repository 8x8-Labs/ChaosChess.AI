using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulator;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance
{
    public sealed class BalanceSimulatorFactoryTests
    {
        [Fact]
        public void CreateBaselineSimulator_NullEngine_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => BalanceSimulatorFactory.CreateBaselineSimulator(null!));
        }

        [Fact]
        public void CreateBaselineSimulator_WithSupportedCard_UsesP10TargetingBaseline()
        {
            var engine = new StubChessEngine(new[] { Move("e2e4", 10) });
            var runner = new BatchSimulationRunner(new HeadlessGameRunner(
                BalanceSimulatorFactory.CreateBaselineSimulator(engine)));
            BalanceSimulationScenario scenario = Scenario(
                "white-charge",
                "charge",
                "Mobility",
                BalanceExpectedBehavior.ShouldUse);

            BatchSimulationResult result = runner.Run(Options(scenario));

            BatchGameResult game = Assert.Single(result.Games);
            Assert.Equal(1, game.GameResult.CardsRecommended);
            Assert.Equal(0, game.GameResult.CardsApplied);
            Assert.Equal("not_applied_contract_missing", game.GameResult.CardsSkippedReason);
        }

        [Fact]
        public void CreateBaselineSimulator_WithUnsupportedCard_DoesNotRecommendCard()
        {
            var engine = new StubChessEngine(new[] { Move("e2e4", 10) });
            var runner = new BatchSimulationRunner(new HeadlessGameRunner(
                BalanceSimulatorFactory.CreateBaselineSimulator(engine)));
            BalanceSimulationScenario scenario = Scenario(
                "white-unknown",
                "unknown-card",
                "Utility",
                BalanceExpectedBehavior.ShouldSkip);

            BatchSimulationResult result = runner.Run(Options(scenario));

            BatchGameResult game = Assert.Single(result.Games);
            Assert.Equal(0, game.GameResult.CardsRecommended);
            Assert.Equal(0, game.GameResult.CardsApplied);
            Assert.Null(game.GameResult.CardsSkippedReason);
        }

        private static BatchSimulationOptions Options(BalanceSimulationScenario scenario)
        {
            return new BatchSimulationOptions(
                "batch-1",
                baseSeed: 12345,
                gameCount: 1,
                scenario.ScenarioId,
                scenario.StartingFen,
                new[] { Matchup("white-baseline", "black-baseline") },
                new HeadlessGameOptions(maxPly: 1, variationCount: 1),
                scenario: scenario);
        }

        private static BalanceSimulationScenario Scenario(
            string scenarioId,
            string cardId,
            string category,
            BalanceExpectedBehavior expectedBehavior)
        {
            return new BalanceSimulationScenario(
                scenarioId,
                schemaVersion: 1,
                "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1",
                PieceColor.White,
                new[] { new BalanceScenarioCard(cardId, category, remainingUses: 1) },
                tileEffects: null,
                engineObservation: null,
                "factory",
                expectedBehavior);
        }

        private static MatchupDefinition Matchup(string whiteProfileId, string blackProfileId)
        {
            return new MatchupDefinition(
                whiteProfileId + "-vs-" + blackProfileId,
                new PlayerSimulationProfile(
                    whiteProfileId,
                    "default",
                    maxCardsPerTurn: 1,
                    useRandomTieBreak: false),
                new PlayerSimulationProfile(
                    blackProfileId,
                    "default",
                    maxCardsPerTurn: 1,
                    useRandomTieBreak: false),
                colorSwap: false);
        }

        private static MoveCandidate Move(string uciMove, int scoreCentipawns)
        {
            return new MoveCandidate(uciMove, scoreCentipawns, mateIn: null);
        }

        private sealed class StubChessEngine : IChessEngine
        {
            private readonly Queue<IReadOnlyList<MoveCandidate>> _moveBatches = new Queue<IReadOnlyList<MoveCandidate>>();

            public StubChessEngine(params IReadOnlyList<MoveCandidate>[] moveBatches)
            {
                foreach (IReadOnlyList<MoveCandidate> batch in moveBatches)
                {
                    _moveBatches.Enqueue(batch);
                }
            }

            public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
            {
                return _moveBatches.Count == 0
                    ? Array.Empty<MoveCandidate>()
                    : _moveBatches.Dequeue();
            }

            public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
            {
                return new PositionEvaluation(boardState.SideToMove, scoreCentipawns: 0, mateIn: null);
            }

            public bool IsInCheck(BoardState boardState)
            {
                return false;
            }
        }
    }
}
