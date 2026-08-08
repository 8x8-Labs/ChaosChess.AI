using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class ShuffleBoardCardTargetStrategy : ICardTargetStrategy
    {
        private const string ShuffleBoardCardId = "shuffle_board";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public ShuffleBoardCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public ShuffleBoardCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => ShuffleBoardCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, ShuffleBoardCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the shuffle board strategy.");
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
                    "Shuffle Board has no legal activation candidate.");
            }

            if (!TryScore(context.GameState.BoardState, context.Actor, out CardPlanScore score))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Shuffle Board has fewer than two random opponent targets.",
                    legalCandidates.Count);
            }

            if (score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Shuffle Board expected value is below the activation threshold.",
                    legalCandidates.Count);
            }

            CardPlanCandidate candidate = legalCandidates[0];
            return CardPlanDecisionResult.Selected(
                new CardPlanCandidate(candidate.Card, candidate.Plan, score, candidate.EnumerationIndex),
                legalCandidates.Count);
        }

        private static bool TryScore(BoardState board, PieceColor actor, out CardPlanScore score)
        {
            int targetCount = 0;
            int material = 0;
            int centrality = 0;

            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == actor ||
                    !ExpectedValueCardScoring.IsShuffleBoardTargetKind(piece.Kind))
                {
                    continue;
                }

                targetCount++;
                material += ExpectedValueCardScoring.PieceValue(piece.Kind);
                centrality += ExpectedValueCardScoring.CenterScore(piece.Square);
            }

            if (targetCount < 2)
            {
                score = new CardPlanScore(
                    0,
                    new[]
                    {
                        new CardPlanScoreComponent(
                            "shuffle_board.insufficient_targets",
                            0,
                            "Shuffle Board requires at least two random opponent targets.")
                    });
                return false;
            }

            var components = new[]
            {
                new CardPlanScoreComponent(
                    "shuffle_board.target_count",
                    rawValue: targetCount,
                    weight: 2,
                    "Shuffle Board disrupts multiple opponent pieces."),
                new CardPlanScoreComponent(
                    "shuffle_board.target_material",
                    rawValue: material,
                    weight: 1,
                    "Shuffle Board affects higher-value opponent pieces."),
                new CardPlanScoreComponent(
                    "shuffle_board.central_disruption",
                    rawValue: centrality,
                    weight: 1,
                    "Shuffle Board is more valuable when it shuffles central opponent pieces.")
            };

            int total = 0;
            foreach (CardPlanScoreComponent component in components)
            {
                total += component.Value;
            }

            score = new CardPlanScore(total, components);
            return true;
        }
    }
}
