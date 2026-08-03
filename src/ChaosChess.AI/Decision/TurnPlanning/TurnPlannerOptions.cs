using System;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlannerOptions
    {
        public const int DefaultNoCardMoveCandidateCount = 3;
        public const int DefaultCardCandidateCount = 3;
        public const int DefaultTargetCandidateCount = 16;
        public const int DefaultPostCardMoveCandidateCount = 3;
        public const int DefaultOpponentReplyCandidateCount = 1;
        public const int DefaultBeamWidth = 3;
        public const int DefaultMaximumEngineCallCount = 32;

        public TurnPlannerOptions(
            int noCardMoveCandidateCount = DefaultNoCardMoveCandidateCount,
            int cardCandidateCount = DefaultCardCandidateCount,
            int targetCandidateCount = DefaultTargetCandidateCount,
            int postCardMoveCandidateCount = DefaultPostCardMoveCandidateCount,
            int opponentReplyCandidateCount = DefaultOpponentReplyCandidateCount,
            int beamWidth = DefaultBeamWidth,
            int maximumEngineCallCount = DefaultMaximumEngineCallCount,
            bool allowCoarseCardEffects = false,
            int? seed = null)
        {
            EnsurePositive(noCardMoveCandidateCount, nameof(noCardMoveCandidateCount));
            EnsurePositive(cardCandidateCount, nameof(cardCandidateCount));
            EnsurePositive(targetCandidateCount, nameof(targetCandidateCount));
            EnsurePositive(postCardMoveCandidateCount, nameof(postCardMoveCandidateCount));

            if (opponentReplyCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(opponentReplyCandidateCount),
                    opponentReplyCandidateCount,
                    "Opponent reply candidate count cannot be negative.");
            }

            EnsurePositive(beamWidth, nameof(beamWidth));
            EnsurePositive(maximumEngineCallCount, nameof(maximumEngineCallCount));

            NoCardMoveCandidateCount = noCardMoveCandidateCount;
            CardCandidateCount = cardCandidateCount;
            TargetCandidateCount = targetCandidateCount;
            PostCardMoveCandidateCount = postCardMoveCandidateCount;
            OpponentReplyCandidateCount = opponentReplyCandidateCount;
            BeamWidth = beamWidth;
            MaximumEngineCallCount = maximumEngineCallCount;
            AllowCoarseCardEffects = allowCoarseCardEffects;
            Seed = seed;
        }

        public int NoCardMoveCandidateCount { get; }

        public int CardCandidateCount { get; }

        public int TargetCandidateCount { get; }

        public int PostCardMoveCandidateCount { get; }

        public int OpponentReplyCandidateCount { get; }

        public int BeamWidth { get; }

        public int MaximumEngineCallCount { get; }

        public bool AllowCoarseCardEffects { get; }

        public int? Seed { get; }

        private static void EnsurePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be positive.");
            }
        }
    }
}
