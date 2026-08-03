using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class CardPlanScoreTests
{
    [Fact]
    public void Constructor_ComponentSumMatchesTotal_CreatesScore()
    {
        var components = new[]
        {
            Component("mobility", 7),
            Component("risk", -2)
        };

        var score = new CardPlanScore(5, components);

        Assert.Equal(5, score.Total);
        Assert.Equal(components, score.Components);
    }

    [Fact]
    public void ScoreComponent_LegacyConstructor_UsesValueAsContribution()
    {
        var component = new CardPlanScoreComponent(
            "legacy.penalty",
            -2,
            "Legacy penalty.");

        Assert.Equal(-2, component.RawValue);
        Assert.Equal(1, component.Weight);
        Assert.Equal(-2, component.Contribution);
        Assert.Equal(component.Contribution, component.Value);
    }

    [Fact]
    public void ScoreComponent_WeightedConstructor_ComputesContribution()
    {
        var component = new CardPlanScoreComponent(
            "charge.movable_pawns",
            rawValue: 3,
            weight: 2,
            "Actor pawns can advance one square.");

        Assert.Equal(3, component.RawValue);
        Assert.Equal(2, component.Weight);
        Assert.Equal(6, component.Contribution);
        Assert.Equal(component.Contribution, component.Value);
    }

    [Fact]
    public void ScoreComponent_WeightedConstructor_AllowsNegativeWeight()
    {
        var component = new CardPlanScoreComponent(
            "fire.own_engine_destination_penalty",
            rawValue: 1,
            weight: -8,
            "Fire target overlaps the actor engine move destination.");

        Assert.Equal(1, component.RawValue);
        Assert.Equal(-8, component.Weight);
        Assert.Equal(-8, component.Contribution);
        Assert.Equal(component.Contribution, component.Value);
    }

    [Fact]
    public void ScoreComponent_WeightedConstructor_RejectsNegativeRawValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardPlanScoreComponent(
                "invalid.raw",
                rawValue: -1,
                weight: 1,
                "Invalid raw value."));
    }

    [Fact]
    public void Constructor_ComponentSumDiffersFromTotal_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new CardPlanScore(10, new[] { Component("mobility", 7) }));

        Assert.Equal("components", exception.ParamName);
    }

    [Fact]
    public void Constructor_CopiesComponentCollection()
    {
        var components = new List<CardPlanScoreComponent>
        {
            Component("first", 1)
        };
        var score = new CardPlanScore(1, components);

        components.Add(Component("second", 2));

        CardPlanScoreComponent component = Assert.Single(score.Components);
        Assert.Equal("first", component.Code);
    }

    [Fact]
    public void Constructor_NullComponent_Throws()
    {
        var components = new CardPlanScoreComponent?[] { null };

        Assert.Throws<ArgumentException>(
            () => new CardPlanScore(0, components!));
    }

    [Fact]
    public void ScoreComponent_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentException>(
            () => new CardPlanScoreComponent(string.Empty, 1, "description"));
        Assert.Throws<ArgumentException>(
            () => new CardPlanScoreComponent("code", 1, string.Empty));
        Assert.Throws<ArgumentException>(
            () => new CardPlanScoreComponent(string.Empty, 1, 1, "description"));
        Assert.Throws<ArgumentException>(
            () => new CardPlanScoreComponent("code", 1, 1, string.Empty));
    }

    private static CardPlanScoreComponent Component(string code, int value)
    {
        return new CardPlanScoreComponent(code, value, code + " description");
    }
}
