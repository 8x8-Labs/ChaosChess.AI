using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class PortalCardTargetStrategyTests
{
    private readonly PortalCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsDistinctOrderedEndpointsNearActorRoute()
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
        Assert.Equal(2, result.SelectedCandidate!.Plan.Target.Squares.Count);
        Assert.NotEqual(
            result.SelectedCandidate.Plan.Target.Squares[0],
            result.SelectedCandidate.Plan.Target.Squares[1]);
        Assert.True(ComponentValue(result.SelectedCandidate.Score, "portal.endpoint_actor_source") > 0);
        Assert.True(ComponentValue(result.SelectedCandidate.Score, "portal.endpoint_actor_destination") > 0);
    }

    [Fact]
    public void Decide_PreservesOrderedPairAndDistanceComponent()
    {
        GameState state = State(PieceColor.White);

        CardPlanDecisionResult result = strategy.Decide(
            Context(state, PieceColor.White));

        Assert.True(result.HasSelection);
        Assert.Equal(new[] { new Square(0, 0), new Square(3, 3) }, result.SelectedCandidate!.Plan.Target.Squares);
        CardPlanScoreComponent distance = Component(result.SelectedCandidate.Score, "portal.endpoint_distance");
        Assert.Equal(6, distance.RawValue);
        Assert.Equal(1, distance.Weight);
        Assert.Equal(6, distance.Contribution);
        Assert.Equal(distance.Contribution, distance.Value);
    }

    [Fact]
    public void Decide_WithTwoEndpointShortlist_PreservesOrderedPairTieBreak()
    {
        GameState state = State(PieceColor.White);

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                options: new CardTargetingOptions(maximumPortalEndpointCandidates: 2)));

        Assert.True(result.HasSelection);
        Assert.Equal(new[] { new Square(3, 2), new Square(3, 3) }, result.SelectedCandidate!.Plan.Target.Squares);
        Assert.True(result.SelectedCandidate.EnumerationIndex < 4032);
    }

    [Fact]
    public void Decide_AvoidsEnemyDestinationAdjacencyWhenPossible()
    {
        var enemyPawn = Piece(PieceKind.Pawn, PieceColor.Black, new Square(4, 6));
        GameState state = State(
            PieceColor.White,
            pieces: new[] { enemyPawn });

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                PieceColor.White,
                new[] { Move("e7e5") }));

        Assert.True(result.HasSelection);
        Assert.True(ComponentValue(result.SelectedCandidate!.Score, "portal.endpoint_enemy_destination_risk") <= 0);
    }

    [Fact]
    public void Decide_NoLegalPair_ReturnsNoLegalCandidate()
    {
        GameState state = State(
            PieceColor.White,
            pieces: CreateFullBoardExcept(new Square(0, 0)));

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
            Context(
                state,
                PieceColor.White,
                options: new CardTargetingOptions(activationThreshold: 99)));

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
        MoveCandidate[]? moves = null,
        CardTargetingOptions? options = null)
    {
        return new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            actor,
            options,
            moves);
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
            new[] { new CardInfo("portal", "Mobility", 1) },
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

    private static PieceInfo[] CreateFullBoardExcept(Square emptySquare)
    {
        var pieces = new PieceInfo[Square.BoardSize * Square.BoardSize - 1];
        int index = 0;

        for (int rank = 0; rank < Square.BoardSize; rank++)
        {
            for (int file = 0; file < Square.BoardSize; file++)
            {
                var square = new Square(file, rank);
                if (square == emptySquare)
                {
                    continue;
                }

                pieces[index] = Piece(PieceKind.Pawn, PieceColor.White, square);
                index++;
            }
        }

        return pieces;
    }
}
