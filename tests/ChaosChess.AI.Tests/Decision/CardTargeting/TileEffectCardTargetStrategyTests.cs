using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class TileEffectCardTargetStrategyTests
{
    private readonly TileEffectCardTargetStrategy strategy = new("at_mine", "AT Mine");

    [Fact]
    public void Decide_SelectsOpponentEngineDestination()
    {
        var enemyKnight = Piece(PieceKind.Knight, PieceColor.Black, new Square(6, 5), "n");
        GameState state = State(
            PieceColor.White,
            Card("at_mine"),
            pieces: new[] { enemyKnight });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("g6e5") }));

        Assert.True(result.HasSelection);
        Assert.Equal(new Square(4, 4), result.SelectedCandidate!.Plan.Target.Squares[0]);
        CardPlanScoreComponent opponentDestination = Component(
            result.SelectedCandidate.Score,
            "at_mine.opponent_engine_destination");
        Assert.Equal(1, opponentDestination.RawValue);
        Assert.Equal(8, opponentDestination.Weight);
        Assert.Equal(8, opponentDestination.Contribution);
    }

    [Fact]
    public void Decide_UsesCenterControlWithoutEngineMove()
    {
        GameState state = State(PieceColor.White, Card("at_mine"));

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(new Square(3, 3), result.SelectedCandidate!.Plan.Target.Squares[0]);
        Assert.Equal(2, ComponentValue(result.SelectedCandidate.Score, "at_mine.center_control"));
    }

    [Fact]
    public void Decide_AvoidsOwnEngineDestination()
    {
        var actorPawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 1));
        GameState state = State(
            PieceColor.White,
            Card("at_mine"),
            pieces: new[] { actorPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("e2e4") }));

        Assert.True(result.HasSelection);
        Assert.NotEqual(new Square(4, 3), result.SelectedCandidate!.Plan.Target.Squares[0]);
    }

    [Theory]
    [InlineData("at_mine")]
    [InlineData("cobweb")]
    [InlineData("jumping_platform")]
    [InlineData("psilocybin_mushroom")]
    public void Decide_GenericTileEffectCardsSelectLegalEmptySquare(string cardId)
    {
        var genericStrategy = new TileEffectCardTargetStrategy(cardId, cardId);
        GameState state = State(PieceColor.White, Card(cardId));

        CardPlanDecisionResult result = genericStrategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(cardId, result.SelectedCandidate!.Plan.CardId);
        Assert.Equal(CardTargetKind.BoardSquare, result.SelectedCandidate.Plan.Target.Kind);
    }

    [Fact]
    public void Decide_NoLegalSquare_ReturnsNoLegalCandidate()
    {
        GameState state = State(
            PieceColor.White,
            Card("at_mine"),
            pieces: CreateFullBoard());

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_UnsupportedCard_ReturnsUnsupported()
    {
        GameState state = State(PieceColor.White, Card("cobweb"));

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.UnsupportedCard, result.SkipCode);
    }

    [Fact]
    public void Decide_ActorMismatch_ReturnsInvalidActor()
    {
        GameState state = State(PieceColor.White, Card("at_mine"));

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.Black));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentException>(() => new TileEffectCardTargetStrategy(string.Empty, "Tile"));
        Assert.Throws<ArgumentNullException>(() => new TileEffectCardTargetStrategy("at_mine", "AT Mine", null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        PieceColor actor,
        MoveCandidate[]? moves = null)
    {
        return new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            actor,
            engineTopMoves: moves);
    }

    private static int ComponentValue(CardPlanScore score, string code)
    {
        return Component(score, code).Value;
    }

    private static CardPlanScoreComponent Component(CardPlanScore score, string code)
    {
        foreach (CardPlanScoreComponent component in score.Components)
        {
            if (component.Code == code)
            {
                return component;
            }
        }

        throw new InvalidOperationException("Component was not found.");
    }

    private static GameState State(
        PieceColor sideToMove,
        CardInfo card,
        PieceInfo[]? pieces = null)
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
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "BoardControl", 1);
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        Square square,
        string fenCode = "p")
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }

    private static PieceInfo[] CreateFullBoard()
    {
        var pieces = new PieceInfo[Square.BoardSize * Square.BoardSize];
        int index = 0;

        for (int rank = 0; rank < Square.BoardSize; rank++)
        {
            for (int file = 0; file < Square.BoardSize; file++)
            {
                pieces[index] = Piece(
                    PieceKind.Pawn,
                    PieceColor.White,
                    new Square(file, rank));
                index++;
            }
        }

        return pieces;
    }
}
