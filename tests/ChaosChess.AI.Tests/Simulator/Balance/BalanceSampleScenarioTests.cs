using System;
using System.Collections.Generic;
using System.IO;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance
{
    public sealed class BalanceSampleScenarioTests
    {
        [Fact]
        public void ChargeStrongSample_LoadsAndProducesRecommendedMetric()
        {
            string samplePath = Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "tools",
                "ChaosChess.AI.Simulator",
                "Balance",
                "Samples",
                "charge-strong.balance-scenario.json");
            samplePath = Path.GetFullPath(samplePath);

            BalanceSimulationScenario scenario = BalanceSimulationScenarioJsonLoader.LoadFromFile(samplePath);
            var engine = new StubChessEngine(new[] { Move("e2e4", 10) });
            GameSimulator simulator = BalanceSimulatorFactory.CreateBaselineSimulator(engine);

            SimulationResult simulation = simulator.SimulateFuture(
                BalanceScenarioGameStateFactory.Create(scenario),
                scenario.Actor,
                new SimulationOptions(horizonPly: 1, variationCount: 1));
            BalanceCardDecisionMetricCollection metrics = BalanceCardDecisionMetricCollector.Collect(
                simulation,
                CardBalanceProfileCatalog.CreateP10Baseline());

            Assert.Equal("charge-strong", scenario.ScenarioId);
            Assert.Equal(BalanceExpectedBehavior.ShouldUse, scenario.ExpectedBehavior);
            Assert.Single(metrics.DecisionEvents);
            Assert.NotEmpty(metrics.ComponentEvents);
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
