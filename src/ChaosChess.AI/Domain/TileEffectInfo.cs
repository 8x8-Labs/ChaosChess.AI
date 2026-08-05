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
            int remainingTurns,
            Square? destinationSquare = null,
            int? sharedRemainingUses = null,
            TileEffectLifetimeKind lifetimeKind = TileEffectLifetimeKind.TurnLimited)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Tile effect ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Tile effect type cannot be empty.", nameof(effectType));
            }

            TileEffectLifetimeKindGuard.EnsureValid(lifetimeKind, nameof(lifetimeKind));

            if (remainingTurns < 0 && lifetimeKind != TileEffectLifetimeKind.PersistentUntilTriggered)
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
            LifetimeKind = lifetimeKind;
        }

        public string Id { get; }

        public string EffectType { get; }

        public Square Square { get; }

        public PieceColor? Owner { get; }

        public int RemainingTurns { get; }

        public Square? DestinationSquare { get; }

        public int? SharedRemainingUses { get; }

        public TileEffectLifetimeKind LifetimeKind { get; }
    }
}
