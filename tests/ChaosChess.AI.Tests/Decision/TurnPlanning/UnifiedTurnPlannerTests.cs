using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Decision.TurnPlanning;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;
using Xunit;

namespace ChaosChess.AI.Tests.Decision.TurnPlanning;

public sealed class UnifiedTurnPlannerTests
{
    [Fact]
    public void PlanTurn_RequestsNoCardMoveCandidateCount()
    {
        var engine = new StubChessEngine(Move("e2e4", 13));
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new TurnPlannerOptions(noCardMoveCandidateCount: 2));

        TurnPlannerResult result = planner.PlanTurn(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")));

        Assert.True(result.HasPlan);
        Assert.Equal(2, engine.LastVariationCount);
    }

    [Fact]
    public void PlanTurn_CreatesNoCardMovePlansFromFilteredRecommendations()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(
                Move("e2e4", 13),
                Move("d2d4", 80))));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")));

        TurnPlan selected = result.SelectedPlan!;

        Assert.Equal(2, result.Candidates.Count);
        Assert.False(selected.UsesCard);
        Assert.Equal("d2d4", selected.MovePlan!.UciMove);
        Assert.Equal("no-card|d2d4", selected.DeterministicRankKey);
        Assert.Equal(CardEffectApplicationStatus.Exact, selected.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, selected.CardApplicationCode);
        Assert.Equal(PieceColor.White, selected.Actor);
    }

    [Fact]
    public void PlanTurn_UsesMoveFilterScoreBreakdown()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 130))));

        TurnPlannerResult result = planner.PlanTurn(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")));

        TurnPlan plan = result.SelectedPlan!;

        Assert.Equal(10, plan.Score.Total);
        Assert.Collection(
            plan.Score.Components,
            component =>
            {
                Assert.Equal("move.engine", component.Code);
                Assert.Equal(10, component.Value);
            },
            component =>
            {
                Assert.Equal("move.adjustment", component.Code);
                Assert.Equal(0, component.Value);
            },
            component =>
            {
                Assert.Equal("move.total", component.Code);
                Assert.Equal(0, component.Value);
            });
    }

    [Fact]
    public void PlanTurn_WhenNoRecommendations_ReturnsNoLegalMoveSkip()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine()));

        TurnPlannerResult result = planner.PlanTurn(
            State(Piece(PieceKind.King, PieceColor.White, "e1", "k")));

        TurnPlanCandidate candidate = Assert.Single(result.Candidates);
        Assert.False(result.HasPlan);
        Assert.Null(result.SelectedPlan);
        Assert.Equal(TurnPlanSkipCode.NoLegalMove, candidate.SkipCode);
        Assert.Equal(
            "No legal no-card move recommendation was available.",
            candidate.SkipReason);
    }

    [Fact]
    public void PlanTurn_TraceSummary_RecordsDeterministicCapsWithoutBeamPruning()
    {
        var engine = new StubChessEngine(Move("e2e4", 13));
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new TurnPlannerOptions(
                noCardMoveCandidateCount: 2,
                cardCandidateCount: 4,
                targetCandidateCount: 8,
                postCardMoveCandidateCount: 5,
                opponentReplyCandidateCount: 0,
                beamWidth: 7,
                maximumEngineCallCount: 11));

        TurnPlannerResult result = planner.PlanTurn(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")));

        TurnPlannerTraceSummary trace = result.TraceSummary;
        Assert.Equal(2, trace.NoCardMoveCandidateLimit);
        Assert.Equal(4, trace.CardCandidateLimit);
        Assert.Equal(8, trace.TargetCandidateLimit);
        Assert.Equal(5, trace.PostCardMoveCandidateLimit);
        Assert.Equal(0, trace.OpponentReplyCandidateLimit);
        Assert.Equal(7, trace.BeamWidth);
        Assert.Equal(22, trace.DeterministicCandidateCap);
        Assert.Equal(1, trace.RootNoCardMoveCandidateCount);
        Assert.Equal(1, trace.SelectedCandidateCount);
        Assert.Equal(0, trace.SkippedCandidateCount);
        Assert.Equal(1, trace.EngineCallCount);
        Assert.Equal(11, trace.MaximumEngineCallCount);
        Assert.Equal(0, trace.EngineCallLimitSkipCount);
        Assert.False(trace.EngineCallLimitReached);
        Assert.Equal(0, trace.BeamPrunedCandidateCount);
        Assert.False(trace.BeamPruningApplied);
        Assert.False(trace.OpponentReplyEvaluationRequested);
        Assert.False(trace.OpponentReplyEvaluationDeferred);
        Assert.Equal(0, trace.OpponentReplyDeferredCandidateCount);
    }

    [Fact]
    public void PlanTurn_TraceSummary_RecordsOpponentReplyDeferredWhenRequested()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 13))));

        TurnPlannerResult result = planner.PlanTurn(
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")));

        TurnPlannerTraceSummary trace = result.TraceSummary;
        Assert.Equal(1, trace.OpponentReplyCandidateLimit);
        Assert.True(trace.OpponentReplyEvaluationRequested);
        Assert.True(trace.OpponentReplyEvaluationDeferred);
        Assert.Equal(1, trace.OpponentReplyDeferredCandidateCount);
    }

    [Fact]
    public void PlanTurn_BeamPruning_LimitsSelectedCandidatesAndKeepsSkips()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(
                Move("e2e4", 13),
                Move("d2d4", 80))),
            new CardTargetingModule(),
            new RecordingProbe(
                (gameState, card, plan) => CardEffectPlanningResult.Unsupported(
                    plan,
                    CardEffectApplicationCode.UnsupportedEffect,
                    "Card effect application is not connected.")),
            new TurnPlannerOptions(
                noCardMoveCandidateCount: 2,
                beamWidth: 1));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                new[] { Card("charge") }));

        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal("d2d4", result.SelectedPlan!.MovePlan!.UciMove);
        Assert.DoesNotContain(
            result.Candidates,
            candidate => candidate.Plan?.MovePlan?.UciMove == "e2e4");
        Assert.Contains(
            result.Candidates,
            candidate => candidate.SkipCode == TurnPlanSkipCode.UnsupportedCardEffect);

        TurnPlannerTraceSummary trace = result.TraceSummary;
        Assert.Equal(1, trace.SelectedCandidateCount);
        Assert.Equal(1, trace.SkippedCandidateCount);
        Assert.Equal(1, trace.BeamPrunedCandidateCount);
        Assert.True(trace.BeamPruningApplied);
    }

    [Fact]
    public void PlanTurn_WithCardTargeting_RecordsSelectedCardAsUnsupportedCardEffectSkip()
    {
        var probe = new RecordingProbe(
            (gameState, card, plan) => CardEffectPlanningResult.Unsupported(
                plan,
                CardEffectApplicationCode.UnsupportedEffect,
                "Card effect application is not connected."));
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 13))),
            new CardTargetingModule(),
            probe);

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                new[] { Card("charge") }));

        Assert.Equal(1, probe.CallCount);
        Assert.Equal(2, result.Candidates.Count);

        TurnPlanCandidate skipped = result.Candidates[1];
        Assert.Equal(TurnPlanSkipCode.UnsupportedCardEffect, skipped.SkipCode);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, skipped.SkippedCardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, skipped.SkippedCardApplicationCode);
        Assert.Equal("charge", skipped.SkippedCardPlan!.CardId);
    }

    [Fact]
    public void PlanTurn_WhenCoarseCardEffectsDisabled_RecordsCoarseSkip()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 13))),
            new CardTargetingModule(),
            new RecordingProbe(
                (gameState, card, plan) => CardEffectPlanningResult.Coarse(
                    plan,
                    gameState,
                    "Card effect only has coarse planning support.")),
            new TurnPlannerOptions(allowCoarseCardEffects: false));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                new[] { Card("charge") }));

        TurnPlanCandidate skipped = result.Candidates[1];
        Assert.Equal(TurnPlanSkipCode.CoarseCardEffectNotAllowed, skipped.SkipCode);
        Assert.Equal(CardEffectApplicationStatus.Coarse, skipped.SkippedCardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.CoarseApplied, skipped.SkippedCardApplicationCode);
    }

    [Fact]
    public void PlanTurn_DefaultCardEffectProbe_RecordsActualUnsupportedStatus()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 13))),
            new CardTargetingModule());

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                new[] { Card("charge") }));

        TurnPlanCandidate skipped = result.Candidates[1];
        Assert.Equal(TurnPlanSkipCode.UnsupportedCardEffect, skipped.SkipCode);
        Assert.Equal(CardEffectApplicationStatus.Unsupported, skipped.SkippedCardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, skipped.SkippedCardApplicationCode);
        Assert.Equal("charge", skipped.SkippedCardPlan!.CardId);
    }

    [Fact]
    public void PlanTurn_DefaultCardEffectProbe_AnalyzesPostCardMoveForExactFire()
    {
        var engine = new StubChessEngine(
            new[] { Move("e2e4", 13) },
            new[] { Move("d2d4", 80) });
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new CardTargetingModule(),
            options: new TurnPlannerOptions(
                noCardMoveCandidateCount: 1,
                cardCandidateCount: 1,
                postCardMoveCandidateCount: 1,
                opponentReplyCandidateCount: 0,
                beamWidth: 4));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                new[] { Card("fire") }));

        TurnPlanCandidate cardCandidate = Assert.Single(
            result.Candidates,
            candidate => candidate.Plan?.CardPlan?.CardId == "fire");
        TurnPlan plan = cardCandidate.Plan!;

        Assert.True(plan.UsesCard);
        Assert.Equal(CardEffectApplicationStatus.Exact, plan.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, plan.CardApplicationCode);
        Assert.Equal(CardTargetKind.BoardSquare, plan.CardPlan!.Target.Kind);
        Assert.Equal("d2d4", plan.MovePlan!.UciMove);
        Assert.Equal(new[] { 1, 1 }, engine.VariationCounts);
        Assert.Equal(2, result.TraceSummary.EngineCallCount);
        Assert.Equal(1, result.TraceSummary.PostCardMoveCandidateCount);
        Assert.Equal(2, result.TraceSummary.SelectedCandidateCount);
        Assert.Equal(0, result.TraceSummary.SkippedCandidateCount);
    }

    [Fact]
    public void PlanTurn_DefaultCardEffectProbe_AnalyzesPostCardMoveForExactPortal()
    {
        var engine = new StubChessEngine(
            new[] { Move("e2e4", 13) },
            new[] { Move("e2e4", 80) });
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new CardTargetingModule(),
            options: new TurnPlannerOptions(
                noCardMoveCandidateCount: 1,
                cardCandidateCount: 1,
                postCardMoveCandidateCount: 1,
                opponentReplyCandidateCount: 0,
                beamWidth: 4));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                new[] { Card("portal") }));

        TurnPlanCandidate cardCandidate = Assert.Single(
            result.Candidates,
            candidate => candidate.Plan?.CardPlan?.CardId == "portal");
        TurnPlan plan = cardCandidate.Plan!;

        Assert.True(plan.UsesCard);
        Assert.Equal(CardEffectApplicationStatus.Exact, plan.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, plan.CardApplicationCode);
        Assert.Equal(CardTargetKind.OrderedSquares, plan.CardPlan!.Target.Kind);
        Assert.Equal(2, plan.CardPlan.Target.Squares.Count);
        Assert.Equal("e2e4", plan.MovePlan!.UciMove);
        Assert.Equal(new[] { 1, 1 }, engine.VariationCounts);
        Assert.Equal(2, result.TraceSummary.EngineCallCount);
        Assert.Equal(1, result.TraceSummary.PostCardMoveCandidateCount);
        Assert.Equal(2, result.TraceSummary.SelectedCandidateCount);
        Assert.Equal(0, result.TraceSummary.SkippedCandidateCount);
    }

    [Fact]
    public void PlanTurn_ExactCardApplication_CreatesCardAndMovePlanFromPostCardState()
    {
        var engine = new StubChessEngine(
            new[] { Move("e2e4", 13) },
            new[] { Move("d2d4", 52) });
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new CardTargetingModule(),
            new RecordingProbe(
                (gameState, card, plan) => CardEffectPlanningResult.Exact(
                    plan,
                    gameState,
                    "Exact effect applied.")));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                new[] { Card("charge") }));

        Assert.Equal(new[] { 3, 3 }, engine.VariationCounts);
        TurnPlan selected = result.SelectedPlan!;
        Assert.True(selected.UsesCard);
        Assert.Equal("charge", selected.CardPlan!.CardId);
        Assert.Equal("d2d4", selected.MovePlan!.UciMove);
        Assert.Equal(CardEffectApplicationStatus.Exact, selected.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, selected.CardApplicationCode);
        Assert.StartsWith("card|charge|", selected.DeterministicRankKey, StringComparison.Ordinal);

        TurnPlannerTraceSummary trace = result.TraceSummary;
        Assert.Equal(1, trace.ConsideredCardCandidateCount);
        Assert.Equal(1, trace.PostCardMoveCandidateCount);
        Assert.Equal(2, trace.SelectedCandidateCount);
        Assert.Equal(0, trace.SkippedCandidateCount);
        Assert.Equal(2, trace.EngineCallCount);
    }

    [Fact]
    public void PlanTurn_CoarseCardApplicationWhenAllowed_CreatesCardAndMovePlan()
    {
        var engine = new StubChessEngine(
            new[] { Move("e2e4", 13) },
            new[] { Move("d2d4", 52) });
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new CardTargetingModule(),
            new RecordingProbe(
                (gameState, card, plan) => CardEffectPlanningResult.Coarse(
                    plan,
                    gameState,
                    "Coarse effect applied.")),
            new TurnPlannerOptions(allowCoarseCardEffects: true));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                new[] { Card("charge") }));

        TurnPlan selected = result.SelectedPlan!;
        Assert.True(selected.UsesCard);
        Assert.Equal(CardEffectApplicationStatus.Coarse, selected.CardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.CoarseApplied, selected.CardApplicationCode);
    }

    [Fact]
    public void PlanTurn_WhenPostCardMoveAnalysisHasNoRecommendations_RecordsMoveFilterSkip()
    {
        var engine = new StubChessEngine(
            new[] { Move("e2e4", 13) },
            Array.Empty<MoveCandidate>());
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new CardTargetingModule(),
            new RecordingProbe(
                (gameState, card, plan) => CardEffectPlanningResult.Exact(
                    plan,
                    gameState,
                    "Exact effect applied.")));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                new[] { Card("charge") }));

        Assert.Contains(
            result.Candidates,
            candidate => candidate.SkipCode == TurnPlanSkipCode.MoveFilterRejected &&
                candidate.SkipReason == "No legal post-card move recommendation was available.");
        Assert.Equal(1, result.TraceSummary.SkippedCandidateCount);
        Assert.Equal(0, result.TraceSummary.PostCardMoveCandidateCount);
    }

    [Fact]
    public void PlanTurn_WhenEngineCallLimitReached_SkipsPostCardMoveAnalysis()
    {
        var engine = new StubChessEngine(
            new[] { Move("e2e4", 13) },
            new[] { Move("d2d4", 52) });
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(engine),
            new CardTargetingModule(),
            new RecordingProbe(
                (gameState, card, plan) => CardEffectPlanningResult.Exact(
                    plan,
                    gameState,
                    "Exact effect applied.")),
            new TurnPlannerOptions(maximumEngineCallCount: 1));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[]
                {
                    Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                    Piece(PieceKind.Pawn, PieceColor.White, "d2", "p")
                },
                new[] { Card("charge") }));

        Assert.Equal(new[] { 3 }, engine.VariationCounts);
        TurnPlanCandidate skipped = Assert.Single(
            result.Candidates,
            candidate => candidate.SkipCode == TurnPlanSkipCode.EngineCallLimitExceeded);
        Assert.Equal("charge", skipped.SkippedCardPlan!.CardId);
        Assert.Equal(CardEffectApplicationStatus.Exact, skipped.SkippedCardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, skipped.SkippedCardApplicationCode);
        Assert.Equal(
            "Post-card move analysis skipped because the engine call limit was reached.",
            skipped.SkipReason);
        Assert.Equal(1, result.TraceSummary.EngineCallCount);
        Assert.Equal(1, result.TraceSummary.MaximumEngineCallCount);
        Assert.Equal(1, result.TraceSummary.EngineCallLimitSkipCount);
        Assert.True(result.TraceSummary.EngineCallLimitReached);
        Assert.Equal(0, result.TraceSummary.PostCardMoveCandidateCount);
    }

    [Fact]
    public void CardEffectApplierPlanningProbe_ExactApplication_ReturnsResultingState()
    {
        var probe = new CardEffectApplierPlanningProbe(
            new DefaultCardEffectDefinitionCatalog(
                new[] { ExactFireDefinition("fire_exact") }));
        GameState state = State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"));
        var plan = new CardUsePlan(
            "fire_exact",
            PieceColor.White,
            CardTargetSelection.BoardSquare(Square.Parse("d4")));

        CardEffectPlanningResult result = probe.Probe(
            state,
            Card("fire_exact"),
            plan);

        Assert.Equal(CardEffectApplicationStatus.Exact, result.Status);
        Assert.Equal(CardEffectApplicationCode.Success, result.Code);
        Assert.True(result.HasResultingState);
        TileEffectInfo effect = Assert.Single(result.ResultingState!.TileEffects);
        Assert.Equal("Fire", effect.EffectType);
        Assert.Equal(Square.Parse("d4"), effect.Square);
    }

    [Fact]
    public void CardEffectApplierPlanningProbe_UnsupportedApplication_MapsStatus()
    {
        var probe = new CardEffectApplierPlanningProbe();
        GameState state = State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"));
        var plan = new CardUsePlan(
            "agile",
            PieceColor.White,
            CardTargetSelection.PieceAtSquare(
                new PieceTargetSnapshot(Square.Parse("e2"), PieceColor.White, PieceKind.Pawn)));

        CardEffectPlanningResult result = probe.Probe(
            state,
            Card("agile"),
            plan);

        Assert.Equal(CardEffectApplicationStatus.Unsupported, result.Status);
        Assert.Equal(CardEffectApplicationCode.UnsupportedEffect, result.Code);
        Assert.False(result.HasResultingState);
    }

    [Fact]
    public void CardEffectApplierPlanningProbe_FailedApplication_MapsStatus()
    {
        var probe = new CardEffectApplierPlanningProbe(
            new DefaultCardEffectDefinitionCatalog(
                new[] { ExactFireDefinition("fire_exact") }));
        GameState state = State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"));
        var plan = new CardUsePlan(
            "fire_exact",
            PieceColor.White,
            CardTargetSelection.BoardSquare(Square.Parse("e2")));

        CardEffectPlanningResult result = probe.Probe(
            state,
            Card("fire_exact"),
            plan);

        Assert.Equal(CardEffectApplicationStatus.Failed, result.Status);
        Assert.Equal(CardEffectApplicationCode.StaleTarget, result.Code);
        Assert.False(result.HasResultingState);
    }

    [Fact]
    public void TurnPlanCandidate_ExactCardApplicationDefersPostCardMoveAnalysis()
    {
        var plan = new CardUsePlan(
            "fire_exact",
            PieceColor.White,
            CardTargetSelection.BoardSquare(Square.Parse("d4")));
        CardEffectPlanningResult planningResult = CardEffectPlanningResult.Exact(
            plan,
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")),
            "Exact effect applied.");

        TurnPlanCandidate candidate = TurnPlanCandidate.SkippedCardEffect(
            planningResult,
            allowCoarseCardEffects: false,
            originalIndex: 0);

        Assert.Equal(TurnPlanSkipCode.PostCardMoveAnalysisDeferred, candidate.SkipCode);
        Assert.Equal(CardEffectApplicationStatus.Exact, candidate.SkippedCardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.Success, candidate.SkippedCardApplicationCode);
    }

    [Fact]
    public void TurnPlanCandidate_CoarseCardApplicationWhenAllowedDefersPostCardMoveAnalysis()
    {
        var plan = new CardUsePlan(
            "fire_coarse",
            PieceColor.White,
            CardTargetSelection.BoardSquare(Square.Parse("d4")));
        CardEffectPlanningResult planningResult = CardEffectPlanningResult.Coarse(
            plan,
            State(Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")),
            "Coarse effect applied.");

        TurnPlanCandidate candidate = TurnPlanCandidate.SkippedCardEffect(
            planningResult,
            allowCoarseCardEffects: true,
            originalIndex: 0);

        Assert.Equal(TurnPlanSkipCode.PostCardMoveAnalysisDeferred, candidate.SkipCode);
        Assert.Equal(CardEffectApplicationStatus.Coarse, candidate.SkippedCardApplicationStatus);
        Assert.Equal(CardEffectApplicationCode.CoarseApplied, candidate.SkippedCardApplicationCode);
    }

    [Fact]
    public void PlanTurn_RespectsCardCandidateCount()
    {
        var probe = new RecordingProbe(
            (gameState, card, plan) => CardEffectPlanningResult.Unsupported(
                plan,
                CardEffectApplicationCode.UnsupportedEffect,
                "Card effect application is not connected."));
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 13))),
            new CardTargetingModule(),
            probe,
            new TurnPlannerOptions(cardCandidateCount: 1));

        TurnPlannerResult result = planner.PlanTurn(
            State(
                new[] { Piece(PieceKind.Pawn, PieceColor.White, "e2", "p") },
                new[] { Card("charge"), Card("fire") }));

        Assert.Equal(1, probe.CallCount);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void PlanTurn_FingerprintIsStableForPieceOrder()
    {
        var planner = new UnifiedTurnPlanner(
            new MoveFilter(new StubChessEngine(Move("e2e4", 13))));

        TurnPlannerResult first = planner.PlanTurn(
            State(
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p"),
                Piece(PieceKind.King, PieceColor.White, "e1", "k")));
        TurnPlannerResult second = planner.PlanTurn(
            State(
                Piece(PieceKind.King, PieceColor.White, "e1", "k"),
                Piece(PieceKind.Pawn, PieceColor.White, "e2", "p")));

        Assert.Equal(
            first.SelectedPlan!.OriginStateFingerprint,
            second.SelectedPlan!.OriginStateFingerprint);
    }

    [Fact]
    public void ConstructorAndPlanTurn_InvalidArguments_Throw()
    {
        var planner = new UnifiedTurnPlanner(new MoveFilter(new StubChessEngine()));

        Assert.Throws<ArgumentNullException>(
            () => new UnifiedTurnPlanner(null!));
        Assert.Throws<ArgumentNullException>(
            () => planner.PlanTurn(null!));
    }

    private static MoveCandidate Move(string uciMove, int scoreCentipawns)
    {
        return new MoveCandidate(uciMove, scoreCentipawns, mateIn: null);
    }

    private static GameState State(params PieceInfo[] pieces)
    {
        return State(pieces, Array.Empty<CardInfo>());
    }

    private static GameState State(
        IEnumerable<PieceInfo> pieces,
        IEnumerable<CardInfo> cards)
    {
        var boardState = new BoardState(
            pieces,
            PieceColor.White,
            CastlingRights.None,
            null,
            0,
            1);

        return new GameState(
            boardState,
            cards,
            Array.Empty<TileEffectInfo>());
    }

    private static CardInfo Card(string id)
    {
        return new CardInfo(id, "test", remainingUses: 1);
    }

    private static CardEffectDefinition ExactFireDefinition(string cardId)
    {
        return new CardEffectDefinition(
            cardId,
            CardTargetQuery.EmptySquare(),
            new[]
            {
                new CardEffectPrimitive(
                    CardEffectPrimitiveKind.AddTileEffect,
                    effectType: "Fire",
                    durationTurns: 2,
                    targetBinding: CardEffectPrimitiveTargetBinding.SelectedSquare)
            });
    }

    private static PieceInfo Piece(
        PieceKind kind,
        PieceColor color,
        string square,
        string fenCode)
    {
        return new PieceInfo(
            kind,
            color,
            Square.Parse(square),
            fenCode);
    }

    private sealed class StubChessEngine : IChessEngine
    {
        private readonly Queue<IReadOnlyList<MoveCandidate>> _moveBatches;
        private readonly IReadOnlyList<MoveCandidate> _fallbackMoves;
        private readonly List<int> _variationCounts = new List<int>();

        public StubChessEngine(params MoveCandidate[] moves)
            : this((IReadOnlyList<MoveCandidate>)moves, Array.Empty<IReadOnlyList<MoveCandidate>>())
        {
        }

        public StubChessEngine(
            IReadOnlyList<MoveCandidate> firstBatch,
            params IReadOnlyList<MoveCandidate>[] remainingBatches)
        {
            if (firstBatch == null)
            {
                throw new ArgumentNullException(nameof(firstBatch));
            }

            _fallbackMoves = remainingBatches.Length == 0
                ? firstBatch
                : remainingBatches[remainingBatches.Length - 1];
            _moveBatches = new Queue<IReadOnlyList<MoveCandidate>>();
            _moveBatches.Enqueue(firstBatch);

            foreach (IReadOnlyList<MoveCandidate> batch in remainingBatches)
            {
                _moveBatches.Enqueue(batch);
            }
        }

        public int LastVariationCount { get; private set; }

        public IReadOnlyList<int> VariationCounts => _variationCounts;

        public IReadOnlyList<MoveCandidate> GetTopMoves(BoardState boardState, int variationCount)
        {
            LastVariationCount = variationCount;
            _variationCounts.Add(variationCount);
            return _moveBatches.Count > 0
                ? _moveBatches.Dequeue()
                : _fallbackMoves;
        }

        public PositionEvaluation EvaluatePosition(BoardState boardState, int depth)
        {
            throw new NotSupportedException();
        }

        public bool IsInCheck(BoardState boardState)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingProbe : ICardEffectPlanningProbe
    {
        private readonly Func<GameState, CardInfo, CardUsePlan, CardEffectPlanningResult> _classify;

        public RecordingProbe(
            Func<GameState, CardInfo, CardUsePlan, CardEffectPlanningResult> classify)
        {
            _classify = classify;
        }

        public int CallCount { get; private set; }

        public CardEffectPlanningResult Probe(
            GameState gameState,
            CardInfo card,
            CardUsePlan plan)
        {
            CallCount++;
            return _classify(gameState, card, plan);
        }
    }
}
