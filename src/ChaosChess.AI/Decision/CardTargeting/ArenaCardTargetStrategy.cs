using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class ArenaCardTargetStrategy : ICardTargetStrategy
    {
        private const string ArenaCardId = "arena";
        private const int MaximumRandomOpponentCount = 3;

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public ArenaCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public ArenaCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => ArenaCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, ArenaCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the arena strategy.");
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
                    "Arena has no legal activation candidate.");
            }

            if (!TryScore(context.GameState.BoardState, context.Actor, out CardPlanScore score))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Arena has no opponent non-king random candidates.",
                    legalCandidates.Count);
            }

            if (score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Arena expected value is below the activation threshold.",
                    legalCandidates.Count);
            }

            CardPlanCandidate candidate = legalCandidates[0];
            return CardPlanDecisionResult.Selected(
                new CardPlanCandidate(candidate.Card, candidate.Plan, score, candidate.EnumerationIndex),
                legalCandidates.Count);
        }

        private static bool TryScore(BoardState board, PieceColor actor, out CardPlanScore score)
        {
            int candidateCount = 0;
            int totalMaterial = 0;
            int totalCenter = 0;

            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Color == actor ||
                    !ExpectedValueCardScoring.IsArenaTargetKind(piece.Kind))
                {
                    continue;
                }

                candidateCount++;
                totalMaterial += ExpectedValueCardScoring.PieceValue(piece.Kind);
                totalCenter += ExpectedValueCardScoring.CenterScore(piece.Square);
            }

            if (candidateCount == 0)
            {
                score = new CardPlanScore(
                    0,
                    new[]
                    {
                        new CardPlanScoreComponent(
                            "arena.no_random_candidates",
                            0,
                            "Arena has no opponent non-king random candidates.")
                    });
                return false;
            }

            int selectedCount = Math.Min(MaximumRandomOpponentCount, candidateCount);
            int expectedMaterial = (totalMaterial * selectedCount) / candidateCount;
            int expectedCenter = (totalCenter * selectedCount) / candidateCount;

            var components = new[]
            {
                new CardPlanScoreComponent(
                    "arena.expected_selected_material",
                    rawValue: expectedMaterial,
                    weight: 2,
                    "Arena averages the material of random opponent pieces pulled into the arena."),
                new CardPlanScoreComponent(
                    "arena.expected_selected_center_presence",
                    rawValue: expectedCenter,
                    weight: 1,
                    "Arena values pulling central opponent pieces into the arena."),
                new CardPlanScoreComponent(
                    "arena.random_candidate_count",
                    rawValue: candidateCount,
                    weight: 1,
                    "Arena has multiple opponent random candidates.")
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
