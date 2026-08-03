using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class TurnPlan
    {
        public TurnPlan(
            PieceColor actor,
            string originStateFingerprint,
            TurnPlanScore score,
            string deterministicRankKey,
            CardEffectApplicationStatus cardApplicationStatus,
            CardEffectApplicationCode cardApplicationCode,
            CardUsePlan? cardPlan = null,
            MovePlan? movePlan = null)
        {
            EnsureValidColor(actor);
            EnsureValidApplicationStatusCode(cardApplicationStatus, cardApplicationCode);

            if (string.IsNullOrWhiteSpace(originStateFingerprint))
            {
                throw new ArgumentException(
                    "Origin state fingerprint cannot be empty.",
                    nameof(originStateFingerprint));
            }

            if (string.IsNullOrWhiteSpace(deterministicRankKey))
            {
                throw new ArgumentException(
                    "Deterministic rank key cannot be empty.",
                    nameof(deterministicRankKey));
            }

            if (cardPlan == null && movePlan == null)
            {
                throw new ArgumentException(
                    "Turn plan must contain a card plan, a move plan, or both.",
                    nameof(movePlan));
            }

            if (cardPlan != null && cardPlan.Actor != actor)
            {
                throw new ArgumentException(
                    "Card plan actor must match the turn plan actor.",
                    nameof(cardPlan));
            }

            Actor = actor;
            OriginStateFingerprint = originStateFingerprint;
            Score = score ?? throw new ArgumentNullException(nameof(score));
            DeterministicRankKey = deterministicRankKey;
            CardApplicationStatus = cardApplicationStatus;
            CardApplicationCode = cardApplicationCode;
            CardPlan = cardPlan;
            MovePlan = movePlan;
        }

        public PieceColor Actor { get; }

        public string OriginStateFingerprint { get; }

        public CardUsePlan? CardPlan { get; }

        public MovePlan? MovePlan { get; }

        public TurnPlanScore Score { get; }

        public CardEffectApplicationStatus CardApplicationStatus { get; }

        public CardEffectApplicationCode CardApplicationCode { get; }

        public string DeterministicRankKey { get; }

        public bool UsesCard => CardPlan != null;

        public bool HasMove => MovePlan != null;

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }

        private static void EnsureValidApplicationStatusCode(
            CardEffectApplicationStatus status,
            CardEffectApplicationCode code)
        {
            switch (status)
            {
                case CardEffectApplicationStatus.Exact:
                    if (code != CardEffectApplicationCode.Success)
                    {
                        throw new ArgumentException("Exact card application must use Success code.", nameof(code));
                    }
                    break;

                case CardEffectApplicationStatus.Coarse:
                    if (code != CardEffectApplicationCode.CoarseApplied)
                    {
                        throw new ArgumentException("Coarse card application must use CoarseApplied code.", nameof(code));
                    }
                    break;

                case CardEffectApplicationStatus.Unsupported:
                    if (code != CardEffectApplicationCode.UnsupportedEffect &&
                        code != CardEffectApplicationCode.RandomSourceMissing)
                    {
                        throw new ArgumentException("Unsupported card application must use an unsupported code.", nameof(code));
                    }
                    break;

                case CardEffectApplicationStatus.Failed:
                    if (code == CardEffectApplicationCode.Success ||
                        code == CardEffectApplicationCode.CoarseApplied ||
                        code == CardEffectApplicationCode.UnsupportedEffect)
                    {
                        throw new ArgumentException("Failed card application must use a failure code.", nameof(code));
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown card application status.");
            }
        }
    }
}
