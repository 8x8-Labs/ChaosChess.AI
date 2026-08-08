using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class GlobalEffectCardTargetStrategyTests
{
    [Theory]
    [InlineData("checkmate_declaration")]
    [InlineData("democracy")]
    [InlineData("destroyer_tank_cards")]
    [InlineData("mutiny")]
    [InlineData("stag_fight")]
    [InlineData("time_reversal")]
    [InlineData("windmill")]
    public void Decide_SelectsSingleNoneTargetActivation(string cardId)
    {
        GameState state = State(PieceColor.White, Card(cardId));
        var strategy = new GlobalEffectCardTargetStrategy(cardId, cardId);

        CardPlanDecisionResult result = strategy.Decide(new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.White));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(1, result.LegalCandidateCount);
        Assert.Equal(cardId, result.SelectedCandidate!.Plan.CardId);
        Assert.Equal(CardTargetKind.None, result.SelectedCandidate.Plan.Target.Kind);
        Assert.Equal(1, result.SelectedCandidate.Score.Total);
        Assert.Equal(cardId + ".base_activation", Assert.Single(result.SelectedCandidate.Score.Components).Code);
    }

    [Fact]
    public void Decide_RejectsMismatchedCard()
    {
        GameState state = State(PieceColor.White, Card("windmill"));
        var strategy = new GlobalEffectCardTargetStrategy("stag_fight", "Stag Fight");

        CardPlanDecisionResult result = strategy.Decide(new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Decide_RejectsActorThatDoesNotMatchSideToMove()
    {
        GameState state = State(PieceColor.White, Card("windmill"));
        var strategy = new GlobalEffectCardTargetStrategy("windmill", "Windmill");

        CardPlanDecisionResult result = strategy.Decide(new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.Black));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, result.SkipCode);
    }

    [Fact]
    public void Constructor_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new GlobalEffectCardTargetStrategy("", "Global"));
        Assert.Throws<ArgumentNullException>(() => new GlobalEffectCardTargetStrategy("windmill", "Windmill", null!));
    }

    private static GameState State(PieceColor sideToMove, CardInfo card)
    {
        return new GameState(
            new BoardState(
                Array.Empty<PieceInfo>(),
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { card },
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "Global", 1);
    }
}
