using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class AgileCardTargetStrategy : ICardTargetStrategy
    {
        private const string AgileCardId = "agile";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public AgileCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public AgileCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => AgileCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, AgileCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the agile strategy.");
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
                    "Agile has no legal actor pawn target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException("Agile candidate contains no piece target.");
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
                    "Agile legal candidates are below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            PieceColor actor,
            Square pawnSquare,
            ParsedMove? topMove)
        {
            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    "agile.actor_pawn",
                    rawValue: 1,
                    weight: 1,
                    "Agile can target one actor pawn."),
                new CardPlanScoreComponent(
                    "agile.promotion_pressure",
                    rawValue: ScorePromotionPressure(actor, pawnSquare),
                    weight: 1,
                    "Pawn has nearby promotion pressure that can benefit from expanded capture lanes.")
            };

            if (topMove.HasValue && topMove.Value.From == pawnSquare)
            {
                components.Add(new CardPlanScoreComponent(
                    "agile.engine_source",
                    rawValue: 1,
                    weight: 8,
                    "Current engine top move starts from this pawn."));
            }

            if (topMove.HasValue && IsDiagonalAdjacent(pawnSquare, topMove.Value.To))
            {
                components.Add(new CardPlanScoreComponent(
                    "agile.engine_destination_relation",
                    rawValue: 1,
                    weight: 4,
                    "Current engine top move destination is adjacent to this pawn's agile capture lane."));
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

        private static bool IsDiagonalAdjacent(Square from, Square to)
        {
            return Math.Abs(from.File - to.File) == 1 &&
                Math.Abs(from.Rank - to.Rank) == 1;
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
