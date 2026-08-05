using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Domain
{
    public sealed class CardPlanningDefinition
    {
        public CardPlanningDefinition(
            string cardId,
            bool isSupported,
            CardTargetRequirement targetRequirement)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            CardId = cardId;
            IsSupported = isSupported;
            TargetRequirement = targetRequirement ?? throw new ArgumentNullException(nameof(targetRequirement));
        }

        public string CardId { get; }

        public bool IsSupported { get; }

        public CardTargetRequirement TargetRequirement { get; }

        public CardTargetKind RequiredTargetKind => TargetRequirement.Kind;

        public int RequiredTargetCount => TargetRequirement.Count;

        public CardTargetOwnerRelation RequiredTargetOwnerRelation => TargetRequirement.OwnerRelation;

        public IReadOnlyList<PieceKind> AllowedTargetPieceKinds => TargetRequirement.AllowedPieceKinds;

        public static CardPlanningDefinition Supported(
            string cardId,
            CardTargetKind targetKind,
            int targetCount)
        {
            return new CardPlanningDefinition(
                cardId,
                isSupported: true,
                new CardTargetRequirement(targetKind, targetCount));
        }

        public static CardPlanningDefinition Supported(
            string cardId,
            CardTargetRequirement targetRequirement)
        {
            return new CardPlanningDefinition(
                cardId,
                isSupported: true,
                targetRequirement);
        }

        public static CardPlanningDefinition Unsupported(string cardId)
        {
            return new CardPlanningDefinition(
                cardId,
                isSupported: false,
                CardTargetRequirement.None());
        }
    }
}
