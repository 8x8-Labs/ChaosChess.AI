using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class PawnMovementOverrideCardTargetStrategy : ICardTargetStrategy
    {
        private readonly CardPlanCandidateEnumerator _candidateEnumerator;
        private readonly string _componentPrefix;
        private readonly string _displayName;

        public PawnMovementOverrideCardTargetStrategy(string cardId, string displayName)
            : this(cardId, displayName, new CardPlanCandidateEnumerator())
        {
        }

        public PawnMovementOverrideCardTargetStrategy(
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

            IReadOnlyList<CardPlanCandidate> legalCandidates = EnumerateLegalCandidates(
                context.GameState,
                context.Card,
                context.Actor);

            if (legalCandidates.Count == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    $"{_displayName} has no legal actor pawn target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException($"{_displayName} candidate contains no piece target.");
                CardPlanScore score = ScoreCandidate(context.Actor, target.Square, topMove);
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

        private IReadOnlyList<CardPlanCandidate> EnumerateLegalCandidates(
            GameState gameState,
            CardInfo card,
            PieceColor actor)
        {
            return _candidateEnumerator.EnumerateLegalCandidates(gameState, card, actor);
        }

        private CardPlanScore ScoreCandidate(
            PieceColor actor,
            Square pawnSquare,
            ParsedMove? topMove)
        {
            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    _componentPrefix + ".actor_pawn",
                    rawValue: 1,
                    weight: 1,
                    $"{_displayName} can target one actor pawn."),
                new CardPlanScoreComponent(
                    _componentPrefix + ".promotion_pressure",
                    rawValue: ScorePromotionPressure(actor, pawnSquare),
                    weight: 1,
                    "Pawn has nearby promotion pressure that can benefit from expanded movement.")
            };

            if (topMove.HasValue && topMove.Value.From == pawnSquare)
            {
                components.Add(new CardPlanScoreComponent(
                    _componentPrefix + ".engine_source",
                    rawValue: 1,
                    weight: 8,
                    "Current engine top move starts from this pawn."));
            }

            if (topMove.HasValue && IsNearEngineDestination(pawnSquare, topMove.Value.To))
            {
                components.Add(new CardPlanScoreComponent(
                    _componentPrefix + ".engine_destination_relation",
                    rawValue: 1,
                    weight: 4,
                    "Current engine top move destination is near this pawn's expanded movement lane."));
            }

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScorePromotionPressure(PieceColor actor, Square pawnSquare)
        {
            int promotionDistance = actor == PieceColor.White
                ? Square.BoardSize - 1 - pawnSquare.Rank
                : pawnSquare.Rank;

            int score = 3 - promotionDistance;
            return score > 0 ? score : 0;
        }

        private static bool IsNearEngineDestination(Square from, Square to)
        {
            return Math.Abs(from.File - to.File) <= 1 &&
                Math.Abs(from.Rank - to.Rank) <= 2 &&
                from != to;
        }

        private static ParsedMove? TryParseFirstEngineMove(
            IReadOnlyList<MoveCandidate> engineTopMoves)
        {
            foreach (MoveCandidate move in engineTopMoves)
            {
                if (TryParseUciMove(move.UciMove, out ParsedMove parsedMove))
                {
                    return parsedMove;
                }
            }

            return null;
        }

        private static bool TryParseUciMove(string uciMove, out ParsedMove parsedMove)
        {
            parsedMove = default;

            if (uciMove == null || (uciMove.Length != 4 && uciMove.Length != 5))
            {
                return false;
            }

            if (!Square.TryParse(uciMove.Substring(0, 2), out Square from) ||
                !Square.TryParse(uciMove.Substring(2, 2), out Square to))
            {
                return false;
            }

            parsedMove = new ParsedMove(from, to);
            return true;
        }

        private readonly struct ParsedMove
        {
            public ParsedMove(Square from, Square to)
            {
                From = from;
                To = to;
            }

            public Square From { get; }

            public Square To { get; }
        }
    }
}
