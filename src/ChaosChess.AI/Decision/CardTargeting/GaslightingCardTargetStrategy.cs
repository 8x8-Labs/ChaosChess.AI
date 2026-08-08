using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class GaslightingCardTargetStrategy : ICardTargetStrategy
    {
        private const string GaslightingCardId = "gaslighting";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public GaslightingCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public GaslightingCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => GaslightingCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, GaslightingCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the gaslighting strategy.");
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
                    "Gaslighting has no legal activation candidate.");
            }

            int candidateCount = 0;
            int totalMaterialSwing = 0;
            foreach (PieceInfo piece in context.GameState.BoardState.Pieces)
            {
                if (piece.Color == context.Actor ||
                    !ExpectedValueCardScoring.IsGaslightingTargetKind(piece.Kind))
                {
                    continue;
                }

                candidateCount++;
                totalMaterialSwing += ExpectedValueCardScoring.PieceValue(piece.Kind) * 2;
            }

            if (candidateCount == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Gaslighting has no random opponent conversion candidates.",
                    legalCandidates.Count);
            }

            int expectedMaterialSwing = totalMaterialSwing / candidateCount;
            CardPlanScore score = Score(expectedMaterialSwing, candidateCount);
            if (score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Gaslighting expected value is below the activation threshold.",
                    legalCandidates.Count);
            }

            CardPlanCandidate candidate = legalCandidates[0];
            return CardPlanDecisionResult.Selected(
                new CardPlanCandidate(candidate.Card, candidate.Plan, score, candidate.EnumerationIndex),
                legalCandidates.Count);
        }

        private static CardPlanScore Score(int expectedMaterialSwing, int candidateCount)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "gaslighting.expected_material_swing",
                    rawValue: expectedMaterialSwing,
                    weight: 2,
                    "Gaslighting averages the material swing over every random conversion candidate."),
                new CardPlanScoreComponent(
                    "gaslighting.random_candidate_count",
                    rawValue: candidateCount,
                    weight: 1,
                    "Gaslighting has multiple legal random conversion outcomes.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }
    }
}
