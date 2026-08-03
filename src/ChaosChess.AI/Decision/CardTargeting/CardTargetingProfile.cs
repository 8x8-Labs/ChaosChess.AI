using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardTargetingProfile
    {
        private readonly IReadOnlyDictionary<string, int> _componentWeights;

        public CardTargetingProfile(
            int activationThreshold,
            int maximumPortalEndpointCandidates,
            IReadOnlyDictionary<string, int>? componentWeights)
        {
            if (activationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activationThreshold), activationThreshold, "Activation threshold cannot be negative.");
            }

            if (maximumPortalEndpointCandidates < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumPortalEndpointCandidates), maximumPortalEndpointCandidates, "Portal endpoint candidate limit must be at least two.");
            }

            ActivationThreshold = activationThreshold;
            MaximumPortalEndpointCandidates = maximumPortalEndpointCandidates;
            _componentWeights = CopyWeights(componentWeights);
        }

        public int ActivationThreshold { get; }

        public int MaximumPortalEndpointCandidates { get; }

        public IReadOnlyDictionary<string, int> ComponentWeights => _componentWeights;

        public static CardTargetingProfile CreateP10Baseline()
        {
            return new CardTargetingProfile(
                CardTargetingOptions.DefaultActivationThreshold,
                CardTargetingOptions.DefaultMaximumPortalEndpointCandidates,
                CreateP10BaselineComponentWeights());
        }

        public static IReadOnlyDictionary<string, int> CreateP10BaselineComponentWeights()
        {
            return new Dictionary<string, int>
            {
                ["agile.actor_pawn"] = 1,
                ["agile.promotion_pressure"] = 1,
                ["agile.engine_source"] = 1,
                ["agile.engine_destination_relation"] = 1,
                ["charge.movable_pawns"] = 1,
                ["charge.promotion_reach"] = 1,
                ["charge.blocked_pawns"] = 1,
                ["fire.enemy_engine_destination"] = 1,
                ["fire.enemy_engine_adjacent"] = 1,
                ["fire.center_control"] = 1,
                ["fire.own_engine_destination_penalty"] = 1,
                ["peace.actor_engine_destination"] = 1,
                ["peace.actor_engine_adjacent"] = 1,
                ["peace.enemy_capture_buffer"] = 1,
                ["peace.center_control"] = 1,
                ["portal.endpoint_actor_source"] = 1,
                ["portal.endpoint_actor_destination"] = 1,
                ["portal.endpoint_center_access"] = 1,
                ["portal.endpoint_enemy_destination_risk"] = 1,
                ["portal.endpoint_distance"] = 1
            };
        }

        private static IReadOnlyDictionary<string, int> CopyWeights(
            IReadOnlyDictionary<string, int>? componentWeights)
        {
            var copy = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (componentWeights == null)
            {
                return new ReadOnlyDictionary<string, int>(copy);
            }

            foreach (KeyValuePair<string, int> weight in componentWeights)
            {
                if (string.IsNullOrWhiteSpace(weight.Key))
                {
                    throw new ArgumentException("Component weight key cannot be empty.", nameof(componentWeights));
                }

                copy.Add(weight.Key, weight.Value);
            }

            return new ReadOnlyDictionary<string, int>(copy);
        }
    }
}
