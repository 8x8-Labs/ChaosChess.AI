using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Domain.CardEffects;

public sealed class CardEffectApplicationResultTests
{
    [Fact]
    public void Context_StoresActorCasterOwnerAndRandom()
    {
        GameState state = CreateState(PieceColor.White);
        var plan = new CardUsePlan("charge", PieceColor.White, CardTargetSelection.None());

        var context = new CardEffectApplicationContext(
            state,
            plan,
            actor: PieceColor.White,
            caster: PieceColor.Black,
            owner: PieceColor.White);

        Assert.Same(state, context.State);
        Assert.Same(plan, context.Plan);
        Assert.Equal(PieceColor.White, context.Actor);
        Assert.Equal(PieceColor.Black, context.Caster);
        Assert.Equal(PieceColor.White, context.Owner);
        Assert.Null(context.Random);
    }

    [Fact]
    public void Context_RejectsInvalidArguments()
    {
        GameState state = CreateState(PieceColor.White);
        var plan = new CardUsePlan("charge", PieceColor.White, CardTargetSelection.None());

        Assert.Throws<ArgumentNullException>(
            () => new CardEffectApplicationContext(
                null!,
                plan,
                PieceColor.White,
                PieceColor.White,
                PieceColor.White));
        Assert.Throws<ArgumentNullException>(
            () => new CardEffectApplicationContext(
                state,
                null!,
                PieceColor.White,
                PieceColor.White,
                PieceColor.White));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectApplicationContext(
                state,
                plan,
                (PieceColor)99,
                PieceColor.White,
                PieceColor.White));
        Assert.Throws<ArgumentException>(
            () => new CardEffectApplicationContext(
                state,
                plan,
                PieceColor.Black,
                PieceColor.Black,
                PieceColor.Black));
    }

    [Fact]
    public void ExactResult_RequiresSuccessAndState()
    {
        GameState state = CreateState(PieceColor.White);

        CardEffectApplicationResult result = CardEffectApplicationResult.Exact(state);

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Equal(CardEffectApplicationCode.Success, result.Code);
        Assert.Same(state, result.State);
        Assert.True(result.HasState);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void CoarseResult_StoresWarningsAsDefensiveCopy()
    {
        GameState state = CreateState(PieceColor.White);
        var warnings = new List<string> { "Fire delayed removal is not represented." };

        CardEffectApplicationResult result = CardEffectApplicationResult.Coarse(
            state,
            warnings);
        warnings.Clear();

        Assert.Equal(CardEffectApplicationStatus.Coarse, result.Status);
        Assert.Equal(CardEffectApplicationCode.CoarseApplied, result.Code);
        Assert.Same(state, result.State);
        Assert.Equal("Fire delayed removal is not represented.", Assert.Single(result.Warnings));
    }

    [Fact]
    public void UnsupportedAndFailedResults_DoNotCarryState()
    {
        CardEffectApplicationResult unsupported = CardEffectApplicationResult.Unsupported(
            CardEffectApplicationCode.UnsupportedEffect,
            new[] { "TimeReversal requires Unity runtime state." });
        CardEffectApplicationResult failed = CardEffectApplicationResult.Failed(
            CardEffectApplicationCode.IllegalTarget);

        Assert.Equal(CardEffectApplicationStatus.Unsupported, unsupported.Status);
        Assert.Null(unsupported.State);
        Assert.False(unsupported.HasState);
        Assert.Equal(CardEffectApplicationStatus.Failed, failed.Status);
        Assert.Null(failed.State);
    }

    [Fact]
    public void Result_RejectsInvalidStatusCodeCombinations()
    {
        GameState state = CreateState(PieceColor.White);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectApplicationResult(
                (CardEffectApplicationStatus)99,
                CardEffectApplicationCode.Success,
                state));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardEffectApplicationResult(
                CardEffectApplicationStatus.Exact,
                (CardEffectApplicationCode)99,
                state));
        Assert.Throws<ArgumentException>(
            () => new CardEffectApplicationResult(
                CardEffectApplicationStatus.Exact,
                CardEffectApplicationCode.CoarseApplied,
                state));
        Assert.Throws<ArgumentNullException>(
            () => new CardEffectApplicationResult(
                CardEffectApplicationStatus.Exact,
                CardEffectApplicationCode.Success,
                state: null));
        Assert.Throws<ArgumentException>(
            () => new CardEffectApplicationResult(
                CardEffectApplicationStatus.Unsupported,
                CardEffectApplicationCode.InvalidContext,
                state: null));
        Assert.Throws<ArgumentException>(
            () => new CardEffectApplicationResult(
                CardEffectApplicationStatus.Failed,
                CardEffectApplicationCode.Success,
                state: null));
        Assert.Throws<ArgumentException>(
            () => new CardEffectApplicationResult(
                CardEffectApplicationStatus.Failed,
                CardEffectApplicationCode.InvalidContext,
                state));
        Assert.Throws<ArgumentException>(
            () => CardEffectApplicationResult.Failed(
                CardEffectApplicationCode.InvalidContext,
                new string[] { null! }));
    }

    private static GameState CreateState(PieceColor sideToMove)
    {
        var board = new BoardState(
            new[]
            {
                new PieceInfo(PieceKind.King, PieceColor.White, new Square(4, 0), "k"),
                new PieceInfo(PieceKind.King, PieceColor.Black, new Square(4, 7), "k")
            },
            sideToMove,
            CastlingRights.None,
            enPassantTarget: null,
            halfmoveClock: 0,
            fullmoveNumber: 1);

        return new GameState(
            board,
            Array.Empty<CardInfo>(),
            Array.Empty<TileEffectInfo>());
    }
}
