using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CastleKnightCardTargetStrategy : ICardTargetStrategy
    {
        private const string CastleKnightCardId = "castle_knight";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public CastleKnightCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public CastleKnightCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => CastleKnightCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, CastleKnightCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the castle knight strategy.");
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
            var scoredCandidates = new List<CardPlanCandidate>();

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException("Castle Knight candidate contains no piece target.");
                if (!TryFindNearestRook(context.GameState.BoardState, target, out _, out int squaredDistance))
                {
                    continue;
                }

                CardPlanScore score = ScoreCandidate(squaredDistance);
                scoredCandidates.Add(new CardPlanCandidate(
                    candidate.Card,
                    candidate.Plan,
                    score,
                    candidate.EnumerationIndex));
            }

            if (scoredCandidates.Count == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    "Castle Knight has no actor knight with a matching actor rook.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Castle Knight legal candidates are below the activation threshold.",
                    scoredCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(int squaredDistance)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "castle_knight.chancellor_gain",
                    rawValue: 1,
                    weight: 6,
                    "Castle Knight upgrades the nearest actor rook into a chancellor and removes the selected knight."),
                new CardPlanScoreComponent(
                    "castle_knight.merge_distance",
                    rawValue: Math.Max(0, 14 - squaredDistance),
                    weight: 1,
                    "Castle Knight prefers a knight close to its merge rook.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static bool TryFindNearestRook(
            BoardState board,
            PieceTargetSnapshot target,
            out PieceInfo? nearest,
            out int squaredDistance)
        {
            nearest = null;
            squaredDistance = int.MaxValue;

            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color != target.ExpectedColor || piece.Kind != PieceKind.Rook)
                {
                    continue;
                }

                int distance = SquaredDistance(target.Square, piece.Square);
                if (distance < squaredDistance)
                {
                    nearest = piece;
                    squaredDistance = distance;
                }
            }

            return nearest != null;
        }

        private static int SquaredDistance(Square left, Square right)
        {
            int file = left.File - right.File;
            int rank = left.Rank - right.Rank;
            return (file * file) + (rank * rank);
        }
    }
}
