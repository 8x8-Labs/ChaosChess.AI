using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class ChaoticKnightCardTargetStrategy : ICardTargetStrategy
    {
        private const string ChaoticKnightCardId = "chaotic_knight";
        private const int RandomMoveRadius = 2;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public ChaoticKnightCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public ChaoticKnightCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => ChaoticKnightCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, ChaoticKnightCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the chaotic knight strategy.");
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
                    "Chaotic Knight has no legal actor knight target.");
            }

            ParsedMove? topMove = TryParseFirstEngineMove(context.EngineTopMoves);
            var scoredCandidates = new List<CardPlanCandidate>();

            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException("Chaotic Knight candidate contains no piece target.");

                if (!TryScoreCandidate(context.GameState.BoardState, target, topMove, out CardPlanScore score))
                {
                    continue;
                }

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
                    "Chaotic Knight targets have no random empty destination.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Chaotic Knight expected value is below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private static bool TryScoreCandidate(
            BoardState board,
            PieceTargetSnapshot target,
            ParsedMove? topMove,
            out CardPlanScore score)
        {
            int destinationCount = 0;
            int centerImprovementTotal = 0;
            int currentCenter = ExpectedValueCardScoring.CenterScore(target.Square);

            for (int rankOffset = -RandomMoveRadius; rankOffset <= RandomMoveRadius; rankOffset++)
            {
                for (int fileOffset = -RandomMoveRadius; fileOffset <= RandomMoveRadius; fileOffset++)
                {
                    if (fileOffset == 0 && rankOffset == 0)
                    {
                        continue;
                    }

                    int file = target.Square.File + fileOffset;
                    int rank = target.Square.Rank + rankOffset;
                    if (file < 0 || file >= Square.BoardSize || rank < 0 || rank >= Square.BoardSize)
                    {
                        continue;
                    }

                    var destination = new Square(file, rank);
                    if (board.FindPiece(destination) != null)
                    {
                        continue;
                    }

                    destinationCount++;
                    centerImprovementTotal += Math.Max(
                        0,
                        ExpectedValueCardScoring.CenterScore(destination) - currentCenter);
                }
            }

            if (destinationCount == 0)
            {
                score = new CardPlanScore(
                    0,
                    new[]
                    {
                        new CardPlanScoreComponent(
                            "chaotic_knight.no_random_destination",
                            0,
                            "Chaotic Knight target has no random empty destination.")
                    });
                return false;
            }

            int centerGain = (centerImprovementTotal * 10) / destinationCount;
            int destinationOptionScore = centerGain > 0
                ? Math.Min(destinationCount, 4)
                : 0;

            var components = new List<CardPlanScoreComponent>
            {
                new CardPlanScoreComponent(
                    "chaotic_knight.expected_center_gain",
                    rawValue: centerGain,
                    weight: 3,
                    "Chaotic Knight averages the target knight's positive random 5x5 relocation value."),
                new CardPlanScoreComponent(
                    "chaotic_knight.random_destination_count",
                    rawValue: destinationOptionScore,
                    weight: 1,
                    "Chaotic Knight has more useful random empty destinations.")
            };

            if (topMove.HasValue && topMove.Value.From == target.Square)
            {
                components.Add(new CardPlanScoreComponent(
                    "chaotic_knight.engine_source",
                    rawValue: 1,
                    weight: 8,
                    "Current engine top move starts from this knight."));
            }

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            score = new CardPlanScore(total, components);
            return true;
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
            }

            public Square From { get; }
        }
    }
}
