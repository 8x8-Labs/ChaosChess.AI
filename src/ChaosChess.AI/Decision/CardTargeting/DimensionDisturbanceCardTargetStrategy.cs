using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class DimensionDisturbanceCardTargetStrategy : ICardTargetStrategy
    {
        private const string DimensionDisturbanceCardId = "dimension_disturbance";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public DimensionDisturbanceCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public DimensionDisturbanceCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => DimensionDisturbanceCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, DimensionDisturbanceCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the dimension disturbance strategy.");
            }

            if (context.Actor != context.GameState.BoardState.SideToMove)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.InvalidActor,
                    "Plan actor does not match side to move.");
            }

            IReadOnlyList<CardPlanCandidate> legalCandidates = _candidateEnumerator.EnumerateLegalCandidates(
                context.GameState,
                context.Card,
                context.Actor);
            if (legalCandidates.Count == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    "Dimension Disturbance has no legal opponent piece pair target.");
            }

            int averageEmptyCenter = AverageEmptyCenter(context.GameState.BoardState);
            var scoredCandidates = new List<CardPlanCandidate>();

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                CardPlanScore score = ScoreCandidate(candidate.Plan.Target.Pieces, averageEmptyCenter);
                scoredCandidates.Add(new CardPlanCandidate(
                    candidate.Card,
                    candidate.Plan,
                    score,
                    candidate.EnumerationIndex));
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Dimension Disturbance expected value is below the activation threshold.",
                    scoredCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            IReadOnlyList<PieceTargetSnapshot> targets,
            int averageEmptyCenter)
        {
            int material = 0;
            int displacement = 0;
            foreach (PieceTargetSnapshot target in targets)
            {
                material += ExpectedValueCardScoring.PieceValue(target.ExpectedKind);
                int currentCenter = ExpectedValueCardScoring.CenterScore(target.Square);
                displacement += Math.Max(0, currentCenter - averageEmptyCenter);
            }

            var components = new[]
            {
                new CardPlanScoreComponent(
                    "dimension_disturbance.target_material",
                    rawValue: material,
                    weight: 2,
                    "Dimension Disturbance favors high-value opponent pieces."),
                new CardPlanScoreComponent(
                    "dimension_disturbance.expected_position_loss",
                    rawValue: displacement,
                    weight: 3,
                    "Dimension Disturbance averages selected pieces' likely positional loss over empty destinations.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int AverageEmptyCenter(BoardState board)
        {
            int total = 0;
            int count = 0;

            for (int rank = 0; rank < Square.BoardSize; rank++)
            {
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    var square = new Square(file, rank);
                    if (board.FindPiece(square) != null)
                    {
                        continue;
                    }

                    total += ExpectedValueCardScoring.CenterScore(square);
                    count++;
                }
            }

            return count == 0 ? 0 : total / count;
        }
    }
}
