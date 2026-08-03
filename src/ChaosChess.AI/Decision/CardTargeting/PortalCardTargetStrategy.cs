using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class PortalCardTargetStrategy : ICardTargetStrategy
    {
        private const string PortalCardId = "portal";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public PortalCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public PortalCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => PortalCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, PortalCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the portal strategy.");
            }

            if (context.Actor != context.GameState.BoardState.SideToMove)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.InvalidActor,
                    "Plan actor does not match side to move.");
            }

            IReadOnlyList<CardPlanCandidate> legalPairs = _candidateEnumerator.EnumerateLegalCandidates(
                context.GameState,
                context.Card,
                context.Actor);

            if (legalPairs.Count == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    "Portal has no legal ordered square pair target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            IReadOnlyList<EndpointScore> shortlistedEndpoints = ShortlistEndpoints(
                legalPairs,
                context.GameState.BoardState,
                context.Actor,
                topMove,
                context.Options.MaximumPortalEndpointCandidates);

            if (shortlistedEndpoints.Count < 2)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    "Portal has fewer than two legal endpoint candidates after shortlisting.",
                    legalPairs.Count);
            }

            Dictionary<Square, EndpointScore> endpointScores = ToEndpointScoreMap(shortlistedEndpoints);
            var scoredPairs = new List<CardPlanCandidate>();

            foreach (CardPlanCandidate pair in legalPairs)
            {
                Square first = pair.Plan.Target.Squares[0];
                Square second = pair.Plan.Target.Squares[1];

                if (!endpointScores.TryGetValue(first, out EndpointScore firstScore) ||
                    !endpointScores.TryGetValue(second, out EndpointScore secondScore))
                {
                    continue;
                }

                CardPlanScore score = ScorePair(firstScore, secondScore, first, second);
                scoredPairs.Add(new CardPlanCandidate(
                    pair.Card,
                    pair.Plan,
                    score,
                    pair.EnumerationIndex));
            }

            if (scoredPairs.Count == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoLegalCandidate,
                    "Portal has no legal ordered square pair after endpoint shortlisting.",
                    legalPairs.Count);
            }

            scoredPairs.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredPairs[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Portal legal candidates are below the activation threshold.",
                    scoredPairs.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredPairs.Count);
        }

        private static IReadOnlyList<EndpointScore> ShortlistEndpoints(
            IReadOnlyList<CardPlanCandidate> legalPairs,
            BoardState board,
            PieceColor actor,
            ParsedMove? topMove,
            int maximumCount)
        {
            var endpointScores = new Dictionary<Square, EndpointScore>();

            foreach (CardPlanCandidate pair in legalPairs)
            {
                foreach (Square square in pair.Plan.Target.Squares)
                {
                    if (endpointScores.ContainsKey(square))
                    {
                        continue;
                    }

                    endpointScores.Add(
                        square,
                        ScoreEndpoint(board, actor, square, topMove, endpointScores.Count));
                }
            }

            var ordered = new List<EndpointScore>(endpointScores.Values);
            ordered.Sort(EndpointScore.CompareByRank);

            if (ordered.Count > maximumCount)
            {
                ordered.RemoveRange(maximumCount, ordered.Count - maximumCount);
            }

            return ordered.AsReadOnly();
        }

        private static EndpointScore ScoreEndpoint(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove,
            int enumerationIndex)
        {
            int actorSourceAdjacency = ScoreActorSourceAdjacency(board, actor, square, topMove);
            int actorDestinationAdjacency = ScoreActorDestinationAdjacency(board, actor, square, topMove);
            int centerAccess = ScoreCenterAccess(square);
            int enemyDestinationRisk = ScoreEnemyDestinationRisk(board, actor, square, topMove);

            return new EndpointScore(
                square,
                (actorSourceAdjacency * 5) +
                    (actorDestinationAdjacency * 5) +
                    centerAccess +
                    (enemyDestinationRisk * -3),
                actorSourceAdjacency,
                actorDestinationAdjacency,
                centerAccess,
                enemyDestinationRisk,
                enumerationIndex);
        }

        private static CardPlanScore ScorePair(
            EndpointScore first,
            EndpointScore second,
            Square firstSquare,
            Square secondSquare)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "portal.endpoint_actor_source",
                    rawValue: first.ActorSourceAdjacency + second.ActorSourceAdjacency,
                    weight: 5,
                    "Portal endpoints are adjacent to the actor engine move source."),
                new CardPlanScoreComponent(
                    "portal.endpoint_actor_destination",
                    rawValue: first.ActorDestinationAdjacency + second.ActorDestinationAdjacency,
                    weight: 5,
                    "Portal endpoints are adjacent to the actor engine move destination."),
                new CardPlanScoreComponent(
                    "portal.endpoint_center_access",
                    rawValue: first.CenterAccess + second.CenterAccess,
                    weight: 1,
                    "Portal endpoints improve central access."),
                new CardPlanScoreComponent(
                    "portal.endpoint_enemy_destination_risk",
                    rawValue: first.EnemyDestinationRisk + second.EnemyDestinationRisk,
                    weight: -3,
                    "Portal endpoints avoid the opponent engine move destination."),
                new CardPlanScoreComponent(
                    "portal.endpoint_distance",
                    rawValue: Math.Min(6, ManhattanDistance(firstSquare, secondSquare)),
                    weight: 1,
                    "Portal endpoints connect distant squares.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static Dictionary<Square, EndpointScore> ToEndpointScoreMap(
            IEnumerable<EndpointScore> endpoints)
        {
            var map = new Dictionary<Square, EndpointScore>();

            foreach (EndpointScore endpoint in endpoints)
            {
                map.Add(endpoint.Square, endpoint);
            }

            return map;
        }

        private static int ScoreActorSourceAdjacency(
            BoardState board,
            PieceColor actor,
            Square square,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || ChebyshevDistance(square, topMove.Value.From) != 1)
            {
                return 0;
            }

            PieceInfo? movingPiece = board.FindPiece(topMove.Value.From);
            return movingPiece != null && movingPiece.Color == actor ? 1 : 0;
        }

        private static int ScoreActorDestinationAdjacency(
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
            return movingPiece != null && movingPiece.Color == actor ? 1 : 0;
        }

        private static int ScoreEnemyDestinationRisk(
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
            return movingPiece != null && movingPiece.Color != actor ? 1 : 0;
        }

        private static int ScoreCenterAccess(Square square)
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

        private static int ManhattanDistance(Square left, Square right)
        {
            return Math.Abs(left.File - right.File) + Math.Abs(left.Rank - right.Rank);
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

        private sealed class EndpointScore
        {
            public EndpointScore(
                Square square,
                int total,
                int actorSourceAdjacency,
                int actorDestinationAdjacency,
                int centerAccess,
                int enemyDestinationRisk,
                int enumerationIndex)
            {
                Square = square;
                Total = total;
                ActorSourceAdjacency = actorSourceAdjacency;
                ActorDestinationAdjacency = actorDestinationAdjacency;
                CenterAccess = centerAccess;
                EnemyDestinationRisk = enemyDestinationRisk;
                EnumerationIndex = enumerationIndex;
            }

            public Square Square { get; }

            public int Total { get; }

            public int ActorSourceAdjacency { get; }

            public int ActorDestinationAdjacency { get; }

            public int CenterAccess { get; }

            public int EnemyDestinationRisk { get; }

            public int EnumerationIndex { get; }

            public static int CompareByRank(EndpointScore left, EndpointScore right)
            {
                int scoreComparison = right.Total.CompareTo(left.Total);
                return scoreComparison != 0
                    ? scoreComparison
                    : left.EnumerationIndex.CompareTo(right.EnumerationIndex);
            }
        }
    }
}
