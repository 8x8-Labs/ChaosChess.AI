using System;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlannerTraceSummary
    {
        public TurnPlannerTraceSummary(
            int noCardMoveCandidateLimit,
            int cardCandidateLimit,
            int targetCandidateLimit,
            int postCardMoveCandidateLimit,
            int opponentReplyCandidateLimit,
            int beamWidth,
            int deterministicCandidateCap,
            int rootNoCardMoveCandidateCount,
            int consideredCardCandidateCount,
            int cardTargetingSkipCount,
            int cardEffectSkipCount,
            int postCardMoveCandidateCount,
            int selectedCandidateCount,
            int skippedCandidateCount,
            int engineCallCount,
            int maximumEngineCallCount,
            int engineCallLimitSkipCount,
            int opponentReplyDeferredCandidateCount,
            int beamPrunedCandidateCount)
        {
            EnsurePositive(noCardMoveCandidateLimit, nameof(noCardMoveCandidateLimit));
            EnsurePositive(cardCandidateLimit, nameof(cardCandidateLimit));
            EnsurePositive(targetCandidateLimit, nameof(targetCandidateLimit));
            EnsurePositive(postCardMoveCandidateLimit, nameof(postCardMoveCandidateLimit));
            EnsureNonNegative(opponentReplyCandidateLimit, nameof(opponentReplyCandidateLimit));
            EnsurePositive(beamWidth, nameof(beamWidth));
            EnsurePositive(deterministicCandidateCap, nameof(deterministicCandidateCap));
            EnsurePositive(maximumEngineCallCount, nameof(maximumEngineCallCount));
            EnsureNonNegative(rootNoCardMoveCandidateCount, nameof(rootNoCardMoveCandidateCount));
            EnsureNonNegative(consideredCardCandidateCount, nameof(consideredCardCandidateCount));
            EnsureNonNegative(cardTargetingSkipCount, nameof(cardTargetingSkipCount));
            EnsureNonNegative(cardEffectSkipCount, nameof(cardEffectSkipCount));
            EnsureNonNegative(postCardMoveCandidateCount, nameof(postCardMoveCandidateCount));
            EnsureNonNegative(selectedCandidateCount, nameof(selectedCandidateCount));
            EnsureNonNegative(skippedCandidateCount, nameof(skippedCandidateCount));
            EnsureNonNegative(engineCallCount, nameof(engineCallCount));
            EnsureNonNegative(engineCallLimitSkipCount, nameof(engineCallLimitSkipCount));
            EnsureNonNegative(opponentReplyDeferredCandidateCount, nameof(opponentReplyDeferredCandidateCount));
            EnsureNonNegative(beamPrunedCandidateCount, nameof(beamPrunedCandidateCount));

            NoCardMoveCandidateLimit = noCardMoveCandidateLimit;
            CardCandidateLimit = cardCandidateLimit;
            TargetCandidateLimit = targetCandidateLimit;
            PostCardMoveCandidateLimit = postCardMoveCandidateLimit;
            OpponentReplyCandidateLimit = opponentReplyCandidateLimit;
            BeamWidth = beamWidth;
            DeterministicCandidateCap = deterministicCandidateCap;
            RootNoCardMoveCandidateCount = rootNoCardMoveCandidateCount;
            ConsideredCardCandidateCount = consideredCardCandidateCount;
            CardTargetingSkipCount = cardTargetingSkipCount;
            CardEffectSkipCount = cardEffectSkipCount;
            PostCardMoveCandidateCount = postCardMoveCandidateCount;
            SelectedCandidateCount = selectedCandidateCount;
            SkippedCandidateCount = skippedCandidateCount;
            EngineCallCount = engineCallCount;
            MaximumEngineCallCount = maximumEngineCallCount;
            EngineCallLimitSkipCount = engineCallLimitSkipCount;
            OpponentReplyDeferredCandidateCount = opponentReplyDeferredCandidateCount;
            BeamPrunedCandidateCount = beamPrunedCandidateCount;
        }

        public int NoCardMoveCandidateLimit { get; }

        public int CardCandidateLimit { get; }

        public int TargetCandidateLimit { get; }

        public int PostCardMoveCandidateLimit { get; }

        public int OpponentReplyCandidateLimit { get; }

        public int BeamWidth { get; }

        public int DeterministicCandidateCap { get; }

        public int RootNoCardMoveCandidateCount { get; }

        public int ConsideredCardCandidateCount { get; }

        public int CardTargetingSkipCount { get; }

        public int CardEffectSkipCount { get; }

        public int PostCardMoveCandidateCount { get; }

        public int SelectedCandidateCount { get; }

        public int SkippedCandidateCount { get; }

        public int EngineCallCount { get; }

        public int MaximumEngineCallCount { get; }

        public int EngineCallLimitSkipCount { get; }

        public bool EngineCallLimitReached => EngineCallCount >= MaximumEngineCallCount;

        public int OpponentReplyDeferredCandidateCount { get; }

        public bool OpponentReplyEvaluationRequested => OpponentReplyCandidateLimit > 0;

        public bool OpponentReplyEvaluationDeferred => OpponentReplyDeferredCandidateCount > 0;

        public int BeamPrunedCandidateCount { get; }

        public bool BeamPruningApplied => BeamPrunedCandidateCount > 0;

        private static void EnsurePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be positive.");
            }
        }

        private static void EnsureNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }
        }
    }
}
