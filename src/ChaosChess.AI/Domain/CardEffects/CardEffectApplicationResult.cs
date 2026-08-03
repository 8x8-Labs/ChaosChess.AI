using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class CardEffectApplicationResult
    {
        private readonly ReadOnlyCollection<string> _warnings;

        public CardEffectApplicationResult(
            CardEffectApplicationStatus status,
            CardEffectApplicationCode code,
            GameState? state,
            IEnumerable<string>? warnings = null)
        {
            EnsureValidStatus(status);
            EnsureValidCode(code);
            EnsureValidStatusCode(status, code, state);

            Status = status;
            Code = code;
            State = state;
            _warnings = CopyWarnings(warnings);
        }

        public CardEffectApplicationStatus Status { get; }

        public CardEffectApplicationCode Code { get; }

        public GameState? State { get; }

        public IReadOnlyList<string> Warnings => _warnings;

        public bool HasState => State != null;

        public static CardEffectApplicationResult Exact(GameState state)
        {
            return new CardEffectApplicationResult(
                CardEffectApplicationStatus.Exact,
                CardEffectApplicationCode.Success,
                state);
        }

        public static CardEffectApplicationResult Coarse(
            GameState state,
            IEnumerable<string> warnings)
        {
            return new CardEffectApplicationResult(
                CardEffectApplicationStatus.Coarse,
                CardEffectApplicationCode.CoarseApplied,
                state,
                warnings);
        }

        public static CardEffectApplicationResult Unsupported(
            CardEffectApplicationCode code,
            IEnumerable<string>? warnings = null)
        {
            return new CardEffectApplicationResult(
                CardEffectApplicationStatus.Unsupported,
                code,
                state: null,
                warnings);
        }

        public static CardEffectApplicationResult Failed(
            CardEffectApplicationCode code,
            IEnumerable<string>? warnings = null)
        {
            return new CardEffectApplicationResult(
                CardEffectApplicationStatus.Failed,
                code,
                state: null,
                warnings);
        }

        private static ReadOnlyCollection<string> CopyWarnings(
            IEnumerable<string>? warnings)
        {
            var copy = new List<string>();

            if (warnings == null)
            {
                return copy.AsReadOnly();
            }

            foreach (string warning in warnings)
            {
                if (warning == null)
                {
                    throw new ArgumentException("Warning collection cannot contain null.", nameof(warnings));
                }

                copy.Add(warning);
            }

            return copy.AsReadOnly();
        }

        private static void EnsureValidStatus(CardEffectApplicationStatus status)
        {
            if (status != CardEffectApplicationStatus.Exact &&
                status != CardEffectApplicationStatus.Coarse &&
                status != CardEffectApplicationStatus.Unsupported &&
                status != CardEffectApplicationStatus.Failed)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application status.");
            }
        }

        private static void EnsureValidCode(CardEffectApplicationCode code)
        {
            if (code != CardEffectApplicationCode.Success &&
                code != CardEffectApplicationCode.CoarseApplied &&
                code != CardEffectApplicationCode.UnsupportedEffect &&
                code != CardEffectApplicationCode.InvalidDefinition &&
                code != CardEffectApplicationCode.InvalidContext &&
                code != CardEffectApplicationCode.IllegalTarget &&
                code != CardEffectApplicationCode.StaleTarget &&
                code != CardEffectApplicationCode.RandomSourceMissing &&
                code != CardEffectApplicationCode.InvariantViolation)
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown application code.");
            }
        }

        private static void EnsureValidStatusCode(
            CardEffectApplicationStatus status,
            CardEffectApplicationCode code,
            GameState? state)
        {
            switch (status)
            {
                case CardEffectApplicationStatus.Exact:
                    if (code != CardEffectApplicationCode.Success)
                    {
                        throw new ArgumentException("Exact application must use Success code.", nameof(code));
                    }

                    if (state == null)
                    {
                        throw new ArgumentNullException(nameof(state), "Exact application requires a resulting state.");
                    }
                    break;

                case CardEffectApplicationStatus.Coarse:
                    if (code != CardEffectApplicationCode.CoarseApplied)
                    {
                        throw new ArgumentException("Coarse application must use CoarseApplied code.", nameof(code));
                    }

                    if (state == null)
                    {
                        throw new ArgumentNullException(nameof(state), "Coarse application requires a resulting state.");
                    }
                    break;

                case CardEffectApplicationStatus.Unsupported:
                    if (code != CardEffectApplicationCode.UnsupportedEffect &&
                        code != CardEffectApplicationCode.RandomSourceMissing)
                    {
                        throw new ArgumentException("Unsupported application must use an unsupported code.", nameof(code));
                    }

                    if (state != null)
                    {
                        throw new ArgumentException("Unsupported application cannot include a resulting state.", nameof(state));
                    }
                    break;

                case CardEffectApplicationStatus.Failed:
                    if (code == CardEffectApplicationCode.Success ||
                        code == CardEffectApplicationCode.CoarseApplied ||
                        code == CardEffectApplicationCode.UnsupportedEffect)
                    {
                        throw new ArgumentException("Failed application must use a failure code.", nameof(code));
                    }

                    if (state != null)
                    {
                        throw new ArgumentException("Failed application cannot include a resulting state.", nameof(state));
                    }
                    break;
            }
        }
    }
}
