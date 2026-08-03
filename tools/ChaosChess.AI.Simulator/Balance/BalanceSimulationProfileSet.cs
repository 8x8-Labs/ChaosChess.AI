using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChaosChess.AI.Decision;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceSimulationProfileSet
    {
        private readonly ReadOnlyCollection<CardBalanceProfile> _candidateProfiles;

        public BalanceSimulationProfileSet(
            string profileSetId,
            CardBalanceProfile baselineProfile,
            IEnumerable<CardBalanceProfile>? candidateProfiles = null)
        {
            if (string.IsNullOrWhiteSpace(profileSetId))
            {
                throw new ArgumentException("Profile set id cannot be empty.", nameof(profileSetId));
            }

            ProfileSetId = profileSetId;
            BaselineProfile = baselineProfile ?? throw new ArgumentNullException(nameof(baselineProfile));
            _candidateProfiles = CopyProfiles(candidateProfiles);
        }

        public string ProfileSetId { get; }

        public CardBalanceProfile BaselineProfile { get; }

        public IReadOnlyList<CardBalanceProfile> CandidateProfiles => _candidateProfiles;

        private static ReadOnlyCollection<CardBalanceProfile> CopyProfiles(
            IEnumerable<CardBalanceProfile>? profiles)
        {
            var copy = new List<CardBalanceProfile>();

            if (profiles == null)
            {
                return copy.AsReadOnly();
            }

            foreach (CardBalanceProfile profile in profiles)
            {
                if (profile == null)
                {
                    throw new ArgumentException("Profile collection cannot contain null.", nameof(profiles));
                }

                copy.Add(profile);
            }

            return copy.AsReadOnly();
        }
    }
}
