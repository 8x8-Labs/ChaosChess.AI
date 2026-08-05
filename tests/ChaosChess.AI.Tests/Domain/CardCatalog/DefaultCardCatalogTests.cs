using System;
using System.Collections.Generic;
using System.Linq;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardCatalog;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Domain.CardCatalog;

public sealed class DefaultCardCatalogTests
{
    private static readonly string[] CurrentUnitySupportedCardIds =
    {
        "agile",
        "charge",
        "fire",
        "peace_zone",
        "portal"
    };

    [Fact]
    public void DefaultCatalog_ContainsUnityCardInventory()
    {
        var catalog = new DefaultCardCatalog();

        Assert.Equal(50, catalog.Entries.Count);
        Assert.Equal(25, catalog.Entries.Values.Count(entry => entry.UnityType == UnityCardType.Piece));
        Assert.Equal(14, catalog.Entries.Values.Count(entry => entry.UnityType == UnityCardType.Tile));
        Assert.Equal(11, catalog.Entries.Values.Count(entry => entry.UnityType == UnityCardType.Global));
    }

    [Fact]
    public void DefaultCatalog_CardIdsAreStableAndCaseInsensitiveUnique()
    {
        var catalog = new DefaultCardCatalog();

        Assert.All(catalog.Entries.Values, entry =>
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.CardId));
            Assert.Equal(entry.CardId.ToLowerInvariant(), entry.CardId);
            Assert.DoesNotContain("-", entry.CardId, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(entry.UnityAssetName));
            Assert.EndsWith(".asset", entry.UnityAssetName, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(entry.DisplayName));
        });

        Assert.Equal(
            catalog.Entries.Count,
            catalog.Entries.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void DefaultCatalog_CurrentUnitySupportedCardsRemainWaveZeroOnly()
    {
        var catalog = new DefaultCardCatalog();

        string[] supported = catalog.Entries.Values
            .Where(entry => entry.CurrentUnityAiSupported)
            .Select(entry => entry.CardId)
            .OrderBy(cardId => cardId, StringComparer.Ordinal)
            .ToArray();
        string[] waveZero = catalog.Entries.Values
            .Where(entry => entry.ActivationWave == CardCatalogActivationWave.Wave0)
            .Select(entry => entry.CardId)
            .OrderBy(cardId => cardId, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(CurrentUnitySupportedCardIds.OrderBy(cardId => cardId, StringComparer.Ordinal), supported);
        Assert.Equal(supported, waveZero);
    }

    [Fact]
    public void DefaultCatalog_IncludesExistingPlanningAndEffectDefinitions()
    {
        var catalog = new DefaultCardCatalog();
        var planningCatalog = new DefaultCardPlanningCatalog();
        var effectCatalog = new DefaultCardEffectDefinitionCatalog();

        foreach (string cardId in CurrentUnitySupportedCardIds)
        {
            CardCatalogEntry entry = catalog.FindEntry(cardId)!;

            Assert.NotNull(entry);
            Assert.True(entry.CurrentUnityAiSupported);
            Assert.True(planningCatalog.GetDefinition(cardId).IsSupported);
            Assert.NotNull(effectCatalog.FindDefinition(cardId));
        }
    }

    [Fact]
    public void Lookup_IsCaseInsensitiveAndUnknownReturnsNullOrFalse()
    {
        var catalog = new DefaultCardCatalog();

        Assert.Equal("fire", catalog.FindEntry("FIRE")!.CardId);
        Assert.Null(catalog.FindEntry("unknown_card"));
        Assert.False(catalog.TryGetEntry("unknown_card", out CardCatalogEntry entry));
        Assert.Null(entry);
    }

    [Fact]
    public void Catalog_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DefaultCardCatalog(null!));
        Assert.Throws<ArgumentException>(
            () => new DefaultCardCatalog(new CardCatalogEntry[] { null! }));
        Assert.Throws<ArgumentException>(
            () => new DefaultCardCatalog(
                new[]
                {
                    CreateEntry("duplicate"),
                    CreateEntry("DUPLICATE")
                }));

        var catalog = new DefaultCardCatalog();
        Assert.Throws<ArgumentException>(() => catalog.FindEntry(""));
        Assert.Throws<ArgumentException>(() => catalog.TryGetEntry("", out _));
    }

    [Fact]
    public void Entry_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(
            () => new CardCatalogEntry(
                string.Empty,
                "FireCard.asset",
                "FireCard",
                UnityCardType.Tile,
                true,
                CardCatalogSupportGrade.NeedsCommonPrimitive,
                CardCatalogActivationWave.Wave0,
                "tile-duration",
                "test"));
        Assert.Throws<ArgumentException>(
            () => new CardCatalogEntry(
                "fire",
                string.Empty,
                "FireCard",
                UnityCardType.Tile,
                true,
                CardCatalogSupportGrade.NeedsCommonPrimitive,
                CardCatalogActivationWave.Wave0,
                "tile-duration",
                "test"));
        Assert.Throws<ArgumentException>(
            () => new CardCatalogEntry(
                "fire",
                "FireCard.asset",
                string.Empty,
                UnityCardType.Tile,
                true,
                CardCatalogSupportGrade.NeedsCommonPrimitive,
                CardCatalogActivationWave.Wave0,
                "tile-duration",
                "test"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardCatalogEntry(
                "fire",
                "FireCard.asset",
                "FireCard",
                (UnityCardType)99,
                true,
                CardCatalogSupportGrade.NeedsCommonPrimitive,
                CardCatalogActivationWave.Wave0,
                "tile-duration",
                "test"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardCatalogEntry(
                "fire",
                "FireCard.asset",
                "FireCard",
                UnityCardType.Tile,
                true,
                (CardCatalogSupportGrade)99,
                CardCatalogActivationWave.Wave0,
                "tile-duration",
                "test"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardCatalogEntry(
                "fire",
                "FireCard.asset",
                "FireCard",
                UnityCardType.Tile,
                true,
                CardCatalogSupportGrade.NeedsCommonPrimitive,
                (CardCatalogActivationWave)99,
                "tile-duration",
                "test"));
    }

    [Fact]
    public void DefinitionsCollection_IsDefensiveReadOnlyCopy()
    {
        var entries = new List<CardCatalogEntry>
        {
            CreateEntry("custom")
        };

        var catalog = new DefaultCardCatalog(entries);
        entries.Clear();

        Assert.Single(catalog.Entries);
        Assert.NotNull(catalog.FindEntry("custom"));
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, CardCatalogEntry>>(catalog.Entries);
    }

    private static CardCatalogEntry CreateEntry(string cardId)
    {
        return new CardCatalogEntry(
            cardId,
            "CustomCard.asset",
            "CustomCard",
            UnityCardType.Piece,
            currentUnityAiSupported: false,
            CardCatalogSupportGrade.DeferredUnsafe,
            CardCatalogActivationWave.Deferred,
            "test-gap",
            "test");
    }
}
