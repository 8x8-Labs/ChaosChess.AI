using System;

namespace ChaosChess.AI.Domain.CardCatalog
{
    public sealed class CardCatalogEntry
    {
        public CardCatalogEntry(
            string cardId,
            string unityAssetName,
            string displayName,
            UnityCardType unityType,
            bool currentUnityAiSupported,
            CardCatalogSupportGrade supportGrade,
            CardCatalogActivationWave activationWave,
            string primitiveGap,
            string notes)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            if (string.IsNullOrWhiteSpace(unityAssetName))
            {
                throw new ArgumentException("Unity asset name cannot be empty.", nameof(unityAssetName));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            EnsureValidUnityType(unityType);
            EnsureValidSupportGrade(supportGrade);
            EnsureValidActivationWave(activationWave);

            CardId = cardId;
            UnityAssetName = unityAssetName;
            DisplayName = displayName;
            UnityType = unityType;
            CurrentUnityAiSupported = currentUnityAiSupported;
            SupportGrade = supportGrade;
            ActivationWave = activationWave;
            PrimitiveGap = string.IsNullOrWhiteSpace(primitiveGap) ? string.Empty : primitiveGap;
            Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes;
        }

        public string CardId { get; }

        public string UnityAssetName { get; }

        public string DisplayName { get; }

        public UnityCardType UnityType { get; }

        public bool CurrentUnityAiSupported { get; }

        public CardCatalogSupportGrade SupportGrade { get; }

        public CardCatalogActivationWave ActivationWave { get; }

        public string PrimitiveGap { get; }

        public string Notes { get; }

        private static void EnsureValidUnityType(UnityCardType unityType)
        {
            if (unityType != UnityCardType.Piece &&
                unityType != UnityCardType.Tile &&
                unityType != UnityCardType.Global)
            {
                throw new ArgumentOutOfRangeException(nameof(unityType), unityType, "Unknown Unity card type.");
            }
        }

        private static void EnsureValidSupportGrade(CardCatalogSupportGrade supportGrade)
        {
            if (supportGrade != CardCatalogSupportGrade.ExactCommon &&
                supportGrade != CardCatalogSupportGrade.CoarseOnly &&
                supportGrade != CardCatalogSupportGrade.NeedsCommonPrimitive &&
                supportGrade != CardCatalogSupportGrade.NeedsSpecialExtension &&
                supportGrade != CardCatalogSupportGrade.DeferredUnsafe)
            {
                throw new ArgumentOutOfRangeException(nameof(supportGrade), supportGrade, "Unknown support grade.");
            }
        }

        private static void EnsureValidActivationWave(CardCatalogActivationWave activationWave)
        {
            if (activationWave != CardCatalogActivationWave.Wave0 &&
                activationWave != CardCatalogActivationWave.Wave1 &&
                activationWave != CardCatalogActivationWave.Wave2 &&
                activationWave != CardCatalogActivationWave.Wave3 &&
                activationWave != CardCatalogActivationWave.Deferred)
            {
                throw new ArgumentOutOfRangeException(nameof(activationWave), activationWave, "Unknown activation wave.");
            }
        }
    }
}
