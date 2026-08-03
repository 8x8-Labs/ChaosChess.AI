using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulation.Metrics
{
    public sealed class CardDecisionMetricEvent
    {
        public CardDecisionMetricEvent(
            string eventId,
            int plyIndex,
            PieceColor actor,
            string cardId,
            string category,
            int remainingUses,
            bool offered,
            bool supported,
            bool eligible,
            int legalCandidateCount,
            bool planSelected,
            bool recommended,
            CardDecisionAppliedStatus appliedStatus,
            CardDecisionMetricCode code,
            int? baseScore,
            int? planScoreTotal,
            int? combinedGainBeforeClamp,
            int? effectiveGain,
            int targetingThreshold,
            int minimumScoreGain,
            CardUsePlan? selectedPlan)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException("Event id cannot be empty.", nameof(eventId));
            }

            if (plyIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(plyIndex), plyIndex, "Ply index cannot be negative.");
            }

            if (actor != PieceColor.White && actor != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(actor), actor, "Unknown actor color.");
            }

            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card id cannot be empty.", nameof(cardId));
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be empty.", nameof(category));
            }

            if (remainingUses < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingUses), remainingUses, "Remaining uses cannot be negative.");
            }

            if (legalCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(legalCandidateCount), legalCandidateCount, "Legal candidate count cannot be negative.");
            }

            if (targetingThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetingThreshold), targetingThreshold, "Targeting threshold cannot be negative.");
            }

            if (minimumScoreGain < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumScoreGain), minimumScoreGain, "Minimum score gain cannot be negative.");
            }

            if (recommended && !planSelected)
            {
                throw new ArgumentException("A recommended card must have a selected plan.", nameof(recommended));
            }

            if (planSelected && selectedPlan == null)
            {
                throw new ArgumentException("Selected plan is required when a plan is selected.", nameof(selectedPlan));
            }

            if (!planSelected && selectedPlan != null)
            {
                throw new ArgumentException("Selected plan must be null when no plan is selected.", nameof(selectedPlan));
            }

            EventId = eventId;
            PlyIndex = plyIndex;
            Actor = actor;
            CardId = cardId;
            Category = category;
            RemainingUses = remainingUses;
            Offered = offered;
            Supported = supported;
            Eligible = eligible;
            LegalCandidateCount = legalCandidateCount;
            PlanSelected = planSelected;
            Recommended = recommended;
            AppliedStatus = appliedStatus;
            Code = code;
            BaseScore = baseScore;
            PlanScoreTotal = planScoreTotal;
            CombinedGainBeforeClamp = combinedGainBeforeClamp;
            EffectiveGain = effectiveGain;
            TargetingThreshold = targetingThreshold;
            MinimumScoreGain = minimumScoreGain;
            SelectedPlan = selectedPlan;
        }

        public string EventId { get; }

        public int PlyIndex { get; }

        public PieceColor Actor { get; }

        public string CardId { get; }

        public string Category { get; }

        public int RemainingUses { get; }

        public bool Offered { get; }

        public bool Supported { get; }

        public bool Eligible { get; }

        public int LegalCandidateCount { get; }

        public bool LegalCandidateAvailable => LegalCandidateCount > 0;

        public bool PlanSelected { get; }

        public bool Recommended { get; }

        public CardDecisionAppliedStatus AppliedStatus { get; }

        public CardDecisionMetricCode Code { get; }

        public int? BaseScore { get; }

        public int? PlanScoreTotal { get; }

        public int? CombinedGainBeforeClamp { get; }

        public int? EffectiveGain { get; }

        public int TargetingThreshold { get; }

        public int MinimumScoreGain { get; }

        public CardUsePlan? SelectedPlan { get; }
    }
}
