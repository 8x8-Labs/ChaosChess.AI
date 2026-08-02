using System;

namespace ChaosChess.AI.Domain
{
    public sealed class CardUsePlan
    {
        public CardUsePlan(
            string cardId,
            PieceColor actor,
            CardTargetSelection target,
            CardEffectParameters? effectParameters = null)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card ID cannot be empty.", nameof(cardId));
            }

            EnsureValidColor(actor);

            CardId = cardId;
            Actor = actor;
            Target = target ?? throw new ArgumentNullException(nameof(target));
            EffectParameters = effectParameters ?? CardEffectParameters.Empty;
        }

        public string CardId { get; }

        public PieceColor Actor { get; }

        public CardTargetSelection Target { get; }

        public CardEffectParameters EffectParameters { get; }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }
    }
}
