using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class PositionSwapCardTargetStrategy : ICardTargetStrategy
    {
        private const string PositionSwapCardId = "position_swap";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public PositionSwapCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public PositionSwapCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => PositionSwapCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, PositionSwapCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the position swap strategy.");
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
                    "Position Swap has no legal activation candidate.");
            }

            int materialSwing = ScoreMaterial(context.GameState.BoardState, OpponentOf(context.Actor)) -
                ScoreMaterial(context.GameState.BoardState, context.Actor);
            if (materialSwing <= 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Position Swap is not beneficial when the actor is not behind in material.",
                    legalCandidates.Count);
            }

            CardPlanScore score = ScoreActivation(materialSwing);
            if (score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Position Swap activation is below the activation threshold.",
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

        private static CardPlanScore ScoreActivation(int materialSwing)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "position_swap.material_swing",
                    rawValue: materialSwing,
                    weight: 2,
                    "Position Swap converts opponent material advantage into actor material.")
            };

            return new CardPlanScore(components[0].Value, components);
        }

        private static int ScoreMaterial(BoardState board, PieceColor color)
        {
            int total = 0;
            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == color)
                {
                    total += GetPieceValue(piece.Kind);
                }
            }

            return total;
        }

        private static int GetPieceValue(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return 1;
                case PieceKind.Knight:
                case PieceKind.Bishop:
                    return 3;
                case PieceKind.Rook:
                    return 5;
                case PieceKind.Queen:
                    return 9;
                case PieceKind.Amazon:
                    return 13;
                case PieceKind.Chancellor:
                case PieceKind.KnightRider:
                    return 8;
                default:
                    return 0;
            }
        }

        private static PieceColor OpponentOf(PieceColor actor)
        {
            return actor == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }
    }
}
