using System;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.CardTargeting;

public sealed class ChaoticKnightCardTargetStrategyTests
{
    private readonly ChaoticKnightCardTargetStrategy strategy = new();

    [Fact]
    public void Decide_SelectsKnightWithBestAverageRandomRelocation()
    {
        var edgeKnight = Piece(PieceKind.Knight, PieceColor.White, "a1", "n");
        var centralKnight = Piece(PieceKind.Knight, PieceColor.White, "d4", "n");
        GameState state = State(edgeKnight, centralKnight);

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(edgeKnight.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "chaotic_knight.expected_center_gain");
    }

    [Fact]
    public void Decide_EngineSourceBreaksRelocationTies()
    {
        var firstKnight = Piece(PieceKind.Knight, PieceColor.White, "b1", "n");
        var secondKnight = Piece(PieceKind.Knight, PieceColor.White, "f1", "n");
        GameState state = State(firstKnight, secondKnight);

        CardPlanDecisionResult result = strategy.Decide(
            Context(
                state,
                new[] { Move("f1g3") }));

        Assert.True(result.HasSelection, result.Reason);
        Assert.Equal(secondKnight.Square, result.SelectedCandidate!.Plan.Target.Piece!.Square);
        Assert.Contains(
            result.SelectedCandidate.Score.Components,
            component => component.Code == "chaotic_knight.engine_source");
    }

    [Fact]
    public void Decide_WithoutRandomDestinations_ReturnsNoBenefit()
    {
        var knight = Piece(PieceKind.Knight, PieceColor.White, "d4", "n");
        GameState state = State(CreateBlockedNeighborhood(knight));

        CardPlanDecisionResult result = strategy.Decide(Context(state));

        Assert.False(result.HasSelection);
        Assert.Equal(CardPlanSkipCode.NoBenefit, result.SkipCode);
    }

    [Fact]
    public void Decide_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => strategy.Decide(null!));
        Assert.Throws<ArgumentNullException>(() => new ChaoticKnightCardTargetStrategy(null!));
    }

    private static CardTargetStrategyContext Context(
        GameState state,
        MoveCandidate[]? moves = null)
    {
        return new CardTargetStrategyContext(
            state,
            state.AvailableCards[0],
            PieceColor.White,
            engineTopMoves: moves);
    }

    private static GameState State(params PieceInfo[] pieces)
    {
        return new GameState(
            new BoardState(
                pieces,
                PieceColor.White,
                CastlingRights.None,
                enPassantTarget: null,
                halfmoveClock: 0,
                fullmoveNumber: 1),
            new[] { new CardInfo("chaotic_knight", "Mobility", 1) },
            Array.Empty<TileEffectInfo>());
    }

    private static PieceInfo[] CreateBlockedNeighborhood(PieceInfo knight)
    {
        var pieces = new PieceInfo[25];
        pieces[0] = knight;
        int index = 1;

        for (int rank = knight.Square.Rank - 2; rank <= knight.Square.Rank + 2; rank++)
        {
            for (int file = knight.Square.File - 2; file <= knight.Square.File + 2; file++)
            {
                if (file == knight.Square.File && rank == knight.Square.Rank)
                {
                    continue;
                }

                pieces[index] = Piece(PieceKind.Pawn, PieceColor.White, new Square(file, rank), "p");
                index++;
            }
        }

        return pieces;
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, string square, string fenCode)
    {
        return new PieceInfo(kind, color, Square.Parse(square), fenCode);
    }

    private static PieceInfo Piece(PieceKind kind, PieceColor color, Square square, string fenCode)
    {
        return new PieceInfo(kind, color, square, fenCode);
    }

    private static MoveCandidate Move(string uciMove)
    {
        return new MoveCandidate(uciMove, scoreCentipawns: 10, mateIn: null);
    }
}
