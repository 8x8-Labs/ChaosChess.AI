using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;
using ChaosChess.AI.Simulation.Metrics;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance
{
    public sealed class BalanceCardDecisionMetricCollectorTests
    {
        [Fact]
        public void Collect_WithRecommendedPlan_CreatesDecisionAndComponentMetrics()
        {
            CardBalanceProfile profile = CardBalanceProfileCatalog.CreateP10Baseline();
            SimulationResult simulation = Simulate(Scenario("charge-strong", "charge", "Mobility"));

            BalanceCardDecisionMetricCollection metrics = BalanceCardDecisionMetricCollector.Collect(
                simulation,
                profile);

            CardDecisionMetricEvent decision = Assert.Single(metrics.DecisionEvents);
            Assert.Equal("ply-0:card-0:charge", decision.EventId);
            Assert.Equal(0, decision.PlyIndex);
            Assert.Equal(PieceColor.White, decision.Actor);
            Assert.Equal("charge", decision.CardId);
            Assert.Equal("Mobility", decision.Category);
            Assert.Equal(1, decision.RemainingUses);
            Assert.True(decision.Offered);
            Assert.True(decision.Supported);
            Assert.True(decision.Eligible);
            Assert.True(decision.LegalCandidateCount > 0);
            Assert.True(decision.PlanSelected);
            Assert.True(decision.Recommended);
            Assert.Equal(CardDecisionAppliedStatus.NotAvailable, decision.AppliedStatus);
            Assert.Equal(CardDecisionMetricCode.Recommended, decision.Code);
            Assert.Equal(8, decision.BaseScore);
            Assert.NotNull(decision.PlanScoreTotal);
            Assert.Null(decision.CombinedGainBeforeClamp);
            Assert.Equal(profile.TargetingProfile.ActivationThreshold, decision.TargetingThreshold);
            Assert.Equal(profile.MinimumScoreGain, decision.MinimumScoreGain);
            Assert.NotNull(decision.SelectedPlan);

            Assert.NotEmpty(metrics.ComponentEvents);
            CardScoreComponentMetricEvent component = Assert.Single(
                metrics.ComponentEvents,
                metric => metric.ComponentCode == "charge.movable_pawns");
            Assert.Equal("ply-0:card-0:charge:component-0", component.EventId);
            Assert.Equal("charge", component.CardId);
            Assert.Null(component.CandidateRank);
            Assert.Equal(1, component.RawValue);
            Assert.Equal(2, component.Weight);
            Assert.Equal(2, component.Contribution);
            Assert.Equal(profile.ProfileId, component.ProfileId);
        }

        [Fact]
        public void Collect_WithoutRecommendations_ReturnsEmptyMetrics()
        {
            CardBalanceProfile profile = CardBalanceProfileCatalog.CreateP10Baseline();
            SimulationResult simulation = Simulate(Scenario("unknown", "unknown-card", "Utility"));

            BalanceCardDecisionMetricCollection metrics = BalanceCardDecisionMetricCollector.Collect(
                simulation,
                profile);

            Assert.Empty(metrics.DecisionEvents);
            Assert.Empty(metrics.ComponentEvents);
        }

        [Fact]
        public void Collect_InvalidArguments_Throw()
        {
            CardBalanceProfile profile = CardBalanceProfileCatalog.CreateP10Baseline();
            SimulationResult simulation = Simulate(Scenario("charge-strong", "charge", "Mobility"));

            Assert.Throws<ArgumentNullException>(
                () => BalanceCardDecisionMetricCollector.Collect(null!, profile));
            Assert.Throws<ArgumentNullException>(
                () => BalanceCardDecisionMetricCollector.Collect(simulation, null!));
        }

        private static SimulationResult Simulate(BalanceSimulationScenario scenario)
        {
            var engine = new StubChessEngine(new[] { Move("e2e4", 10) });
            GameSimulator simulator = BalanceSimulatorFactory.CreateBaselineSimulator(engine);

            return simulator.SimulateFuture(
                BalanceScenarioGameStateFactory.Create(scenario),
                scenario.Actor,
                new SimulationOptions(horizonPly: 1, variationCount: 1));
        }

        private static BalanceSimulationScenario Scenario(
            string scenarioId,
            string cardId,
            string category)
        {
            return new BalanceSimulationScenario(
                scenarioId,
                schemaVersion: 1,
                "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1",
                PieceColor.White,
                new[] { new BalanceScenarioCard(cardId, category, remainingUses: 1) },
                tileEffects: null,
                engineObservation: null,
                "collector",
                BalanceExpectedBehavior.ShouldUse);
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
