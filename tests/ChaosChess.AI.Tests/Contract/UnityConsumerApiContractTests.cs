using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Decision.TurnPlanning;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using ChaosChess.AI.Evaluation;
using ChaosChess.AI.Fen;
using ChaosChess.AI.Simulation;
using Xunit;

namespace ChaosChess.AI.Tests.Contract;

public sealed class UnityConsumerApiContractTests
{
    [Fact]
    public void UnityConsumerSurface_CompilesAndPreservesExpectedMembers()
    {
        BoardState boardState = FenParser.Parse("8/8/8/8/8/8/4P3/4K3 w - - 0 1");
        string canonicalFen = FenParser.Serialize(boardState);
        Assert.Equal("8/8/8/8/8/8/4P3/4K3 w - - 0 1", canonicalFen);

        var card = new CardInfo("fire", "Tactical", remainingUses: 1);
        var tileEffect = new TileEffectInfo(
            "fire:e4",
            "Fire",
            new Square(4, 3),
            PieceColor.Black,
            remainingTurns: 2);
        var gameState = new GameState(
            boardState,
            new[] { card },
            new[] { tileEffect });

        var evaluator = new GameStateEvaluator(
            new StaticChessEngine(),
            new EvaluationOptions(searchDepth: 12));
        EvaluationResult evaluation = evaluator.Evaluate(gameState, PieceColor.Black);

        var module = new CardDecisionModule(
            new ConfiguredCardScorer(
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Tactical"] = 10
                }),
            new EloCardProfile(minimumScoreGain: 1, maximumCardsPerTurn: 1));

        CardDecisionResult decision = module.Decide(
            gameState,
            evaluation,
            PieceColor.Black);

        CardUseRecommendation recommendation = Assert.Single(decision.Recommendations);
        Assert.True(decision.ShouldUseCards);
        Assert.Same(card, recommendation.Card);
        Assert.Equal("fire", recommendation.Card.Id);
        Assert.Equal("Tactical", recommendation.Card.Category);
        Assert.Equal(1, recommendation.Card.RemainingUses);
        Assert.True(recommendation.EffectiveGain > 0);
        Assert.Equal(PieceColor.White, boardState.SideToMove);
        Assert.Single(gameState.AvailableCards);
        Assert.Single(gameState.TileEffects);
        Assert.Equal(4, tileEffect.Square.File);
        Assert.Equal(3, tileEffect.Square.Rank);
    }

    [Fact]
    public void UnityConsumerSurface_CompilesWithCardUsePlanContracts()
    {
        BoardState boardState = FenParser.Parse("4k3/r7/8/8/8/8/4P3/RN1QK3 w - - 0 1");
        var agile = new CardInfo("agile", "Mobility", remainingUses: 1);
        var aim = new CardInfo("aim", "Mobility", remainingUses: 1);
        var atMine = new CardInfo("at_mine", "BoardControl", remainingUses: 1);
        var blessing = new CardInfo("blessing", "Transformation", remainingUses: 1);
        var caterpillar = new CardInfo("caterpillar", "Mobility", remainingUses: 1);
        var charge = new CardInfo("charge", "Mobility", remainingUses: 1);
        var cobweb = new CardInfo("cobweb", "BoardControl", remainingUses: 1);
        var concentration = new CardInfo("concentration", "Mobility", remainingUses: 1);
        var darkHand = new CardInfo("dark_hand", "Tactical", remainingUses: 1);
        var dimensionInstability = new CardInfo("dimension_instability", "Mobility", remainingUses: 1);
        var fastMarch = new CardInfo("fast_march", "Mobility", remainingUses: 1);
        var fire = new CardInfo("fire", "BoardControl", remainingUses: 1);
        var giant = new CardInfo("giant", "Transformation", remainingUses: 1);
        var godsMove = new CardInfo("gods_move", "Mobility", remainingUses: 1);
        var jumpingPlatform = new CardInfo("jumping_platform", "BoardControl", remainingUses: 1);
        var limitless = new CardInfo("limitless", "Mobility", remainingUses: 1);
        var obeyOrder = new CardInfo("obey_order", "Utility", remainingUses: 1);
        var peaceZone = new CardInfo("peace_zone", "BoardControl", remainingUses: 1);
        var portal = new CardInfo("portal", "Mobility", remainingUses: 1);
        var psilocybinMushroom = new CardInfo("psilocybin_mushroom", "BoardControl", remainingUses: 1);
        var sneakPawn = new CardInfo("sneak_pawn", "Mobility", remainingUses: 1);
        var sunsetBlade = new CardInfo("sunset_blade", "Tactical", remainingUses: 1);
        var timeBomb = new CardInfo("time_bomb", "BoardControl", remainingUses: 1);
        var thunderclapFlash = new CardInfo("thunderclap_flash", "Mobility", remainingUses: 1);
        var gameState = new GameState(
            boardState,
            new[]
            {
                agile,
                aim,
                atMine,
                blessing,
                caterpillar,
                charge,
                cobweb,
                concentration,
                darkHand,
                dimensionInstability,
                fastMarch,
                fire,
                giant,
                godsMove,
                jumpingPlatform,
                limitless,
                obeyOrder,
                peaceZone,
                portal,
                psilocybinMushroom,
                sneakPawn,
                sunsetBlade,
                timeBomb,
                thunderclapFlash
            },
            Array.Empty<TileEffectInfo>());

        var catalog = new DefaultCardPlanningCatalog();
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("agile").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("aim").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("at_mine").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("blessing").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("caterpillar").RequiredTargetKind);
        Assert.Equal(CardTargetKind.None, catalog.GetDefinition("charge").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("cobweb").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("concentration").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("dark_hand").RequiredTargetKind);
        Assert.Equal(CardTargetOwnerRelation.Opponent, catalog.GetDefinition("dark_hand").RequiredTargetOwnerRelation);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("dimension_instability").RequiredTargetKind);
        Assert.Equal(CardTargetOwnerRelation.Self, catalog.GetDefinition("dimension_instability").RequiredTargetOwnerRelation);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("fast_march").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("fire").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("giant").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("gods_move").RequiredTargetKind);
        Assert.Equal(CardTargetOwnerRelation.Self, catalog.GetDefinition("gods_move").RequiredTargetOwnerRelation);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("jumping_platform").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("limitless").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("obey_order").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("peace_zone").RequiredTargetKind);
        Assert.Equal(2, catalog.GetDefinition("portal").RequiredTargetCount);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("psilocybin_mushroom").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("sneak_pawn").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("sunset_blade").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("time_bomb").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("thunderclap_flash").RequiredTargetKind);

        var validator = new CardUsePlanValidator(catalog);
        var traceRecorder = new CardUsePlanTraceRecorder(validator);

        CardUsePlan agilePlan = new CardUsePlan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan aimPlan = new CardUsePlan(
            "aim",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan atMinePlan = new CardUsePlan(
            "at_mine",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(3, 3)));
        CardUsePlan blessingPlan = new CardUsePlan(
            "blessing",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(2, 3)));
        CardUsePlan chargePlan = new CardUsePlan(
            "charge",
            PieceColor.White,
            CardTargetSelection.None());
        CardUsePlan cobwebPlan = new CardUsePlan(
            "cobweb",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(4, 3)));
        CardUsePlan caterpillarPlan = new CardUsePlan(
            "caterpillar",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(1, 0),
                    PieceColor.White,
                    PieceKind.Knight)));
        CardUsePlan concentrationPlan = new CardUsePlan(
            "concentration",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(3, 0),
                    PieceColor.White,
                    PieceKind.Queen)));
        CardUsePlan darkHandPlan = new CardUsePlan(
            "dark_hand",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(0, 6),
                    PieceColor.Black,
                    PieceKind.Rook)));
        CardUsePlan dimensionInstabilityPlan = new CardUsePlan(
            "dimension_instability",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(1, 0),
                    PieceColor.White,
                    PieceKind.Knight)));
        CardUsePlan fastMarchPlan = new CardUsePlan(
            "fast_march",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan firePlan = new CardUsePlan(
            "fire",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(4, 3)));
        CardUsePlan giantPlan = new CardUsePlan(
            "giant",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan godsMovePlan = new CardUsePlan(
            "gods_move",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan jumpingPlatformPlan = new CardUsePlan(
            "jumping_platform",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(5, 3)));
        CardUsePlan limitlessPlan = new CardUsePlan(
            "limitless",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(3, 0),
                    PieceColor.White,
                    PieceKind.Queen)));
        CardUsePlan obeyOrderPlan = new CardUsePlan(
            "obey_order",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(5, 3)));
        CardUsePlan peaceZonePlan = new CardUsePlan(
            "peace_zone",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(3, 3)));
        CardUsePlan portalPlan = new CardUsePlan(
            "portal",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(
                new[] { new Square(2, 2), new Square(7, 7) }));
        CardUsePlan psilocybinMushroomPlan = new CardUsePlan(
            "psilocybin_mushroom",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(3, 4)));
        CardUsePlan sneakPawnPlan = new CardUsePlan(
            "sneak_pawn",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan sunsetBladePlan = new CardUsePlan(
            "sunset_blade",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(4, 1),
                    PieceColor.White,
                    PieceKind.Pawn)));
        CardUsePlan timeBombPlan = new CardUsePlan(
            "time_bomb",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(6, 3)));
        CardUsePlan thunderclapFlashPlan = new CardUsePlan(
            "thunderclap_flash",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(
                    new Square(0, 0),
                    PieceColor.White,
                    PieceKind.Rook)));

        Assert.True(validator.Validate(gameState, agilePlan).IsValid);
        Assert.True(validator.Validate(gameState, aimPlan).IsValid);
        Assert.True(validator.Validate(gameState, atMinePlan).IsValid);
        Assert.True(validator.Validate(gameState, blessingPlan).IsValid);
        Assert.True(validator.Validate(gameState, caterpillarPlan).IsValid);
        Assert.True(validator.Validate(gameState, chargePlan).IsValid);
        Assert.True(validator.Validate(gameState, cobwebPlan).IsValid);
        Assert.True(validator.Validate(gameState, concentrationPlan).IsValid);
        Assert.True(validator.Validate(gameState, darkHandPlan).IsValid);
        Assert.True(validator.Validate(gameState, dimensionInstabilityPlan).IsValid);
        Assert.True(validator.Validate(gameState, fastMarchPlan).IsValid);
        Assert.True(validator.Validate(gameState, firePlan).IsValid);
        Assert.True(validator.Validate(gameState, giantPlan).IsValid);
        Assert.True(validator.Validate(gameState, godsMovePlan).IsValid);
        Assert.True(validator.Validate(gameState, jumpingPlatformPlan).IsValid);
        Assert.True(validator.Validate(gameState, limitlessPlan).IsValid);
        Assert.True(validator.Validate(gameState, obeyOrderPlan).IsValid);
        Assert.True(validator.Validate(gameState, peaceZonePlan).IsValid);
        Assert.True(validator.Validate(gameState, portalPlan).IsValid);
        Assert.True(validator.Validate(gameState, psilocybinMushroomPlan).IsValid);
        Assert.True(validator.Validate(gameState, sneakPawnPlan).IsValid);
        Assert.True(validator.Validate(gameState, sunsetBladePlan).IsValid);
        Assert.True(validator.Validate(gameState, timeBombPlan).IsValid);
        Assert.True(validator.Validate(gameState, thunderclapFlashPlan).IsValid);
        Assert.Equal(new[] { new Square(2, 2), new Square(7, 7) }, portalPlan.Target.Squares);

        CardUsePlan invalidPlan = new CardUsePlan(
            "fire",
            PieceColor.Black,
            CardTargetSelection.BoardSquare(new Square(4, 3)));
        CardPlanValidationResult rejected = validator.Validate(gameState, invalidPlan);
        CardUsePlanTrace rejectedTrace = traceRecorder.Record(gameState, invalidPlan);

        Assert.False(rejected.IsValid);
        Assert.Equal(CardPlanValidationCode.ActorDoesNotMatchSideToMove, rejected.Code);
        Assert.False(rejectedTrace.Accepted);
        Assert.Equal(rejected.Code, rejectedTrace.Code);
        Assert.Same(invalidPlan, rejectedTrace.Plan);
    }

    [Fact]
    public void UnityConsumerSurface_CompilesWithCardTargetingContracts()
    {
        BoardState boardState = FenParser.Parse("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1");
        var agile = new CardInfo("agile", "Mobility", remainingUses: 1);
        var gameState = new GameState(
            boardState,
            new[] { agile },
            Array.Empty<TileEffectInfo>());
        var targetingModule = new CardTargetingModule();

        CardPlanDecisionResult planDecision = targetingModule.DecideBestPlan(
            gameState,
            agile,
            PieceColor.White,
            new CardTargetingOptions(activationThreshold: 1),
            new[] { new MoveCandidate("e2e4", scoreCentipawns: 30, mateIn: null) });

        Assert.True(planDecision.HasSelection);
        Assert.Equal("agile", planDecision.SelectedCandidate!.Plan.CardId);
        Assert.NotNull(planDecision.SelectedCandidate.Score);
        Assert.Contains(
            planDecision.SelectedCandidate.Score.Components,
            component => component.Code == "agile.engine_source");

        var evaluator = new GameStateEvaluator(new StaticChessEngine());
        EvaluationResult evaluation = evaluator.Evaluate(gameState, PieceColor.White);
        var decisionModule = new CardDecisionModule(
            new ConfiguredCardScorer(
                cardScores: new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["agile"] = 1
                }));

        CardDecisionResult cardDecision = decisionModule.Decide(
            gameState,
            evaluation,
            PieceColor.White,
            targetingModule,
            engineTopMoves: new[] { new MoveCandidate("e2e4", scoreCentipawns: 30, mateIn: null) });

        CardUseRecommendation recommendation = Assert.Single(cardDecision.Recommendations);
        Assert.NotNull(recommendation.Plan);
        Assert.NotNull(recommendation.PlanScore);
        Assert.Equal(CardPlanSkipCode.None, recommendation.PlanSkipCode);
    }

    [Fact]
    public void UnityConsumerSurface_CompilesWithUnifiedTurnPlannerContracts()
    {
        BoardState boardState = FenParser.Parse("8/8/8/8/8/8/4P3/4K3 w - - 0 1");
        var gameState = new GameState(
            boardState,
            Array.Empty<CardInfo>(),
            Array.Empty<TileEffectInfo>());
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StaticChessEngine()),
            new TurnPlannerOptions(
                noCardMoveCandidateCount: 1,
                opponentReplyCandidateCount: 0,
                beamWidth: 1));

        TurnPlannerResult result = planner.PlanTurn(gameState);

        Assert.True(result.HasPlan);
        TurnPlan plan = result.SelectedPlan!;
        Assert.False(plan.UsesCard);
        Assert.True(plan.HasMove);
        Assert.Equal(PieceColor.White, plan.Actor);
        Assert.Equal(CardEffectApplicationStatus.Exact, plan.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, plan.CardApplicationCode);
        Assert.Equal("e2e4", plan.MovePlan!.UciMove);
        Assert.Equal(new Square(4, 1), plan.MovePlan.Source);
        Assert.Equal(new Square(4, 3), plan.MovePlan.Destination);
        Assert.Null(plan.MovePlan.Promotion);
        Assert.Equal("no-card|e2e4", plan.DeterministicRankKey);
        Assert.NotEmpty(plan.OriginStateFingerprint);

        TurnPlannerTraceSummary trace = result.TraceSummary;
        Assert.Equal(1, trace.NoCardMoveCandidateLimit);
        Assert.Equal(1, trace.BeamWidth);
        Assert.Equal(1, trace.RootNoCardMoveCandidateCount);
        Assert.Equal(1, trace.SelectedCandidateCount);
        Assert.Equal(0, trace.SkippedCandidateCount);
        Assert.Equal(1, trace.EngineCallCount);
        Assert.False(trace.OpponentReplyEvaluationRequested);
        Assert.False(trace.BeamPruningApplied);

        TurnPlanCandidate candidate = Assert.Single(result.Candidates);
        Assert.True(candidate.HasPlan);
        Assert.Equal(TurnPlanSkipCode.None, candidate.SkipCode);
        Assert.Same(plan, candidate.Plan);
    }

    private sealed class StaticChessEngine : IChessEngine
    {
        public IReadOnlyList<MoveCandidate> GetTopMoves(
            BoardState boardState,
            int variationCount)
        {
            return new[]
            {
                new MoveCandidate("e2e4", scoreCentipawns: 30, mateIn: null)
            };
        }

        public PositionEvaluation EvaluatePosition(
            BoardState boardState,
            int depth)
        {
            return new PositionEvaluation(
                PieceColor.White,
                scoreCentipawns: 0,
                mateIn: null);
        }

        public bool IsInCheck(BoardState boardState)
        {
            return false;
        }
    }
}
