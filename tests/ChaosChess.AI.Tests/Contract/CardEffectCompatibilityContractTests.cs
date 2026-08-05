using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Contract;

public sealed class CardEffectCompatibilityContractTests
{
    [Theory]
    [InlineData("agile")]
    [InlineData("charge")]
    [InlineData("fire")]
    [InlineData("peace_zone")]
    [InlineData("portal")]
    public void DefaultEffectDefinitions_PreservePlanningCatalogTargetShape(string cardId)
    {
        var planningCatalog = new DefaultCardPlanningCatalog();
        var effectCatalog = new DefaultCardEffectDefinitionCatalog();

        CardPlanningDefinition planning = planningCatalog.GetDefinition(cardId);
        CardEffectDefinition effect = effectCatalog.FindDefinition(cardId)!;

        Assert.True(planning.IsSupported);
        Assert.Equal(planning.RequiredTargetKind, effect.TargetQuery.Kind);
        Assert.Equal(planning.RequiredTargetCount, effect.TargetQuery.Count);
    }

    [Fact]
    public void ValidP9Plans_EnterP12EffectBoundaryWithoutTargetContractFailure()
    {
        GameState state = CreateState();
        var validator = new CardUsePlanValidator();
        var effectCatalog = new DefaultCardEffectDefinitionCatalog();
        var applier = new CardEffectApplier();

        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "agile",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("charge", PieceColor.White, CardTargetSelection.None()),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("fire", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("peace_zone", PieceColor.White, CardTargetSelection.BoardSquare(new Square(4, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "portal",
                PieceColor.White,
                CardTargetSelection.OrderedSquares(new[] { new Square(2, 2), new Square(5, 5) })),
            CardEffectApplicationStatus.Exact);
    }

    private static void AssertValidWithStatus(
        GameState state,
        CardUsePlanValidator validator,
        DefaultCardEffectDefinitionCatalog effectCatalog,
        CardEffectApplier applier,
        CardUsePlan plan,
        CardEffectApplicationStatus expectedStatus)
    {
        CardPlanValidationResult validation = validator.Validate(state, plan);
        Assert.True(validation.IsValid);

        CardEffectApplicationResult result = applier.Apply(
            effectCatalog.FindDefinition(plan.CardId)!,
            new CardEffectApplicationContext(state, plan, plan.Actor, plan.Actor, plan.Actor));

        Assert.Equal(expectedStatus, result.Status);
        Assert.NotEqual(CardEffectApplicationCode.InvalidContext, result.Code);
        Assert.NotEqual(CardEffectApplicationCode.IllegalTarget, result.Code);
    }

    private static GameState CreateState()
    {
        var board = new BoardState(
            new[]
            {
                new PieceInfo(PieceKind.King, PieceColor.White, new Square(4, 0), "K"),
                new PieceInfo(PieceKind.King, PieceColor.Black, new Square(4, 7), "k"),
                new PieceInfo(PieceKind.Pawn, PieceColor.White, new Square(4, 1), "P")
            },
            PieceColor.White,
            CastlingRights.None,
            enPassantTarget: null,
            halfmoveClock: 0,
            fullmoveNumber: 1);

        return new GameState(
            board,
            new[]
            {
                new CardInfo("agile", "Mobility", 1),
                new CardInfo("charge", "Mobility", 1),
                new CardInfo("fire", "BoardControl", 1),
                new CardInfo("peace_zone", "BoardControl", 1),
                new CardInfo("portal", "Mobility", 1)
            },
            Array.Empty<TileEffectInfo>());
    }
}
