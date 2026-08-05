using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class PieceValueCardTargetStrategy : ICardTargetStrategy
    {
        private readonly CardPlanCandidateEnumerator _candidateEnumerator;
        private readonly string _componentPrefix;
        private readonly string _displayName;

        public PieceValueCardTargetStrategy(string cardId, string displayName)
            : this(cardId, displayName, new CardPlanCandidateEnumerator())
        {
        }

        public PieceValueCardTargetStrategy(
            string cardId,
            string displayName,
            CardPlanCandidateEnumerator candidateEnumerator)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            CardId = cardId;
            _componentPrefix = cardId;
            _displayName = string.IsNullOrWhiteSpace(displayName) ? cardId : displayName;
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId { get; }

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, CardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the {CardId} strategy.");
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
                    $"{_displayName} has no legal piece target.");
            }

            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);
            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException($"{_displayName} candidate contains no piece target.");
                CardPlanScore score = ScoreCandidate(target);
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
                    $"{_displayName} legal candidates are below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private CardPlanScore ScoreCandidate(PieceTargetSnapshot target)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    _componentPrefix + ".target_piece_value",
                    rawValue: GetPieceValue(target.ExpectedKind),
                    weight: 1,
                    "Targeting a higher value piece is more valuable.")
            };

            return new CardPlanScore(components[0].Value, components);
        }

        private static int GetPieceValue(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return 1;
                case PieceKind.Knight:
                case PieceKind.Bishop:
                case PieceKind.King:
                    return 3;
                case PieceKind.Rook:
                    return 5;
                case PieceKind.KnightRider:
                    return 7;
                case PieceKind.Queen:
                case PieceKind.Chancellor:
                    return 9;
                case PieceKind.Amazon:
                    return 13;
                default:
                    return 0;
            }
        }
    }
}
