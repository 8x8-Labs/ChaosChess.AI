using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance
{
    public sealed class BalanceSimulationScenarioJsonLoaderTests
    {
        [Fact]
        public void Load_ValidJson_ReturnsScenario()
        {
            BalanceSimulationScenario scenario = BalanceSimulationScenarioJsonLoader.Load(@"
{
  ""scenarioId"": ""charge-strong"",
  ""schemaVersion"": 1,
  ""startingFen"": ""4k3/8/8/8/8/8/4P3/4K3 w - - 0 1"",
  ""actor"": ""White"",
  ""cards"": [
    { ""cardId"": ""charge"", ""category"": ""Mobility"", ""remainingUses"": 1 }
  ],
  ""tileEffects"": [
    {
      ""id"": ""portal-a"",
      ""effectType"": ""Portal"",
      ""square"": ""e4"",
      ""owner"": ""White"",
      ""remainingTurns"": 2,
      ""destinationSquare"": ""h4"",
      ""sharedRemainingUses"": 1
    }
  ],
  ""engineObservation"": {
    ""moves"": [
      { ""uciMove"": ""e2e4"", ""scoreCentipawns"": 10, ""mateIn"": null }
    ]
  },
  ""scenarioGroup"": ""strong"",
  ""expectedBehavior"": ""ShouldUse""
}");

            Assert.Equal("charge-strong", scenario.ScenarioId);
            Assert.Equal(PieceColor.White, scenario.Actor);
            Assert.Equal(BalanceExpectedBehavior.ShouldUse, scenario.ExpectedBehavior);
            Assert.Equal("charge", Assert.Single(scenario.Cards).CardId);
            BalanceScenarioTileEffect tileEffect = Assert.Single(scenario.TileEffects);
            Assert.Equal(Square.Parse("e4"), tileEffect.Square);
            Assert.Equal(Square.Parse("h4"), tileEffect.DestinationSquare);
            Assert.Equal("e2e4", Assert.Single(scenario.EngineObservation.Moves).UciMove);
        }

        [Fact]
        public void Load_MinimalJson_UsesEmptyCollectionsAndDefaultExpectedBehavior()
        {
            BalanceSimulationScenario scenario = BalanceSimulationScenarioJsonLoader.Load(@"
{
  ""scenarioId"": ""empty"",
  ""schemaVersion"": 1,
  ""startingFen"": ""4k3/8/8/8/8/8/4P3/4K3 w - - 0 1"",
  ""actor"": ""white"",
  ""scenarioGroup"": ""smoke""
}");

            Assert.Empty(scenario.Cards);
            Assert.Empty(scenario.TileEffects);
            Assert.Empty(scenario.EngineObservation.Moves);
            Assert.Equal(BalanceExpectedBehavior.Unspecified, scenario.ExpectedBehavior);
        }

        [Fact]
        public void Load_MissingRequiredProperty_Throws()
        {
            Assert.Throws<FormatException>(
                () => BalanceSimulationScenarioJsonLoader.Load("{}"));
        }
    }
}
