using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class TransmigrationCardTargetStrategy : ICardTargetStrategy
    {
        private const string TransmigrationCardId = "transmigration";

        private readonly CardPlanCandidateEnumerator _candidateEnumerator;

        public TransmigrationCardTargetStrategy()
            : this(new CardPlanCandidateEnumerator())
        {
        }

        public TransmigrationCardTargetStrategy(CardPlanCandidateEnumerator candidateEnumerator)
        {
            _candidateEnumerator = candidateEnumerator ?? throw new ArgumentNullException(nameof(candidateEnumerator));
        }

        public string CardId => TransmigrationCardId;

        public CardPlanDecisionResult Decide(CardTargetStrategyContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!string.Equals(context.Card.Id, TransmigrationCardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.UnsupportedCard,
                    $"Card '{context.Card.Id}' cannot be handled by the transmigration strategy.");
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
                    "Transmigration has no legal opponent promoted piece target.");
            }

            var scoredCandidates = new List<CardPlanCandidate>();
            foreach (CardPlanCandidate candidate in legalCandidates)
            {
                PieceTargetSnapshot target = candidate.Plan.Target.Piece
                    ?? throw new InvalidOperationException("Transmigration candidate contains no piece target.");

                if (!TryScoreCandidate(context.GameState.BoardState, target, out CardPlanScore score))
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
                    "Transmigration targets are missing promotion metadata or have occupied start squares.",
                    legalCandidates.Count);
            }

            scoredCandidates.Sort(CardPlanCandidate.CompareByRank);
            CardPlanCandidate bestCandidate = scoredCandidates[0];
            if (bestCandidate.Score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    "Transmigration expected value is below the activation threshold.",
                    legalCandidates.Count);
            }

            return CardPlanDecisionResult.Selected(bestCandidate, legalCandidates.Count);
        }

        private static bool TryScoreCandidate(
            BoardState board,
            PieceTargetSnapshot target,
            out CardPlanScore score)
        {
            if (!target.IsPromotioned ||
                !target.StartSquare.HasValue ||
                target.StartSquare.Value == target.Square ||
                board.FindPiece(target.StartSquare.Value) != null)
            {
                score = new CardPlanScore(
                    0,
                    new[]
                    {
                        new CardPlanScoreComponent(
                            "transmigration.invalid_promotion_metadata",
                            0,
                            "Transmigration requires promoted piece metadata and an empty start square.")
                    });
                return false;
            }

            int materialLoss = Math.Max(0, ExpectedValueCardScoring.PieceValue(target.ExpectedKind) - ExpectedValueCardScoring.PieceValue(PieceKind.Pawn));
            int distance = Math.Abs(target.Square.File - target.StartSquare.Value.File) +
                Math.Abs(target.Square.Rank - target.StartSquare.Value.Rank);
            int centerLoss = Math.Max(
                0,
                ExpectedValueCardScoring.CenterScore(target.Square) -
                ExpectedValueCardScoring.CenterScore(target.StartSquare.Value));

            var components = new[]
            {
                new CardPlanScoreComponent(
                    "transmigration.material_reversion",
                    rawValue: materialLoss,
                    weight: 3,
                    "Transmigration reverts a promoted opponent piece to a pawn."),
                new CardPlanScoreComponent(
                    "transmigration.position_reversion",
                    rawValue: distance,
                    weight: 1,
                    "Transmigration sends the promoted piece back to its original square."),
                new CardPlanScoreComponent(
                    "transmigration.center_loss",
                    rawValue: centerLoss,
                    weight: 2,
                    "Transmigration removes central presence from the promoted piece.")
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
