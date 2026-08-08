using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Domain;

public sealed class CardUsePlanValidatorTests
{
    private readonly CardUsePlanValidator validator = new();

    [Theory]
    [InlineData("charge")]
    public void Validate_ValidNoneTargetPlan_ReturnsValid(string cardId)
    {
        GameState state = CreateState(PieceColor.White, Card(cardId));
        CardUsePlan plan = Plan(cardId, PieceColor.White, CardTargetSelection.None());

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
    }

    [Fact]
    public void Validate_ValidAgilePlan_ReturnsValid()
    {
        var square = new Square(4, 1);
        GameState state = CreateState(
            PieceColor.White,
            Card("agile"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.White, square) });
        CardUsePlan plan = Plan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.White, PieceKind.Pawn)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
    }

    [Theory]
    [InlineData("fire")]
    [InlineData("peace_zone")]
    public void Validate_ValidSingleSquarePlan_ReturnsValid(string cardId)
    {
        GameState state = CreateState(PieceColor.Black, Card(cardId));
        CardUsePlan plan = Plan(
            cardId,
            PieceColor.Black,
            CardTargetSelection.BoardSquare(new Square(4, 4)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
    }

    [Fact]
    public void Validate_ValidPortalPlan_PreservesOrderedSquaresAndReturnsValid()
    {
        var first = new Square(0, 0);
        var second = new Square(7, 7);
        GameState state = CreateState(PieceColor.Black, Card("portal"));
        CardUsePlan plan = Plan(
            "portal",
            PieceColor.Black,
            CardTargetSelection.OrderedSquares(new[] { first, second }));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
        Assert.Equal(new[] { first, second }, plan.Target.Squares);
    }

    [Fact]
    public void Validate_ValidOrderedPiecesPlan_PreservesOrderAndReturnsValid()
    {
        var first = new Square(0, 6);
        var second = new Square(2, 6);
        GameState state = CreateState(
            PieceColor.White,
            Card("dimension_disturbance"),
            pieces: new[]
            {
                Piece(PieceKind.Rook, PieceColor.Black, first, "r"),
                Piece(PieceKind.Bishop, PieceColor.Black, second, "b")
            });
        CardUsePlan plan = Plan(
            "dimension_disturbance",
            PieceColor.White,
            CardTargetSelection.OrderedPieces(new[]
            {
                new PieceTargetSnapshot(first, PieceColor.Black, PieceKind.Rook),
                new PieceTargetSnapshot(second, PieceColor.Black, PieceKind.Bishop)
            }));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
        Assert.Equal(new[] { first, second }, plan.Target.Squares);
        Assert.Equal(first, plan.Target.Piece!.Square);
        Assert.Equal(2, plan.Target.Pieces.Count);
    }

    [Fact]
    public void Validate_ValidTeleportPlan_ReturnsValid()
    {
        var source = new Square(4, 1);
        var destination = new Square(4, 5);
        GameState state = CreateState(
            PieceColor.White,
            Card("teleport"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.White, source) });
        CardUsePlan plan = Plan(
            "teleport",
            PieceColor.White,
            CardTargetSelection.PieceAndSquare(
                new PieceTargetSnapshot(source, PieceColor.White, PieceKind.Pawn),
                destination));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
    }

    [Fact]
    public void Validate_NullInputs_ReturnInvalidCodes()
    {
        CardUsePlan plan = Plan("charge", PieceColor.White, CardTargetSelection.None());
        GameState state = CreateState(PieceColor.White, Card("charge"));

        AssertInvalid(
            validator.Validate(null, plan),
            CardPlanValidationCode.NullGameState);
        AssertInvalid(
            validator.Validate(state, null),
            CardPlanValidationCode.NullPlan);
    }

    [Fact]
    public void Validate_CardNotInHand_ReturnsCardNotInHand()
    {
        GameState state = CreateState(PieceColor.White);
        CardUsePlan plan = Plan("charge", PieceColor.White, CardTargetSelection.None());

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.CardNotInHand);
    }

    [Fact]
    public void Validate_CardHasNoRemainingUses_ReturnsCardHasNoRemainingUses()
    {
        GameState state = CreateState(PieceColor.White, Card("charge", remainingUses: 0));
        CardUsePlan plan = Plan("charge", PieceColor.White, CardTargetSelection.None());

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.CardHasNoRemainingUses);
    }

    [Fact]
    public void Validate_UnsupportedCardInHand_ReturnsUnsupportedCard()
    {
        GameState state = CreateState(PieceColor.White, Card("unknown_card"));
        CardUsePlan plan = Plan("unknown_card", PieceColor.White, CardTargetSelection.None());

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.UnsupportedCard);
    }

    [Fact]
    public void Validate_ActorMismatch_ReturnsActorDoesNotMatchSideToMove()
    {
        GameState state = CreateState(PieceColor.White, Card("charge"));
        CardUsePlan plan = Plan("charge", PieceColor.Black, CardTargetSelection.None());

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.ActorDoesNotMatchSideToMove);
    }

    [Fact]
    public void Validate_TargetKindMismatch_ReturnsTargetKindMismatch()
    {
        GameState state = CreateState(PieceColor.White, Card("charge"));
        CardUsePlan plan = Plan(
            "charge",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(1, 1)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetKindMismatch);
    }

    [Fact]
    public void Validate_TargetCountMismatch_ReturnsTargetCountMismatch()
    {
        GameState state = CreateState(PieceColor.White, Card("portal"));
        CardUsePlan plan = Plan(
            "portal",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(new[] { new Square(1, 1) }));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetCountMismatch);
    }

    [Fact]
    public void Validate_MissingPieceTarget_ReturnsTargetPieceMissing()
    {
        var square = new Square(4, 1);
        GameState state = CreateState(PieceColor.White, Card("agile"));
        CardUsePlan plan = Plan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.White, PieceKind.Pawn)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetPieceMissing);
    }

    [Fact]
    public void Validate_PieceColorMismatch_ReturnsTargetPieceColorMismatch()
    {
        var square = new Square(4, 1);
        GameState state = CreateState(
            PieceColor.White,
            Card("agile"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.Black, square) });
        CardUsePlan plan = Plan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.White, PieceKind.Pawn)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetPieceColorMismatch);
    }

    [Fact]
    public void Validate_SelfPieceRequirementRejectsOpponentPiece()
    {
        var square = new Square(4, 1);
        GameState state = CreateState(
            PieceColor.White,
            Card("agile"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.Black, square) });
        CardUsePlan plan = Plan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.Black, PieceKind.Pawn)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetPieceColorMismatch);
    }

    [Fact]
    public void Validate_OpponentPieceRequirementAcceptsOpponentPiece()
    {
        var square = new Square(3, 6);
        var customValidator = new CardUsePlanValidator(
            new DefaultCardPlanningCatalog(
                new[]
                {
                    CardPlanningDefinition.Supported(
                        "missing_promotion",
                        CardTargetRequirement.Piece(CardTargetOwnerRelation.Opponent, PieceKind.Queen))
                }));
        GameState state = CreateState(
            PieceColor.White,
            Card("missing_promotion"),
            pieces: new[] { Piece(PieceKind.Queen, PieceColor.Black, square, "q") });
        CardUsePlan plan = Plan(
            "missing_promotion",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.Black, PieceKind.Queen)));

        CardPlanValidationResult result = customValidator.Validate(state, plan);

        AssertValid(result);
    }

    [Fact]
    public void Validate_OpponentPieceRequirementRejectsActorPiece()
    {
        var square = new Square(3, 0);
        var customValidator = new CardUsePlanValidator(
            new DefaultCardPlanningCatalog(
                new[]
                {
                    CardPlanningDefinition.Supported(
                        "missing_promotion",
                        CardTargetRequirement.Piece(CardTargetOwnerRelation.Opponent, PieceKind.Queen))
                }));
        GameState state = CreateState(
            PieceColor.White,
            Card("missing_promotion"),
            pieces: new[] { Piece(PieceKind.Queen, PieceColor.White, square, "q") });
        CardUsePlan plan = Plan(
            "missing_promotion",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.White, PieceKind.Queen)));

        CardPlanValidationResult result = customValidator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetPieceColorMismatch);
    }

    [Fact]
    public void Validate_PieceKindMismatch_ReturnsTargetPieceKindMismatch()
    {
        var square = new Square(4, 1);
        GameState state = CreateState(
            PieceColor.White,
            Card("agile"),
            pieces: new[] { Piece(PieceKind.Knight, PieceColor.White, square, "n") });
        CardUsePlan plan = Plan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.White, PieceKind.Pawn)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetPieceKindMismatch);
    }

    [Fact]
    public void Validate_DisallowedPieceKind_ReturnsTargetPieceKindMismatch()
    {
        var square = new Square(4, 1);
        GameState state = CreateState(
            PieceColor.White,
            Card("caterpillar"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.White, square) });
        CardUsePlan plan = Plan(
            "caterpillar",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(square, PieceColor.White, PieceKind.Pawn)));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetPieceKindMismatch);
    }

    [Fact]
    public void Validate_OccupiedBoardSquare_ReturnsTargetSquareOccupied()
    {
        var square = new Square(4, 4);
        GameState state = CreateState(
            PieceColor.Black,
            Card("fire"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.White, square) });
        CardUsePlan plan = Plan(
            "fire",
            PieceColor.Black,
            CardTargetSelection.BoardSquare(square));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetSquareOccupied);
    }

    [Fact]
    public void Validate_BoardSquareWithTileEffect_ReturnsTargetSquareHasTileEffect()
    {
        var square = new Square(4, 4);
        GameState state = CreateState(
            PieceColor.Black,
            Card("peace_zone"),
            tileEffects: new[] { TileEffect("existing", square) });
        CardUsePlan plan = Plan(
            "peace_zone",
            PieceColor.Black,
            CardTargetSelection.BoardSquare(square));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetSquareHasTileEffect);
    }

    [Fact]
    public void Validate_TeleportDestinationWithTileEffect_ReturnsTargetSquareHasTileEffect()
    {
        var source = new Square(4, 1);
        var destination = new Square(4, 5);
        GameState state = CreateState(
            PieceColor.White,
            Card("teleport"),
            pieces: new[] { Piece(PieceKind.Pawn, PieceColor.White, source) },
            tileEffects: new[] { TileEffect("existing", destination) });
        CardUsePlan plan = Plan(
            "teleport",
            PieceColor.White,
            CardTargetSelection.PieceAndSquare(
                new PieceTargetSnapshot(source, PieceColor.White, PieceKind.Pawn),
                destination));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.TargetSquareHasTileEffect);
    }

    [Fact]
    public void Validate_PortalDuplicateSquare_ReturnsDuplicateTargetSquare()
    {
        var square = new Square(4, 4);
        GameState state = CreateState(PieceColor.White, Card("portal"));
        CardUsePlan plan = Plan(
            "portal",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(new[] { square, square }));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.DuplicateTargetSquare);
    }

    [Fact]
    public void Validate_OrderedPiecesDuplicateSquare_ReturnsDuplicateTargetSquare()
    {
        var square = new Square(0, 6);
        GameState state = CreateState(
            PieceColor.White,
            Card("dimension_disturbance"),
            pieces: new[] { Piece(PieceKind.Rook, PieceColor.Black, square, "r") });
        CardUsePlan plan = Plan(
            "dimension_disturbance",
            PieceColor.White,
            CardTargetSelection.OrderedPieces(new[]
            {
                new PieceTargetSnapshot(square, PieceColor.Black, PieceKind.Rook),
                new PieceTargetSnapshot(square, PieceColor.Black, PieceKind.Rook)
            }));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertInvalid(result, CardPlanValidationCode.DuplicateTargetSquare);
    }

    [Fact]
    public void Validate_DoesNotMutateGameState()
    {
        var targetSquare = new Square(4, 4);
        var pieceSquare = new Square(0, 0);
        CardInfo card = Card("fire");
        PieceInfo piece = Piece(PieceKind.King, PieceColor.White, pieceSquare, "k");
        TileEffectInfo tileEffect = TileEffect("existing", new Square(1, 1));
        GameState state = CreateState(
            PieceColor.Black,
            card,
            pieces: new[] { piece },
            tileEffects: new[] { tileEffect });
        CardUsePlan plan = Plan(
            "fire",
            PieceColor.Black,
            CardTargetSelection.BoardSquare(targetSquare));

        CardPlanValidationResult result = validator.Validate(state, plan);

        AssertValid(result);
        Assert.Same(card, Assert.Single(state.AvailableCards));
        Assert.Same(piece, Assert.Single(state.BoardState.Pieces));
        Assert.Same(tileEffect, Assert.Single(state.TileEffects));
    }

    private static void AssertValid(CardPlanValidationResult result)
    {
        Assert.True(result.IsValid);
        Assert.Equal(CardPlanValidationCode.Valid, result.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    private static void AssertInvalid(
        CardPlanValidationResult result,
        CardPlanValidationCode expectedCode)
    {
        Assert.False(result.IsValid);
        Assert.Equal(expectedCode, result.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    private static CardUsePlan Plan(
        string cardId,
        PieceColor actor,
        CardTargetSelection target)
    {
        return new CardUsePlan(cardId, actor, target);
    }

    private static CardInfo Card(string id, int remainingUses = 1)
    {
        return new CardInfo(id, "Mobility", remainingUses);
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode = "p")
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static TileEffectInfo TileEffect(string id, Square square)
    {
        return new TileEffectInfo(
            id,
            "Fire",
            square,
            PieceColor.Black,
            remainingTurns: 1);
    }

    private static GameState CreateState(
        PieceColor sideToMove,
        CardInfo? card = null,
        PieceInfo[]? pieces = null,
        TileEffectInfo[]? tileEffects = null)
    {
        var board = new BoardState(
            pieces ?? Array.Empty<PieceInfo>(),
            sideToMove,
            CastlingRights.None,
            enPassantTarget: null,
            halfmoveClock: 0,
            fullmoveNumber: 1);

        return new GameState(
            board,
            card == null ? Array.Empty<CardInfo>() : new[] { card },
            tileEffects ?? Array.Empty<TileEffectInfo>());
    }
}
