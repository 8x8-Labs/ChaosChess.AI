using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class HoneyTrapCardTargetStrategy : ICardTargetStrategy
    {
        private const string HoneyTrapCardId = "honey_trap";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public HoneyTrapCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public HoneyTrapCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => HoneyTrapCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, HoneyTrapCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the honey trap strategy.");
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
                    "Honey Trap has no legal activation candidate.");
            }

            PieceInfo? opponentKing = FindKing(context.GameState.BoardState, ExpectedValueCardScoring.OpponentOf(context.Actor));
            if (opponentKing == null)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Honey Trap could not find the opponent king.",
                    legalCandidates.Count);
            }

            int queenCount = 0;
            int totalGain = 0;
            int totalLoss = 0;
            int totalKingPressure = 0;
            foreach (PieceInfo queen in context.GameState.BoardState.Pieces)
            {
                if (queen.Color != context.Actor || queen.Kind != PieceKind.Queen)
                {
                    continue;
                }

                Square destination = GetDestination(opponentKing.Square, queen.Square);
                queenCount++;
                totalKingPressure += ExpectedValueCardScoring.CenterScore(destination) +
                    Math.Max(0, 7 - ChebyshevDistance(destination, queen.Square));

                PieceInfo? occupant = context.GameState.BoardState.FindPiece(destination);
                if (occupant == null || occupant.Kind == PieceKind.King)
                {
                    continue;
                }

                int value = ExpectedValueCardScoring.PieceValue(occupant.Kind);
                if (occupant.Color == context.Actor)
                {
                    totalLoss += value;
                }
                else
                {
                    totalGain += value;
                }
            }

            if (queenCount == 0)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Honey Trap has no actor queen random candidates.",
                    legalCandidates.Count);
            }

            CardPlanScore score = Score(
                totalGain / queenCount,
                totalLoss / queenCount,
                totalKingPressure / queenCount);
            if (score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Honey Trap expected value is below the activation threshold.",
                    legalCandidates.Count);
            }

            CardPlanCandidate candidate = legalCandidates[0];
            return CardPlanDecisionResult.Selected(
                new CardPlanCandidate(candidate.Card, candidate.Plan, score, candidate.EnumerationIndex),
                legalCandidates.Count);
        }

        private static CardPlanScore Score(
            int expectedCaptureGain,
            int expectedFriendlyLoss,
            int expectedKingPressure)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "honey_trap.expected_capture_gain",
                    rawValue: expectedCaptureGain,
                    weight: 3,
                    "Honey Trap averages enemy piece removal over random queen outcomes."),
                new CardPlanScoreComponent(
                    "honey_trap.expected_friendly_loss",
                    rawValue: expectedFriendlyLoss,
                    weight: -4,
                    "Honey Trap penalizes expected friendly piece removal."),
                new CardPlanScoreComponent(
                    "honey_trap.king_pressure",
                    rawValue: expectedKingPressure,
                    weight: 1,
                    "Honey Trap values pulling the opponent king into queen pressure.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }

        private static PieceInfo? FindKing(BoardState board, PieceColor color)
        {
            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == color && piece.Kind == PieceKind.King)
                {
                    return piece;
                }
            }

            return null;
        }

        private static Square GetDestination(Square king, Square queen)
        {
            int stepFile = Math.Sign(queen.File - king.File);
            int stepRank = Math.Sign(queen.Rank - king.Rank);
            return new Square(king.File + stepFile, king.Rank + stepRank);
        }

        private static int ChebyshevDistance(Square left, Square right)
        {
            return Math.Max(
                Math.Abs(left.File - right.File),
                Math.Abs(left.Rank - right.Rank));
        }
    }
}
