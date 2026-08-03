using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class CardEffectDefinition
    {
        private readonly ReadOnlyCollection<CardEffectPrimitive> _primitives;

        public CardEffectDefinition(
            string cardId,
            CardTargetQuery targetQuery,
            IEnumerable<CardEffectPrimitive> primitives)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            CardId = cardId;
            TargetQuery = targetQuery ?? throw new ArgumentNullException(nameof(targetQuery));
            _primitives = CopyPrimitives(primitives);
        }

        public string CardId { get; }

        public CardTargetQuery TargetQuery { get; }

        public IReadOnlyList<CardEffectPrimitive> Primitives => _primitives;

        private static ReadOnlyCollection<CardEffectPrimitive> CopyPrimitives(
            IEnumerable<CardEffectPrimitive> primitives)
        {
            if (primitives == null)
            {
                throw new ArgumentNullException(nameof(primitives));
            }

            var copy = new List<CardEffectPrimitive>();

            foreach (CardEffectPrimitive primitive in primitives)
            {
                if (primitive == null)
                {
                    throw new ArgumentException("Primitive collection cannot contain null.", nameof(primitives));
                }

                copy.Add(primitive);
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException("Effect definition must contain at least one primitive.", nameof(primitives));
            }

            return copy.AsReadOnly();
        }
    }
}
