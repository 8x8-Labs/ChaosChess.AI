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
        CardEffectApplicationResult blessing = ApplyTile(catalog, applier, "blessing", "Blessing", new Square(4, 2));
        CardEffectApplicationResult cobweb = ApplyTile(catalog, applier, "cobweb", "Cobweb", new Square(1, 2));
        CardEffectApplicationResult jumpingPlatform = ApplyTile(catalog, applier, "jumping_platform", "JumpingPlatform", new Square(2, 3));
        CardEffectApplicationResult obeyOrder = ApplyTile(catalog, applier, "obey_order", "ObeyOrder", new Square(4, 4));
        CardEffectApplicationResult psilocybinMushroom = ApplyTile(catalog, applier, "psilocybin_mushroom", "PsilocybinMushroom", new Square(3, 4));
        CardEffectApplicationResult timeBomb = ApplyTile(catalog, applier, "time_bomb", "TimeBomb", new Square(6, 3));

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
        AssertPersistentTileEffect(blessing, "Blessing", new Square(4, 2));
        AssertPersistentTileEffect(cobweb, "Cobweb", new Square(1, 2));
        AssertPersistentTileEffect(jumpingPlatform, "JumpingPlatform", new Square(2, 3));
        AssertPersistentTileEffect(obeyOrder, "ObeyOrder", new Square(4, 4));
        AssertPersistentTileEffect(psilocybinMushroom, "PsilocybinMushroom", new Square(3, 4));
        AssertDurationTileEffect(timeBomb, "TimeBomb", new Square(6, 3), 3);
    }

    [Fact]
    public void Apply_SyncCreatesMirroredLinkedTileEffects()
    {
        var selected = new Square(2, 2);
        var mirrored = new Square(5, 2);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Rook, PieceColor.White, mirrored, "r")
        });
        var plan = new CardUsePlan(
            "sync",
            PieceColor.White,
            CardTargetSelection.BoardSquare(selected));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("sync")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Collection(
            result.State!.TileEffects,
            first =>
            {
                Assert.Equal("Sync", first.EffectType);
                Assert.Equal(selected, first.Square);
                Assert.Equal(mirrored, first.DestinationSquare);
                Assert.Equal(1, first.SharedRemainingUses);
                Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, first.LifetimeKind);
            },
            second =>
            {
                Assert.Equal("Sync", second.EffectType);
                Assert.Equal(mirrored, second.Square);
                Assert.Equal(selected, second.DestinationSquare);
                Assert.Equal(1, second.SharedRemainingUses);
                Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, second.LifetimeKind);
            });
        Assert.NotNull(result.State.BoardState.FindPiece(mirrored));
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
    public void Apply_TeleportMovesSelectedPawnToSelectedDestination()
    {
        var source = new Square(4, 1);
        var destination = new Square(4, 5);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, source, "p")
        });
        var plan = new CardUsePlan(
            "teleport",
            PieceColor.White,
            CardTargetSelection.PieceAndSquare(
                new PieceTargetSnapshot(source, PieceColor.White, PieceKind.Pawn),
                destination));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("teleport")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Null(result.State!.BoardState.FindPiece(source));
        PieceInfo? moved = result.State.BoardState.FindPiece(destination);
        Assert.NotNull(moved);
        Assert.Equal(PieceKind.Pawn, moved.Kind);
        Assert.Equal(PieceColor.White, moved.Color);
        Assert.NotNull(state.BoardState.FindPiece(source));
    }

    [Fact]
    public void Apply_TransmigrationRevertsSelectedPromotedPieceToStartSquarePawn()
    {
        var current = new Square(3, 3);
        var start = new Square(4, 6);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(
                PieceKind.Chancellor,
                PieceColor.Black,
                current,
                "y",
                isPromotioned: true,
                start)
        });
        var plan = new CardUsePlan(
            "transmigration",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    current,
                    PieceColor.Black,
                    PieceKind.Chancellor,
                    isPromotioned: true,
                    start)));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("transmigration")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Null(result.State!.BoardState.FindPiece(current));
        PieceInfo reverted = result.State.BoardState.FindPiece(start)!;
        Assert.Equal(PieceKind.Pawn, reverted.Kind);
        Assert.Equal(PieceColor.Black, reverted.Color);
        Assert.False(reverted.IsPromotioned);
        Assert.Null(reverted.StartSquare);
        Assert.NotNull(state.BoardState.FindPiece(current));
    }

    [Fact]
    public void Apply_ReviveCreatesHighestValueCapturedActorPieceAndConsumesIt()
    {
        var target = new Square(3, 3);
        var state = CreateState(
            capturedPieces: new CapturedPieceState(
                new[] { PieceKind.Knight, PieceKind.Queen, PieceKind.Rook },
                Array.Empty<PieceKind>()));
        var plan = new CardUsePlan(
            "revive",
            PieceColor.White,
            CardTargetSelection.BoardSquare(target));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("revive")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        PieceInfo revived = result.State!.BoardState.FindPiece(target)!;
        Assert.Equal(PieceKind.Queen, revived.Kind);
        Assert.Equal(PieceColor.White, revived.Color);
        Assert.Equal(new[] { PieceKind.Knight, PieceKind.Rook }, result.State.CapturedPieces.WhitePieces);
        Assert.Equal(new[] { PieceKind.Knight, PieceKind.Queen, PieceKind.Rook }, state.CapturedPieces.WhitePieces);
    }

    [Fact]
    public void Apply_ReviveCreatesWallWhenActorHasNoCapturedPieces()
    {
        var target = new Square(3, 3);
        var state = CreateState();
        var plan = new CardUsePlan(
            "revive",
            PieceColor.White,
            CardTargetSelection.BoardSquare(target));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("revive")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        PieceInfo wall = result.State!.BoardState.FindPiece(target)!;
        Assert.Equal(PieceKind.Wall, wall.Kind);
        Assert.Equal(PieceColor.White, wall.Color);
        Assert.Empty(result.State.CapturedPieces.WhitePieces);
    }

    [Fact]
    public void Apply_TimeReversalStoresBoardSnapshotForActor()
    {
        var state = CreateState();
        var plan = new CardUsePlan("time_reversal", PieceColor.White, CardTargetSelection.None());
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("time_reversal")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        TimeReversalState timeReversal = Assert.Single(result.State!.TimeReversals);
        Assert.Equal("time_reversal:TimeReversal:0", timeReversal.Id);
        Assert.Equal(PieceColor.White, timeReversal.Owner);
        Assert.Equal(8, timeReversal.RemainingTurns);
        Assert.Same(state.BoardState, timeReversal.SavedBoardState);
        Assert.Empty(state.TimeReversals);
    }

    [Fact]
    public void Apply_RampartCreatesTwoActorWallPieces()
    {
        var first = new Square(2, 2);
        var second = new Square(5, 5);
        var state = CreateState();
        var plan = new CardUsePlan(
            "rampart",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(new[] { first, second }));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("rampart")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        PieceInfo firstWall = result.State!.BoardState.FindPiece(first)!;
        PieceInfo secondWall = result.State.BoardState.FindPiece(second)!;
        Assert.Equal(PieceKind.Wall, firstWall.Kind);
        Assert.Equal(PieceColor.White, firstWall.Color);
        Assert.Equal(PieceKind.Wall, secondWall.Kind);
        Assert.Equal(PieceColor.White, secondWall.Color);
        Assert.Null(state.BoardState.FindPiece(first));
        Assert.Null(state.BoardState.FindPiece(second));
    }

    [Fact]
    public void Apply_PositionSwapFlipsPiecesAndBoardMetadata()
    {
        var tileEffect = new TileEffectInfo("effect:d4", "Fire", new Square(3, 3), PieceColor.White, 2);
        var state = new GameState(
            new BoardState(
                new[]
                {
                    new PieceInfo(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
                    new PieceInfo(PieceKind.King, PieceColor.Black, new Square(4, 7), "k"),
                    new PieceInfo(PieceKind.Queen, PieceColor.White, new Square(0, 1), "q"),
                    new PieceInfo(PieceKind.Rook, PieceColor.Black, new Square(7, 6), "r")
                },
                PieceColor.White,
                CastlingRights.WhiteKingSide | CastlingRights.BlackQueenSide,
                new Square(2, 2),
                halfmoveClock: 4,
                fullmoveNumber: 12),
            Array.Empty<CardInfo>(),
            new[] { tileEffect });
        var plan = new CardUsePlan("position_swap", PieceColor.White, CardTargetSelection.None());
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("position_swap")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Empty(result.State!.TileEffects);
        Assert.Equal(CastlingRights.BlackKingSide | CastlingRights.WhiteQueenSide, result.State.BoardState.CastlingRights);
        Assert.Equal(new Square(5, 5), result.State.BoardState.EnPassantTarget);
        Assert.Equal(4, result.State.BoardState.HalfmoveClock);
        Assert.Equal(12, result.State.BoardState.FullmoveNumber);
        Assert.Equal(PieceColor.Black, result.State.BoardState.FindPiece(new Square(0, 6))!.Color);
        Assert.Equal(PieceKind.Queen, result.State.BoardState.FindPiece(new Square(0, 6))!.Kind);
        Assert.Equal(PieceColor.White, result.State.BoardState.FindPiece(new Square(7, 1))!.Color);
        Assert.Equal(PieceKind.Rook, result.State.BoardState.FindPiece(new Square(7, 1))!.Kind);
        Assert.Single(state.TileEffects);
    }

    [Fact]
    public void Apply_CastleKnightMergesSelectedKnightIntoNearestActorRook()
    {
        var knightSquare = new Square(1, 0);
        var nearestRookSquare = new Square(0, 0);
        var farRookSquare = new Square(7, 0);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Knight, PieceColor.White, knightSquare, "n"),
            new PieceInfo(PieceKind.Rook, PieceColor.White, nearestRookSquare, "r"),
            new PieceInfo(PieceKind.Rook, PieceColor.White, farRookSquare, "r")
        });
        var plan = new CardUsePlan(
            "castle_knight",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(knightSquare, PieceColor.White, PieceKind.Knight)));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("castle_knight")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Null(result.State!.BoardState.FindPiece(knightSquare));
        PieceInfo merged = result.State.BoardState.FindPiece(nearestRookSquare)!;
        Assert.Equal(PieceKind.Chancellor, merged.Kind);
        Assert.Equal(PieceColor.White, merged.Color);
        Assert.Equal(PieceKind.Rook, result.State.BoardState.FindPiece(farRookSquare)!.Kind);
        Assert.NotNull(state.BoardState.FindPiece(knightSquare));
    }

    [Fact]
    public void Apply_WeirdCastlingSwapsSelectedPieceWithActorKing()
    {
        var kingSquare = new Square(4, 0);
        var pawnSquare = new Square(4, 1);
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, pawnSquare, "p")
        });
        var plan = new CardUsePlan(
            "weird_castling",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(pawnSquare, PieceColor.White, PieceKind.Pawn)));
        var catalog = new DefaultCardEffectDefinitionCatalog();

        CardEffectApplicationResult result = new CardEffectApplier().Apply(
            catalog.FindDefinition("weird_castling")!,
            CreateContext(state, plan));

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        PieceInfo movedKing = result.State!.BoardState.FindPiece(pawnSquare)!;
        PieceInfo movedPawn = result.State.BoardState.FindPiece(kingSquare)!;
        Assert.Equal(PieceKind.King, movedKing.Kind);
        Assert.Equal(PieceColor.White, movedKing.Color);
        Assert.Equal(PieceKind.Pawn, movedPawn.Kind);
        Assert.Equal(PieceColor.White, movedPawn.Color);
        Assert.Equal(PieceKind.King, state.BoardState.FindPiece(kingSquare)!.Kind);
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
    public void Apply_UnsupportedDefaultEffectsReturnUnsupportedByCurrentStateContract()
    {
        var catalog = new DefaultCardEffectDefinitionCatalog();
        var state = CreateState(extraPieces: new[]
        {
            new PieceInfo(PieceKind.Pawn, PieceColor.White, new Square(0, 1), "p"),
            new PieceInfo(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n"),
            new PieceInfo(PieceKind.Pawn, PieceColor.Black, new Square(0, 6), "p"),
            new PieceInfo(PieceKind.Bishop, PieceColor.Black, new Square(2, 6), "b")
        });
        var agilePlan = new CardUsePlan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(new Square(0, 1), PieceColor.White, PieceKind.Pawn)));
        var dimensionInstabilityPlan = new CardUsePlan(
            "dimension_instability",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(new Square(1, 0), PieceColor.White, PieceKind.Knight)));
        var chaoticKnightPlan = new CardUsePlan(
            "chaotic_knight",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(new Square(1, 0), PieceColor.White, PieceKind.Knight)));
        var desperadoPlan = new CardUsePlan(
            "desperado",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(new Square(0, 1), PieceColor.White, PieceKind.Pawn)));
        var fatherEnemyPlan = new CardUsePlan(
            "father_enemy",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(new Square(0, 1), PieceColor.White, PieceKind.Pawn)));
        var chargePlan = new CardUsePlan("charge", PieceColor.White, CardTargetSelection.None());
        var arenaPlan = new CardUsePlan("arena", PieceColor.White, CardTargetSelection.None());
        var checkmateDeclarationPlan = new CardUsePlan("checkmate_declaration", PieceColor.White, CardTargetSelection.None());
        var democracyPlan = new CardUsePlan("democracy", PieceColor.White, CardTargetSelection.None());
        var destroyerTankPlan = new CardUsePlan("destroyer_tank_cards", PieceColor.White, CardTargetSelection.None());
        var dimensionDisturbancePlan = new CardUsePlan(
            "dimension_disturbance",
            PieceColor.White,
            CardTargetSelection.OrderedPieces(new[]
            {
                new PieceTargetSnapshot(new Square(0, 6), PieceColor.Black, PieceKind.Pawn),
                new PieceTargetSnapshot(new Square(2, 6), PieceColor.Black, PieceKind.Bishop)
            }));
        var gaslightingPlan = new CardUsePlan("gaslighting", PieceColor.White, CardTargetSelection.None());
        var honeyTrapPlan = new CardUsePlan("honey_trap", PieceColor.White, CardTargetSelection.None());
        var magnetPlan = new CardUsePlan("magnet", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 3)));
        var mutinyPlan = new CardUsePlan("mutiny", PieceColor.White, CardTargetSelection.None());
        var overbearingPlan = new CardUsePlan("overbearing", PieceColor.White, CardTargetSelection.None());
        var shuffleBoardPlan = new CardUsePlan("shuffle_board", PieceColor.White, CardTargetSelection.None());
        var stagFightPlan = new CardUsePlan("stag_fight", PieceColor.White, CardTargetSelection.None());
        var windmillPlan = new CardUsePlan("windmill", PieceColor.White, CardTargetSelection.None());
        var applier = new CardEffectApplier();

        CardEffectApplicationResult agile = applier.Apply(
            catalog.FindDefinition("agile")!,
            CreateContext(state, agilePlan));
        CardEffectApplicationResult dimensionInstability = applier.Apply(
            catalog.FindDefinition("dimension_instability")!,
            CreateContext(state, dimensionInstabilityPlan));
        CardEffectApplicationResult chaoticKnight = applier.Apply(
            catalog.FindDefinition("chaotic_knight")!,
            CreateContext(state, chaoticKnightPlan));
        CardEffectApplicationResult desperado = applier.Apply(
            catalog.FindDefinition("desperado")!,
            CreateContext(state, desperadoPlan));
        CardEffectApplicationResult fatherEnemy = applier.Apply(
            catalog.FindDefinition("father_enemy")!,
            CreateContext(state, fatherEnemyPlan));
        CardEffectApplicationResult charge = applier.Apply(
            catalog.FindDefinition("charge")!,
            CreateContext(state, chargePlan));
        CardEffectApplicationResult arena = applier.Apply(
            catalog.FindDefinition("arena")!,
            CreateContext(state, arenaPlan));
        CardEffectApplicationResult checkmateDeclaration = applier.Apply(
            catalog.FindDefinition("checkmate_declaration")!,
            CreateContext(state, checkmateDeclarationPlan));
        CardEffectApplicationResult democracy = applier.Apply(
            catalog.FindDefinition("democracy")!,
            CreateContext(state, democracyPlan));
        CardEffectApplicationResult destroyerTank = applier.Apply(
            catalog.FindDefinition("destroyer_tank_cards")!,
            CreateContext(state, destroyerTankPlan));
        CardEffectApplicationResult dimensionDisturbance = applier.Apply(
            catalog.FindDefinition("dimension_disturbance")!,
            CreateContext(state, dimensionDisturbancePlan));
        CardEffectApplicationResult gaslighting = applier.Apply(
            catalog.FindDefinition("gaslighting")!,
            CreateContext(state, gaslightingPlan));
        CardEffectApplicationResult honeyTrap = applier.Apply(
            catalog.FindDefinition("honey_trap")!,
            CreateContext(state, honeyTrapPlan));
        CardEffectApplicationResult magnet = applier.Apply(
            catalog.FindDefinition("magnet")!,
            CreateContext(state, magnetPlan));
        CardEffectApplicationResult mutiny = applier.Apply(
            catalog.FindDefinition("mutiny")!,
            CreateContext(state, mutinyPlan));
        CardEffectApplicationResult overbearing = applier.Apply(
            catalog.FindDefinition("overbearing")!,
            CreateContext(state, overbearingPlan));
        CardEffectApplicationResult shuffleBoard = applier.Apply(
            catalog.FindDefinition("shuffle_board")!,
            CreateContext(state, shuffleBoardPlan));
        CardEffectApplicationResult stagFight = applier.Apply(
            catalog.FindDefinition("stag_fight")!,
            CreateContext(state, stagFightPlan));
        CardEffectApplicationResult windmill = applier.Apply(
            catalog.FindDefinition("windmill")!,
            CreateContext(state, windmillPlan));

        Assert.Equal(CardEffectApplicationStatus.Unsupported, agile.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, agile.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, chaoticKnight.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, chaoticKnight.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, desperado.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, desperado.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, dimensionInstability.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, dimensionInstability.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, fatherEnemy.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, fatherEnemy.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, charge.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, charge.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, arena.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, arena.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, checkmateDeclaration.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, checkmateDeclaration.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, democracy.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, democracy.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, destroyerTank.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, destroyerTank.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, dimensionDisturbance.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, dimensionDisturbance.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, gaslighting.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, gaslighting.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, honeyTrap.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, honeyTrap.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, magnet.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, magnet.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, mutiny.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, mutiny.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, overbearing.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, overbearing.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, shuffleBoard.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, shuffleBoard.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, stagFight.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, stagFight.Code);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, windmill.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, windmill.Code);
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

    private static void AssertDurationTileEffect(
        CardEffectApplicationResult result,
        string expectedEffectType,
        Square expectedSquare,
        int expectedRemainingTurns)
    {
        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        TileEffectInfo effect = Assert.Single(result.State!.TileEffects);
        Assert.Equal(expectedEffectType, effect.EffectType);
        Assert.Equal(expectedSquare, effect.Square);
        Assert.Equal(expectedRemainingTurns, effect.RemainingTurns);
        Assert.Equal(TileEffectLifetimeKind.TurnLimited, effect.LifetimeKind);
    }

    private static GameState CreateState(
        IEnumerable<PieceInfo>? extraPieces = null,
        IEnumerable<TileEffectInfo>? tileEffects = null,
        CapturedPieceState? capturedPieces = null)
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
            tileEffects ?? Enumerable.Empty<TileEffectInfo>(),
            capturedPieces);
    }
}
