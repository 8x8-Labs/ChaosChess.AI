using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Domain;

public sealed class CardPlanningDefinitionTests
{
    [Theory]
    [InlineData("agile", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("aim", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("caterpillar", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("charge", CardTargetKind.None, 0)]
    [InlineData("concentration", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("fast_march", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("fire", CardTargetKind.BoardSquare, 1)]
    [InlineData("limitless", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("peace_zone", CardTargetKind.BoardSquare, 1)]
    [InlineData("portal", CardTargetKind.OrderedSquares, 2)]
    [InlineData("sneak_pawn", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("thunderclap_flash", CardTargetKind.PieceAtSquare, 1)]
    public void DefaultCatalog_ReturnsSupportedDefinitions(
        string cardId,
        CardTargetKind expectedKind,
        int expectedCount)
    {
        var catalog = new DefaultCardPlanningCatalog();

        CardPlanningDefinition definition = catalog.GetDefinition(cardId);

        Assert.True(definition.IsSupported);
        Assert.Equal(cardId, definition.CardId);
        Assert.Equal(expectedKind, definition.RequiredTargetKind);
        Assert.Equal(expectedCount, definition.RequiredTargetCount);
    }

    [Fact]
    public void DefaultCatalog_UnknownCardReturnsUnsupportedDefinition()
    {
        var catalog = new DefaultCardPlanningCatalog();

        CardPlanningDefinition definition = catalog.GetDefinition("unknown_card");

        Assert.False(definition.IsSupported);
        Assert.Equal("unknown_card", definition.CardId);
        Assert.Equal(CardTargetKind.None, definition.RequiredTargetKind);
        Assert.Equal(0, definition.RequiredTargetCount);
    }

    [Fact]
    public void DefaultCatalog_LookupIsCaseInsensitive()
    {
        var catalog = new DefaultCardPlanningCatalog();

        CardPlanningDefinition definition = catalog.GetDefinition("FIRE");

        Assert.True(definition.IsSupported);
        Assert.Equal("fire", definition.CardId);
        Assert.Equal(CardTargetKind.BoardSquare, definition.RequiredTargetKind);
    }

    [Fact]
    public void DefaultCatalog_RejectsInvalidInput()
    {
        var catalog = new DefaultCardPlanningCatalog();

        Assert.Throws<ArgumentException>(
            () => catalog.GetDefinition(""));
        Assert.Throws<ArgumentException>(
            () => new DefaultCardPlanningCatalog(new CardPlanningDefinition[] { null! }));
        Assert.Throws<ArgumentException>(
            () => new DefaultCardPlanningCatalog(
                new[]
                {
                    CardPlanningDefinition.Unsupported("duplicate"),
                    CardPlanningDefinition.Unsupported("DUPLICATE")
                }));
    }

    [Fact]
    public void Definition_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(
            () => new CardPlanningDefinition("", true, CardTargetRequirement.None()));
        Assert.Throws<ArgumentNullException>(
            () => new CardPlanningDefinition("fire", true, null!));
    }

    [Fact]
    public void TargetRequirement_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetRequirement((CardTargetKind)99, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetRequirement(CardTargetKind.BoardSquare, -1));
        Assert.Throws<ArgumentException>(
            () => new CardTargetRequirement(CardTargetKind.None, 1));
        Assert.Throws<ArgumentException>(
            () => new CardTargetRequirement(CardTargetKind.BoardSquare, 0));
    }

    [Fact]
    public void DefinitionsCollection_IsDefensiveReadOnlyCopy()
    {
        var definitions = new List<CardPlanningDefinition>
        {
            CardPlanningDefinition.Unsupported("custom")
        };

        var catalog = new DefaultCardPlanningCatalog(definitions);
        definitions.Clear();

        Assert.Single(catalog.Definitions);
        Assert.False(catalog.GetDefinition("custom").IsSupported);
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, CardPlanningDefinition>>(catalog.Definitions);
    }
}
