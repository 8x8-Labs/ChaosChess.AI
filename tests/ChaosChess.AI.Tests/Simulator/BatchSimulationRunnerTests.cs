using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Simulation;
using ChaosChess.AI.Simulator;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator
{
    public sealed class BatchSimulationRunnerTests
    {
        private const string StartingFen = "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1";

        [Fact]
        public void Run_ExecutesSequentialGamesForEachMatchup()
        {
            var engine = new StubChessEngine(
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) });
            BatchSimulationResult result = Runner(engine).Run(Options(
                gameCount: 2,
                Matchup("a", "b"),
                Matchup("c", "d")));

            Assert.Equal(4, result.Games.Count);
            Assert.Equal(new[] { 0, 0, 1, 1 }, SelectGameIndexes(result.Games));
            Assert.Equal(new[] { 0, 1, 0, 1 }, SelectMatchupOrdinals(result.Games));
        }

        [Fact]
        public void Run_CreatesStableGameIdsAndSeeds()
        {
            var engine = new StubChessEngine(
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) });
            BatchSimulationOptions options = Options(
                gameCount: 1,
                Matchup("white", "black"),
                Matchup("black", "white", colorSwap: true));

            BatchSimulationResult result = Runner(engine).Run(options);

            Assert.Equal(
                SeedDerivation.DeriveGameSeed(options.BaseSeed, 0, 0, colorSwap: false),
                result.Games[0].GameSeed);
            Assert.Equal(
                SeedDerivation.DeriveGameSeed(options.BaseSeed, 0, 1, colorSwap: true),
                result.Games[1].GameSeed);
            Assert.Equal(
                "batch-1:opening:m0:g0:s" + result.Games[0].GameSeed,
                result.Games[0].GameId);
            Assert.Equal(
                "batch-1:opening:m1:g0:s" + result.Games[1].GameSeed,
                result.Games[1].GameId);
        }

        [Fact]
        public void Run_RecordsProfileIdsFromMatchup()
        {
            var engine = new StubChessEngine(new[] { Move("e2e4", 10) });
            BatchSimulationResult result = Runner(engine).Run(Options(
                gameCount: 1,
                Matchup("aggressive", "defensive")));

            BatchGameResult game = Assert.Single(result.Games);
            Assert.Equal("aggressive", game.WhiteProfileId);
            Assert.Equal("defensive", game.BlackProfileId);
        }

        [Fact]
        public void Run_UsesBalanceScenarioInitialCardsAndActorPerspective()
        {
            var engine = new StubChessEngine(new[] { Move("e7e5", 10) });
            var scenario = new BalanceSimulationScenario(
                "black-charge",
                schemaVersion: 1,
                "4k3/4p3/8/8/8/8/8/4K3 b - - 0 1",
                PieceColor.Black,
                new[] { new BalanceScenarioCard("charge", "Mobility", remainingUses: 1) },
                tileEffects: null,
                engineObservation: null,
                "strong",
                BalanceExpectedBehavior.ShouldUse);
            var options = new BatchSimulationOptions(
                "batch-1",
                baseSeed: 12345,
                gameCount: 1,
                "black-charge",
                scenario.StartingFen,
                new[] { Matchup("black-profile", "white-profile") },
                new HeadlessGameOptions(maxPly: 1, variationCount: 1),
                scenario: scenario);

            BatchSimulationResult result = RunnerWithTargeting(engine).Run(options);

            BatchGameResult game = Assert.Single(result.Games);
            Assert.Equal(1, game.GameResult.CardsRecommended);
            Assert.Equal("not_applied_contract_missing", game.GameResult.CardsSkippedReason);
        }

        [Fact]
        public void Run_KeepsInvalidGamesInResultList()
        {
            var engine = new StubChessEngine(
                new[] { Move("e2e4", 10) },
                new[] { Move("z9z9", 10) });
            BatchSimulationResult result = Runner(engine).Run(Options(
                gameCount: 2,
                Matchup("a", "b")));

            Assert.Equal(2, result.Games.Count);
            Assert.Equal(GameResult.Aborted, result.Games[0].GameResult.Result);
            Assert.Equal(GameResult.Invalid, result.Games[1].GameResult.Result);
            Assert.Equal(1, result.InvalidGameCount);
            Assert.Equal(1, result.AbortedGameCount);
        }

        [Fact]
        public void Run_SameInput_ReproducesLogicalOrder()
        {
            BatchSimulationOptions options = Options(
                gameCount: 2,
                Matchup("a", "b"),
                Matchup("c", "d", colorSwap: true));
            BatchSimulationResult first = Runner(new StubChessEngine(
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) })).Run(options);
            BatchSimulationResult second = Runner(new StubChessEngine(
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) },
                new[] { Move("e2e4", 10) })).Run(options);

            Assert.Equal(JoinGameIds(first.Games), JoinGameIds(second.Games));
            Assert.Equal(JoinSeeds(first.Games), JoinSeeds(second.Games));
            Assert.Equal(JoinResults(first.Games), JoinResults(second.Games));
        }

        [Fact]
        public void Constructor_InvalidOptions_Throw()
        {
            PlayerSimulationProfile profile = Profile("p");

            Assert.Throws<ArgumentException>(
                () => new BatchSimulationOptions("", 1, 1, "scenario", StartingFen, new[] { Matchup("a", "b") }));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BatchSimulationOptions("batch", 1, 0, "scenario", StartingFen, new[] { Matchup("a", "b") }));
            Assert.Throws<ArgumentException>(
                () => new BatchSimulationOptions("batch", 1, 1, "", StartingFen, new[] { Matchup("a", "b") }));
            Assert.Throws<ArgumentException>(
                () => new BatchSimulationOptions("batch", 1, 1, "scenario", "", new[] { Matchup("a", "b") }));
            Assert.Throws<ArgumentException>(
                () => new BatchSimulationOptions("batch", 1, 1, "scenario", StartingFen, Array.Empty<MatchupDefinition>()));
            Assert.Throws<ArgumentException>(
                () => new BatchSimulationOptions("batch", 1, 1, "scenario", StartingFen, NullMatchups()));
            Assert.Throws<ArgumentException>(
                () => new BatchSimulationOptions(
                    "batch",
                    1,
                    1,
                    "other-scenario",
                    StartingFen,
                    new[] { Matchup("a", "b") },
                    scenario: new BalanceSimulationScenario(
                        "scenario",
                        schemaVersion: 1,
                        StartingFen,
                        PieceColor.White,
                        cards: null,
                        tileEffects: null,
                        engineObservation: null,
                        "strong")));
            Assert.NotNull(profile);
        }

        private static BatchSimulationRunner Runner(StubChessEngine engine)
        {
            var simulator = new GameSimulator(
                engine,
                new GameStateEvaluator(engine),
                new CardDecisionModule(new ConfiguredCardScorer()),
                new MoveFilter(engine));
            return new BatchSimulationRunner(new HeadlessGameRunner(simulator));
        }

        private static BatchSimulationRunner RunnerWithTargeting(StubChessEngine engine)
        {
            return new BatchSimulationRunner(new HeadlessGameRunner(
                BalanceSimulatorFactory.CreateBaselineSimulator(engine)));
        }

        private static BatchSimulationOptions Options(
            int gameCount,
            params MatchupDefinition[] matchups)
        {
            return new BatchSimulationOptions(
                "batch-1",
                baseSeed: 12345,
                gameCount,
                "opening",
                StartingFen,
                matchups,
                new HeadlessGameOptions(maxPly: 1, variationCount: 1));
        }

        private static MatchupDefinition Matchup(string whiteProfileId, string blackProfileId, bool colorSwap = false)
        {
            return new MatchupDefinition(
                whiteProfileId + "-vs-" + blackProfileId,
                Profile(whiteProfileId),
                Profile(blackProfileId),
                colorSwap);
        }

        private static PlayerSimulationProfile Profile(string profileId)
        {
            return new PlayerSimulationProfile(profileId, "default", maxCardsPerTurn: 1, useRandomTieBreak: false);
        }

        private static IEnumerable<MatchupDefinition> NullMatchups()
        {
            yield return null!;
        }

        private static MoveCandidate Move(string uciMove, int scoreCentipawns)
        {
            return new MoveCandidate(uciMove, scoreCentipawns, mateIn: null);
        }

        private static int[] SelectGameIndexes(IReadOnlyList<BatchGameResult> games)
        {
            var values = new int[games.Count];

            for (int i = 0; i < games.Count; i++)
            {
                values[i] = games[i].GameIndex;
            }

            return values;
        }

        private static int[] SelectMatchupOrdinals(IReadOnlyList<BatchGameResult> games)
        {
            var values = new int[games.Count];

            for (int i = 0; i < games.Count; i++)
            {
                values[i] = games[i].MatchupOrdinal;
            }

            return values;
        }

        private static string JoinGameIds(IReadOnlyList<BatchGameResult> games)
        {
            var values = new List<string>();

            foreach (BatchGameResult game in games)
            {
                values.Add(game.GameId);
            }

            return string.Join("|", values);
        }

        private static string JoinSeeds(IReadOnlyList<BatchGameResult> games)
        {
            var values = new List<string>();

            foreach (BatchGameResult game in games)
            {
                values.Add(game.GameSeed.ToString());
            }

            return string.Join("|", values);
        }

        private static string JoinResults(IReadOnlyList<BatchGameResult> games)
        {
            var values = new List<string>();

            foreach (BatchGameResult game in games)
            {
                values.Add(game.GameResult.Result.ToString());
            }

            return string.Join("|", values);
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
