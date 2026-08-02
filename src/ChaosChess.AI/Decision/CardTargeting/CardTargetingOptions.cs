using System;

namespace ChaosChess.AI.Decision.CardTargeting
{
    public sealed class CardTargetingOptions
    {
        public const int DefaultActivationThreshold = 1;
        public const int DefaultMaximumPortalEndpointCandidates = 16;

        public CardTargetingOptions(
            int activationThreshold = DefaultActivationThreshold,
            int maximumPortalEndpointCandidates = DefaultMaximumPortalEndpointCandidates)
        {
            if (activationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activationThreshold),
                    activationThreshold,
                    "Activation threshold cannot be negative.");
            }

            if (maximumPortalEndpointCandidates < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumPortalEndpointCandidates),
                    maximumPortalEndpointCandidates,
                    "Portal endpoint candidate limit must be at least two.");
            }

            ActivationThreshold = activationThreshold;
            MaximumPortalEndpointCandidates = maximumPortalEndpointCandidates;
        }

        public int ActivationThreshold { get; }

        public int MaximumPortalEndpointCandidates { get; }
    }
}
