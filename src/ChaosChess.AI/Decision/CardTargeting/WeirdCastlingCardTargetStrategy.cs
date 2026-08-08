using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class WeirdCastlingCardTargetStrategy : ICardTargetStrategy
    {
        private const string WeirdCastlingCardId = "weird_castling";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public WeirdCastlingCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public WeirdCastlingCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => WeirdCastlingCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, WeirdCastlingCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the weird castling strategy.");
            }

            if (context.Actor != context.GameState.BoardState.SideToMove)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.InvalidActor,
                    "Plan actor does not match side to move.");
            }

            PieceInfo? actorKing = FindActorKing(context.GameState.BoardState, context.Actor);
            if (actorKing == null)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    "Weird Castling requires an actor king.");
            }

            IReadOnlyList<CardPlanCandidate> legalCandidates = _candidateEnumerator.EnumerateLegalCandidates(
                context.GameState,
                context.Card,
                context.Actor);
            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException("Weird Castling candidate contains no piece target.");
                CardPlanScore score = ScoreCandidate(context.Actor, actorKing.Square, target);
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
                    "Weird Castling has no legal actor piece target.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Weird Castling legal candidates are below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            PieceColor actor,
            Square kingSquare,
            PieceTargetSnapshot target)
        {
            int forwardDelta = actor == PieceColor.White
                ? target.Square.Rank - kingSquare.Rank
                : kingSquare.Rank - target.Square.Rank;
            int forwardImprovement = forwardDelta > 0 ? Math.Min(4, forwardDelta) : 0;

            var components = new[]
            {
                new CardPlanScoreComponent(
                    "weird_castling.king_forward_improvement",
                    rawValue: forwardImprovement,
                    weight: 4,
                    "Weird Castling moves the actor king away from the back rank."),
                new CardPlanScoreComponent(
                    "weird_castling.king_center_access",
                    rawValue: ScoreCenterAccess(target.Square),
                    weight: 1,
                    "Weird Castling moves the actor king toward central files."),
                new CardPlanScoreComponent(
                    "weird_castling.target_piece_value_penalty",
                    rawValue: GetPieceValue(target.ExpectedKind),
                    weight: -1,
                    "Weird Castling prefers swapping with lower value actor pieces.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static PieceInfo? FindActorKing(BoardState board, PieceColor actor)
        {
            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == actor && piece.Kind == PieceKind.King)
                {
                    return piece;
                }
            }

            return null;
        }

        private static int ScoreCenterAccess(Square square)
        {
            int distance = Math.Abs(square.File - CenterAnchor);
            int score = 3 - distance;
            return score > 0 ? score : 0;
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
                case PieceKind.Chancellor:
                    return 9;
                case PieceKind.Amazon:
                    return 13;
                case PieceKind.KnightRider:
                    return 7;
                default:
                    return 0;
            }
        }
    }
}
