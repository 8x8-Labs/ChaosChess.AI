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
        BoardState boardState = FenParser.Parse("8/8/8/8/8/8/4P3/4K3 w - - 0 1");
        var agile = new CardInfo("agile", "Mobility", remainingUses: 1);
        var aim = new CardInfo("aim", "Mobility", remainingUses: 1);
        var charge = new CardInfo("charge", "Mobility", remainingUses: 1);
        var fastMarch = new CardInfo("fast_march", "Mobility", remainingUses: 1);
        var fire = new CardInfo("fire", "BoardControl", remainingUses: 1);
        var peaceZone = new CardInfo("peace_zone", "BoardControl", remainingUses: 1);
        var portal = new CardInfo("portal", "Mobility", remainingUses: 1);
        var gameState = new GameState(
            boardState,
            new[] { agile, aim, charge, fastMarch, fire, peaceZone, portal },
            Array.Empty<TileEffectInfo>());

        var catalog = new DefaultCardPlanningCatalog();
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("agile").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("aim").RequiredTargetKind);
        Assert.Equal(CardTargetKind.None, catalog.GetDefinition("charge").RequiredTargetKind);
        Assert.Equal(CardTargetKind.PieceAtSquare, catalog.GetDefinition("fast_march").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("fire").RequiredTargetKind);
        Assert.Equal(CardTargetKind.BoardSquare, catalog.GetDefinition("peace_zone").RequiredTargetKind);
        Assert.Equal(2, catalog.GetDefinition("portal").RequiredTargetCount);

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
        CardUsePlan chargePlan = new CardUsePlan(
            "charge",
            PieceColor.White,
            CardTargetSelection.None());
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
        CardUsePlan peaceZonePlan = new CardUsePlan(
            "peace_zone",
            PieceColor.White,
            CardTargetSelection.BoardSquare(new Square(3, 3)));
        CardUsePlan portalPlan = new CardUsePlan(
            "portal",
            PieceColor.White,
            CardTargetSelection.OrderedSquares(
                new[] { new Square(0, 0), new Square(7, 7) }));

        Assert.True(validator.Validate(gameState, agilePlan).IsValid);
        Assert.True(validator.Validate(gameState, aimPlan).IsValid);
        Assert.True(validator.Validate(gameState, chargePlan).IsValid);
        Assert.True(validator.Validate(gameState, fastMarchPlan).IsValid);
        Assert.True(validator.Validate(gameState, firePlan).IsValid);
        Assert.True(validator.Validate(gameState, peaceZonePlan).IsValid);
        Assert.True(validator.Validate(gameState, portalPlan).IsValid);
        Assert.Equal(new[] { new Square(0, 0), new Square(7, 7) }, portalPlan.Target.Squares);

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
