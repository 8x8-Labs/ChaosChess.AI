using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class FireCardTargetStrategyTests
{
    private readonly FireCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsEnemyEngineDestination()
    {
        var enemyKnight = Piece(PieceKind.Knight, PieceColor.Black, new Square(6, 5), "n");
        GameState state = State(
            PieceColor.White,
            pieces: new[] { enemyKnight });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("g6e5") }));

        Assert.True(result.HasSelection);
        Assert.Equal(new Square(4, 4), result.SelectedCandidate!.Plan.Target.Squares[0]);
        Assert.Equal(10, ComponentValue(result.SelectedCandidate.Score, "fire.enemy_engine_destination"));
    }

    [Fact]
    public void Decide_ScoresAdjacentEnemyEngineDestination()
    {
        var enemyKnight = Piece(PieceKind.Knight, PieceColor.Black, new Square(6, 5), "n");
        var occupiedDestination = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 4));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { enemyKnight, occupiedDestination });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("g6e5") }));

        Assert.True(result.HasSelection);
        Assert.Equal(3, ComponentValue(result.SelectedCandidate!.Score, "fire.enemy_engine_adjacent"));
    }

    [Fact]
    public void Decide_UsesCenterControlWithoutEngineMove()
    {
        GameState state = State(PieceColor.White);

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(new Square(3, 3), result.SelectedCandidate!.Plan.Target.Squares[0]);
        Assert.Equal(3, ComponentValue(result.SelectedCandidate.Score, "fire.center_control"));
    }

    [Fact]
    public void Decide_AvoidsOwnEngineDestination()
    {
        var actorPawn = Piece(PieceKind.Pawn, PieceColor.White, new Square(4, 1));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { actorPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("e2e4") }));

        Assert.True(result.HasSelection);
        Assert.NotEqual(new Square(4, 3), result.SelectedCandidate!.Plan.Target.Squares[0]);
    }

    [Fact]
    public void Decide_NoLegalSquare_ReturnsNoLegalCandidate()
    {
        PieceInfo[] pieces = CreateFullBoard();
        GameState state = State(
            PieceColor.White,
            pieces: pieces);

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoLegalCandidate, result.SkipCode);
    }

    [Fact]
    public void Decide_ThresholdAboveBestScore_ReturnsNoBenefit()
    {
        GameState state = State(PieceColor.White);

        CardPlanDecisionResult result = strategy.Decide(
            new CardTargetStrategyContext(
                state,
                state.AvailableCards[0],
                PieceColor.White,
                new CardTargetingOptions(activationThreshold: 4)));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_ActorMismatch_ReturnsInvalidActor()
    {
        GameState state = State(PieceColor.White);

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.Black));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.InvalidActor, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
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
        foreach (CardPlanScoreComponent component in score.Components)
        {
            if (component.Code == code)
            {
                return component.Value;
            }
        }

        throw new InvalidOperationException("Component was not found.");
    }

    private static GameState State(
        PieceColor sideToMove,
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
            new[] { new CardInfo("fire", "BoardControl", 1) },
            Array.Empty<TileEffectInfo>());
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
