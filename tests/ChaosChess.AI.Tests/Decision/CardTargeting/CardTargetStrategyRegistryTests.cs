using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class CardTargetStrategyRegistryTests
{
    [Fact]
    public void Constructor_CopiesStrategiesAndLooksUpCaseInsensitively()
    {
        var strategies = new List<ICardTargetStrategy>
        {
            new StubStrategy("fire")
        };
        var registry = new CardTargetStrategyRegistry(strategies);

        strategies.Add(new StubStrategy("portal"));

        Assert.Single(registry.Strategies);
        Assert.True(registry.TryGetStrategy("FIRE", out ICardTargetStrategy? strategy));
        Assert.Equal("fire", strategy!.CardId);
    }

    [Fact]
    public void Constructor_InvalidStrategies_Throw()
    {
        Assert.Throws<ArgumentException>(
            () => new CardTargetStrategyRegistry(new ICardTargetStrategy[] { null! }));
        Assert.Throws<ArgumentException>(
            () => new CardTargetStrategyRegistry(new[] { new StubStrategy(string.Empty) }));
        Assert.Throws<ArgumentException>(
            () => new CardTargetStrategyRegistry(new[]
            {
                new StubStrategy("fire"),
                new StubStrategy("FIRE")
            }));
    }

    [Fact]
    public void Decide_UnsupportedCard_ReturnsUnsupportedSkipCode()
    {
        var registry = new CardTargetStrategyRegistry(new[] { new StubStrategy("unknown") });
        GameState state = State(Card("unknown"));

        CardPlanDecisionResult result = registry.Decide(
            state,
            state.AvailableCards[0],
            PieceColor.White);

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Decide_MissingStrategy_ReturnsMissingStrategySkipCode()
    {
        var registry = new CardTargetStrategyRegistry();
        GameState state = State(Card("fire"));

        CardPlanDecisionResult result = registry.Decide(
            state,
            state.AvailableCards[0],
            PieceColor.White);

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.MissingStrategy, result.SkipCode);
    }

    [Fact]
    public void Decide_ExistingStrategy_ReceivesContextAndReturnsResult()
    {
        var strategy = new StubStrategy("charge");
        var options = new CardTargetingOptions(activationThreshold: 5);
        var registry = new CardTargetStrategyRegistry(new[] { strategy });
        GameState state = State(Card("charge"));
        CardInfo card = state.AvailableCards[0];

        CardPlanDecisionResult result = registry.Decide(
            state,
            card,
            PieceColor.White,
            options);

        Assert.True(result.HasSelection);
        Assert.Same(state, strategy.LastContext!.GameState);
        Assert.Same(card, strategy.LastContext.Card);
        Assert.Equal(PieceColor.White, strategy.LastContext.Actor);
        Assert.Same(options, strategy.LastContext.Options);
    }

    [Fact]
    public void Decide_StrategyReturnsNull_Throws()
    {
        var registry = new CardTargetStrategyRegistry(new[] { new NullResultStrategy("charge") });
        GameState state = State(Card("charge"));

        Assert.Throws<InvalidOperationException>(
            () => registry.Decide(state, state.AvailableCards[0], PieceColor.White));
    }

    [Fact]
    public void Decide_StrategyReturnsDifferentCard_Throws()
    {
        var registry = new CardTargetStrategyRegistry(new[] { new DifferentCardStrategy("charge") });
        GameState state = State(Card("charge"));

        Assert.Throws<InvalidOperationException>(
            () => registry.Decide(state, state.AvailableCards[0], PieceColor.White));
    }

    [Fact]
    public void Context_InvalidArguments_Throw()
    {
        GameState state = State(Card("charge"));
        CardInfo card = state.AvailableCards[0];

        Assert.Throws<ArgumentNullException>(
            () => new CardTargetStrategyContext(null!, card, PieceColor.White));
        Assert.Throws<ArgumentNullException>(
            () => new CardTargetStrategyContext(state, null!, PieceColor.White));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CardTargetStrategyContext(state, card, (PieceColor)99));
    }

    private static GameState State(CardInfo card)
    {
        return new GameState(
            new BoardState(
                Array.Empty<PieceInfo>(),
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { card },
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "Mobility", 1);
    }

    private static CardPlanCandidate Candidate(CardInfo card)
    {
        return new CardPlanCandidate(
            card,
            new CardUsePlan(
                card.Id,
                PieceColor.White,
                CardTargetSelection.None()),
            new CardPlanScore(
                1,
                new[] { new CardPlanScoreComponent("test", 1, "Test.") }),
            enumerationIndex: 0);
    }

    private sealed class StubStrategy : ICardTargetStrategy
    {
        public StubStrategy(string cardId)
        {
            CardId = cardId;
        }

        public string CardId { get; }

        public CardTargetStrategyContext? LastContext { get; private set; }

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            LastContext = context;
            return CardPlanDecisionResult.Selected(Candidate(context.Card));
        }
    }

    private sealed class NullResultStrategy : ICardTargetStrategy
    {
        public NullResultStrategy(string cardId)
        {
            CardId = cardId;
        }

        public string CardId { get; }

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            return null!;
        }
    }

    private sealed class DifferentCardStrategy : ICardTargetStrategy
    {
        public DifferentCardStrategy(string cardId)
        {
            CardId = cardId;
        }

        public string CardId { get; }

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            return CardPlanDecisionResult.Selected(Candidate(Card("fire")));
        }
    }
}
