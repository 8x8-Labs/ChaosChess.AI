using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class FireCardTargetStrategy : ICardTargetStrategy
    {
        private const string FireCardId = "fire";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public FireCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public FireCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => FireCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, FireCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the fire strategy.");
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
                    "Fire has no legal empty square target.");
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
                    "Fire legal candidates are below the activation threshold.");
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
                    "fire.enemy_engine_destination",
                    ScoreEnemyEngineDestination(board, actor, square, topMove),
                    "Fire target matches the opponent engine move destination."),
                new CardPlanScoreComponent(
                    "fire.enemy_engine_adjacent",
                    ScoreEnemyEngineAdjacent(board, actor, square, topMove),
                    "Fire target is adjacent to the opponent engine move destination."),
                new CardPlanScoreComponent(
                    "fire.center_control",
                    ScoreCenterControl(square),
                    "Fire target is closer to the board center."),
                new CardPlanScoreComponent(
                    "fire.own_engine_destination_penalty",
                    ScoreOwnEngineDestinationPenalty(board, actor, square, topMove),
                    "Fire target overlaps the actor engine move destination.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreEnemyEngineDestination(
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
            return movingPiece != null && movingPiece.Color != actor ? 10 : 0;
        }

        private static int ScoreEnemyEngineAdjacent(
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
            return movingPiece != null && movingPiece.Color != actor ? 3 : 0;
        }

        private static int ScoreOwnEngineDestinationPenalty(
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
            return movingPiece != null && movingPiece.Color == actor ? -8 : 0;
        }

        private static int ScoreCenterControl(Square square)
        {
            int distance = Math.Abs(square.File - CenterAnchor) + Math.Abs(square.Rank - CenterAnchor);
            int score = 3 - distance;
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
