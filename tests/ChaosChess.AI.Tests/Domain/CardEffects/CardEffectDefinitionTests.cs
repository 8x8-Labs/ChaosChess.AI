using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Domain.CardEffects;

public sealed class CardEffectDefinitionTests
{
    [Fact]
    public void TargetQuery_StoresOwnerRelationAndTargetShape()
    {
        var query = CardTargetQuery.Piece(CardTargetOwnerRelation.Opponent, 1);

        Assert.Equal(CardTargetKind.PieceAtSquare, query.Kind);
        Assert.Equal(CardTargetOwnerRelation.Opponent, query.OwnerRelation);
        Assert.Equal(1, query.Count);
        Assert.True(query.RequiresOccupiedSquares);
        Assert.False(query.RequiresEmptySquares);
        Assert.False(query.IsOrdered);
    }

    [Fact]
    public void TargetQuery_PieceAndEmptySquareStoresTargetShape()
    {
        CardTargetQuery query = CardTargetQuery.PieceAndEmptySquare(CardTargetOwnerRelation.Self);

        Assert.Equal(CardTargetKind.PieceAndSquare, query.Kind);
        Assert.Equal(CardTargetOwnerRelation.Self, query.OwnerRelation);
        Assert.Equal(2, query.Count);
        Assert.True(query.RequiresEmptySquares);
        Assert.False(query.RequiresOccupiedSquares);
        Assert.True(query.IsOrdered);
    }

    [Fact]
    public void TargetQuery_OrderedPiecesStoresTargetShape()
    {
        CardTargetQuery query = CardTargetQuery.OrderedPieces(CardTargetOwnerRelation.Opponent, 2);

        Assert.Equal(CardTargetKind.OrderedPieces, query.Kind);
        Assert.Equal(CardTargetOwnerRelation.Opponent, query.OwnerRelation);
        Assert.Equal(2, query.Count);
        Assert.False(query.RequiresEmptySquares);
        Assert.True(query.RequiresOccupiedSquares);
        Assert.True(query.IsOrdered);
    }

    [Fact]
    public void TargetQuery_RejectsInvalidShape()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetQuery(
                (CardTargetKind)99,
                CardTargetOwnerRelation.Any,
                1,
                false,
                false,
                true,
                false));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetQuery(
                CardTargetKind.None,
                (CardTargetOwnerRelation)99,
                0,
                false,
                false,
                true,
                false));
        Assert.Throws<ArgumentException>(
            () => new CardTargetQuery(
                CardTargetKind.None,
                CardTargetOwnerRelation.Any,
                1,
                false,
                false,
                true,
                false));
        Assert.Throws<ArgumentException>(
            () => new CardTargetQuery(
                CardTargetKind.BoardSquare,
                CardTargetOwnerRelation.Any,
                1,
                true,
                true,
                false,
                false));
        Assert.Throws<ArgumentException>(
            () => new CardTargetQuery(
                CardTargetKind.OrderedSquares,
                CardTargetOwnerRelation.Any,
                2,
                true,
                false,
                false,
                false));
        Assert.Throws<ArgumentException>(
            () => new CardTargetQuery(
                CardTargetKind.PieceAndSquare,
                CardTargetOwnerRelation.Self,
                1,
                true,
                false,
                false,
                true));
        Assert.Throws<ArgumentException>(
            () => new CardTargetQuery(
                CardTargetKind.OrderedPieces,
                CardTargetOwnerRelation.Opponent,
                2,
                false,
                true,
                false,
                false));
    }

    [Fact]
    public void Primitive_StoresTypedEffectData()
    {
        var square = new Square(4, 3);

        CardEffectPrimitive primitive = CardEffectPrimitive.AddTileEffect(
            square,
            "Fire",
            PieceColor.White,
            durationTurns: 2);

        Assert.Equal(CardEffectPrimitiveKind.AddTileEffect, primitive.Kind);
        Assert.Equal(square, primitive.SourceSquare);
        Assert.Equal("Fire", primitive.EffectType);
        Assert.Equal(PieceColor.White, primitive.Owner);
        Assert.Equal(2, primitive.DurationTurns);
        Assert.Equal(TileEffectLifetimeKind.TurnLimited, primitive.TileEffectLifetimeKind);
        Assert.Equal(CardEffectPrimitiveTargetBinding.SelectedSquare, primitive.TargetBinding);
        Assert.Null(primitive.TargetIndex);
        Assert.Null(primitive.DestinationTargetIndex);
    }

    [Fact]
    public void Primitive_AllowsPersistentTileEffectsWithoutTurnLimitedDuration()
    {
        CardEffectPrimitive primitive = CardEffectPrimitive.AddTileEffect(
            new Square(2, 2),
            "Portal",
            PieceColor.White,
            durationTurns: -1,
            sharedRemainingUses: 2,
            tileEffectLifetimeKind: TileEffectLifetimeKind.PersistentUntilTriggered,
            targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
            targetIndex: 0,
            destinationTargetIndex: 1);

        Assert.Equal(-1, primitive.DurationTurns);
        Assert.Equal(2, primitive.SharedRemainingUses);
        Assert.Equal(TileEffectLifetimeKind.PersistentUntilTriggered, primitive.TileEffectLifetimeKind);
        Assert.Equal(0, primitive.TargetIndex);
        Assert.Equal(1, primitive.DestinationTargetIndex);
    }

    [Fact]
    public void Primitive_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive((CardEffectPrimitiveKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.CreatePiece,
                pieceKind: PieceKind.Unknown));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(CardEffectPrimitiveKind.CreatePiece));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.CreatePiece,
                pieceKindBinding: (CardEffectPrimitivePieceKindBinding)99));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.MovePiece,
                pieceKindBinding: CardEffectPrimitivePieceKindBinding.ActorHighestValueCapturedOrWall));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.MergeSelectedPieceIntoNearestAlly,
                pieceKind: PieceKind.Chancellor));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.ChangeOwner,
                owner: (PieceColor)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Fire",
                durationTurns: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Portal",
                sharedRemainingUses: -1));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(CardEffectPrimitiveKind.AddTileEffect));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(CardEffectPrimitiveKind.AddMirroredTileEffectPair));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(CardEffectPrimitiveKind.AddGlobalEffect));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(CardEffectPrimitiveKind.SetMovementOverride));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.MovePiece,
                targetBinding: (CardEffectPrimitiveTargetBinding)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.MovePiece,
                targetIndex: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Portal",
                destinationTargetIndex: -1));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Portal",
                targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Portal",
                destinationTargetIndex: 1));
        Assert.Throws<ArgumentException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Portal",
                targetBinding: CardEffectPrimitiveTargetBinding.OrderedSquareByIndex,
                targetIndex: 1,
                destinationTargetIndex: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                effectType: "Portal",
                tileEffectLifetimeKind: (TileEffectLifetimeKind)99));
    }

    [Fact]
    public void Definition_DefensivelyCopiesPrimitives()
    {
        var primitives = new List<CardEffectPrimitive>
        {
            CardEffectPrimitive.SetMovementOverride(
                new Square(0, 1),
                "u",
                durationTurns: 1)
        };

        var definition = new CardEffectDefinition(
            "agile",
            CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
            primitives);
        primitives.Clear();

        Assert.Equal("agile", definition.CardId);
        Assert.Equal(CardTargetOwnerRelation.Self, definition.TargetQuery.OwnerRelation);
        Assert.Single(definition.Primitives);
    }

    [Fact]
    public void Definition_RejectsInvalidArguments()
    {
        CardEffectPrimitive primitive = CardEffectPrimitive.SetMovementOverride(
            new Square(0, 1),
            "u",
            durationTurns: 1);

        Assert.Throws<ArgumentException>(
            () => new CardEffectDefinition(
                "",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new[] { primitive }));
        Assert.Throws<ArgumentNullException>(
            () => new CardEffectDefinition("agile", null!, new[] { primitive }));
        Assert.Throws<ArgumentNullException>(
            () => new CardEffectDefinition(
                "agile",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                null!));
        Assert.Throws<ArgumentException>(
            () => new CardEffectDefinition(
                "agile",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                Array.Empty<CardEffectPrimitive>()));
        Assert.Throws<ArgumentException>(
            () => new CardEffectDefinition(
                "agile",
                CardTargetQuery.Piece(CardTargetOwnerRelation.Self, 1),
                new CardEffectPrimitive[] { null! }));
    }
}
