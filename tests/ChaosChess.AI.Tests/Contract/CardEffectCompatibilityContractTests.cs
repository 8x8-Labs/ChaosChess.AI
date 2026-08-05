using System;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Contract;

public sealed class CardEffectCompatibilityContractTests
{
    [Theory]
    [InlineData("agile")]
    [InlineData("aim")]
    [InlineData("at_mine")]
    [InlineData("blessing")]
    [InlineData("caterpillar")]
    [InlineData("chaotic_knight")]
    [InlineData("charge")]
    [InlineData("cobweb")]
    [InlineData("concentration")]
    [InlineData("dark_hand")]
    [InlineData("desperado")]
    [InlineData("dimension_instability")]
    [InlineData("father_enemy")]
    [InlineData("fast_march")]
    [InlineData("fire")]
    [InlineData("giant")]
    [InlineData("gods_move")]
    [InlineData("jumping_platform")]
    [InlineData("limitless")]
    [InlineData("missing_promotion")]
    [InlineData("obey_order")]
    [InlineData("peace_zone")]
    [InlineData("portal")]
    [InlineData("psilocybin_mushroom")]
    [InlineData("sneak_pawn")]
    [InlineData("sunset_blade")]
    [InlineData("time_bomb")]
    [InlineData("thunderclap_flash")]
    public void DefaultEffectDefinitions_PreservePlanningCatalogTargetShape(string cardId)
    {
        var planningCatalog = new DefaultCardPlanningCatalog();
        var effectCatalog = new DefaultCardEffectDefinitionCatalog();

        CardPlanningDefinition planning = planningCatalog.GetDefinition(cardId);
        CardEffectDefinition effect = effectCatalog.FindDefinition(cardId)!;

        Assert.True(planning.IsSupported);
        Assert.Equal(planning.RequiredTargetKind, effect.TargetQuery.Kind);
        Assert.Equal(planning.RequiredTargetOwnerRelation, effect.TargetQuery.OwnerRelation);
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
            new CardUsePlan(
                "aim",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("at_mine", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("blessing", PieceColor.White, CardTargetSelection.BoardSquare(new Square(2, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "caterpillar",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(1, 0), PieceColor.White, PieceKind.Knight))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "chaotic_knight",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(1, 0), PieceColor.White, PieceKind.Knight))),
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
            new CardUsePlan("cobweb", PieceColor.White, CardTargetSelection.BoardSquare(new Square(4, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "concentration",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(3, 0), PieceColor.White, PieceKind.Queen))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "dark_hand",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(0, 7), PieceColor.Black, PieceKind.Rook))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "dimension_instability",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(1, 0), PieceColor.White, PieceKind.Knight))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "desperado",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "father_enemy",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "missing_promotion",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(0, 7), PieceColor.Black, PieceKind.Rook))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("obey_order", PieceColor.White, CardTargetSelection.BoardSquare(new Square(5, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "fast_march",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "limitless",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(3, 0), PieceColor.White, PieceKind.Queen))),
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
            new CardUsePlan(
                "giant",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "gods_move",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("jumping_platform", PieceColor.White, CardTargetSelection.BoardSquare(new Square(5, 3))),
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
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("psilocybin_mushroom", PieceColor.White, CardTargetSelection.BoardSquare(new Square(3, 4))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "sneak_pawn",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "sunset_blade",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(4, 1), PieceColor.White, PieceKind.Pawn))),
            CardEffectApplicationStatus.Unsupported);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan("time_bomb", PieceColor.White, CardTargetSelection.BoardSquare(new Square(6, 3))),
            CardEffectApplicationStatus.Exact);
        AssertValidWithStatus(
            state,
            validator,
            effectCatalog,
            applier,
            new CardUsePlan(
                "thunderclap_flash",
                PieceColor.White,
                CardTargetSelection.PieceAtSquare(
                    new PieceTargetSnapshot(new Square(0, 0), PieceColor.White, PieceKind.Rook))),
            CardEffectApplicationStatus.Unsupported);
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
                new PieceInfo(PieceKind.Rook, PieceColor.White, new Square(0, 0), "R"),
                new PieceInfo(PieceKind.Knight, PieceColor.White, new Square(1, 0), "N"),
                new PieceInfo(PieceKind.Queen, PieceColor.White, new Square(3, 0), "Q"),
                new PieceInfo(PieceKind.Pawn, PieceColor.White, new Square(4, 1), "P"),
                new PieceInfo(PieceKind.Rook, PieceColor.Black, new Square(0, 7), "r")
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
                new CardInfo("aim", "Mobility", 1),
                new CardInfo("at_mine", "BoardControl", 1),
                new CardInfo("blessing", "Transformation", 1),
                new CardInfo("caterpillar", "Mobility", 1),
                new CardInfo("chaotic_knight", "Utility", 1),
                new CardInfo("charge", "Mobility", 1),
                new CardInfo("cobweb", "BoardControl", 1),
                new CardInfo("concentration", "Mobility", 1),
                new CardInfo("dark_hand", "Tactical", 1),
                new CardInfo("desperado", "Tactical", 1),
                new CardInfo("dimension_instability", "Mobility", 1),
                new CardInfo("father_enemy", "Tactical", 1),
                new CardInfo("fast_march", "Mobility", 1),
                new CardInfo("fire", "BoardControl", 1),
                new CardInfo("giant", "Transformation", 1),
                new CardInfo("gods_move", "Mobility", 1),
                new CardInfo("jumping_platform", "BoardControl", 1),
                new CardInfo("limitless", "Mobility", 1),
                new CardInfo("missing_promotion", "Transformation", 1),
                new CardInfo("obey_order", "Utility", 1),
                new CardInfo("peace_zone", "BoardControl", 1),
                new CardInfo("portal", "Mobility", 1),
                new CardInfo("psilocybin_mushroom", "BoardControl", 1),
                new CardInfo("sneak_pawn", "Mobility", 1),
                new CardInfo("sunset_blade", "Tactical", 1),
                new CardInfo("time_bomb", "BoardControl", 1),
                new CardInfo("thunderclap_flash", "Mobility", 1)
            },
            Array.Empty<TileEffectInfo>());
    }
}
