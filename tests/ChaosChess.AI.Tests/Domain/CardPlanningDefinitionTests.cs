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
    [InlineData("at_mine", CardTargetKind.BoardSquare, 1)]
    [InlineData("blessing", CardTargetKind.BoardSquare, 1)]
    [InlineData("caterpillar", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("chaotic_knight", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("charge", CardTargetKind.None, 0)]
    [InlineData("checkmate_declaration", CardTargetKind.None, 0)]
    [InlineData("cobweb", CardTargetKind.BoardSquare, 1)]
    [InlineData("concentration", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("dark_hand", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("democracy", CardTargetKind.None, 0)]
    [InlineData("desperado", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("destroyer_tank_cards", CardTargetKind.None, 0)]
    [InlineData("dimension_instability", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("father_enemy", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("fast_march", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("fire", CardTargetKind.BoardSquare, 1)]
    [InlineData("giant", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("gods_move", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("jumping_platform", CardTargetKind.BoardSquare, 1)]
    [InlineData("limitless", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("missing_promotion", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("mutiny", CardTargetKind.None, 0)]
    [InlineData("obey_order", CardTargetKind.BoardSquare, 1)]
    [InlineData("overbearing", CardTargetKind.None, 0)]
    [InlineData("peace_zone", CardTargetKind.BoardSquare, 1)]
    [InlineData("portal", CardTargetKind.OrderedSquares, 2)]
    [InlineData("psilocybin_mushroom", CardTargetKind.BoardSquare, 1)]
    [InlineData("sneak_pawn", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("stag_fight", CardTargetKind.None, 0)]
    [InlineData("sunset_blade", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("time_bomb", CardTargetKind.BoardSquare, 1)]
    [InlineData("thunderclap_flash", CardTargetKind.PieceAtSquare, 1)]
    [InlineData("windmill", CardTargetKind.None, 0)]
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
    [InlineData("at_mine", CardTargetOwnerRelation.Any)]
    [InlineData("blessing", CardTargetOwnerRelation.Any)]
    [InlineData("caterpillar", CardTargetOwnerRelation.Self)]
    [InlineData("chaotic_knight", CardTargetOwnerRelation.Self)]
    [InlineData("charge", CardTargetOwnerRelation.Any)]
    [InlineData("checkmate_declaration", CardTargetOwnerRelation.Any)]
    [InlineData("cobweb", CardTargetOwnerRelation.Any)]
    [InlineData("concentration", CardTargetOwnerRelation.Self)]
    [InlineData("dark_hand", CardTargetOwnerRelation.Opponent)]
    [InlineData("democracy", CardTargetOwnerRelation.Any)]
    [InlineData("desperado", CardTargetOwnerRelation.Self)]
    [InlineData("destroyer_tank_cards", CardTargetOwnerRelation.Any)]
    [InlineData("dimension_instability", CardTargetOwnerRelation.Self)]
    [InlineData("father_enemy", CardTargetOwnerRelation.Self)]
    [InlineData("fast_march", CardTargetOwnerRelation.Self)]
    [InlineData("fire", CardTargetOwnerRelation.Any)]
    [InlineData("giant", CardTargetOwnerRelation.Self)]
    [InlineData("gods_move", CardTargetOwnerRelation.Self)]
    [InlineData("jumping_platform", CardTargetOwnerRelation.Any)]
    [InlineData("limitless", CardTargetOwnerRelation.Self)]
    [InlineData("missing_promotion", CardTargetOwnerRelation.Opponent)]
    [InlineData("mutiny", CardTargetOwnerRelation.Any)]
    [InlineData("obey_order", CardTargetOwnerRelation.Any)]
    [InlineData("overbearing", CardTargetOwnerRelation.Any)]
    [InlineData("peace_zone", CardTargetOwnerRelation.Any)]
    [InlineData("portal", CardTargetOwnerRelation.Any)]
    [InlineData("psilocybin_mushroom", CardTargetOwnerRelation.Any)]
    [InlineData("sneak_pawn", CardTargetOwnerRelation.Self)]
    [InlineData("stag_fight", CardTargetOwnerRelation.Any)]
    [InlineData("sunset_blade", CardTargetOwnerRelation.Self)]
    [InlineData("time_bomb", CardTargetOwnerRelation.Any)]
    [InlineData("thunderclap_flash", CardTargetOwnerRelation.Self)]
    [InlineData("windmill", CardTargetOwnerRelation.Any)]
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
        yield return new object[] { "chaotic_knight", new[] { PieceKind.Knight } };
        yield return new object[] { "concentration", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen } };
        yield return new object[] { "dark_hand", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook } };
        yield return new object[] { "desperado", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen, PieceKind.Amazon, PieceKind.Chancellor, PieceKind.KnightRider } };
        yield return new object[] { "dimension_instability", new[] { PieceKind.Knight } };
        yield return new object[] { "father_enemy", new[] { PieceKind.Pawn } };
        yield return new object[] { "fast_march", new[] { PieceKind.Pawn } };
        yield return new object[] { "giant", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop } };
        yield return new object[] { "gods_move", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook } };
        yield return new object[] { "limitless", new[] { PieceKind.Pawn, PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Queen, PieceKind.King, PieceKind.Amazon, PieceKind.Chancellor, PieceKind.KnightRider } };
        yield return new object[] { "missing_promotion", new[] { PieceKind.Knight, PieceKind.Bishop, PieceKind.Rook, PieceKind.Amazon, PieceKind.Chancellor, PieceKind.KnightRider } };
        yield return new object[] { "sneak_pawn", new[] { PieceKind.Pawn } };
        yield return new object[] { "sunset_blade", new[] { PieceKind.Pawn } };
        yield return new object[] { "thunderclap_flash", new[] { PieceKind.Rook } };
    }
}
