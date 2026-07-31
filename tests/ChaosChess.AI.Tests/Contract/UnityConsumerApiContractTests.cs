using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Fen;
using Xunit;

namespace ChaosChess.AI.Tests.Contract;

public sealed class UnityConsumerApiContractTests
{
    [Fact]
    public void UnityConsumerSurface_CompilesAndPreservesExpectedMembers()
    {
        BoardState boardState = FenParser.Parse("8/8/8/8/8/8/4P3/4K3 w - - 0 1");
        string canonicalFen = FenParser.Serialize(boardState);
        Assert.Equal("8/8/8/8/8/8/4P3/4K3 w - - 0 1", canonicalFen);

        var card = new CardInfo("fire", "Tactical", remainingUses: 1);
        var tileEffect = new TileEffectInfo(
            "fire:e4",
            "Fire",
            new Square(4, 3),
            PieceColor.Black,
            remainingTurns: 2);
        var gameState = new GameState(
            boardState,
            new[] { card },
            new[] { tileEffect });

        var evaluator = new GameStateEvaluator(
            new StaticChessEngine(),
            new EvaluationOptions(searchDepth: 12));
        EvaluationResult evaluation = evaluator.Evaluate(gameState, PieceColor.Black);

        var module = new CardDecisionModule(
            new ConfiguredCardScorer(
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Tactical"] = 10
                }),
            new EloCardProfile(minimumScoreGain: 1, maximumCardsPerTurn: 1));

        CardDecisionResult decision = module.Decide(
            gameState,
            evaluation,
            PieceColor.Black);

        CardUseRecommendation recommendation = Assert.Single(decision.Recommendations);
        Assert.True(decision.ShouldUseCards);
        Assert.Same(card, recommendation.Card);
        Assert.Equal("fire", recommendation.Card.Id);
        Assert.Equal("Tactical", recommendation.Card.Category);
        Assert.Equal(1, recommendation.Card.RemainingUses);
        Assert.True(recommendation.EffectiveGain > 0);
        Assert.Equal(PieceColor.White, boardState.SideToMove);
        Assert.Single(gameState.AvailableCards);
        Assert.Single(gameState.TileEffects);
        Assert.Equal(4, tileEffect.Square.File);
        Assert.Equal(3, tileEffect.Square.Rank);
    }

    private sealed class StaticChessEngine : IChessEngine
    {
        public IReadOnlyList<MoveCandidate> GetTopMoves(
            BoardState boardState,
            int variationCount)
        {
            return new[]
            {
                new MoveCandidate("e2e4", scoreCentipawns: 30, mateIn: null)
            };
        }

        public PositionEvaluation EvaluatePosition(
            BoardState boardState,
            int depth)
        {
            return new PositionEvaluation(
                PieceColor.White,
                scoreCentipawns: 0,
                mateIn: null);
        }

        public bool IsInCheck(BoardState boardState)
        {
            return false;
        }
    }
}
