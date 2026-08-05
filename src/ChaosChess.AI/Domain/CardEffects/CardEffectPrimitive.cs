using System;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class CardEffectPrimitive
    {
        public CardEffectPrimitive(
            CardEffectPrimitiveKind kind,
            Square? sourceSquare = null,
            Square? destinationSquare = null,
            PieceKind? pieceKind = null,
            PieceColor? owner = null,
            string? effectType = null,
            string? movementOverrideCode = null,
            int? durationTurns = null,
            int? sharedRemainingUses = null,
            TileEffectLifetimeKind tileEffectLifetimeKind = TileEffectLifetimeKind.TurnLimited,
            CardEffectPrimitiveTargetBinding targetBinding = CardEffectPrimitiveTargetBinding.None,
            int? targetIndex = null,
            int? destinationTargetIndex = null)
        {
            EnsureValidKind(kind);
            EnsureValidTargetBinding(targetBinding);
            TileEffectLifetimeKindGuard.EnsureValid(tileEffectLifetimeKind, nameof(tileEffectLifetimeKind));

            if (pieceKind.HasValue && pieceKind.Value == ChaosChess.AI.Domain.PieceKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceKind), pieceKind, "Unknown piece kind.");
            }

            if (owner.HasValue)
            {
                EnsureValidColor(owner.Value, nameof(owner));
            }

            if (durationTurns.HasValue &&
                durationTurns.Value < 0 &&
                tileEffectLifetimeKind != TileEffectLifetimeKind.PersistentUntilTriggered)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTurns), durationTurns, "Duration cannot be negative.");
            }

            if (sharedRemainingUses.HasValue && sharedRemainingUses.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sharedRemainingUses), sharedRemainingUses, "Shared remaining uses cannot be negative.");
            }

            if (targetIndex.HasValue && targetIndex.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetIndex), targetIndex, "Target index cannot be negative.");
            }

            if (destinationTargetIndex.HasValue && destinationTargetIndex.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationTargetIndex), destinationTargetIndex, "Destination target index cannot be negative.");
            }

            if (targetBinding == CardEffectPrimitiveTargetBinding.OrderedSquareByIndex &&
                !targetIndex.HasValue)
            {
                throw new ArgumentException("Ordered square target binding requires a target index.", nameof(targetIndex));
            }

            if (destinationTargetIndex.HasValue &&
                targetBinding != CardEffectPrimitiveTargetBinding.OrderedSquareByIndex)
            {
                throw new ArgumentException(
                    "Destination target index requires ordered square target binding.",
                    nameof(destinationTargetIndex));
            }

            if (destinationTargetIndex.HasValue &&
                targetIndex.HasValue &&
                destinationTargetIndex.Value == targetIndex.Value)
            {
                throw new ArgumentException(
                    "Destination target index cannot match the source target index.",
                    nameof(destinationTargetIndex));
            }

            if (kind == CardEffectPrimitiveKind.AddTileEffect && string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Tile effect primitives require a non-empty effect type.", nameof(effectType));
            }

            if (kind == CardEffectPrimitiveKind.AddPieceEffect && string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Piece effect primitives require a non-empty effect type.", nameof(effectType));
            }

            if (kind == CardEffectPrimitiveKind.SetMovementOverride && string.IsNullOrWhiteSpace(movementOverrideCode))
            {
                throw new ArgumentException("Movement override primitives require a non-empty override code.", nameof(movementOverrideCode));
            }

            Kind = kind;
            SourceSquare = sourceSquare;
            DestinationSquare = destinationSquare;
            PieceKind = pieceKind;
            Owner = owner;
            EffectType = string.IsNullOrWhiteSpace(effectType) ? null : effectType;
            MovementOverrideCode = string.IsNullOrWhiteSpace(movementOverrideCode) ? null : movementOverrideCode;
            DurationTurns = durationTurns;
            SharedRemainingUses = sharedRemainingUses;
            TileEffectLifetimeKind = tileEffectLifetimeKind;
            TargetBinding = targetBinding;
            TargetIndex = targetIndex;
            DestinationTargetIndex = destinationTargetIndex;
        }

        public CardEffectPrimitiveKind Kind { get; }

        public Square? SourceSquare { get; }

        public Square? DestinationSquare { get; }

        public PieceKind? PieceKind { get; }

        public PieceColor? Owner { get; }

        public string? EffectType { get; }

        public string? MovementOverrideCode { get; }

        public int? DurationTurns { get; }

        public int? SharedRemainingUses { get; }

        public TileEffectLifetimeKind TileEffectLifetimeKind { get; }

        public CardEffectPrimitiveTargetBinding TargetBinding { get; }

        public int? TargetIndex { get; }

        public int? DestinationTargetIndex { get; }

        public static CardEffectPrimitive AddTileEffect(
            Square square,
            string effectType,
            PieceColor? owner,
            int? durationTurns,
            int? sharedRemainingUses = null,
            Square? destinationSquare = null,
            TileEffectLifetimeKind tileEffectLifetimeKind = TileEffectLifetimeKind.TurnLimited,
            CardEffectPrimitiveTargetBinding targetBinding = CardEffectPrimitiveTargetBinding.SelectedSquare,
            int? targetIndex = null,
            int? destinationTargetIndex = null)
        {
            return new CardEffectPrimitive(
                CardEffectPrimitiveKind.AddTileEffect,
                sourceSquare: square,
                destinationSquare: destinationSquare,
                owner: owner,
                effectType: effectType,
                durationTurns: durationTurns,
                sharedRemainingUses: sharedRemainingUses,
                tileEffectLifetimeKind: tileEffectLifetimeKind,
                targetBinding: targetBinding,
                targetIndex: targetIndex,
                destinationTargetIndex: destinationTargetIndex);
        }

        public static CardEffectPrimitive SetMovementOverride(
            Square square,
            string movementOverrideCode,
            int? durationTurns)
        {
            return new CardEffectPrimitive(
                CardEffectPrimitiveKind.SetMovementOverride,
                sourceSquare: square,
                movementOverrideCode: movementOverrideCode,
                durationTurns: durationTurns,
                targetBinding: CardEffectPrimitiveTargetBinding.SelectedPiece);
        }

        private static void EnsureValidKind(CardEffectPrimitiveKind kind)
        {
            if (kind != CardEffectPrimitiveKind.MovePiece &&
                kind != CardEffectPrimitiveKind.RemovePiece &&
                kind != CardEffectPrimitiveKind.CreatePiece &&
                kind != CardEffectPrimitiveKind.ChangePieceKind &&
                kind != CardEffectPrimitiveKind.ChangeOwner &&
                kind != CardEffectPrimitiveKind.SetMovementOverride &&
                kind != CardEffectPrimitiveKind.AddTileEffect &&
                kind != CardEffectPrimitiveKind.RemoveTileEffect &&
                kind != CardEffectPrimitiveKind.AddPieceEffect)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown effect primitive kind.");
            }
        }

        private static void EnsureValidColor(PieceColor color, string parameterName)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(parameterName, color, "Unknown piece color.");
            }
        }

        private static void EnsureValidTargetBinding(CardEffectPrimitiveTargetBinding targetBinding)
        {
            if (targetBinding != CardEffectPrimitiveTargetBinding.None &&
                targetBinding != CardEffectPrimitiveTargetBinding.SelectedPiece &&
                targetBinding != CardEffectPrimitiveTargetBinding.SelectedSquare &&
                targetBinding != CardEffectPrimitiveTargetBinding.OrderedSquareByIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(targetBinding), targetBinding, "Unknown primitive target binding.");
            }
        }
    }
}
