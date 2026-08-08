using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class ReviveCardTargetStrategy : ICardTargetStrategy
    {
        private const string ReviveCardId = "revive";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public ReviveCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public ReviveCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => ReviveCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, ReviveCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the revive strategy.");
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
                    "Revive has no legal empty square target.");
            }

            PieceKind revivedKind = SelectRevivedKind(context.GameState.CapturedPieces.GetPieces(context.Actor));
            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                Square square = candidate.Plan.Target.Squares[0];
                CardPlanScore score = ScoreCandidate(context.GameState.BoardState, context.Actor, revivedKind, square);
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
                    "Revive legal candidates are below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            BoardState board,
            PieceColor actor,
            PieceKind revivedKind,
            Square square)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "revive.piece_value",
                    ExpectedValueCardScoring.PieceValue(revivedKind),
                    weight: 4,
                    "Revive restores the highest-value captured actor piece."),
                new CardPlanScoreComponent(
                    "revive.center_control",
                    ExpectedValueCardScoring.CenterScore(square),
                    weight: 1,
                    "Revive target is closer to the board center."),
                new CardPlanScoreComponent(
                    "revive.king_cover",
                    IsNearActorKing(board, actor, square) ? 1 : 0,
                    weight: revivedKind == PieceKind.Wall ? 2 : 1,
                    "Revive target can reinforce the actor king.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static PieceKind SelectRevivedKind(IReadOnlyList<PieceKind> capturedPieces)
        {
            if (capturedPieces.Count == 0)
            {
                return PieceKind.Wall;
            }

            PieceKind best = capturedPieces[0];
            int bestValue = ExpectedValueCardScoring.PieceValue(best);

            for (int i = 1; i < capturedPieces.Count; i++)
            {
                int value = ExpectedValueCardScoring.PieceValue(capturedPieces[i]);
                if (value > bestValue)
                {
                    best = capturedPieces[i];
                    bestValue = value;
                }
            }

            return best;
        }

        private static bool IsNearActorKing(BoardState board, PieceColor actor, Square square)
        {
            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == actor &&
                    piece.Kind == PieceKind.King &&
                    Math.Max(
                        Math.Abs(piece.Square.File - square.File),
                        Math.Abs(piece.Square.Rank - square.Rank)) <= 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
