using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public enum PieceEffectTargetProfile
    {
        General = 0,
        CaptureSetup = 1
    }

    public sealed class PieceEffectCardTargetStrategy : ICardTargetStrategy
    {
        private readonly CardPlanCandidateEnumerator _candidateEnumerator;
        private readonly string _componentPrefix;
        private readonly string _displayName;
        private readonly PieceEffectTargetProfile _profile;

        public PieceEffectCardTargetStrategy(string cardId, string displayName)
            : this(cardId, displayName, PieceEffectTargetProfile.General)
        {
        }

        public PieceEffectCardTargetStrategy(
            string cardId,
            string displayName,
            PieceEffectTargetProfile profile)
            : this(cardId, displayName, profile, new CardPlanCandidateEnumerator())
        {
        }

        public PieceEffectCardTargetStrategy(
            string cardId,
            string displayName,
            PieceEffectTargetProfile profile,
            CardPlanCandidateEnumerator candidateEnumerator)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            EnsureValidProfile(profile);
            CardId = cardId;
            _componentPrefix = cardId;
            _displayName = string.IsNullOrWhiteSpace(displayName) ? cardId : displayName;
            _profile = profile;
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
                    $"{_displayName} has no legal actor piece target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            var scoredCandidates = new List<CardPlanCandidate>(legalCandidates.Count);

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException($"{_displayName} candidate contains no piece target.");
                CardPlanScore score = ScoreCandidate(context.GameState.BoardState, context.Actor, target, topMove);
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
            PieceTargetSnapshot target,
            ParsedMove? topMove)
        {
            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    _componentPrefix + ".target_piece_value",
                    rawValue: GetPieceValue(target.ExpectedKind),
                    weight: 1,
                    "Targeting a higher value actor piece is more valuable."),
                new CardPlanScoreComponent(
                    _componentPrefix + ".center_pressure",
                    rawValue: ScoreCenterPressure(target.Square),
                    weight: 1,
                    $"{_displayName} target is closer to the board center.")
            };

            if (topMove.HasValue && topMove.Value.From == target.Square)
            {
                components.Add(new CardPlanScoreComponent(
                    _componentPrefix + ".engine_source",
                    rawValue: 1,
                    weight: 8,
                    "Current engine top move starts from this piece."));
            }

            if (_profile == PieceEffectTargetProfile.CaptureSetup)
            {
                components.Add(new CardPlanScoreComponent(
                    _componentPrefix + ".engine_capture",
                    rawValue: ScoreEngineCapture(board, actor, target.Square, topMove),
                    weight: 6,
                    "Current engine top move uses this piece to capture an opponent piece."));
            }

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static int ScoreEngineCapture(
            BoardState board,
            PieceColor actor,
            Square targetSquare,
            ParsedMove? topMove)
        {
            if (!topMove.HasValue || topMove.Value.From != targetSquare)
            {
                return 0;
            }

            PieceInfo? captured = board.FindPiece(topMove.Value.To);
            return captured != null && captured.Color != actor ? 1 : 0;
        }

        private static int ScoreCenterPressure(Square square)
        {
            int fileDistance = Math.Min(square.File, Square.BoardSize - 1 - square.File);
            int rankDistance = Math.Min(square.Rank, Square.BoardSize - 1 - square.Rank);
            return Math.Min(fileDistance, rankDistance);
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

        private static void EnsureValidProfile(PieceEffectTargetProfile profile)
        {
            if (profile != PieceEffectTargetProfile.General &&
                profile != PieceEffectTargetProfile.CaptureSetup)
            {
                throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown piece effect target profile.");
            }
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
