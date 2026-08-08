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
            CardEffectPrimitiveDestinationBinding destinationBinding = CardEffectPrimitiveDestinationBinding.None,
            CardEffectPrimitivePieceKindBinding pieceKindBinding = CardEffectPrimitivePieceKindBinding.None,
            int? targetIndex = null,
            int? destinationTargetIndex = null)
        {
            EnsureValidKind(kind);
            EnsureValidTargetBinding(targetBinding);
            EnsureValidDestinationBinding(destinationBinding);
            EnsureValidPieceKindBinding(pieceKindBinding);
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
                targetBinding != CardEffectPrimitiveTargetBinding.OrderedSquareByIndex &&
                targetBinding != CardEffectPrimitiveTargetBinding.SelectedPiece)
            {
                throw new ArgumentException(
                    "Destination target index requires a selected piece or ordered square target binding.",
                    nameof(destinationTargetIndex));
            }

            if (destinationBinding != CardEffectPrimitiveDestinationBinding.None &&
                (destinationSquare.HasValue || destinationTargetIndex.HasValue))
            {
                throw new ArgumentException(
                    "Destination binding cannot be combined with an explicit destination square or destination target index.",
                    nameof(destinationBinding));
            }

            if (destinationBinding == CardEffectPrimitiveDestinationBinding.SelectedPieceStartSquare &&
                kind != CardEffectPrimitiveKind.MovePiece)
            {
                throw new ArgumentException(
                    "Selected piece start square destination binding is only supported for MovePiece primitives.",
                    nameof(destinationBinding));
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

            if (kind == CardEffectPrimitiveKind.AddMirroredTileEffectPair && string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Mirrored tile effect pair primitives require a non-empty effect type.", nameof(effectType));
            }

            if (kind == CardEffectPrimitiveKind.AddPieceEffect && string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Piece effect primitives require a non-empty effect type.", nameof(effectType));
            }

            if (kind == CardEffectPrimitiveKind.AddGlobalEffect && string.IsNullOrWhiteSpace(effectType))
            {
                throw new ArgumentException("Global effect primitives require a non-empty effect type.", nameof(effectType));
            }

            if (kind == CardEffectPrimitiveKind.CreatePiece &&
                !pieceKind.HasValue &&
                pieceKindBinding == CardEffectPrimitivePieceKindBinding.None)
            {
                throw new ArgumentException("Create piece primitives require a piece kind or piece kind binding.", nameof(pieceKind));
            }

            if (pieceKindBinding != CardEffectPrimitivePieceKindBinding.None &&
                kind != CardEffectPrimitiveKind.CreatePiece)
            {
                throw new ArgumentException("Piece kind binding is only supported for CreatePiece primitives.", nameof(pieceKindBinding));
            }

            if (kind == CardEffectPrimitiveKind.MergeSelectedPieceIntoNearestAlly &&
                (!pieceKind.HasValue || string.IsNullOrWhiteSpace(effectType)))
            {
                throw new ArgumentException(
                    "Merge primitives require a result piece kind and nearest ally effect type.",
                    nameof(pieceKind));
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
            DestinationBinding = destinationBinding;
            PieceKindBinding = pieceKindBinding;
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

        public CardEffectPrimitiveDestinationBinding DestinationBinding { get; }

        public CardEffectPrimitivePieceKindBinding PieceKindBinding { get; }

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
            CardEffectPrimitiveDestinationBinding destinationBinding = CardEffectPrimitiveDestinationBinding.None,
            CardEffectPrimitivePieceKindBinding pieceKindBinding = CardEffectPrimitivePieceKindBinding.None,
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
                destinationBinding: destinationBinding,
                pieceKindBinding: pieceKindBinding,
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
                kind != CardEffectPrimitiveKind.AddPieceEffect &&
                kind != CardEffectPrimitiveKind.AddGlobalEffect &&
                kind != CardEffectPrimitiveKind.FlipBoardPerspective &&
                kind != CardEffectPrimitiveKind.MergeSelectedPieceIntoNearestAlly &&
                kind != CardEffectPrimitiveKind.SwapSelectedPieceWithActorKing &&
                kind != CardEffectPrimitiveKind.AddMirroredTileEffectPair)
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

        private static void EnsureValidDestinationBinding(CardEffectPrimitiveDestinationBinding destinationBinding)
        {
            if (destinationBinding != CardEffectPrimitiveDestinationBinding.None &&
                destinationBinding != CardEffectPrimitiveDestinationBinding.SelectedPieceStartSquare)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationBinding), destinationBinding, "Unknown primitive destination binding.");
            }
        }

        private static void EnsureValidPieceKindBinding(CardEffectPrimitivePieceKindBinding pieceKindBinding)
        {
            if (pieceKindBinding != CardEffectPrimitivePieceKindBinding.None &&
                pieceKindBinding != CardEffectPrimitivePieceKindBinding.ActorHighestValueCapturedOrWall)
            {
                throw new ArgumentOutOfRangeException(nameof(pieceKindBinding), pieceKindBinding, "Unknown primitive piece kind binding.");
            }
        }
    }
}
