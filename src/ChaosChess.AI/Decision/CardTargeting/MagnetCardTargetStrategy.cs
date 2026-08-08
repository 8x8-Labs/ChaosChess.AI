using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class MagnetCardTargetStrategy : ICardTargetStrategy
    {
        private const string MagnetCardId = "magnet";

        private static readonly int[] DeltaFiles = { -1, -1, -1, 0, 1, 1, 1, 0 };
        private static readonly int[] DeltaRanks = { -1, 0, 1, 1, 1, 0, -1, -1 };

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public MagnetCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public MagnetCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => MagnetCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, MagnetCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the magnet strategy.");
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
                    "Magnet has no legal empty square target.");
            }

            var scoredCandidates = new List<CardPlanCandidate>();
            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                Square target = candidate.Plan.Target.Squares[0];
                if (!TryScoreTarget(context.GameState.BoardState, context.Actor, target, out CardPlanScore score))
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
                    "Magnet has no adjacent random piece candidates.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Magnet expected value is below the activation threshold.",
                    scoredCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, scoredCandidates.Count);
        }

        private static bool TryScoreTarget(
            BoardState board,
            PieceColor actor,
            Square target,
            out CardPlanScore score)
        {
            int candidateCount = 0;
            int totalGain = 0;
            int totalLoss = 0;
            int totalPositionGain = 0;

            for (int i = 0; i < DeltaFiles.Length; i++)
            {
                int file = target.File + DeltaFiles[i];
                int rank = target.Rank + DeltaRanks[i];
                if (file < 0 || file >= Square.BoardSize || rank < 0 || rank >= Square.BoardSize)
                {
                    continue;
                }

                Square source = new Square(file, rank);
                PieceInfo? piece = board.FindPiece(source);
                if (piece == null)
                {
                    continue;
                }

                candidateCount++;
                int value = ExpectedValueCardScoring.PieceValue(piece.Kind);
                int positionDelta = ExpectedValueCardScoring.CenterScore(target) -
                    ExpectedValueCardScoring.CenterScore(source);

                if (piece.Color == actor)
                {
                    totalLoss += value;
                    totalPositionGain += Math.Max(0, positionDelta);
                }
                else
                {
                    totalGain += value;
                    totalPositionGain += Math.Max(0, -positionDelta);
                }
            }

            if (candidateCount == 0)
            {
                score = null!;
                return false;
            }

            score = Score(
                totalGain / candidateCount,
                totalLoss / candidateCount,
                totalPositionGain / candidateCount,
                candidateCount);
            return true;
        }

        private static CardPlanScore Score(
            int expectedEnemyPull,
            int expectedFriendlyPull,
            int expectedPositionSwing,
            int candidateCount)
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    "magnet.expected_enemy_pull",
                    rawValue: expectedEnemyPull,
                    weight: 2,
                    "Magnet averages the value of adjacent enemy pieces that may be pulled."),
                new CardPlanScoreComponent(
                    "magnet.expected_friendly_pull",
                    rawValue: expectedFriendlyPull,
                    weight: -2,
                    "Magnet penalizes adjacent actor pieces that may be pulled."),
                new CardPlanScoreComponent(
                    "magnet.expected_position_swing",
                    rawValue: expectedPositionSwing,
                    weight: 1,
                    "Magnet averages coarse positional displacement over random outcomes."),
                new CardPlanScoreComponent(
                    "magnet.random_candidate_count",
                    rawValue: candidateCount,
                    weight: 1,
                    "Magnet has multiple random adjacent piece outcomes.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            return new CardPlanScore(total, components);
        }
    }
}
