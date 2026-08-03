using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Simulator.Balance;
using Xunit;

namespace ChaosChess.AI.Tests.Simulator.Balance;

public sealed class BalanceScenarioGameStateFactoryTests
{
    private const string StartingFen = "4k3/8/8/8/8/8/4P3/4K3 w - - 0 1";

    [Fact]
    public void Create_MapsScenarioCardsAndTileEffectsIntoGameState()
    {
        var scenario = new BalanceSimulationScenario(
            "fire-strong",
            schemaVersion: 1,
            StartingFen,
            PieceColor.White,
            new[]
            {
                new BalanceScenarioCard("fire", "BoardControl", remainingUses: 1),
                new BalanceScenarioCard("charge", "Mobility", remainingUses: 0)
            },
            new[]
            {
                new BalanceScenarioTileEffect(
                    "portal-a",
                    "Portal",
                    new Square(0, 0),
                    PieceColor.White,
                    remainingTurns: 3,
                    destinationSquare: new Square(7, 7),
                    sharedRemainingUses: 2)
            },
            engineObservation: null,
            "strong",
            BalanceExpectedBehavior.ShouldUse);

        GameState state = BalanceScenarioGameStateFactory.Create(scenario);

        Assert.Equal(PieceColor.White, state.BoardState.SideToMove);
        Assert.NotNull(state.BoardState.FindPiece(new Square(4, 1)));
        Assert.Equal(2, state.AvailableCards.Count);
        Assert.Equal("fire", state.AvailableCards[0].Id);
        Assert.Equal("BoardControl", state.AvailableCards[0].Category);
        Assert.Equal(1, state.AvailableCards[0].RemainingUses);
        Assert.Equal("charge", state.AvailableCards[1].Id);
        Assert.Equal(0, state.AvailableCards[1].RemainingUses);

        TileEffectInfo tileEffect = Assert.Single(state.TileEffects);
        Assert.Equal("portal-a", tileEffect.Id);
        Assert.Equal("Portal", tileEffect.EffectType);
        Assert.Equal(new Square(0, 0), tileEffect.Square);
        Assert.Equal(PieceColor.White, tileEffect.Owner);
        Assert.Equal(3, tileEffect.RemainingTurns);
        Assert.Equal(new Square(7, 7), tileEffect.DestinationSquare);
        Assert.Equal(2, tileEffect.SharedRemainingUses);
    }

    [Fact]
    public void Create_RejectsNullScenario()
    {
        Assert.Throws<ArgumentNullException>(
            () => BalanceScenarioGameStateFactory.Create(null!));
    }
}
