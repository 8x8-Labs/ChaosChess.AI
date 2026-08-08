using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class OverbearingCardTargetStrategy : ICardTargetStrategy
    {
        private const string OverbearingCardId = "overbearing";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public OverbearingCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public OverbearingCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => OverbearingCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, OverbearingCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the overbearing strategy.");
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
                    "Overbearing has no legal activation candidate.");
            }

            CardPlanScore score = ScoreActivation(context.GameState.BoardState, context.Actor);
            if (!HasComponentValue(score, "overbearing.retreatable_opponents") ||
                score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Overbearing activation is below the activation threshold.",
                    legalCandidates.Count);
            }

            CardPlanCandidate neutralCandidate = legalCandidates[0];
            return CardPlanDecisionResult.Selected(
                new CardPlanCandidate(
                    neutralCandidate.Card,
                    neutralCandidate.Plan,
                    score,
                    neutralCandidate.EnumerationIndex),
                legalCandidates.Count);
        }

        private static CardPlanScore ScoreActivation(BoardState board, PieceColor actor)
        {
            int retreatableOpponents = 0;
            int blockedOpponents = 0;

            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == actor)
                {
                    continue;
                }

                int retreatDirection = piece.Color == PieceColor.White ? -1 : 1;
                int targetRank = piece.Square.Rank + retreatDirection;
                if (targetRank < 0 || targetRank >= Square.BoardSize)
                {
                    blockedOpponents++;
                    continue;
                }

                var target = new Square(piece.Square.File, targetRank);
                if (board.FindPiece(target) != null)
                {
                    blockedOpponents++;
                    continue;
                }

                retreatableOpponents++;
            }

            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    "overbearing.retreatable_opponents",
                    rawValue: retreatableOpponents,
                    weight: 2,
                    "Opponent pieces can be pushed one square backward."),
                new CardPlanScoreComponent(
                    "overbearing.blocked_opponents",
                    rawValue: blockedOpponents,
                    weight: -1,
                    "Opponent pieces cannot be pushed backward.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static bool HasComponentValue(CardPlanScore score, string componentCode)
        {
            foreach (CardPlanScoreComponent component in score.Components)
            {
                if (string.Equals(component.Code, componentCode, StringComparison.Ordinal) &&
                    component.Value > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
