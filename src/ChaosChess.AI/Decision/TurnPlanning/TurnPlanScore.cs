using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlanScore
    {
        private readonly ReadOnlyCollection<TurnPlanScoreComponent> _components;

        public TurnPlanScore(
            int total,
            IEnumerable<TurnPlanScoreComponent> components)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            var copy = new List<TurnPlanScoreComponent>();
            int componentTotal = 0;

            foreach (TurnPlanScoreComponent component in components)
            {
                if (component == null)
                {
                    throw new ArgumentException(
                        "Score component collection cannot contain null.",
                        nameof(components));
                }

                copy.Add(component);
                componentTotal += component.Value;
            }

            if (componentTotal != total)
            {
                throw new ArgumentException(
                    "Score component values must sum to the total score.",
                    nameof(components));
            }

            Total = total;
            _components = copy.AsReadOnly();
        }

        public int Total { get; }

        public IReadOnlyList<TurnPlanScoreComponent> Components => _components;
    }
}
