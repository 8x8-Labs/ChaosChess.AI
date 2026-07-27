using System;

namespace ChaosChess.AI.Domain
{
    public sealed class TileEffectInfo
    {
        public TileEffectInfo(
            string id,
            string effectType,
            Square square,
            PieceColor? owner,
            int remainingTurns)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Tile effect ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Tile effect type cannot be empty.", nameof(effectType));
            }

            if (remainingTurns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingTurns), remainingTurns, "Remaining turns cannot be negative.");
            }

            Id = id;
            EffectType = effectType;
            Square = square;
            Owner = owner;
            RemainingTurns = remainingTurns;
        }

        public string Id { get; }

        public string EffectType { get; }

        public Square Square { get; }

        public PieceColor? Owner { get; }

        public int RemainingTurns { get; }
    }
}
