using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class ChargeCardTargetStrategy : ICardTargetStrategy
    {
        private const string ChargeCardId = "charge";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public ChargeCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public ChargeCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => ChargeCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, ChargeCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the charge strategy.");
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
                    "Charge has no legal activation candidate.");
            }

            CardPlanScore score = ScoreActivation(context.GameState.BoardState, context.Actor);
            if (!HasComponentValue(score, "charge.movable_pawns") ||
                score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Charge activation is below the activation threshold.",
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

        private static CardPlanScore ScoreActivation(
            BoardState board,
            PieceColor actor)
        {
            int movablePawns = 0;
            int promotionPawns = 0;
            int blockedPawns = 0;
            int direction = actor == PieceColor.White ? 1 : -1;
            int promotionRank = actor == PieceColor.White ? Square.BoardSize - 1 : 0;

            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color != actor || piece.Kind != PieceKind.Pawn)
                {
                    continue;
                }

                int targetRank = piece.Square.Rank + direction;
                if (targetRank < 0 || targetRank >= Square.BoardSize)
                {
                    blockedPawns++;
                    continue;
                }

                var target = new Square(piece.Square.File, targetRank);
                if (board.FindPiece(target) != null)
                {
                    blockedPawns++;
                    continue;
                }

                movablePawns++;
                if (target.Rank == promotionRank)
                {
                    promotionPawns++;
                }
            }

            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    "charge.movable_pawns",
                    rawValue: movablePawns,
                    weight: 2,
                    "Actor pawns can advance one square."),
                new CardPlanScoreComponent(
                    "charge.promotion_reach",
                    rawValue: promotionPawns,
                    weight: 8,
                    "Actor pawns can reach promotion row."),
                new CardPlanScoreComponent(
                    "charge.blocked_pawns",
                    rawValue: blockedPawns,
                    weight: -1,
                    "Actor pawns are blocked from advancing.")
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
