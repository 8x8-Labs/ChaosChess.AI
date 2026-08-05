using System;
using System.Linq;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class CardPlanCandidateEnumeratorTests
{
    private readonly CardPlanCandidateEnumerator enumerator = new();
    private readonly CardUsePlanValidator validator = new();

    [Fact]
    public void EnumerateLegalCandidates_Charge_ReturnsSingleNoneCandidate()
    {
        GameState state = State(PieceColor.White, Card("charge"));

        CardPlanCandidate candidate = Assert.Single(
            enumerator.EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.White));

        Assert.Equal(CardTargetKind.None, candidate.Plan.Target.Kind);
        Assert.Equal(0, candidate.EnumerationIndex);
        AssertValid(state, candidate);
    }

    [Theory]
    [InlineData("agile")]
    [InlineData("aim")]
    [InlineData("fast_march")]
    public void EnumerateLegalCandidates_PawnMovementCards_ReturnActorPawnCandidatesInBoardOrderOnly(
        string cardId)
    {
        var blackPawn = Piece(PieceKind.Pawn, PieceColor.Black, new Square(0, 6));
        var whiteKnight = Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n");
        var firstPawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 1));
        var secondPawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(6, 1));
        GameState state = State(
            PieceColor.White,
            Card(cardId),
            pieces: new[] { blackPawn, whiteKnight, firstPawn, secondPawn });

        CardPlanCandidate[] candidates = enumerator
            .EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.White)
            .ToArray();

        Assert.Equal(2, candidates.Length);
        Assert.Equal(firstPawn.Square, candidates[0].Plan.Target.Piece!.Square);
        Assert.Equal(secondPawn.Square, candidates[1].Plan.Target.Piece!.Square);
        Assert.Equal(new[] { 0, 1 }, candidates.Select(candidate => candidate.EnumerationIndex));
        Assert.All(candidates, candidate => AssertValid(state, candidate));
    }

    [Fact]
    public void EnumerateLegalCandidates_UsesAllowedPieceKindsFromPlanningDefinition()
    {
        var whitePawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 1));
        var whiteKnight = Piece(PieceKind.Knight, PieceColor.White, new Square(1, 0), "n");
        var blackKnight = Piece(PieceKind.Knight, PieceColor.Black, new Square(1, 7), "n");
        GameState state = State(
            PieceColor.White,
            Card("caterpillar"),
            pieces: new[] { whitePawn, whiteKnight, blackKnight });

        CardPlanCandidate candidate = Assert.Single(
            enumerator.EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.White));

        Assert.Equal(whiteKnight.Square, candidate.Plan.Target.Piece!.Square);
        Assert.Equal(PieceKind.Knight, candidate.Plan.Target.Piece.ExpectedKind);
        AssertValid(state, candidate);
    }

    [Fact]
    public void EnumerateLegalCandidates_UsesOpponentOwnerRelationFromPlanningDefinition()
    {
        var actorRook = Piece(PieceKind.Rook, PieceColor.White, new Square(0, 0), "r");
        var opponentRook = Piece(PieceKind.Rook, PieceColor.Black, new Square(0, 7), "r");
        var opponentPawn = Piece(PieceKind.Pawn, PieceColor.Black, new Square(4, 6));
        GameState state = State(
            PieceColor.White,
            Card("missing_promotion"),
            pieces: new[] { actorRook, opponentRook, opponentPawn });

        CardPlanCandidate candidate = Assert.Single(
            enumerator.EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.White));

        Assert.Equal(opponentRook.Square, candidate.Plan.Target.Piece!.Square);
        Assert.Equal(PieceColor.Black, candidate.Plan.Target.Piece.ExpectedColor);
    }

    [Theory]
    [InlineData("dimension_instability", PieceKind.Knight)]
    [InlineData("giant", PieceKind.Pawn)]
    [InlineData("giant", PieceKind.Knight)]
    [InlineData("giant", PieceKind.Bishop)]
    [InlineData("sunset_blade", PieceKind.Pawn)]
    public void EnumerateLegalCandidates_PieceEffectCards_ReturnActorAllowedPieceCandidates(
        string cardId,
        PieceKind expectedKind)
    {
        PieceInfo actorPiece = Piece(expectedKind, PieceColor.White, new Square(3, 3), "p");
        PieceInfo opponentPiece = Piece(expectedKind, PieceColor.Black, new Square(4, 4), "p");
        GameState state = State(
            PieceColor.White,
            Card(cardId),
            pieces: new[] { actorPiece, opponentPiece });

        CardPlanCandidate candidate = Assert.Single(
            enumerator.EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.White));

        Assert.Equal(actorPiece.Square, candidate.Plan.Target.Piece!.Square);
        Assert.Equal(expectedKind, candidate.Plan.Target.Piece.ExpectedKind);
        Assert.Equal(PieceColor.White, candidate.Plan.Target.Piece.ExpectedColor);
        AssertValid(state, candidate);
    }

    [Theory]
    [InlineData("at_mine")]
    [InlineData("blessing")]
    [InlineData("cobweb")]
    [InlineData("fire")]
    [InlineData("jumping_platform")]
    [InlineData("obey_order")]
    [InlineData("peace_zone")]
    [InlineData("psilocybin_mushroom")]
    [InlineData("time_bomb")]
    public void EnumerateLegalCandidates_BoardSquareCards_ExcludeOccupiedAndEffectSquares(
        string cardId)
    {
        var occupied = new Square(0, 0);
        var effected = new Square(1, 0);
        GameState state = State(
            PieceColor.Black,
            Card(cardId),
            pieces: new[] { Piece(PieceKind.King, PieceColor.White, occupied, "k") },
            tileEffects: new[] { TileEffect(effected) });

        CardPlanCandidate[] candidates = enumerator
            .EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.Black)
            .ToArray();

        Assert.Equal(62, candidates.Length);
        Assert.Equal(new Square(2, 0), candidates[0].Plan.Target.Squares[0]);
        Assert.DoesNotContain(candidates, candidate => candidate.Plan.Target.Squares[0] == occupied);
        Assert.DoesNotContain(candidates, candidate => candidate.Plan.Target.Squares[0] == effected);
        Assert.Equal(Enumerable.Range(0, candidates.Length), candidates.Select(candidate => candidate.EnumerationIndex));
        Assert.All(candidates, candidate => AssertValid(state, candidate));
    }

    [Fact]
    public void EnumerateLegalCandidates_Portal_ReturnsDistinctOrderedPairsAndPreservesOrder()
    {
        var occupied = new Square(0, 0);
        var effected = new Square(1, 0);
        GameState state = State(
            PieceColor.White,
            Card("portal"),
            pieces: new[] { Piece(PieceKind.King, PieceColor.Black, occupied, "k") },
            tileEffects: new[] { TileEffect(effected) });

        CardPlanCandidate[] candidates = enumerator
            .EnumerateLegalCandidates(state, state.AvailableCards[0], PieceColor.White)
            .ToArray();

        Assert.Equal(62 * 61, candidates.Length);
        Assert.Equal(new[] { new Square(2, 0), new Square(3, 0) }, candidates[0].Plan.Target.Squares);
        Assert.Equal(new[] { new Square(3, 0), new Square(2, 0) }, candidates[61].Plan.Target.Squares);
        Assert.DoesNotContain(
            candidates,
            candidate => candidate.Plan.Target.Squares[0] == candidate.Plan.Target.Squares[1]);
        Assert.All(candidates, candidate => AssertValid(state, candidate));
    }

    [Fact]
    public void EnumerateLegalCandidates_UnsupportedCard_ReturnsEmpty()
    {
        GameState state = State(PieceColor.White, Card("unknown"));

        Assert.Empty(enumerator.EnumerateLegalCandidates(
            state,
            state.AvailableCards[0],
            PieceColor.White));
    }

    [Fact]
    public void EnumerateLegalCandidates_DoesNotMutateGameState()
    {
        CardInfo card = Card("fire");
        PieceInfo piece = Piece(PieceKind.King, PieceColor.White, new Square(0, 0), "k");
        TileEffectInfo effect = TileEffect(new Square(1, 0));
        GameState state = State(
            PieceColor.Black,
            card,
            pieces: new[] { piece },
            tileEffects: new[] { effect });

        enumerator.EnumerateLegalCandidates(state, card, PieceColor.Black);

        Assert.Same(card, Assert.Single(state.AvailableCards));
        Assert.Same(piece, Assert.Single(state.BoardState.Pieces));
        Assert.Same(effect, Assert.Single(state.TileEffects));
    }

    [Fact]
    public void EnumerateLegalCandidates_InvalidArguments_Throw()
    {
        GameState state = State(PieceColor.White, Card("charge"));
        CardInfo card = state.AvailableCards[0];

        Assert.Throws<ArgumentNullException>(
            () => enumerator.EnumerateLegalCandidates(null!, card, PieceColor.White));
        Assert.Throws<ArgumentNullException>(
            () => enumerator.EnumerateLegalCandidates(state, null!, PieceColor.White));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => enumerator.EnumerateLegalCandidates(state, card, (PieceColor)99));
    }

    private void AssertValid(GameState state, CardPlanCandidate candidate)
    {
        CardPlanValidationResult result = validator.Validate(state, candidate.Plan);

        Assert.True(result.IsValid, result.Reason);
        Assert.Same(state.AvailableCards[0], candidate.Card);
        Assert.Equal(0, candidate.Score.Total);
        Assert.Equal("enumeration.neutral", Assert.Single(candidate.Score.Components).Code);
    }

    private static GameState State(
        PieceColor sideToMove,
        CardInfo card,
        PieceInfo[]? pieces = null,
        TileEffectInfo[]? tileEffects = null)
    {
        return new GameState(
            new BoardState(
                pieces ?? Array.Empty<PieceInfo>(),
                sideToMove,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { card },
            tileEffects ?? Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "Mobility", 1);
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode = "p")
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static TileEffectInfo TileEffect(Square square)
    {
        return new TileEffectInfo(
            "effect:" + square,
            "Fire",
            square,
            owner: null,
            remainingTurns: 1);
    }
}
