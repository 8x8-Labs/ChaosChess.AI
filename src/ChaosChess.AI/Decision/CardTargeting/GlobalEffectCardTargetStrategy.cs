using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class GlobalEffectCardTargetStrategy : ICardTargetStrategy
    {
        private readonly CardPlanCandidateEnumerator _candidateEnumerator;
        private readonly string _componentPrefix;
        private readonly string _displayName;

        public GlobalEffectCardTargetStrategy(string cardId, string displayName)
            : this(cardId, displayName, new CardPlanCandidateEnumerator())
        {
        }

        public GlobalEffectCardTargetStrategy(
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
                    $"{_displayName} has no legal activation candidate.");
            }

            CardPlanScore score = ScoreActivation();
            if (score.Total < context.Options.ActivationThreshold)
            {
                return CardPlanDecisionResult.Skipped(
                    CardPlanSkipCode.NoBenefit,
                    $"{_displayName} activation is below the activation threshold.",
                    legalCandidates.Count);
            }

            CardPlanCandidate neutralCandidate = legalCandidates[0];
            return CardPlanDecisionResult.Selected(
                new CardPlanCandidate(
                    neutralCandidate.Card,
                    neutralCandidate.Plan,
                    score,
                    neutralCandidate.EnumerationIndex),
                legalCandidates.Count);
        }

        private CardPlanScore ScoreActivation()
        {
            var components = new[]
            {
                new CardPlanScoreComponent(
                    _componentPrefix + ".base_activation",
                    1,
                    "No-target global effect can be activated.")
            };

            return new CardPlanScore(1, components);
        }
    }
}
