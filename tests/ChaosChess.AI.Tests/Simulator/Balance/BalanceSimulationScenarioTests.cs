using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance;

public sealed class BalanceSimulationScenarioTests
{
    private const string StartingFen = "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1";

    [Fact]
    public void Scenario_PreservesInputFields()
    {
        var card = new BalanceScenarioCard("fire", "BoardControl", remainingUses: 1);
        var tileEffect = new BalanceScenarioTileEffect(
            "peace-1",
            "Peace",
            new Square(3, 3),
            PieceColor.White,
            remainingTurns: 2);
        var observation = new BalanceEngineObservation(new[] { Move("e2e4") });

        var scenario = new BalanceSimulationScenario(
            "fire-strong",
            schemaVersion: 1,
            StartingFen,
            PieceColor.White,
            new[] { card },
            new[] { tileEffect },
            observation,
            "strong",
            BalanceExpectedBehavior.ShouldUse);

        Assert.Equal("fire-strong", scenario.ScenarioId);
        Assert.Equal(1, scenario.SchemaVersion);
        Assert.Equal(StartingFen, scenario.StartingFen);
        Assert.Equal(PieceColor.White, scenario.Actor);
        Assert.Same(card, Assert.Single(scenario.Cards));
        Assert.Same(tileEffect, Assert.Single(scenario.TileEffects));
        Assert.Same(observation, scenario.EngineObservation);
        Assert.Equal("strong", scenario.ScenarioGroup);
        Assert.Equal(BalanceExpectedBehavior.ShouldUse, scenario.ExpectedBehavior);
    }

    [Fact]
    public void Scenario_DefaultsOptionalCollectionsAndBehavior()
    {
        var scenario = new BalanceSimulationScenario(
            "neutral",
            schemaVersion: 1,
            StartingFen,
            PieceColor.Black,
            cards: null,
            tileEffects: null,
            engineObservation: null,
            "neutral");

        Assert.Empty(scenario.Cards);
        Assert.Empty(scenario.TileEffects);
        Assert.Empty(scenario.EngineObservation.Moves);
        Assert.Equal(BalanceExpectedBehavior.Unspecified, scenario.ExpectedBehavior);
    }

    [Fact]
    public void Scenario_DefensivelyCopiesCollections()
    {
        var cards = new List<BalanceScenarioCard>
        {
            new BalanceScenarioCard("agile", "Mobility", remainingUses: 1)
        };
        var tileEffects = new List<BalanceScenarioTileEffect>
        {
            new BalanceScenarioTileEffect("portal-a", "Portal", new Square(0, 0), PieceColor.White, 3)
        };
        var moves = new List<MoveCandidate>
        {
            Move("e2e4")
        };

        var scenario = new BalanceSimulationScenario(
            "copy",
            schemaVersion: 1,
            StartingFen,
            PieceColor.White,
            cards,
            tileEffects,
            new BalanceEngineObservation(moves),
            "strong");
        cards.Add(new BalanceScenarioCard("fire", "BoardControl", remainingUses: 1));
        tileEffects.Add(new BalanceScenarioTileEffect("fire-1", "Fire", new Square(1, 1), PieceColor.Black, 1));
        moves.Add(Move("a2a4"));

        Assert.Single(scenario.Cards);
        Assert.Single(scenario.TileEffects);
        Assert.Single(scenario.EngineObservation.Moves);
        Assert.Throws<NotSupportedException>(
            () => ((IList<BalanceScenarioCard>)scenario.Cards).Add(new BalanceScenarioCard("portal", "Mobility", 1)));
    }

    [Fact]
    public void Scenario_RejectsInvalidValues()
    {
        Assert.Throws<ArgumentException>(
            () => Scenario(scenarioId: ""));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Scenario(schemaVersion: 0));
        Assert.Throws<ArgumentException>(
            () => Scenario(startingFen: ""));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Scenario(actor: (PieceColor)99));
        Assert.Throws<ArgumentException>(
            () => Scenario(scenarioGroup: ""));
        Assert.Throws<ArgumentException>(
            () => Scenario(cards: new BalanceScenarioCard?[] { null }!));
        Assert.Throws<ArgumentException>(
            () => Scenario(tileEffects: new BalanceScenarioTileEffect?[] { null }!));
    }

    [Fact]
    public void ScenarioCard_AllowsZeroRemainingUsesAndRejectsInvalidValues()
    {
        var exhausted = new BalanceScenarioCard("charge", "Mobility", remainingUses: 0);

        Assert.Equal(0, exhausted.RemainingUses);
        Assert.Throws<ArgumentException>(
            () => new BalanceScenarioCard("", "Mobility", 1));
        Assert.Throws<ArgumentException>(
            () => new BalanceScenarioCard("charge", "", 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BalanceScenarioCard("charge", "Mobility", -1));
    }

    [Fact]
    public void TileEffect_RejectsInvalidValues()
    {
        Assert.Throws<ArgumentException>(
            () => new BalanceScenarioTileEffect("", "Fire", new Square(0, 0), PieceColor.White, 1));
        Assert.Throws<ArgumentException>(
            () => new BalanceScenarioTileEffect("id", "", new Square(0, 0), PieceColor.White, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BalanceScenarioTileEffect("id", "Fire", new Square(0, 0), (PieceColor)99, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BalanceScenarioTileEffect("id", "Fire", new Square(0, 0), PieceColor.White, -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BalanceScenarioTileEffect("id", "Portal", new Square(0, 0), PieceColor.White, 1, sharedRemainingUses: -1));
    }

    [Fact]
    public void EngineObservation_RejectsNullMove()
    {
        Assert.Throws<ArgumentException>(
            () => new BalanceEngineObservation(new MoveCandidate?[] { null }!));
    }

    [Fact]
    public void ProfileSet_PreservesAndCopiesProfiles()
    {
        CardBalanceProfile baseline = CardBalanceProfileCatalog.CreateP10Baseline();
        CardBalanceProfile candidate = new CardBalanceProfile(
            "candidate",
            schemaVersion: 1,
            categoryScores: null,
            cardScores: null,
            minimumScoreGain: 2,
            maximumCardsPerTurn: 1,
            baseline.TargetingProfile);
        var candidates = new List<CardBalanceProfile> { candidate };

        var profileSet = new BalanceSimulationProfileSet(
            "balance-baseline-candidates",
            baseline,
            candidates);
        candidates.Clear();

        Assert.Equal("balance-baseline-candidates", profileSet.ProfileSetId);
        Assert.Same(baseline, profileSet.BaselineProfile);
        Assert.Same(candidate, Assert.Single(profileSet.CandidateProfiles));
        Assert.Throws<NotSupportedException>(
            () => ((IList<CardBalanceProfile>)profileSet.CandidateProfiles).Add(baseline));
    }

    [Fact]
    public void ProfileSet_RejectsInvalidValues()
    {
        CardBalanceProfile baseline = CardBalanceProfileCatalog.CreateP10Baseline();

        Assert.Throws<ArgumentException>(
            () => new BalanceSimulationProfileSet("", baseline));
        Assert.Throws<ArgumentNullException>(
            () => new BalanceSimulationProfileSet("profiles", null!));
        Assert.Throws<ArgumentException>(
            () => new BalanceSimulationProfileSet("profiles", baseline, new CardBalanceProfile?[] { null }!));
    }

    private static BalanceSimulationScenario Scenario(
        string scenarioId = "scenario",
        int schemaVersion = 1,
        string startingFen = StartingFen,
        PieceColor actor = PieceColor.White,
        IEnumerable<BalanceScenarioCard>? cards = null,
        IEnumerable<BalanceScenarioTileEffect>? tileEffects = null,
        string scenarioGroup = "strong")
    {
        return new BalanceSimulationScenario(
            scenarioId,
            schemaVersion,
            startingFen,
            actor,
            cards,
            tileEffects,
            engineObservation: null,
            scenarioGroup);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
