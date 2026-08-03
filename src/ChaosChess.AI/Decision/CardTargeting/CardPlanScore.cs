using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardPlanScore
    {
        private readonly ReadOnlyCollection<CardPlanScoreComponent> _components;

        public CardPlanScore(
            int total,
            IEnumerable<CardPlanScoreComponent> components)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            var copy = new List<CardPlanScoreComponent>();
            int componentTotal = 0;

            foreach (CardPlanScoreComponent component in components)
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

        public IReadOnlyList<CardPlanScoreComponent> Components => _components;
    }
}
