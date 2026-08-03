using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Simulation.Metrics;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceCardDecisionMetricCollection
    {
        private readonly ReadOnlyCollection<CardDecisionMetricEvent> _decisionEvents;
        private readonly ReadOnlyCollection<CardScoreComponentMetricEvent> _componentEvents;

        public BalanceCardDecisionMetricCollection(
            IEnumerable<CardDecisionMetricEvent> decisionEvents,
            IEnumerable<CardScoreComponentMetricEvent> componentEvents)
        {
            _decisionEvents = CopyDecisionEvents(decisionEvents);
            _componentEvents = CopyComponentEvents(componentEvents);
        }

        public IReadOnlyList<CardDecisionMetricEvent> DecisionEvents => _decisionEvents;

        public IReadOnlyList<CardScoreComponentMetricEvent> ComponentEvents => _componentEvents;

        private static ReadOnlyCollection<CardDecisionMetricEvent> CopyDecisionEvents(
            IEnumerable<CardDecisionMetricEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var copy = new List<CardDecisionMetricEvent>();

            foreach (CardDecisionMetricEvent metricEvent in events)
            {
                if (metricEvent == null)
                {
                    throw new ArgumentException("Decision event collection cannot contain null.", nameof(events));
                }

                copy.Add(metricEvent);
            }

            return copy.AsReadOnly();
        }

        private static ReadOnlyCollection<CardScoreComponentMetricEvent> CopyComponentEvents(
            IEnumerable<CardScoreComponentMetricEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var copy = new List<CardScoreComponentMetricEvent>();

            foreach (CardScoreComponentMetricEvent metricEvent in events)
            {
                if (metricEvent == null)
                {
                    throw new ArgumentException("Component event collection cannot contain null.", nameof(events));
                }

                copy.Add(metricEvent);
            }

            return copy.AsReadOnly();
        }
    }
}
