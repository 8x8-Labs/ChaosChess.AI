using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;

namespace ChaosChess.AI.Decision
{
    public sealed class CardDecisionModule
    {
        private const int MinimumProjectedScore = -99;
        private const int MaximumProjectedScore = 99;

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

        public CardDecisionResult Decide(
            GameState gameState,
            EvaluationResult currentEvaluation,
            PieceColor perspective,
            CardTargetingModule cardTargetingModule,
            CardTargetingOptions? targetingOptions = null,
            IEnumerable<MoveCandidate>? engineTopMoves = null)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (currentEvaluation == null)
            {
                throw new ArgumentNullException(nameof(currentEvaluation));
            }

            if (cardTargetingModule == null)
            {
                throw new ArgumentNullException(nameof(cardTargetingModule));
            }

            EnsureValidColor(perspective);

            int initialScore = currentEvaluation.TotalScore;
            int currentScore = initialScore;
            var selectedCards = new HashSet<CardInfo>();
            var recommendations = new List<CardUseRecommendation>();

            while (recommendations.Count < _profile.MaximumCardsPerTurn)
            {
                PlanAwareCardScore? bestScore = FindBestPlanAwareScore(
                    gameState,
                    currentEvaluation,
                    perspective,
                    currentScore,
                    selectedCards,
                    cardTargetingModule,
                    targetingOptions,
                    engineTopMoves);

                if (bestScore == null ||
                    bestScore.EffectiveGain < _profile.MinimumScoreGain)
                {
                    break;
                }

                selectedCards.Add(bestScore.CardScore.Card);
                recommendations.Add(new CardUseRecommendation(
                    bestScore.CardScore.Card,
                    bestScore.CardScore.BaseScore,
                    bestScore.ProjectedScore,
                    bestScore.EffectiveGain,
                    bestScore.PlanDecision.SelectedCandidate!.Plan,
                    bestScore.PlanDecision.SelectedCandidate.Score,
                    CardPlanSkipCode.None,
                    planSkipReason: null));
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

        private PlanAwareCardScore? FindBestPlanAwareScore(
            GameState gameState,
            EvaluationResult currentEvaluation,
            PieceColor perspective,
            int currentScore,
            ISet<CardInfo> selectedCards,
            CardTargetingModule cardTargetingModule,
            CardTargetingOptions? targetingOptions,
            IEnumerable<MoveCandidate>? engineTopMoves)
        {
            PlanAwareCardScore? bestScore = null;

            foreach (CardInfo card in gameState.AvailableCards)
            {
                if (card.RemainingUses <= 0 || selectedCards.Contains(card))
                {
                    continue;
                }

                CardPlanDecisionResult planDecision = cardTargetingModule.DecideBestPlan(
                    gameState,
                    card,
                    perspective,
                    targetingOptions,
                    engineTopMoves);

                if (!planDecision.HasSelection)
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

                PlanAwareCardScore planAwareScore = CreatePlanAwareScore(
                    score,
                    currentScore,
                    planDecision);

                if (bestScore == null ||
                    planAwareScore.EffectiveGain > bestScore.EffectiveGain)
                {
                    bestScore = planAwareScore;
                }
            }

            return bestScore;
        }

        private static PlanAwareCardScore CreatePlanAwareScore(
            CardScore cardScore,
            int currentScore,
            CardPlanDecisionResult planDecision)
        {
            int combinedGain = cardScore.EffectiveGain +
                planDecision.SelectedCandidate!.Score.Total;
            int projectedScore = ClampProjectedScore(currentScore + combinedGain);
            int effectiveGain = projectedScore - currentScore;

            return new PlanAwareCardScore(
                cardScore,
                planDecision,
                projectedScore,
                effectiveGain);
        }

        private static int ClampProjectedScore(int score)
        {
            if (score < MinimumProjectedScore)
            {
                return MinimumProjectedScore;
            }

            return score > MaximumProjectedScore ? MaximumProjectedScore : score;
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }

        private sealed class PlanAwareCardScore
        {
            public PlanAwareCardScore(
                CardScore cardScore,
                CardPlanDecisionResult planDecision,
                int projectedScore,
                int effectiveGain)
            {
                CardScore = cardScore ?? throw new ArgumentNullException(nameof(cardScore));
                PlanDecision = planDecision ?? throw new ArgumentNullException(nameof(planDecision));
                ProjectedScore = projectedScore;
                EffectiveGain = effectiveGain;
            }

            public CardScore CardScore { get; }

            public CardPlanDecisionResult PlanDecision { get; }

            public int ProjectedScore { get; }

            public int EffectiveGain { get; }
        }
    }
}
