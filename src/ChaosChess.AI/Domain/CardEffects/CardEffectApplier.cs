using System;
using System.Collections.Generic;

namespace ChaosChess.AI.Domain.CardEffects
{
    public sealed class CardEffectApplier
    {
        public CardEffectApplicationResult Apply(
            CardEffectDefinition definition,
            CardEffectApplicationContext context)
        {
            if (definition == null)
            {
                return CardEffectApplicationResult.Failed(CardEffectApplicationCode.InvalidDefinition);
            }

            if (context == null)
            {
                return CardEffectApplicationResult.Failed(CardEffectApplicationCode.InvalidContext);
            }

            if (!string.Equals(definition.CardId, context.Plan.CardId, StringComparison.OrdinalIgnoreCase))
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.InvalidContext,
                    new[] { "Card effect definition does not match the card use plan." });
            }

            if (definition.TargetQuery.Kind != context.Plan.Target.Kind)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.IllegalTarget,
                    new[] { "Card use plan target kind does not match the effect definition query." });
            }

            CardEffectApplicationResult? targetValidation = ValidateTargetQuery(
                definition.TargetQuery,
                context.State,
                context.Plan);
            if (targetValidation != null)
            {
                return targetValidation;
            }

            var pieces = new List<PieceInfo>(context.State.BoardState.Pieces);
            var tileEffects = new List<TileEffectInfo>(context.State.TileEffects);

            foreach (CardEffectPrimitive primitive in definition.Primitives)
            {
                CardEffectApplicationResult? result = ApplyPrimitive(
                    definition,
                    context,
                    primitive,
                    pieces,
                    tileEffects);
                if (result != null)
                {
                    return result;
                }
            }

            BoardState nextBoard = new BoardState(
                pieces,
                context.State.BoardState.SideToMove,
                context.State.BoardState.CastlingRights,
                context.State.BoardState.EnPassantTarget,
                context.State.BoardState.HalfmoveClock,
                context.State.BoardState.FullmoveNumber);
            var nextState = new GameState(
                nextBoard,
                context.State.AvailableCards,
                tileEffects);

            return CardEffectApplicationResult.Exact(nextState);
        }

        private static CardEffectApplicationResult? ApplyPrimitive(
            CardEffectDefinition definition,
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces,
            IList<TileEffectInfo> tileEffects)
        {
            switch (primitive.Kind)
            {
                case CardEffectPrimitiveKind.AddTileEffect:
                    return ApplyAddTileEffect(definition, context, primitive, pieces, tileEffects);

                case CardEffectPrimitiveKind.RemoveTileEffect:
                    return ApplyRemoveTileEffect(context, primitive, tileEffects);

                case CardEffectPrimitiveKind.MovePiece:
                    return ApplyMovePiece(context, primitive, pieces);

                case CardEffectPrimitiveKind.SetMovementOverride:
                    return CardEffectApplicationResult.Unsupported(
                        CardEffectApplicationCode.UnsupportedEffect,
                        new[] { "Movement override effects are not represented by the current GameState contract." });

                default:
                    return CardEffectApplicationResult.Unsupported(
                        CardEffectApplicationCode.UnsupportedEffect,
                        new[] { "This effect primitive is not supported by the current CardEffectApplier." });
            }
        }

        private static CardEffectApplicationResult? ApplyAddTileEffect(
            CardEffectDefinition definition,
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces,
            IList<TileEffectInfo> tileEffects)
        {
            if (!primitive.DurationTurns.HasValue)
            {
                return CardEffectApplicationResult.Unsupported(
                    CardEffectApplicationCode.UnsupportedEffect,
                    new[] { "Tile effect duration is not represented for this definition." });
            }

            if (!TryResolveSquare(context.Plan, primitive, out Square square, out CardEffectApplicationResult? failure))
            {
                return failure;
            }

            if (!TryResolveDestinationSquare(context.Plan, primitive, out Square? destination, out failure))
            {
                return failure;
            }

            if (FindPiece(pieces, square) != null || HasTileEffect(tileEffects, square))
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "Tile effect target square is no longer selectable." });
            }

            tileEffects.Add(new TileEffectInfo(
                CreateTileEffectId(definition.CardId, primitive.EffectType!, square),
                primitive.EffectType!,
                square,
                primitive.Owner ?? context.Owner,
                primitive.DurationTurns.Value,
                destination,
                primitive.SharedRemainingUses,
                primitive.TileEffectLifetimeKind));
            return null;
        }

        private static CardEffectApplicationResult? ApplyRemoveTileEffect(
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<TileEffectInfo> tileEffects)
        {
            if (!TryResolveSquare(context.Plan, primitive, out Square square, out CardEffectApplicationResult? failure))
            {
                return failure;
            }

            for (int i = tileEffects.Count - 1; i >= 0; i--)
            {
                if (tileEffects[i].Square == square)
                {
                    tileEffects.RemoveAt(i);
                    return null;
                }
            }

            return CardEffectApplicationResult.Failed(
                CardEffectApplicationCode.StaleTarget,
                new[] { "No tile effect exists at the selected square." });
        }

        private static CardEffectApplicationResult? ApplyMovePiece(
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces)
        {
            Square? source = primitive.SourceSquare;
            Square? destination = primitive.DestinationSquare;

            if (!source.HasValue || !destination.HasValue)
            {
                return CardEffectApplicationResult.Unsupported(
                    CardEffectApplicationCode.UnsupportedEffect,
                    new[] { "MovePiece requires explicit source and destination squares." });
            }

            PieceInfo? piece = FindPiece(pieces, source.Value);
            if (piece == null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "MovePiece source square has no piece." });
            }

            if (FindPiece(pieces, destination.Value) != null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "MovePiece destination square is occupied." });
            }

            pieces.Remove(piece);
            pieces.Add(new PieceInfo(
                piece.Kind,
                piece.Color,
                destination.Value,
                piece.FenCode));
            return null;
        }

        private static CardEffectApplicationResult? ValidateTargetQuery(
            CardTargetQuery query,
            GameState state,
            CardUsePlan plan)
        {
            if (query.Count != GetPlanTargetCount(plan))
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.IllegalTarget,
                    new[] { "Card use plan target count does not match the effect definition query." });
            }

            if (plan.Target.Piece != null)
            {
                PieceInfo? piece = state.BoardState.FindPiece(plan.Target.Piece.Square);
                if (piece == null ||
                    piece.Color != plan.Target.Piece.ExpectedColor ||
                    piece.Kind != plan.Target.Piece.ExpectedKind)
                {
                    return CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.StaleTarget,
                        new[] { "Selected piece target no longer matches the expected snapshot." });
                }

                if (!MatchesOwnerRelation(query.OwnerRelation, plan.Actor, piece.Color))
                {
                    return CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.IllegalTarget,
                        new[] { "Selected piece owner does not match the target query relation." });
                }
            }

            foreach (Square square in plan.Target.Squares)
            {
                bool hasPiece = state.BoardState.FindPiece(square) != null;
                if (query.RequiresEmptySquares && hasPiece)
                {
                    return CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.StaleTarget,
                        new[] { "Selected square is occupied." });
                }

                if (query.RequiresOccupiedSquares && !hasPiece)
                {
                    return CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.StaleTarget,
                        new[] { "Selected square is empty." });
                }

                if (!query.AllowsExistingTileEffect && HasTileEffect(state.TileEffects, square))
                {
                    return CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.StaleTarget,
                        new[] { "Selected square already has a tile effect." });
                }
            }

            return null;
        }

        private static bool TryResolveSquare(
            CardUsePlan plan,
            CardEffectPrimitive primitive,
            out Square square,
            out CardEffectApplicationResult? failure)
        {
            failure = null;

            switch (primitive.TargetBinding)
            {
                case CardEffectPrimitiveTargetBinding.SelectedPiece:
                    if (plan.Target.Piece != null)
                    {
                        square = plan.Target.Piece.Square;
                        return true;
                    }
                    break;

                case CardEffectPrimitiveTargetBinding.SelectedSquare:
                    if (plan.Target.Squares.Count == 1)
                    {
                        square = plan.Target.Squares[0];
                        return true;
                    }
                    break;

                case CardEffectPrimitiveTargetBinding.OrderedSquareByIndex:
                    if (primitive.TargetIndex.HasValue &&
                        primitive.TargetIndex.Value < plan.Target.Squares.Count)
                    {
                        square = plan.Target.Squares[primitive.TargetIndex.Value];
                        return true;
                    }
                    break;

                case CardEffectPrimitiveTargetBinding.None:
                    if (primitive.SourceSquare.HasValue)
                    {
                        square = primitive.SourceSquare.Value;
                        return true;
                    }
                    break;
            }

            square = default;
            failure = CardEffectApplicationResult.Failed(
                CardEffectApplicationCode.InvalidDefinition,
                new[] { "Primitive target binding cannot be resolved from the card use plan." });
            return false;
        }

        private static bool TryResolveDestinationSquare(
            CardUsePlan plan,
            CardEffectPrimitive primitive,
            out Square? destination,
            out CardEffectApplicationResult? failure)
        {
            failure = null;

            if (primitive.DestinationSquare.HasValue)
            {
                destination = primitive.DestinationSquare.Value;
                return true;
            }

            if (!primitive.DestinationTargetIndex.HasValue)
            {
                destination = null;
                return true;
            }

            int index = primitive.DestinationTargetIndex.Value;
            if (index < plan.Target.Squares.Count)
            {
                destination = plan.Target.Squares[index];
                return true;
            }

            destination = null;
            failure = CardEffectApplicationResult.Failed(
                CardEffectApplicationCode.InvalidDefinition,
                new[] { "Primitive destination target binding cannot be resolved from the card use plan." });
            return false;
        }

        private static int GetPlanTargetCount(CardUsePlan plan)
        {
            switch (plan.Target.Kind)
            {
                case CardTargetKind.None:
                    return 0;
                case CardTargetKind.PieceAtSquare:
                    return plan.Target.Piece == null ? 0 : 1;
                case CardTargetKind.BoardSquare:
                case CardTargetKind.OrderedSquares:
                    return plan.Target.Squares.Count;
                default:
                    return plan.Target.Squares.Count;
            }
        }

        private static bool MatchesOwnerRelation(
            CardTargetOwnerRelation relation,
            PieceColor actor,
            PieceColor target)
        {
            switch (relation)
            {
                case CardTargetOwnerRelation.Self:
                    return target == actor;
                case CardTargetOwnerRelation.Opponent:
                    return target != actor;
                case CardTargetOwnerRelation.Any:
                    return true;
                default:
                    return false;
            }
        }

        private static PieceInfo? FindPiece(IEnumerable<PieceInfo> pieces, Square square)
        {
            foreach (PieceInfo piece in pieces)
            {
                if (piece.Square == square)
                {
                    return piece;
                }
            }

            return null;
        }

        private static bool HasTileEffect(IEnumerable<TileEffectInfo> tileEffects, Square square)
        {
            foreach (TileEffectInfo effect in tileEffects)
            {
                if (effect.Square == square)
                {
                    return true;
                }
            }

            return false;
        }

        private static string CreateTileEffectId(
            string cardId,
            string effectType,
            Square square)
        {
            return cardId + ":" + effectType + ":" + square;
        }
    }
}
