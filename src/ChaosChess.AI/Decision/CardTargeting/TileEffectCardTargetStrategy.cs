using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class TileEffectCardTargetStrategy : ICardTargetStrategy
    {
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;
        private readonly string _componentPrefix;
        private readonly string _displayName;

        public TileEffectCardTargetStrategy(string cardId, string displayName)
            : this(cardId, displayName, new CardPlanCandidateEnumerator())
        {
        }

        public TileEffectCardTargetStrategy(
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
                    $"{_displayName} has no legal empty square target.");
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
                    $"{_displayName} legal candidates are below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private CardPlanScore ScoreCandidate(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove)
        {
            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    _componentPrefix + ".opponent_engine_destination",
                    rawValue: ScoreEngineDestination(board, actor, square, topMove, opponent: true),
                    weight: 8,
                    $"{_displayName} target matches the opponent engine move destination."),
                new CardPlanScoreComponent(
                    _componentPrefix + ".opponent_engine_adjacent",
                    rawValue: ScoreEngineAdjacent(board, actor, square, topMove, opponent: true),
                    weight: 3,
                    $"{_displayName} target is adjacent to the opponent engine move destination."),
                new CardPlanScoreComponent(
                    _componentPrefix + ".center_control",
                    rawValue: ScoreCenterControl(square),
                    weight: 1,
                    $"{_displayName} target is closer to the board center."),
                new CardPlanScoreComponent(
                    _componentPrefix + ".own_engine_destination_penalty",
                    rawValue: ScoreEngineDestination(board, actor, square, topMove, opponent: false),
                    weight: -8,
                    $"{_displayName} target overlaps the actor engine move destination.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreEngineDestination(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove,
            bool opponent)
        {
            if (!topMove.HasValue || topMove.Value.To != square)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            return MatchesMover(actor, movingPiece, opponent) ? 1 : 0;
        }

        private static int ScoreEngineAdjacent(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove,
            bool opponent)
        {
            if (!topMove.HasValue || ChebyshevDistance(square, topMove.Value.To) != 1)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            return MatchesMover(actor, movingPiece, opponent) ? 1 : 0;
        }

        private static bool MatchesMover(PieceColor actor, PieceInfo? movingPiece, bool opponent)
        {
            if (movingPiece == null)
            {
                return false;
            }

            return opponent
                ? movingPiece.Color != actor
                : movingPiece.Color == actor;
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
