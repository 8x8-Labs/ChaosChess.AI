using System;
using System.Collections.Generic;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Domain
{
    public sealed class CardUsePlanValidator
    {
        private readonly DefaultCardPlanningCatalog _catalog;

        public CardUsePlanValidator()
            : this(new DefaultCardPlanningCatalog())
        {
        }

        public CardUsePlanValidator(DefaultCardPlanningCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public CardPlanValidationResult Validate(
            GameState? gameState,
            CardUsePlan? plan)
        {
            if (gameState == null)
            {
                return Invalid(
                    CardPlanValidationCode.NullGameState,
                    "Game state is null.");
            }

            if (plan == null)
            {
                return Invalid(
                    CardPlanValidationCode.NullPlan,
                    "Card use plan is null.");
            }

            CardInfo? card = FindCard(gameState, plan.CardId);
            if (card == null)
            {
                return Invalid(
                    CardPlanValidationCode.CardNotInHand,
                    $"Card '{plan.CardId}' is not in hand.");
            }

            if (card.RemainingUses <= 0)
            {
                return Invalid(
                    CardPlanValidationCode.CardHasNoRemainingUses,
                    $"Card '{plan.CardId}' has no remaining uses.");
            }

            CardPlanningDefinition definition = _catalog.GetDefinition(plan.CardId);
            if (!definition.IsSupported)
            {
                return Invalid(
                    CardPlanValidationCode.UnsupportedCard,
                    $"Card '{plan.CardId}' is not supported for planning.");
            }

            if (plan.Actor != gameState.BoardState.SideToMove)
            {
                return Invalid(
                    CardPlanValidationCode.ActorDoesNotMatchSideToMove,
                    "Plan actor does not match side to move.");
            }

            if (plan.Target.Kind != definition.RequiredTargetKind)
            {
                return Invalid(
                    CardPlanValidationCode.TargetKindMismatch,
                    $"Card '{plan.CardId}' requires target kind '{definition.RequiredTargetKind}'.");
            }

            int actualTargetCount = GetTargetCount(plan.Target);
            if (actualTargetCount != definition.RequiredTargetCount)
            {
                return Invalid(
                    CardPlanValidationCode.TargetCountMismatch,
                    $"Card '{plan.CardId}' requires {definition.RequiredTargetCount} target(s).");
            }

            switch (definition.RequiredTargetKind)
            {
                case CardTargetKind.None:
                    return CardPlanValidationResult.Valid();

                case CardTargetKind.PieceAtSquare:
                    return ValidatePieceAtSquare(gameState, plan, definition);

                case CardTargetKind.PieceAndSquare:
                    CardPlanValidationResult pieceResult = ValidatePieceAtSquare(gameState, plan, definition);
                    return pieceResult.IsValid
                        ? ValidateBoardSquares(gameState, plan.Target.Squares)
                        : pieceResult;

                case CardTargetKind.OrderedPieces:
                    CardPlanValidationResult pieceDuplicateResult = ValidateNoDuplicateSquares(plan.Target.Squares);
                    return pieceDuplicateResult.IsValid
                        ? ValidateOrderedPieces(gameState, plan, definition)
                        : pieceDuplicateResult;

                case CardTargetKind.BoardSquare:
                    return ValidateBoardSquares(gameState, plan.Target.Squares);

                case CardTargetKind.OrderedSquares:
                    CardPlanValidationResult duplicateResult = ValidateNoDuplicateSquares(plan.Target.Squares);
                    return duplicateResult.IsValid
                        ? ValidateBoardSquares(gameState, plan.Target.Squares)
                        : duplicateResult;

                default:
                    return Invalid(
                        CardPlanValidationCode.TargetKindMismatch,
                        $"Unknown target kind '{definition.RequiredTargetKind}'.");
            }
        }

        private static CardInfo? FindCard(GameState gameState, string cardId)
        {
            foreach (CardInfo card in gameState.AvailableCards)
            {
                if (string.Equals(card.Id, cardId, StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }

            return null;
        }

        private static int GetTargetCount(CardTargetSelection target)
        {
            switch (target.Kind)
            {
                case CardTargetKind.None:
                    return 0;
                case CardTargetKind.PieceAtSquare:
                    return target.Piece == null ? 0 : 1;
                case CardTargetKind.PieceAndSquare:
                    return (target.Piece == null ? 0 : 1) + target.Squares.Count;
                case CardTargetKind.OrderedPieces:
                    return target.Pieces.Count;
                case CardTargetKind.BoardSquare:
                case CardTargetKind.OrderedSquares:
                    return target.Squares.Count;
                default:
                    return target.Squares.Count;
            }
        }

        private static CardPlanValidationResult ValidatePieceAtSquare(
            GameState gameState,
            CardUsePlan plan,
            CardPlanningDefinition definition)
        {
            PieceTargetSnapshot pieceTarget = plan.Target.Piece
                ?? throw new InvalidOperationException("Piece target selection contains no piece snapshot.");
            return ValidatePieceTarget(gameState, plan, definition, pieceTarget);
        }

        private static CardPlanValidationResult ValidatePieceTarget(
            GameState gameState,
            CardUsePlan plan,
            CardPlanningDefinition definition,
            PieceTargetSnapshot pieceTarget)
        {
            PieceInfo? piece = gameState.BoardState.FindPiece(pieceTarget.Square);

            if (piece == null)
            {
                return Invalid(
                    CardPlanValidationCode.TargetPieceMissing,
                    $"No piece exists at {pieceTarget.Square}.");
            }

            if (piece.Color != pieceTarget.ExpectedColor ||
                !MatchesOwnerRelation(definition.RequiredTargetOwnerRelation, plan.Actor, piece.Color))
            {
                return Invalid(
                    CardPlanValidationCode.TargetPieceColorMismatch,
                    $"Piece at {pieceTarget.Square} has an unexpected color.");
            }

            if (piece.Kind != pieceTarget.ExpectedKind)
            {
                return Invalid(
                    CardPlanValidationCode.TargetPieceKindMismatch,
                    $"Piece at {pieceTarget.Square} has an unexpected kind.");
            }

            if (!IsAllowedPieceKind(definition.AllowedTargetPieceKinds, piece.Kind))
            {
                return Invalid(
                    CardPlanValidationCode.TargetPieceKindMismatch,
                    $"Piece at {pieceTarget.Square} has a disallowed kind for card '{plan.CardId}'.");
            }

            if (pieceTarget.IsPromotioned && !piece.IsPromotioned)
            {
                return Invalid(
                    CardPlanValidationCode.TargetPieceKindMismatch,
                    $"Piece at {pieceTarget.Square} is no longer promoted.");
            }

            if (pieceTarget.StartSquare.HasValue && piece.StartSquare != pieceTarget.StartSquare)
            {
                return Invalid(
                    CardPlanValidationCode.TargetPieceKindMismatch,
                    $"Piece at {pieceTarget.Square} has an unexpected start square.");
            }

            return CardPlanValidationResult.Valid();
        }

        private static CardPlanValidationResult ValidateOrderedPieces(
            GameState gameState,
            CardUsePlan plan,
            CardPlanningDefinition definition)
        {
            foreach (PieceTargetSnapshot pieceTarget in plan.Target.Pieces)
            {
                CardPlanValidationResult result = ValidatePieceTarget(
                    gameState,
                    plan,
                    definition,
                    pieceTarget);
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return CardPlanValidationResult.Valid();
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

        private static bool IsAllowedPieceKind(
            IReadOnlyList<PieceKind> allowedKinds,
            PieceKind kind)
        {
            if (allowedKinds.Count == 0)
            {
                return true;
            }

            foreach (PieceKind allowedKind in allowedKinds)
            {
                if (allowedKind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static CardPlanValidationResult ValidateBoardSquares(
            GameState gameState,
            IEnumerable<Square> squares)
        {
            foreach (Square square in squares)
            {
                if (gameState.BoardState.FindPiece(square) != null)
                {
                    return Invalid(
                        CardPlanValidationCode.TargetSquareOccupied,
                        $"Target square {square} is occupied.");
                }

                if (HasTileEffect(gameState, square))
                {
                    return Invalid(
                        CardPlanValidationCode.TargetSquareHasTileEffect,
                        $"Target square {square} already has a tile effect.");
                }
            }

            return CardPlanValidationResult.Valid();
        }

        private static CardPlanValidationResult ValidateNoDuplicateSquares(
            IEnumerable<Square> squares)
        {
            var seen = new HashSet<Square>();

            foreach (Square square in squares)
            {
                if (!seen.Add(square))
                {
                    return Invalid(
                        CardPlanValidationCode.DuplicateTargetSquare,
                        $"Target square {square} is duplicated.");
                }
            }

            return CardPlanValidationResult.Valid();
        }

        private static bool HasTileEffect(GameState gameState, Square square)
        {
            foreach (TileEffectInfo effect in gameState.TileEffects)
            {
                if (effect.Square == square)
                {
                    return true;
                }
            }

            return false;
        }

        private static CardPlanValidationResult Invalid(
            CardPlanValidationCode code,
            string reason)
        {
            return CardPlanValidationResult.Invalid(code, reason);
        }
    }
}
