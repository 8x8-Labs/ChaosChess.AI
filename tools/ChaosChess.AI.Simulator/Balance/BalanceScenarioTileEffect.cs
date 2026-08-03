using System;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Simulator.Balance
{
    public sealed class BalanceScenarioTileEffect
    {
        public BalanceScenarioTileEffect(
            string id,
            string effectType,
            Square square,
            PieceColor owner,
            int remainingTurns,
            Square? destinationSquare = null,
            int? sharedRemainingUses = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Tile effect id cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Tile effect type cannot be empty.", nameof(effectType));
            }

            if (owner != PieceColor.White && owner != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(owner), owner, "Unknown owner color.");
            }

            if (remainingTurns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remainingTurns), remainingTurns, "Remaining turns cannot be negative.");
            }

            if (sharedRemainingUses.HasValue && sharedRemainingUses.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sharedRemainingUses), sharedRemainingUses, "Shared remaining uses cannot be negative.");
            }

            Id = id;
            EffectType = effectType;
            Square = square;
            Owner = owner;
            RemainingTurns = remainingTurns;
            DestinationSquare = destinationSquare;
            SharedRemainingUses = sharedRemainingUses;
        }

        public string Id { get; }

        public string EffectType { get; }

        public Square Square { get; }

        public PieceColor Owner { get; }

        public int RemainingTurns { get; }

        public Square? DestinationSquare { get; }

        public int? SharedRemainingUses { get; }
    }
}
