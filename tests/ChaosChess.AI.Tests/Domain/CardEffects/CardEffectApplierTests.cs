using System;
using System.Collections.Generic;
using System.Linq;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Domain.CardEffects;

public sealed class CardEffectApplierTests
{
    [Fact]
    public void Apply_AddsDurationTileEffectWithoutMutatingOriginalState()
    {
        var state = CreateState();
        var plan = new CardUsePlan("time_bomb_fixture", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 3)));
        var definition = new CardEffectDefinition(
            "time_bomb_fixture",
            CardTargetQuery.EmptySquare(),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "TimeBomb",
                    durationTurns: 3,
                    targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        TileEffectInfo effect = Assert.Single(result.State!.TileEffects);
        Assert.Equal("time_bomb_fixture:TimeBomb:d4", effect.Id);
        Assert.Equal("TimeBomb", effect.EffectType);
        Assert.Equal(new Square(3, 3), effect.Square);
        Assert.Equal(PieceColor.White, effect.Owner);
        Assert.Equal(3, effect.RemainingTurns);
        Assert.Empty(state.TileEffects);
    }

    [Fact]
    public void Apply_DefaultTileDefinitionsCreatePersistentTileEffects()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();
        var applier = new CardEffectApplier();
        var firstPortal = new Square(2, 2);
        var secondPortal = new Square(5, 5);

        CardEffectApplicationResult fire = applier.Apply(
            catalog.FindDefinition("fire")!,
            CreateContext(
                CreateState(),
                new CardUsePlan("fire", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 3)))));
        CardEffectApplicationResult peace = applier.Apply(
            catalog.FindDefinition("peace_zone")!,
            CreateContext(
                CreateState(),
                new CardUsePlan("peace_zone", PieceColor.White, CardTargetSelection.BoardSquare(new Square(4, 3)))));
        CardEffectApplicationResult portal = applier.Apply(
            catalog.FindDefinition("portal")!,
            CreateContext(
                CreateState(),
                new CardUsePlan(
                "portal",
                PieceColor.White,
                CardTargetSelection.OrderedSquares(new[] { firstPortal, secondPortal }))));
        CardEffectApplicationResult atMine = ApplyTile(catalog, applier, "at_mine", "ATMine", new Square(0, 2));
        CardEffectApplicationResult cobweb = ApplyTile(catalog, applier, "cobweb", "Cobweb", new Square(1, 2));
        CardEffectApplicationResult jumpingPlatform = ApplyTile(catalog, applier, "jumping_platform", "JumpingPlatform", new Square(2, 3));
        CardEffectApplicationResult psilocybinMushroom = ApplyTile(catalog, applier, "psilocybin_mushroom", "PsilocybinMushroom", new Square(3, 4));

        Assert.Equal(CardEffectApplicationStatus.Exact, fire.Status);
        TileEffectInfo fireEffect = Assert.Single(fire.State!.TileEffects);
        Assert.Equal("Fire", fireEffect.EffectType);
        Assert.Equal(-1, fireEffect.RemainingTurns);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, fireEffect.LifetimeKind);

        Assert.Equal(CardEffectApplicationStatus.Exact, peace.Status);
        TileEffectInfo peaceEffect = Assert.Single(peace.State!.TileEffects);
        Assert.Equal("Peace", peaceEffect.EffectType);
        Assert.Equal(-1, peaceEffect.RemainingTurns);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, peaceEffect.LifetimeKind);

        Assert.Equal(CardEffectApplicationStatus.Exact, portal.Status);
        Assert.Collection(
            portal.State!.TileEffects,
            first =>
            {
                Assert.Equal("Portal", first.EffectType);
                Assert.Equal(firstPortal, first.Square);
                Assert.Equal(secondPortal, first.DestinationSquare);
                Assert.Equal(2, first.SharedRemainingUses);
                Assert.Equal(-1, first.RemainingTurns);
                Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, first.LifetimeKind);
            },
            second =>
            {
                Assert.Equal("Portal", second.EffectType);
                Assert.Equal(secondPortal, second.Square);
                Assert.Equal(firstPortal, second.DestinationSquare);
                Assert.Equal(2, second.SharedRemainingUses);
                Assert.Equal(-1, second.RemainingTurns);
                Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, second.LifetimeKind);
            });
        AssertPersistentTileEffect(atMine, "ATMine", new Square(0, 2));
        AssertPersistentTileEffect(cobweb, "Cobweb", new Square(1, 2));
        AssertPersistentTileEffect(jumpingPlatform, "JumpingPlatform", new Square(2, 3));
        AssertPersistentTileEffect(psilocybinMushroom, "PsilocybinMushroom", new Square(3, 4));
    }

    [Fact]
    public void Apply_RemovesTileEffectAtSelectedSquare()
    {
        var square = new Square(3, 3);
        var state = CreateState(tileEffects: new[]
        {
            new TileEffectInfo("effect:d4", "Fire", square, PieceColor.White, 2)
        });
        var plan = new CardUsePlan("remove_fire", PieceColor.White, CardTargetSelection.BoardSquare(square));
        var definition = new CardEffectDefinition(
            "remove_fire",
            new CardTargetQuery(
                CardTargetKind.BoardSquare,
                CardTargetOwnerRelation.Any,
                1,
                requiresEmptySquares: true,
                requiresOccupiedSquares: false,
                allowsExistingTileEffect: true,
                isOrdered: false),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.RemoveTileEffect,
                    targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Empty(result.State!.TileEffects);
        Assert.Single(state.TileEffects);
    }

    [Fact]
    public void Apply_MovesPieceWhenSourceAndDestinationAreExplicit()
    {
        var source = new Square(0, 1);
        var destination = new Square(0, 2);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, source, "p")
        });
        var plan = new CardUsePlan("move_fixture", PieceColor.White, CardTargetSelection.None());
        var definition = new CardEffectDefinition(
            "move_fixture",
            CardTargetQuery.None(),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.MovePiece,
                    sourceSquare: source,
                    destinationSquare: destination)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Null(result.State!.BoardState.FindPiece(source));
        PieceInfo? moved = result.State.BoardState.FindPiece(destination);
        Assert.NotNull(moved);
        Assert.Equal(PieceKind.Pawn, moved.Kind);
        Assert.NotNull(state.BoardState.FindPiece(source));
    }

    [Fact]
    public void Apply_RejectsStaleOccupiedTileTarget()
    {
        var target = new Square(3, 3);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, target, "p")
        });
        var plan = new CardUsePlan("time_bomb_fixture", PieceColor.White, CardTargetSelection.BoardSquare(target));
        var definition = new CardEffectDefinition(
            "time_bomb_fixture",
            CardTargetQuery.EmptySquare(),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "TimeBomb",
                    durationTurns: 3,
                    targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Failed, result.Status);
        Assert.Equal(CardEffectApplicationCode.StaleTarget, result.Code);
        Assert.Null(result.State);
    }

    [Fact]
    public void Apply_RejectsStaleExistingTileEffect()
    {
        var target = new Square(3, 3);
        var state = CreateState(tileEffects: new[]
        {
            new TileEffectInfo("effect:d4", "Fire", target, PieceColor.White, 2)
        });
        var plan = new CardUsePlan("time_bomb_fixture", PieceColor.White, CardTargetSelection.BoardSquare(target));
        var definition = new CardEffectDefinition(
            "time_bomb_fixture",
            CardTargetQuery.EmptySquare(),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "TimeBomb",
                    durationTurns: 3,
                    targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Failed, result.Status);
        Assert.Equal(CardEffectApplicationCode.StaleTarget, result.Code);
        Assert.Null(result.State);
    }

    [Fact]
    public void Apply_AgileAndChargeDefaultsAreUnsupportedByCurrentStateContract()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, new Square(0, 1), "p")
        });
        var agilePlan = new CardUsePlan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(new Square(0, 1), PieceColor.White, PieceKind.Pawn)));
        var chargePlan = new CardUsePlan("charge", PieceColor.White, CardTargetSelection.None());
        var applier = new CardEffectApplier();

        CardEffectApplicationResult agile = applier.Apply(
            catalog.FindDefinition("agile")!,
            CreateContext(state, agilePlan));
        CardEffectApplicationResult charge = applier.Apply(
            catalog.FindDefinition("charge")!,
            CreateContext(state, chargePlan));

        Assert.Equal(CardEffectApplicationStatus.Unsupported, agile.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, agile.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, charge.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, charge.Code);
    }

    [Fact]
    public void Apply_RejectsUnresolvableDestinationTargetIndex()
    {
        var state = CreateState();
        var plan = new CardUsePlan(
            "broken_portal",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(new[] { new Square(2, 2), new Square(5, 5) }));
        var definition = new CardEffectDefinition(
            "broken_portal",
            CardTargetQuery.OrderedEmptySquares(2),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "Portal",
                    durationTurns: -1,
                    tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
                    targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
                    targetIndex: 0,
                    destinationTargetIndex: 2)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Failed, result.Status);
        Assert.Equal(CardEffectApplicationCode.InvalidDefinition, result.Code);
        Assert.Null(result.State);
    }

    [Fact]
    public void Apply_IsAtomicWhenSecondPrimitiveFails()
    {
        var first = new Square(2, 2);
        var occupied = new Square(3, 3);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, occupied, "p")
        });
        var plan = new CardUsePlan(
            "atomic_fixture",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(new[] { first, occupied }));
        var definition = new CardEffectDefinition(
            "atomic_fixture",
            new CardTargetQuery(
                CardTargetKind.OrderedSquares,
                CardTargetOwnerRelation.Any,
                2,
                requiresEmptySquares: false,
                requiresOccupiedSquares: false,
                allowsExistingTileEffect: false,
                isOrdered: true),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "First",
                    durationTurns: 1,
                    targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
                    targetIndex: 0),
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "Second",
                    durationTurns: 1,
                    targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
                    targetIndex: 1)
            });

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            definition,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Failed, result.Status);
        Assert.Equal(CardEffectApplicationCode.StaleTarget, result.Code);
        Assert.Null(result.State);
        Assert.Empty(state.TileEffects);
    }

    [Fact]
    public void Apply_RejectsInvalidDefinitionOrContext()
    {
        var state = CreateState();
        var plan = new CardUsePlan("fire", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 3)));
        var definition = new CardEffectDefinition(
            "other",
            CardTargetQuery.EmptySquare(),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "Fire",
                    durationTurns: 1,
                    targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
            });
        var applier = new CardEffectApplier();

        Assert.Equal(
            CardEffectApplicationCode.InvalidDefinition,
            applier.Apply(null!, CreateContext(state, plan)).Code);
        Assert.Equal(
            CardEffectApplicationCode.InvalidContext,
            applier.Apply(definition, null!).Code);

        CardEffectApplicationResult mismatch = applier.Apply(
            definition,
            CreateContext(state, plan));
        Assert.Equal(CardEffectApplicationCode.InvalidContext, mismatch.Code);
    }

    private static CardEffectApplicationContext CreateContext(
        GameState state,
        CardUsePlan plan)
    {
        return new CardEffectApplicationContext(
            state,
            plan,
            plan.Actor,
            caster: plan.Actor,
            owner: plan.Actor);
    }

    private static CardEffectApplicationResult ApplyTile(
        DefaultCardEffectDefinitionCatalog catalog,
        CardEffectApplier applier,
        string cardId,
        string expectedEffectType,
        Square square)
    {
        CardEffectApplicationResult result = applier.Apply(
            catalog.FindDefinition(cardId)!,
            CreateContext(
                CreateState(),
                new CardUsePlan(cardId, PieceColor.White, CardTargetSelection.BoardSquare(square))));

        Assert.Equal(expectedEffectType, Assert.Single(result.State!.TileEffects).EffectType);
        return result;
    }

    private static void AssertPersistentTileEffect(
        CardEffectApplicationResult result,
        string expectedEffectType,
        Square expectedSquare)
    {
        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        TileEffectInfo effect = Assert.Single(result.State!.TileEffects);
        Assert.Equal(expectedEffectType, effect.EffectType);
        Assert.Equal(expectedSquare, effect.Square);
        Assert.Equal(-1, effect.RemainingTurns);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, effect.LifetimeKind);
    }

    private static GameState CreateState(
        IEnumerable<PieceInfo>? extraPieces = null,
        IEnumerable<TileEffectInfo>? tileEffects = null)
    {
        var pieces = new List<PieceInfo>
        {
            new PieceInfo(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
            new PieceInfo(PieceKind.King, PieceColor.Black, new Square(4, 7), "k")
        };

        if (extraPieces != null)
        {
            pieces.AddRange(extraPieces);
        }

        return new GameState(
            new BoardState(
                pieces,
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            Array.Empty<CardInfo>(),
            tileEffects ?? Enumerable.Empty<TileEffectInfo>());
    }
}
