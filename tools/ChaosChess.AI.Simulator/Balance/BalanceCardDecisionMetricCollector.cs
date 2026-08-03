using System;
using System.Collections.Generic;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Simulation;
using ChaosChess.AI.Simulation.Metrics;

namespace ChaosChess.AI.Simulator.Balance
{
    public static class BalanceCardDecisionMetricCollector
    {
        public static BalanceCardDecisionMetricCollection Collect(
            SimulationResult simulation,
            CardBalanceProfile profile)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var decisionEvents = new List<CardDecisionMetricEvent>();
            var componentEvents = new List<CardScoreComponentMetricEvent>();

            foreach (SimulationStep step in simulation.Steps)
            {
                for (int recommendationIndex = 0; recommendationIndex < step.CardDecision.Recommendations.Count; recommendationIndex++)
                {
                    CardUseRecommendation recommendation = step.CardDecision.Recommendations[recommendationIndex];
                    string eventId = CreateDecisionEventId(step.PlyIndex, recommendationIndex, recommendation.Card.Id);
                    decisionEvents.Add(CreateDecisionEvent(eventId, step, recommendation, profile));
                    AddComponentEvents(componentEvents, eventId, recommendation, profile);
                }
            }

            return new BalanceCardDecisionMetricCollection(decisionEvents, componentEvents);
        }

        private static CardDecisionMetricEvent CreateDecisionEvent(
            string eventId,
            SimulationStep step,
            CardUseRecommendation recommendation,
            CardBalanceProfile profile)
        {
            CardPlanScore planScore = RequirePlanScore(recommendation);
            int legalCandidateCount = RequireLegalCandidateCount(recommendation);

            return new CardDecisionMetricEvent(
                eventId,
                step.PlyIndex,
                step.SideToMove,
                recommendation.Card.Id,
                recommendation.Card.Category,
                recommendation.Card.RemainingUses,
                offered: true,
                supported: true,
                eligible: true,
                legalCandidateCount,
                planSelected: true,
                recommended: true,
                CardDecisionAppliedStatus.NotAvailable,
                CardDecisionMetricCode.Recommended,
                recommendation.BaseScore,
                planScore.Total,
                combinedGainBeforeClamp: null,
                recommendation.EffectiveGain,
                profile.TargetingProfile.ActivationThreshold,
                profile.MinimumScoreGain,
                recommendation.Plan);
        }

        private static void AddComponentEvents(
            ICollection<CardScoreComponentMetricEvent> componentEvents,
            string decisionEventId,
            CardUseRecommendation recommendation,
            CardBalanceProfile profile)
        {
            CardPlanScore planScore = RequirePlanScore(recommendation);

            for (int componentIndex = 0; componentIndex < planScore.Components.Count; componentIndex++)
            {
                CardPlanScoreComponent component = planScore.Components[componentIndex];
                componentEvents.Add(new CardScoreComponentMetricEvent(
                    decisionEventId + ":component-" + componentIndex,
                    recommendation.Card.Id,
                    candidateRank: null,
                    component.Code,
                    component.RawValue,
                    component.Weight,
                    component.Contribution,
                    profile.ProfileId));
            }
        }

        private static CardPlanScore RequirePlanScore(CardUseRecommendation recommendation)
        {
            if (recommendation.Plan == null || recommendation.PlanScore == null)
            {
                throw new InvalidOperationException("Balance metrics require plan-aware card recommendations.");
            }

            return recommendation.PlanScore;
        }

        private static int RequireLegalCandidateCount(CardUseRecommendation recommendation)
        {
            if (!recommendation.PlanLegalCandidateCount.HasValue)
            {
                throw new InvalidOperationException("Balance metrics require the legal candidate count.");
            }

            return recommendation.PlanLegalCandidateCount.Value;
        }

        private static string CreateDecisionEventId(
            int plyIndex,
            int recommendationIndex,
            string cardId)
        {
            return "ply-" + plyIndex + ":card-" + recommendationIndex + ":" + cardId;
        }
    }
}
