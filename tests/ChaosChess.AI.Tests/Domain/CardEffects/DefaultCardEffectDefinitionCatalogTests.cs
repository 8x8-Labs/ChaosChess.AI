using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Domain.CardEffects;

public sealed class DefaultCardEffectDefinitionCatalogTests
{
    [Theory]
    [InlineData("agile", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("aim", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("caterpillar", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("charge", CardTargetKind.None, CardTargetOwnerRelation.Any, 0)]
    [InlineData("concentration", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("dark_hand", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Opponent, 1)]
    [InlineData("fast_march", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("fire", CardTargetKind.BoardSquare, CardTargetOwnerRelation.Any, 1)]
    [InlineData("limitless", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("missing_promotion", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Opponent, 1)]
    [InlineData("peace_zone", CardTargetKind.BoardSquare, CardTargetOwnerRelation.Any, 1)]
    [InlineData("portal", CardTargetKind.OrderedSquares, CardTargetOwnerRelation.Any, 2)]
    [InlineData("sneak_pawn", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    [InlineData("thunderclap_flash", CardTargetKind.PieceAtSquare, CardTargetOwnerRelation.Self, 1)]
    public void DefaultCatalog_ReturnsSupportedDefinitions(
        string cardId,
        CardTargetKind expectedKind,
        CardTargetOwnerRelation expectedRelation,
        int expectedCount)
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectDefinition? definition = catalog.FindDefinition(cardId);

        Assert.NotNull(definition);
        Assert.Equal(cardId, definition!.CardId);
        Assert.Equal(expectedKind, definition.TargetQuery.Kind);
        Assert.Equal(expectedRelation, definition.TargetQuery.OwnerRelation);
        Assert.Equal(expectedCount, definition.TargetQuery.Count);
        Assert.NotEmpty(definition.Primitives);
    }

    [Theory]
    [InlineData("agile", "u", 1)]
    [InlineData("aim", "t", 1)]
    [InlineData("caterpillar", "z", 2)]
    [InlineData("concentration", "a", 5)]
    [InlineData("dark_hand", "a", 2)]
    [InlineData("fast_march", "f", 1)]
    [InlineData("limitless", "a", null)]
    [InlineData("sneak_pawn", "e", 1)]
    [InlineData("thunderclap_flash", "m", 1)]
    public void MovementOverrideDefinitions_UseSelectedPieceMovementOverride(
        string cardId,
        string expectedOverrideCode,
        int? expectedDurationTurns)
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectDefinition definition = catalog.FindDefinition(cardId)!;
        CardEffectPrimitive primitive = Assert.Single(definition.Primitives);

        Assert.Equal(CardEffectPrimitiveKind.SetMovementOverride, primitive.Kind);
        Assert.Equal(CardEffectPrimitiveTargetBinding.SelectedPiece, primitive.TargetBinding);
        Assert.Equal(expectedOverrideCode, primitive.MovementOverrideCode);
        Assert.Equal(expectedDurationTurns, primitive.DurationTurns);
    }

    [Fact]
    public void TileDefinitions_UseSelectedSquareTileEffects()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectPrimitive fire = Assert.Single(catalog.FindDefinition("fire")!.Primitives);
        CardEffectPrimitive peace = Assert.Single(catalog.FindDefinition("peace_zone")!.Primitives);

        Assert.Equal("Fire", fire.EffectType);
        Assert.Equal(CardEffectPrimitiveTargetBinding.SelectedSquare, fire.TargetBinding);
        Assert.Equal(-1, fire.DurationTurns);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, fire.TileEffectLifetimeKind);
        Assert.Equal("Peace", peace.EffectType);
        Assert.Equal(CardEffectPrimitiveTargetBinding.SelectedSquare, peace.TargetBinding);
        Assert.Equal(-1, peace.DurationTurns);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, peace.TileEffectLifetimeKind);
    }

    [Fact]
    public void MissingPromotionDefinition_UsesSelectedOpponentPieceChangeKind()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectDefinition definition = catalog.FindDefinition("missing_promotion")!;
        CardEffectPrimitive primitive = Assert.Single(definition.Primitives);

        Assert.Equal(CardEffectPrimitiveKind.ChangePieceKind, primitive.Kind);
        Assert.Equal(CardEffectPrimitiveTargetBinding.SelectedPiece, primitive.TargetBinding);
        Assert.Equal(PieceKind.Pawn, primitive.PieceKind);
    }

    [Fact]
    public void PortalDefinition_PreservesOrderedEndpointBindingsAndSharedUses()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectDefinition definition = catalog.FindDefinition("portal")!;

        Assert.True(definition.TargetQuery.IsOrdered);
        Assert.Equal(2, definition.Primitives.Count);
        Assert.All(definition.Primitives, primitive =>
        {
            Assert.Equal(CardEffectPrimitiveKind.AddTileEffect, primitive.Kind);
            Assert.Equal("Portal", primitive.EffectType);
            Assert.Equal(-1, primitive.DurationTurns);
            Assert.Equal(2, primitive.SharedRemainingUses);
            Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, primitive.TileEffectLifetimeKind);
            Assert.Equal(CardEffectPrimitiveTargetBinding.OrderedSquareByIndex, primitive.TargetBinding);
        });
        Assert.Equal(0, definition.Primitives[0].TargetIndex);
        Assert.Equal(1, definition.Primitives[0].DestinationTargetIndex);
        Assert.Equal(1, definition.Primitives[1].TargetIndex);
        Assert.Equal(0, definition.Primitives[1].DestinationTargetIndex);
    }

    [Fact]
    public void Lookup_IsCaseInsensitiveAndUnknownReturnsNullOrFalse()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();

        Assert.NotNull(catalog.FindDefinition("FIRE"));
        Assert.Null(catalog.FindDefinition("unknown_card"));
        Assert.False(catalog.TryGetDefinition("unknown_card", out CardEffectDefinition definition));
        Assert.Null(definition);
    }

    [Fact]
    public void Catalog_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DefaultCardEffectDefinitionCatalog(null!));
        Assert.Throws<ArgumentException>(
            () => new DefaultCardEffectDefinitionCatalog(new CardEffectDefinition[] { null! }));
        Assert.Throws<ArgumentException>(
            () => new DefaultCardEffectDefinitionCatalog(
                new[]
                {
                    CreateUnsupportedLikeDefinition("duplicate"),
                    CreateUnsupportedLikeDefinition("DUPLICATE")
                }));

        var catalog = new DefaultCardEffectDefinitionCatalog();
        Assert.Throws<ArgumentException>(() => catalog.FindDefinition(""));
        Assert.Throws<ArgumentException>(() => catalog.TryGetDefinition("", out _));
    }

    [Fact]
    public void DefinitionsCollection_IsDefensiveReadOnlyCopy()
    {
        var definitions = new List<CardEffectDefinition>
        {
            CreateUnsupportedLikeDefinition("custom")
        };

        var catalog = new DefaultCardEffectDefinitionCatalog(definitions);
        definitions.Clear();

        Assert.Single(catalog.Definitions);
        Assert.NotNull(catalog.FindDefinition("custom"));
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, CardEffectDefinition>>(catalog.Definitions);
    }

    private static CardEffectDefinition CreateUnsupportedLikeDefinition(string cardId)
    {
        return new CardEffectDefinition(
            cardId,
            CardTargetQuery.None(),
            new[] { new CardEffectPrimitive(CardEffectPrimitiveKind.RemoveTileEffect) });
    }
}
