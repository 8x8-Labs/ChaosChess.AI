using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;

namespace ChaosChess.AI.Decision
{
    public sealed class CardDecisionModule
    {
        private readonly ICardScorer _cardScorer;
        private readonly EloCardProfile _profile;

        public CardDecisionModule(
            ICardScorer cardScorer,
            EloCardProfile? profile = null)
        {
            _cardScorer = cardScorer ?? throw new ArgumentNullException(nameof(cardScorer));
            _profile = profile ?? new EloCardProfile();
        }

        public CardDecisionResult Decide(
            GameState gameState,
            EvaluationResult currentEvaluation,
            PieceColor perspective)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (currentEvaluation == null)
            {
                throw new ArgumentNullException(nameof(currentEvaluation));
            }

            EnsureValidColor(perspective);

            int initialScore = currentEvaluation.TotalScore;
            int currentScore = initialScore;
            var selectedCards = new HashSet<CardInfo>();
            var recommendations = new List<CardUseRecommendation>();

            while (recommendations.Count < _profile.MaximumCardsPerTurn)
            {
                CardScore? bestScore = FindBestScore(
                    gameState,
                    currentEvaluation,
                    perspective,
                    currentScore,
                    selectedCards);

                if (bestScore == null ||
                    bestScore.EffectiveGain < _profile.MinimumScoreGain)
                {
                    break;
                }

                selectedCards.Add(bestScore.Card);
                recommendations.Add(new CardUseRecommendation(
                    bestScore.Card,
                    bestScore.BaseScore,
                    bestScore.ProjectedScore,
                    bestScore.EffectiveGain));
                currentScore = bestScore.ProjectedScore;
            }

            return new CardDecisionResult(
                recommendations,
                initialScore,
                currentScore);
        }

        private CardScore? FindBestScore(
            GameState gameState,
            EvaluationResult currentEvaluation,
            PieceColor perspective,
            int currentScore,
            ISet<CardInfo> selectedCards)
        {
            CardScore? bestScore = null;

            foreach (CardInfo card in gameState.AvailableCards)
            {
                if (card.RemainingUses <= 0 || selectedCards.Contains(card))
                {
                    continue;
                }

                var context = new CardScoringContext(
                    gameState,
                    card,
                    perspective,
                    currentEvaluation,
                    currentScore);
                CardScore score = _cardScorer.Score(context);

                if (score == null)
                {
                    throw new InvalidOperationException("Card scorer returned no score.");
                }

                if (!ReferenceEquals(score.Card, card))
                {
                    throw new InvalidOperationException("Card scorer returned a score for a different card.");
                }

                if (bestScore == null || score.EffectiveGain > bestScore.EffectiveGain)
                {
                    bestScore = score;
                }
            }

            return bestScore;
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
