using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class PeaceZoneCardTargetStrategy : ICardTargetStrategy
    {
        private const string PeaceZoneCardId = "peace_zone";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public PeaceZoneCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public PeaceZoneCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => PeaceZoneCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, PeaceZoneCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the peace zone strategy.");
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
                    "Peace Zone has no legal empty square target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                Square square = candidate.Plan.Target.Squares[0];
                CardPlanScore score = ScoreCandidate(context.GameState.BoardState, context.Actor, square, topMove);
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
                    "Peace Zone legal candidates are below the activation threshold.");
            }

            return CardPlanDecisionResult.Selected(bestCandidate);
        }

        private static CardPlanScore ScoreCandidate(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove)
        {
            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    "peace.actor_engine_destination",
                    ScoreActorEngineDestination(board, actor, square, topMove),
                    "Peace Zone target matches the actor engine move destination."),
                new CardPlanScoreComponent(
                    "peace.actor_engine_adjacent",
                    ScoreActorEngineAdjacent(board, actor, square, topMove),
                    "Peace Zone target is adjacent to the actor engine move destination."),
                new CardPlanScoreComponent(
                    "peace.enemy_capture_buffer",
                    ScoreEnemyCaptureBuffer(board, actor, square, topMove),
                    "Peace Zone target buffers a threatened actor piece from a nearby empty square."),
                new CardPlanScoreComponent(
                    "peace.center_control",
                    ScoreCenterControl(square),
                    "Peace Zone target is closer to the board center.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreActorEngineDestination(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || topMove.Value.To != square)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            return movingPiece != null && movingPiece.Color == actor ? 8 : 0;
        }

        private static int ScoreActorEngineAdjacent(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || ChebyshevDistance(square, topMove.Value.To) != 1)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            return movingPiece != null && movingPiece.Color == actor ? 3 : 0;
        }

        private static int ScoreEnemyCaptureBuffer(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || ChebyshevDistance(square, topMove.Value.To) != 1)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            PieceInfo? targetPiece = board.FindPiece(topMove.Value.To);
            return movingPiece != null &&
                targetPiece != null &&
                movingPiece.Color != actor &&
                targetPiece.Color == actor
                    ? 6
                    : 0;
        }

        private static int ScoreCenterControl(Square square)
        {
            int distance = Math.Abs(square.File - CenterAnchor) + Math.Abs(square.Rank - CenterAnchor);
            int score = 2 - distance;
            return score > 0 ? score : 0;
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

        private static int ChebyshevDistance(Square left, Square right)
        {
            return Math.Max(
                Math.Abs(left.File - right.File),
                Math.Abs(left.Rank - right.Rank));
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
