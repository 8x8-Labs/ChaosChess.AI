using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class SyncCardTargetStrategy : ICardTargetStrategy
    {
        private const string SyncCardId = "sync";
        private const int CenterAnchor = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public SyncCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public SyncCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => SyncCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, SyncCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the sync strategy.");
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
                    "Sync has no legal empty square target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves, context.Actor);
            var scoredCandidates = new List<CardPlanCandidate>();

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                Square square = candidate.Plan.Target.Squares[0];
                Square mirrored = CreateMirroredSquare(square);
                PieceInfo? linkedPiece = context.GameState.BoardState.FindPiece(mirrored);

                if (linkedPiece == null || linkedPiece.Color != context.Actor)
                {
                    continue;
                }

                CardPlanScore score = ScoreCandidate(square, linkedPiece, topMove);
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
                    "Sync has no legal target mirrored from an actor piece.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Sync legal candidates are below the activation threshold.",
                    scoredCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredCandidates.Count);
        }

        private static CardPlanScore ScoreCandidate(
            Square square,
            PieceInfo linkedPiece,
            ParsedMove? topMove)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "sync.linked_piece_present",
                    rawValue: 1,
                    weight: 4,
                    "Sync target is mirrored from an actor piece."),
                new CardPlanScoreComponent(
                    "sync.linked_piece_value",
                    rawValue: PieceValue(linkedPiece.Kind),
                    weight: 1,
                    "Sync preserves mobility upside for a valuable linked actor piece."),
                new CardPlanScoreComponent(
                    "sync.actor_engine_destination",
                    rawValue: ScoreActorEngineDestination(square, linkedPiece.Color, topMove),
                    weight: 3,
                    "Sync target matches the actor engine move destination."),
                new CardPlanScoreComponent(
                    "sync.center_access",
                    rawValue: ScoreCenterControl(square),
                    weight: 1,
                    "Sync target keeps the linked pair near the center.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreActorEngineDestination(
            Square square,
            PieceColor actor,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || topMove.Value.To != square)
            {
                return 0;
            }

            return topMove.Value.Actor == actor ? 1 : 0;
        }

        private static int ScoreCenterControl(Square square)
        {
            int distance = Math.Abs(square.File - CenterAnchor) + Math.Abs(square.Rank - CenterAnchor);
            int score = 2 - distance;
            return score > 0 ? score : 0;
        }

        private static int PieceValue(PieceKind kind)
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
                    return 12;
                case PieceKind.King:
                    return 2;
                default:
                    return 1;
            }
        }

        private static Square CreateMirroredSquare(Square square)
        {
            return new Square(Square.BoardSize - 1 - square.File, square.Rank);
        }

        private static ParsedMove? TryParseFirstEngineMove(
            IReadOnlyList<MoveCandidate> engineTopMoves,
            PieceColor actor)
        {
            foreach (MoveCandidate move in engineTopMoves)
            {
                if (TryParseUciMove(move.UciMove, actor, out ParsedMove parsedMove))
                {
                    return parsedMove;
                }
            }

            return null;
        }

        private static bool TryParseUciMove(
            string uciMove,
            PieceColor actor,
            out ParsedMove parsedMove)
        {
            parsedMove = default;

            if (string.IsNullOrWhiteSpace(uciMove) || uciMove.Length < 4)
            {
                return false;
            }

            if (!Square.TryParse(uciMove.Substring(0, 2), out Square from) ||
                !Square.TryParse(uciMove.Substring(2, 2), out Square to))
            {
                return false;
            }

            parsedMove = new ParsedMove(from, to, actor);
            return true;
        }

        private readonly struct ParsedMove
        {
            public ParsedMove(Square from, Square to, PieceColor actor)
            {
                From = from;
                To = to;
                Actor = actor;
            }

            public Square From { get; }

            public Square To { get; }

            public PieceColor Actor { get; }
        }
    }
}
