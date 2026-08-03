using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class CardEffectPlanningResult
    {
        public CardEffectPlanningResult(
            CardUsePlan plan,
            CardEffectApplicationStatus status,
            CardEffectApplicationCode code,
            string reason,
            GameState? resultingState = null)
        {
            EnsureValidStatusCode(status, code);
            EnsureValidState(status, resultingState);

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "Planning reason cannot be empty.",
                    nameof(reason));
            }

            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Status = status;
            Code = code;
            Reason = reason;
            ResultingState = resultingState;
        }

        public CardUsePlan Plan { get; }

        public CardEffectApplicationStatus Status { get; }

        public CardEffectApplicationCode Code { get; }

        public string Reason { get; }

        public GameState? ResultingState { get; }

        public bool HasResultingState => ResultingState != null;

        public static CardEffectPlanningResult FromApplicationResult(
            CardUsePlan plan,
            CardEffectApplicationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new CardEffectPlanningResult(
                plan,
                result.Status,
                result.Code,
                CreateReason(result),
                result.State);
        }

        public static CardEffectPlanningResult Exact(
            CardUsePlan plan,
            GameState resultingState,
            string reason)
        {
            return new CardEffectPlanningResult(
                plan,
                CardEffectApplicationStatus.Exact,
                CardEffectApplicationCode.Success,
                reason,
                resultingState ?? throw new ArgumentNullException(nameof(resultingState)));
        }

        public static CardEffectPlanningResult Unsupported(
            CardUsePlan plan,
            CardEffectApplicationCode code,
            string reason)
        {
            return new CardEffectPlanningResult(
                plan,
                CardEffectApplicationStatus.Unsupported,
                code,
                reason);
        }

        public static CardEffectPlanningResult Coarse(
            CardUsePlan plan,
            GameState resultingState,
            string reason)
        {
            return new CardEffectPlanningResult(
                plan,
                CardEffectApplicationStatus.Coarse,
                CardEffectApplicationCode.CoarseApplied,
                reason,
                resultingState ?? throw new ArgumentNullException(nameof(resultingState)));
        }

        public static CardEffectPlanningResult Failed(
            CardUsePlan plan,
            CardEffectApplicationCode code,
            string reason)
        {
            return new CardEffectPlanningResult(
                plan,
                CardEffectApplicationStatus.Failed,
                code,
                reason);
        }

        private static void EnsureValidStatusCode(
            CardEffectApplicationStatus status,
            CardEffectApplicationCode code)
        {
            switch (status)
            {
                case CardEffectApplicationStatus.Exact:
                    if (code != CardEffectApplicationCode.Success)
                    {
                        throw new ArgumentException("Exact planning must use Success code.", nameof(code));
                    }

                    break;

                case CardEffectApplicationStatus.Coarse:
                    if (code != CardEffectApplicationCode.CoarseApplied)
                    {
                        throw new ArgumentException("Coarse planning must use CoarseApplied code.", nameof(code));
                    }

                    break;

                case CardEffectApplicationStatus.Unsupported:
                    if (code != CardEffectApplicationCode.UnsupportedEffect &&
                        code != CardEffectApplicationCode.RandomSourceMissing)
                    {
                        throw new ArgumentException("Unsupported planning must use an unsupported code.", nameof(code));
                    }

                    break;

                case CardEffectApplicationStatus.Failed:
                    if (code == CardEffectApplicationCode.Success ||
                        code == CardEffectApplicationCode.CoarseApplied ||
                        code == CardEffectApplicationCode.UnsupportedEffect)
                    {
                        throw new ArgumentException("Failed planning must use a failure code.", nameof(code));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application status.");
            }
        }

        private static void EnsureValidState(
            CardEffectApplicationStatus status,
            GameState? resultingState)
        {
            switch (status)
            {
                case CardEffectApplicationStatus.Exact:
                case CardEffectApplicationStatus.Coarse:
                    if (resultingState == null)
                    {
                        throw new ArgumentNullException(
                            nameof(resultingState),
                            "Exact and coarse card planning results require a resulting state.");
                    }

                    break;

                case CardEffectApplicationStatus.Unsupported:
                case CardEffectApplicationStatus.Failed:
                    if (resultingState != null)
                    {
                        throw new ArgumentException(
                            "Unsupported and failed card planning results cannot include a resulting state.",
                            nameof(resultingState));
                    }

                    break;
            }
        }

        private static string CreateReason(CardEffectApplicationResult result)
        {
            if (result.Warnings.Count > 0)
            {
                return result.Warnings[0];
            }

            switch (result.Status)
            {
                case CardEffectApplicationStatus.Exact:
                    return "Card effect applied exactly; post-card move analysis is pending.";

                case CardEffectApplicationStatus.Coarse:
                    return "Card effect applied coarsely; post-card move analysis is pending.";

                case CardEffectApplicationStatus.Unsupported:
                    return "Card effect is unsupported by the current planner.";

                case CardEffectApplicationStatus.Failed:
                    return "Card effect application failed.";

                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result.Status, "Unknown application status.");
            }
        }
    }
}
