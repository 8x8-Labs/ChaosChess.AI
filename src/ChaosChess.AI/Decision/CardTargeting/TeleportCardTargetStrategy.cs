using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class TeleportCardTargetStrategy : ICardTargetStrategy
    {
        private const string TeleportCardId = "teleport";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public TeleportCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public TeleportCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => TeleportCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, TeleportCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the teleport strategy.");
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
                    "Teleport has no legal pawn and empty square target.");
            }

            var scoredCandidates = new List<CardPlanCandidate>();
            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot pawn = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException("Teleport candidate contains no piece target.");
                Square destination = candidate.Plan.Target.Squares[0];
                int advancement = ScoreForwardAdvancement(context.Actor, pawn.Square, destination);
                if (advancement <= 0)
                {
                    continue;
                }

                CardPlanScore score = ScoreCandidate(context.Actor, pawn.Square, destination, advancement);
                scoredCandidates.Add(new CardPlanCandidate(
                    candidate.Card,
                    candidate.Plan,
                    score,
                    candidate.EnumerationIndex));
            }

            if (scoredCandidates.Count == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Teleport has no forward pawn relocation candidate.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Teleport legal candidates are below the activation threshold.",
                    scoredCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            PieceColor actor,
            Square pawnSquare,
            Square destination,
            int advancement)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "teleport.forward_advancement",
                    rawValue: advancement,
                    weight: 4,
                    "Teleport advances an actor pawn toward promotion."),
                new CardPlanScoreComponent(
                    "teleport.promotion_pressure",
                    rawValue: ScorePromotionPressure(actor, destination),
                    weight: 3,
                    "Teleport places an actor pawn closer to promotion."),
                new CardPlanScoreComponent(
                    "teleport.center_access",
                    rawValue: ScoreCenterAccess(destination),
                    weight: 1,
                    "Teleport keeps the pawn near central files.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreForwardAdvancement(
            PieceColor actor,
            Square source,
            Square destination)
        {
            return actor == PieceColor.White
                ? destination.Rank - source.Rank
                : source.Rank - destination.Rank;
        }

        private static int ScorePromotionPressure(PieceColor actor, Square square)
        {
            int promotionDistance = actor == PieceColor.White
                ? Square.BoardSize - 1 - square.Rank
                : square.Rank;

            int score = 4 - promotionDistance;
            return score > 0 ? score : 0;
        }

        private static int ScoreCenterAccess(Square square)
        {
            int distance = Math.Abs(square.File - CenterAnchor);
            int score = 3 - distance;
            return score > 0 ? score : 0;
        }
    }
}
