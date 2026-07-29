using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using Xunit;

namespace ChaosChess.AI.Tests.Decision;

public sealed class CardDecisionModuleTests
{
    [Fact]
    public void Decide_NoCardMeetsThreshold_ReturnsNoRecommendation()
    {
        GameState state = CreateState(Card("card.small", "Tactical"));
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(CategoryScores(("Tactical", 4))),
            new EloCardProfile(minimumScoreGain: 5));

        CardDecisionResult result = module.Decide(
            state,
            Evaluation(totalScore: 10),
            PieceColor.White);

        Assert.False(result.ShouldUseCards);
        Assert.Empty(result.Recommendations);
        Assert.Equal(10, result.InitialScore);
        Assert.Equal(10, result.FinalProjectedScore);
    }

    [Fact]
    public void Decide_SelectsHighestEffectiveGain()
    {
        CardInfo low = Card("card.low", "Utility");
        CardInfo high = Card("card.high", "Tactical");
        GameState state = CreateState(low, high);
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(CategoryScores(
                ("Utility", 3),
                ("Tactical", 8))));

        CardDecisionResult result = module.Decide(
            state,
            Evaluation(totalScore: 20),
            PieceColor.White);

        CardUseRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Same(high, recommendation.Card);
        Assert.Equal(8, recommendation.BaseScore);
        Assert.Equal(8, recommendation.EffectiveGain);
        Assert.Equal(28, recommendation.ProjectedScore);
        Assert.Equal(28, result.FinalProjectedScore);
    }

    [Fact]
    public void Decide_CardIdScoreOverridesCategoryScore()
    {
        CardInfo card = Card("card.portal-save", "Utility");
        GameState state = CreateState(card);
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(
                CategoryScores(("Utility", 3)),
                CardScores(("card.portal-save", 12))));

        CardDecisionResult result = module.Decide(
            state,
            Evaluation(totalScore: -20),
            PieceColor.White);

        CardUseRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Same(card, recommendation.Card);
        Assert.Equal(12, recommendation.BaseScore);
        Assert.Equal(-8, recommendation.ProjectedScore);
    }

    [Fact]
    public void Decide_ExcludesCardsWithNoRemainingUses()
    {
        CardInfo exhausted = Card("card.exhausted", "Tactical", remainingUses: 0);
        CardInfo usable = Card("card.usable", "Utility");
        GameState state = CreateState(exhausted, usable);
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(CategoryScores(
                ("Tactical", 20),
                ("Utility", 5))));

        CardDecisionResult result = module.Decide(
            state,
            Evaluation(totalScore: 0),
            PieceColor.Black);

        CardUseRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Same(usable, recommendation.Card);
        Assert.Equal(5, result.FinalProjectedScore);
    }

    [Fact]
    public void Decide_UsesMaximumCardsPerTurnForMultiCardLoop()
    {
        CardInfo first = Card("card.first", "Burst");
        CardInfo second = Card("card.second", "Support");
        CardInfo third = Card("card.third", "Utility");
        GameState state = CreateState(first, second, third);
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(CategoryScores(
                ("Burst", 10),
                ("Support", 7),
                ("Utility", 6))),
            new EloCardProfile(maximumCardsPerTurn: 2));

        CardDecisionResult result = module.Decide(
            state,
            Evaluation(totalScore: 80),
            PieceColor.White);

        Assert.Equal(2, result.Recommendations.Count);
        Assert.Same(first, result.Recommendations[0].Card);
        Assert.Equal(90, result.Recommendations[0].ProjectedScore);
        Assert.Same(second, result.Recommendations[1].Card);
        Assert.Equal(97, result.Recommendations[1].ProjectedScore);
        Assert.Equal(97, result.FinalProjectedScore);
    }

    [Fact]
    public void Decide_PreservesInputOrderWhenEffectiveGainTies()
    {
        CardInfo first = Card("card.first", "A");
        CardInfo second = Card("card.second", "B");
        GameState state = CreateState(first, second);
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(CategoryScores(
                ("A", 5),
                ("B", 5))));

        CardDecisionResult result = module.Decide(
            state,
            Evaluation(totalScore: 0),
            PieceColor.White);

        CardUseRecommendation recommendation = Assert.Single(result.Recommendations);
        Assert.Same(first, recommendation.Card);
    }

    [Fact]
    public void Decide_SameInputReturnsSameRecommendations()
    {
        CardInfo first = Card("card.first", "A");
        CardInfo second = Card("card.second", "B");
        GameState state = CreateState(first, second);
        var module = new CardDecisionModule(
            new ConfiguredCardScorer(CategoryScores(
                ("A", 2),
                ("B", 7))));
        EvaluationResult evaluation = Evaluation(totalScore: 12);

        CardDecisionResult firstResult = module.Decide(
            state,
            evaluation,
            PieceColor.Black);
        CardDecisionResult secondResult = module.Decide(
            state,
            evaluation,
            PieceColor.Black);

        Assert.Equal(firstResult.FinalProjectedScore, secondResult.FinalProjectedScore);
        Assert.Equal(firstResult.Recommendations.Count, secondResult.Recommendations.Count);
        Assert.Same(
            firstResult.Recommendations[0].Card,
            secondResult.Recommendations[0].Card);
    }

    [Fact]
    public void ConfiguredCardScorer_ClampsProjectedScoreBelowTerminalValues()
    {
        CardInfo card = Card("card.large", "Burst");
        var scorer = new ConfiguredCardScorer(CategoryScores(("Burst", 20)));

        CardScore score = scorer.Score(new CardScoringContext(
            CreateState(card),
            card,
            PieceColor.White,
            Evaluation(totalScore: 90),
            currentScore: 90));

        Assert.Equal(99, score.ProjectedScore);
        Assert.Equal(9, score.EffectiveGain);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CardDecisionModule(null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EloCardProfile(minimumScoreGain: -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new EloCardProfile(maximumCardsPerTurn: 0));
        Assert.Throws<ArgumentException>(
            () => new ConfiguredCardScorer(CategoryScores(("", 1))));
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        var module = new CardDecisionModule(new ConfiguredCardScorer());
        GameState state = CreateState(Card("card.test", "Utility"));
        EvaluationResult evaluation = Evaluation(totalScore: 0);

        Assert.Throws<ArgumentNullException>(
            () => module.Decide(null!, evaluation, PieceColor.White));
        Assert.Throws<ArgumentNullException>(
            () => module.Decide(state, null!, PieceColor.White));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => module.Decide(state, evaluation, (PieceColor)99));
    }

    private static GameState CreateState(params CardInfo[] cards)
    {
        var boardState = new BoardState(
            Array.Empty<PieceInfo>(),
            PieceColor.White,
            CastlingRights.None,
            null,
            0,
            1);

        return new GameState(
            boardState,
            cards,
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(
        string id,
        string category,
        int remainingUses = 1)
    {
        return new CardInfo(id, category, remainingUses);
    }

    private static EvaluationResult Evaluation(int totalScore)
    {
        return new EvaluationResult(
            boardScore: 0,
            mateIn: null,
            threat: 0,
            advantage: 0,
            totalScore: totalScore);
    }

    private static IReadOnlyDictionary<string, int> CategoryScores(
        params (string Key, int Value)[] scores)
    {
        return ToDictionary(scores);
    }

    private static IReadOnlyDictionary<string, int> CardScores(
        params (string Key, int Value)[] scores)
    {
        return ToDictionary(scores);
    }

    private static IReadOnlyDictionary<string, int> ToDictionary(
        params (string Key, int Value)[] scores)
    {
        var dictionary = new Dictionary<string, int>();

        foreach ((string key, int value) in scores)
        {
            dictionary.Add(key, value);
        }

        return dictionary;
    }
}
