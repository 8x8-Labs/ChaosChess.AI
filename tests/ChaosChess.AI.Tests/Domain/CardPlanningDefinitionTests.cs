using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
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
    [InlineData("dark_hand", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("fast_march", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("fire", CardTargetKind.BoardSquare, 1)]
    [InlineData("limitless", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("missing_promotion", CardTargetKind.PieceAtSquare, 1)]
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

    [Theory]
    [InlineData("agile", CardTargetOwnerRelation.Self)]
    [InlineData("aim", CardTargetOwnerRelation.Self)]
    [InlineData("caterpillar", CardTargetOwnerRelation.Self)]
    [InlineData("charge", CardTargetOwnerRelation.Any)]
    [InlineData("concentration", CardTargetOwnerRelation.Self)]
    [InlineData("dark_hand", CardTargetOwnerRelation.Opponent)]
    [InlineData("fast_march", CardTargetOwnerRelation.Self)]
    [InlineData("fire", CardTargetOwnerRelation.Any)]
    [InlineData("limitless", CardTargetOwnerRelation.Self)]
    [InlineData("missing_promotion", CardTargetOwnerRelation.Opponent)]
    [InlineData("peace_zone", CardTargetOwnerRelation.Any)]
    [InlineData("portal", CardTargetOwnerRelation.Any)]
    [InlineData("sneak_pawn", CardTargetOwnerRelation.Self)]
    [InlineData("thunderclap_flash", CardTargetOwnerRelation.Self)]
    public void DefaultCatalog_ReturnsTargetOwnerRelations(
        string cardId,
        CardTargetOwnerRelation expectedRelation)
    {
        var catalog = new DefaultCardPlanningCatalog();

        CardPlanningDefinition definition = catalog.GetDefinition(cardId);

        Assert.Equal(expectedRelation, definition.RequiredTargetOwnerRelation);
    }

    [Theory]
    [MemberData(nameof(DefaultPieceKindRequirements))]
    public void DefaultCatalog_ReturnsAllowedPieceKinds(
        string cardId,
        PieceKind[] expectedKinds)
    {
        var catalog = new DefaultCardPlanningCatalog();

        CardPlanningDefinition definition = catalog.GetDefinition(cardId);

        Assert.Equal(expectedKinds, definition.AllowedTargetPieceKinds);
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
        Assert.Equal(CardTargetOwnerRelation.Any, definition.RequiredTargetOwnerRelation);
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
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetRequirement(CardTargetKind.PieceAtSquare, 1, (CardTargetOwnerRelation)99, Array.Empty<PieceKind>()));
        Assert.Throws<ArgumentNullException>(
            () => new CardTargetRequirement(CardTargetKind.PieceAtSquare, 1, CardTargetOwnerRelation.Self, null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CardTargetRequirement.Piece(CardTargetOwnerRelation.Self, PieceKind.Unknown));
        Assert.Throws<ArgumentException>(
            () => new CardTargetRequirement(CardTargetKind.BoardSquare, 1, CardTargetOwnerRelation.Self, Array.Empty<PieceKind>()));
        Assert.Throws<ArgumentException>(
            () => new CardTargetRequirement(CardTargetKind.BoardSquare, 1, CardTargetOwnerRelation.Any, new[] { PieceKind.Pawn }));
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

    public static IEnumerable<object[]> DefaultPieceKindRequirements()
    {
        yield return new object[] { "agile", new[] { PieceKind.Pawn } };
        yield return new object[] { "aim", new[] { PieceKind.Pawn } };
        yield return new object[] { "caterpillar", new[] { PieceKind.Knight } };
        yield return new object[] { "concentration", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen } };
        yield return new object[] { "dark_hand", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook } };
        yield return new object[] { "fast_march", new[] { PieceKind.Pawn } };
        yield return new object[] { "limitless", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen, PieceKind.King, PieceKind.Amazon, PieceKind.Chancellor, PieceKind.KnightRider } };
        yield return new object[] { "missing_promotion", new[] { PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Amazon, PieceKind.Chancellor, PieceKind.KnightRider } };
        yield return new object[] { "sneak_pawn", new[] { PieceKind.Pawn } };
        yield return new object[] { "thunderclap_flash", new[] { PieceKind.Rook } };
    }
}
