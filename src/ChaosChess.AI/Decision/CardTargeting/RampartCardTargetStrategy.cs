using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class RampartCardTargetStrategy : ICardTargetStrategy
    {
        private const string RampartCardId = "rampart";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public RampartCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public RampartCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => RampartCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, RampartCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the rampart strategy.");
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
                    "Rampart has no legal wall square pair target.");
            }

            PieceInfo? actorKing = FindKing(context.GameState.BoardState, context.Actor);
            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            var scoredCandidates = new List<CardPlanCandidate>();

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                Square first = candidate.Plan.Target.Squares[0];
                Square second = candidate.Plan.Target.Squares[1];

                if (actorKing != null &&
                    (ChebyshevDistance(first, actorKing.Square) <= 1 ||
                        ChebyshevDistance(second, actorKing.Square) <= 1))
                {
                    continue;
                }

                CardPlanScore score = ScoreCandidate(context.GameState.BoardState, context.Actor, first, second, topMove);
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
                    "Rampart has no wall pair outside the actor king escape zone.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Rampart legal candidates are below the activation threshold.",
                    scoredCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            BoardState board,
            PieceColor actor,
            Square first,
            Square second,
            ParsedMove? topMove)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "rampart.opponent_engine_destination",
                    rawValue: ScoreOpponentEngineDestination(board, actor, first, second, topMove),
                    weight: 8,
                    "Rampart walls cover the opponent engine move destination."),
                new CardPlanScoreComponent(
                    "rampart.opponent_engine_adjacency",
                    rawValue: ScoreOpponentEngineAdjacency(board, actor, first, second, topMove),
                    weight: 3,
                    "Rampart walls restrict squares adjacent to the opponent engine destination."),
                new CardPlanScoreComponent(
                    "rampart.center_control",
                    rawValue: ScoreCenterControl(first) + ScoreCenterControl(second),
                    weight: 1,
                    "Rampart walls contest central squares.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreOpponentEngineDestination(
            BoardState board,
            PieceColor actor,
            Square first,
            Square second,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || (first != topMove.Value.To && second != topMove.Value.To))
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            return movingPiece != null && movingPiece.Color != actor ? 1 : 0;
        }

        private static int ScoreOpponentEngineAdjacency(
            BoardState board,
            PieceColor actor,
            Square first,
            Square second,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            if (movingPiece == null || movingPiece.Color == actor)
            {
                return 0;
            }

            int score = 0;
            if (ChebyshevDistance(first, topMove.Value.To) == 1)
            {
                score++;
            }

            if (ChebyshevDistance(second, topMove.Value.To) == 1)
            {
                score++;
            }

            return score;
        }

        private static int ScoreCenterControl(Square square)
        {
            int distance = Math.Abs(square.File - CenterAnchor) + Math.Abs(square.Rank - CenterAnchor);
            int score = 2 - distance;
            return score > 0 ? score : 0;
        }

        private static PieceInfo? FindKing(BoardState board, PieceColor actor)
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
