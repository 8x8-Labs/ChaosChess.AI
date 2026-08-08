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
            var whiteCapturedPieces = new List<PieceKind>(context.State.CapturedPieces.WhitePieces);
            var blackCapturedPieces = new List<PieceKind>(context.State.CapturedPieces.BlackPieces);
            var timeReversals = new List<TimeReversalState>(context.State.TimeReversals);
            CastlingRights castlingRights = context.State.BoardState.CastlingRights;
            Square? enPassantTarget = context.State.BoardState.EnPassantTarget;

            foreach (CardEffectPrimitive primitive in definition.Primitives)
            {
                CardEffectApplicationResult? result = ApplyPrimitive(
                    definition,
                    context,
                    primitive,
                    pieces,
                    tileEffects,
                    whiteCapturedPieces,
                    blackCapturedPieces,
                    timeReversals,
                    ref castlingRights,
                    ref enPassantTarget);
                if (result != null)
                {
                    return result;
                }
            }

            BoardState nextBoard = new BoardState(
                pieces,
                context.State.BoardState.SideToMove,
                castlingRights,
                enPassantTarget,
                context.State.BoardState.HalfmoveClock,
                context.State.BoardState.FullmoveNumber);
            var nextState = new GameState(
                nextBoard,
                context.State.AvailableCards,
                tileEffects,
                new CapturedPieceState(whiteCapturedPieces, blackCapturedPieces),
                timeReversals);

            return CardEffectApplicationResult.Exact(nextState);
        }

        private static CardEffectApplicationResult? ApplyPrimitive(
            CardEffectDefinition definition,
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces,
            IList<TileEffectInfo> tileEffects,
            IList<PieceKind> whiteCapturedPieces,
            IList<PieceKind> blackCapturedPieces,
            IList<TimeReversalState> timeReversals,
            ref CastlingRights castlingRights,
            ref Square? enPassantTarget)
        {
            switch (primitive.Kind)
            {
                case CardEffectPrimitiveKind.AddTileEffect:
                    return ApplyAddTileEffect(definition, context, primitive, pieces, tileEffects);

                case CardEffectPrimitiveKind.AddMirroredTileEffectPair:
                    return ApplyAddMirroredTileEffectPair(definition, context, primitive, pieces, tileEffects);

                case CardEffectPrimitiveKind.RemoveTileEffect:
                    return ApplyRemoveTileEffect(context, primitive, tileEffects);

                case CardEffectPrimitiveKind.MovePiece:
                    return ApplyMovePiece(context, primitive, pieces);

                case CardEffectPrimitiveKind.CreatePiece:
                    return ApplyCreatePiece(context, primitive, pieces, whiteCapturedPieces, blackCapturedPieces);

                case CardEffectPrimitiveKind.ChangePieceKind:
                    return ApplyChangePieceKind(context, primitive, pieces);

                case CardEffectPrimitiveKind.FlipBoardPerspective:
                    ApplyFlipBoardPerspective(pieces, tileEffects, ref castlingRights, ref enPassantTarget);
                    return null;

                case CardEffectPrimitiveKind.MergeSelectedPieceIntoNearestAlly:
                    return ApplyMergeSelectedPieceIntoNearestAlly(context, primitive, pieces);

                case CardEffectPrimitiveKind.SwapSelectedPieceWithActorKing:
                    return ApplySwapSelectedPieceWithActorKing(context, pieces);

                case CardEffectPrimitiveKind.SetMovementOverride:
                    return CardEffectApplicationResult.Unsupported(
                        CardEffectApplicationCode.UnsupportedEffect,
                        new[] { "Movement override effects are not represented by the current GameState contract." });

                case CardEffectPrimitiveKind.AddPieceEffect:
                    return CardEffectApplicationResult.Unsupported(
                        CardEffectApplicationCode.UnsupportedEffect,
                        new[] { "Piece-attached effects are not represented by the current GameState contract." });

                case CardEffectPrimitiveKind.AddGlobalEffect:
                    return ApplyAddGlobalEffect(definition, context, primitive, timeReversals);

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

        private static CardEffectApplicationResult? ApplyAddGlobalEffect(
            CardEffectDefinition definition,
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<TimeReversalState> timeReversals)
        {
            if (!string.Equals(primitive.EffectType, "TimeReversal", StringComparison.Ordinal))
            {
                return CardEffectApplicationResult.Unsupported(
                    CardEffectApplicationCode.UnsupportedEffect,
                    new[] { "Global ongoing effects are not represented by the current GameState contract." });
            }

            if (!primitive.DurationTurns.HasValue)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.InvalidDefinition,
                    new[] { "TimeReversal global effect requires a duration." });
            }

            timeReversals.Add(new TimeReversalState(
                CreateGlobalEffectId(definition.CardId, primitive.EffectType!, timeReversals.Count),
                primitive.Owner ?? context.Owner,
                primitive.DurationTurns.Value,
                context.State.BoardState));
            return null;
        }

        private static CardEffectApplicationResult? ApplyAddMirroredTileEffectPair(
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
                    new[] { "Mirrored tile effect pair duration is not represented for this definition." });
            }

            if (!TryResolveSquare(context.Plan, primitive, out Square square, out CardEffectApplicationResult? failure))
            {
                return failure;
            }

            Square mirrored = CreateMirroredSquare(square);

            if (FindPiece(pieces, square) != null ||
                HasTileEffect(tileEffects, square) ||
                HasTileEffect(tileEffects, mirrored))
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "Mirrored tile effect pair target is no longer selectable." });
            }

            tileEffects.Add(new TileEffectInfo(
                CreateTileEffectId(definition.CardId, primitive.EffectType!, square),
                primitive.EffectType!,
                square,
                primitive.Owner ?? context.Owner,
                primitive.DurationTurns.Value,
                mirrored,
                primitive.SharedRemainingUses,
                primitive.TileEffectLifetimeKind));
            tileEffects.Add(new TileEffectInfo(
                CreateTileEffectId(definition.CardId, primitive.EffectType!, mirrored),
                primitive.EffectType!,
                mirrored,
                primitive.Owner ?? context.Owner,
                primitive.DurationTurns.Value,
                square,
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

            if (!source.HasValue)
            {
                if (primitive.TargetBinding == CardEffectPrimitiveTargetBinding.None)
                {
                    return CardEffectApplicationResult.Unsupported(
                        CardEffectApplicationCode.UnsupportedEffect,
                        new[] { "MovePiece requires a source square." });
                }

                if (!TryResolveSquare(context.Plan, primitive, out Square resolvedSource, out CardEffectApplicationResult? failure))
                {
                    return failure;
                }

                source = resolvedSource;
            }

            if (!destination.HasValue)
            {
                if (primitive.DestinationBinding == CardEffectPrimitiveDestinationBinding.None &&
                    !primitive.DestinationTargetIndex.HasValue)
                {
                    return CardEffectApplicationResult.Unsupported(
                        CardEffectApplicationCode.UnsupportedEffect,
                        new[] { "MovePiece requires a destination square." });
                }

                if (!TryResolveDestinationSquare(context.Plan, primitive, out Square? resolvedDestination, out CardEffectApplicationResult? failure))
                {
                    return failure;
                }

                destination = resolvedDestination;
            }

            if (!destination.HasValue)
            {
                return CardEffectApplicationResult.Unsupported(
                    CardEffectApplicationCode.UnsupportedEffect,
                    new[] { "MovePiece requires a destination square." });
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
                piece.FenCode,
                piece.IsPromotioned,
                piece.StartSquare));
            return null;
        }

        private static CardEffectApplicationResult? ApplyChangePieceKind(
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces)
        {
            PieceInfo? selected = FindSelectedPiece(context, pieces);
            if (selected == null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "ChangePieceKind source piece no longer matches the selected target." });
            }

            if (!primitive.PieceKind.HasValue)
            {
                return CardEffectApplicationResult.Unsupported(
                    CardEffectApplicationCode.UnsupportedEffect,
                    new[] { "ChangePieceKind requires a result piece kind." });
            }

            pieces.Remove(selected);
            pieces.Add(new PieceInfo(
                primitive.PieceKind.Value,
                selected.Color,
                selected.Square,
                GetFenCode(primitive.PieceKind.Value)));
            return null;
        }

        private static CardEffectApplicationResult? ApplyCreatePiece(
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces,
            IList<PieceKind> whiteCapturedPieces,
            IList<PieceKind> blackCapturedPieces)
        {
            if (!TryResolveSquare(context.Plan, primitive, out Square square, out CardEffectApplicationResult? failure))
            {
                return failure;
            }

            if (!TryResolveCreatedPieceKind(
                context,
                primitive,
                whiteCapturedPieces,
                blackCapturedPieces,
                out PieceKind createdKind,
                out failure))
            {
                return failure;
            }

            if (FindPiece(pieces, square) != null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "CreatePiece target square is occupied." });
            }

            pieces.Add(new PieceInfo(
                createdKind,
                primitive.Owner ?? context.Owner,
                square,
                GetFenCode(createdKind)));
            return null;
        }

        private static bool TryResolveCreatedPieceKind(
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceKind> whiteCapturedPieces,
            IList<PieceKind> blackCapturedPieces,
            out PieceKind pieceKind,
            out CardEffectApplicationResult? failure)
        {
            pieceKind = PieceKind.Unknown;
            failure = null;

            if (primitive.PieceKind.HasValue)
            {
                pieceKind = primitive.PieceKind.Value;
                return true;
            }

            switch (primitive.PieceKindBinding)
            {
                case CardEffectPrimitivePieceKindBinding.ActorHighestValueCapturedOrWall:
                    PieceColor owner = primitive.Owner ?? context.Owner;
                    IList<PieceKind> capturedPieces = owner == PieceColor.White
                        ? whiteCapturedPieces
                        : blackCapturedPieces;
                    pieceKind = TakeHighestValueCapturedOrWall(capturedPieces);
                    return true;

                case CardEffectPrimitivePieceKindBinding.None:
                    failure = CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.InvalidDefinition,
                        new[] { "CreatePiece requires a piece kind." });
                    return false;

                default:
                    failure = CardEffectApplicationResult.Failed(
                        CardEffectApplicationCode.InvalidDefinition,
                        new[] { "CreatePiece has an unknown piece kind binding." });
                    return false;
            }
        }

        private static PieceKind TakeHighestValueCapturedOrWall(IList<PieceKind> capturedPieces)
        {
            if (capturedPieces.Count == 0)
            {
                return PieceKind.Wall;
            }

            int bestIndex = 0;
            int bestValue = GetPieceValue(capturedPieces[0]);

            for (int i = 1; i < capturedPieces.Count; i++)
            {
                int value = GetPieceValue(capturedPieces[i]);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            PieceKind result = capturedPieces[bestIndex];
            capturedPieces.RemoveAt(bestIndex);
            return result;
        }

        private static void ApplyFlipBoardPerspective(
            IList<PieceInfo> pieces,
            IList<TileEffectInfo> tileEffects,
            ref CastlingRights castlingRights,
            ref Square? enPassantTarget)
        {
            var flippedPieces = new List<PieceInfo>(pieces.Count);
            foreach (PieceInfo piece in pieces)
            {
                PieceColor nextColor = piece.Color == PieceColor.White
                    ? PieceColor.Black
                    : PieceColor.White;
                flippedPieces.Add(new PieceInfo(
                    piece.Kind,
                    nextColor,
                    new Square(piece.Square.File, Square.BoardSize - 1 - piece.Square.Rank),
                    piece.FenCode,
                    piece.IsPromotioned,
                    piece.StartSquare.HasValue
                        ? (Square?)new Square(
                            piece.StartSquare.Value.File,
                            Square.BoardSize - 1 - piece.StartSquare.Value.Rank)
                        : null));
            }

            pieces.Clear();
            foreach (PieceInfo piece in flippedPieces)
            {
                pieces.Add(piece);
            }

            tileEffects.Clear();
            castlingRights = FlipCastlingRights(castlingRights);
            enPassantTarget = enPassantTarget.HasValue
                ? (Square?)new Square(
                    Square.BoardSize - 1 - enPassantTarget.Value.File,
                    Square.BoardSize - 1 - enPassantTarget.Value.Rank)
                : null;
        }

        private static CardEffectApplicationResult? ApplyMergeSelectedPieceIntoNearestAlly(
            CardEffectApplicationContext context,
            CardEffectPrimitive primitive,
            IList<PieceInfo> pieces)
        {
            PieceInfo? selected = FindSelectedPiece(context, pieces);
            if (selected == null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "Merge source piece no longer matches the selected target." });
            }

            if (!primitive.PieceKind.HasValue ||
                !TryParsePieceKind(primitive.EffectType, out PieceKind nearestKind))
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.InvalidDefinition,
                    new[] { "Merge primitive requires result and nearest ally piece kinds." });
            }

            PieceInfo? nearest = FindNearestPiece(
                pieces,
                selected,
                nearestKind);
            if (nearest == null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "Merge primitive could not find a nearest matching ally." });
            }

            pieces.Remove(selected);
            pieces.Remove(nearest);
            pieces.Add(new PieceInfo(
                primitive.PieceKind.Value,
                nearest.Color,
                nearest.Square,
                GetFenCode(primitive.PieceKind.Value)));
            return null;
        }

        private static CardEffectApplicationResult? ApplySwapSelectedPieceWithActorKing(
            CardEffectApplicationContext context,
            IList<PieceInfo> pieces)
        {
            PieceInfo? selected = FindSelectedPiece(context, pieces);
            if (selected == null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "Swap target piece no longer matches the selected target." });
            }

            PieceInfo? king = null;
            foreach (PieceInfo piece in pieces)
            {
                if (piece.Color == context.Plan.Actor && piece.Kind == PieceKind.King)
                {
                    king = piece;
                    break;
                }
            }

            if (king == null)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.StaleTarget,
                    new[] { "Swap primitive could not find the actor king." });
            }

            if (king.Square == selected.Square)
            {
                return CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.IllegalTarget,
                    new[] { "Swap target cannot be the actor king." });
            }

            pieces.Remove(selected);
            pieces.Remove(king);
            pieces.Add(new PieceInfo(
                selected.Kind,
                selected.Color,
                king.Square,
                selected.FenCode));
            pieces.Add(new PieceInfo(
                king.Kind,
                king.Color,
                selected.Square,
                king.FenCode));
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

            foreach (PieceTargetSnapshot pieceTarget in plan.Target.Pieces)
            {
                PieceInfo? piece = state.BoardState.FindPiece(pieceTarget.Square);
                if (piece == null ||
                    piece.Color != pieceTarget.ExpectedColor ||
                    piece.Kind != pieceTarget.ExpectedKind)
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

            if (primitive.DestinationBinding == CardEffectPrimitiveDestinationBinding.SelectedPieceStartSquare)
            {
                if (plan.Target.Piece != null && plan.Target.Piece.StartSquare.HasValue)
                {
                    destination = plan.Target.Piece.StartSquare.Value;
                    return true;
                }

                destination = null;
                failure = CardEffectApplicationResult.Failed(
                    CardEffectApplicationCode.InvalidContext,
                    new[] { "Selected piece start square destination binding requires target start square metadata." });
                return false;
            }

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
                case CardTargetKind.PieceAndSquare:
                    return (plan.Target.Piece == null ? 0 : 1) + plan.Target.Squares.Count;
                case CardTargetKind.OrderedPieces:
                    return plan.Target.Pieces.Count;
                case CardTargetKind.BoardSquare:
                case CardTargetKind.OrderedSquares:
                    return plan.Target.Squares.Count;
                default:
                    return plan.Target.Squares.Count;
            }
        }

        private static CastlingRights FlipCastlingRights(CastlingRights rights)
        {
            CastlingRights flipped = CastlingRights.None;

            if ((rights & CastlingRights.WhiteKingSide) != 0)
            {
                flipped |= CastlingRights.BlackKingSide;
            }

            if ((rights & CastlingRights.WhiteQueenSide) != 0)
            {
                flipped |= CastlingRights.BlackQueenSide;
            }

            if ((rights & CastlingRights.BlackKingSide) != 0)
            {
                flipped |= CastlingRights.WhiteKingSide;
            }

            if ((rights & CastlingRights.BlackQueenSide) != 0)
            {
                flipped |= CastlingRights.WhiteQueenSide;
            }

            return flipped;
        }

        private static string GetFenCode(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return "p";
                case PieceKind.Knight:
                    return "n";
                case PieceKind.Bishop:
                    return "b";
                case PieceKind.Rook:
                    return "r";
                case PieceKind.Queen:
                    return "q";
                case PieceKind.King:
                    return "k";
                case PieceKind.Wall:
                    return "a";
                case PieceKind.Amazon:
                    return "s";
                case PieceKind.Chancellor:
                    return "y";
                case PieceKind.KnightRider:
                    return "z";
                default:
                    return "?";
            }
        }

        private static int GetPieceValue(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return 1;
                case PieceKind.Knight:
                case PieceKind.Bishop:
                case PieceKind.King:
                    return 3;
                case PieceKind.Rook:
                    return 5;
                case PieceKind.KnightRider:
                    return 7;
                case PieceKind.Queen:
                case PieceKind.Chancellor:
                    return 9;
                case PieceKind.Amazon:
                    return 13;
                default:
                    return 0;
            }
        }

        private static PieceInfo? FindSelectedPiece(
            CardEffectApplicationContext context,
            IEnumerable<PieceInfo> pieces)
        {
            if (context.Plan.Target.Piece == null)
            {
                return null;
            }

            PieceInfo? selected = FindPiece(pieces, context.Plan.Target.Piece.Square);
            if (selected == null ||
                selected.Color != context.Plan.Target.Piece.ExpectedColor ||
                selected.Kind != context.Plan.Target.Piece.ExpectedKind)
            {
                return null;
            }

            return selected;
        }

        private static PieceInfo? FindNearestPiece(
            IEnumerable<PieceInfo> pieces,
            PieceInfo selected,
            PieceKind nearestKind)
        {
            PieceInfo? nearest = null;
            int nearestDistance = int.MaxValue;

            foreach (PieceInfo piece in pieces)
            {
                if (piece == selected ||
                    piece.Color != selected.Color ||
                    piece.Kind != nearestKind)
                {
                    continue;
                }

                int distance = SquaredDistance(selected.Square, piece.Square);
                if (distance < nearestDistance)
                {
                    nearest = piece;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static int SquaredDistance(Square left, Square right)
        {
            int file = left.File - right.File;
            int rank = left.Rank - right.Rank;
            return (file * file) + (rank * rank);
        }

        private static bool TryParsePieceKind(string? value, out PieceKind kind)
        {
            kind = PieceKind.Unknown;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Enum.TryParse(value, ignoreCase: true, result: out kind) &&
                kind != PieceKind.Unknown;
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

        private static Square CreateMirroredSquare(Square square)
        {
            return new Square(Square.BoardSize - 1 - square.File, square.Rank);
        }

        private static string CreateTileEffectId(
            string cardId,
            string effectType,
            Square square)
        {
            return cardId + ":" + effectType + ":" + square;
        }

        private static string CreateGlobalEffectId(
            string cardId,
            string effectType,
            int index)
        {
            return cardId + ":" + effectType + ":" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
