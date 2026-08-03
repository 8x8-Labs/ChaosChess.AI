using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Domain.CardEffects;

namespace ChaosChess.AI.Decision.TurnPlanning
{
    public sealed class UnifiedTurnPlanner
    {
        private readonly MoveFilter _moveFilter;
        private readonly TurnPlannerOptions _options;
        private readonly CardTargetingModule? _cardTargetingModule;
        private readonly ICardEffectPlanningProbe _cardEffectPlanningProbe;

        public UnifiedTurnPlanner(
            MoveFilter moveFilter,
            TurnPlannerOptions? options = null)
            : this(moveFilter, cardTargetingModule: null, cardEffectPlanningProbe: null, options, enableCardTargeting: false)
        {
        }

        public UnifiedTurnPlanner(
            MoveFilter moveFilter,
            CardTargetingModule cardTargetingModule,
            ICardEffectPlanningProbe? cardEffectPlanningProbe = null,
            TurnPlannerOptions? options = null)
            : this(moveFilter, cardTargetingModule, cardEffectPlanningProbe, options, enableCardTargeting: true)
        {
        }

        private UnifiedTurnPlanner(
            MoveFilter moveFilter,
            CardTargetingModule? cardTargetingModule,
            ICardEffectPlanningProbe? cardEffectPlanningProbe,
            TurnPlannerOptions? options,
            bool enableCardTargeting)
        {
            _moveFilter = moveFilter ?? throw new ArgumentNullException(nameof(moveFilter));
            _cardTargetingModule = enableCardTargeting
                ? cardTargetingModule ?? throw new ArgumentNullException(nameof(cardTargetingModule))
                : null;
            _cardEffectPlanningProbe = cardEffectPlanningProbe ?? new CardEffectApplierPlanningProbe();
            _options = options ?? new TurnPlannerOptions();
        }

        public TurnPlannerResult PlanTurn(GameState gameState)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            MoveFilterResult moveResult = _moveFilter.GetFilteredMoves(
                gameState,
                _options.NoCardMoveCandidateCount);
            var trace = new TurnPlannerTraceBuilder
            {
                EngineCallCount = 1,
                RootNoCardMoveCandidateCount = moveResult.Recommendations.Count
            };

            var candidates = new List<TurnPlanCandidate>();
            string originStateFingerprint = CreateStateFingerprint(gameState);

            foreach (MoveRecommendation recommendation in moveResult.Recommendations)
            {
                MovePlan movePlan = MovePlan.FromRecommendation(recommendation);
                TurnPlanScore score = CreateMoveScore(recommendation);
                TurnPlan plan = new TurnPlan(
                    gameState.BoardState.SideToMove,
                    originStateFingerprint,
                    score,
                    CreateNoCardRankKey(movePlan),
                    CardEffectApplicationStatus.Exact,
                    CardEffectApplicationCode.Success,
                    cardPlan: null,
                    movePlan: movePlan);

                candidates.Add(TurnPlanCandidate.Selected(plan, recommendation.OriginalIndex));
            }

            AddCardCandidates(
                gameState,
                moveResult,
                originStateFingerprint,
                candidates,
                trace);

            if (candidates.Count == 0)
            {
                candidates.Add(TurnPlanCandidate.Skipped(
                    TurnPlanSkipCode.NoLegalMove,
                    "No legal no-card move recommendation was available.",
                    originalIndex: 0));
            }

            ApplyBeamPruning(candidates, trace);
            RecordOpponentReplyDeferral(candidates, trace);

            return new TurnPlannerResult(
                candidates,
                CreateTraceSummary(candidates, trace));
        }

        private void AddCardCandidates(
            GameState gameState,
            MoveFilterResult moveResult,
            string originStateFingerprint,
            IList<TurnPlanCandidate> candidates,
            TurnPlannerTraceBuilder trace)
        {
            if (_cardTargetingModule == null)
            {
                return;
            }

            IReadOnlyList<MoveCandidate> engineTopMoves = CopyEngineTopMoves(moveResult.Recommendations);
            CardTargetingOptions targetingOptions = new CardTargetingOptions(
                maximumPortalEndpointCandidates: Math.Max(2, _options.TargetCandidateCount));
            int consideredCards = 0;
            int originalIndex = moveResult.Recommendations.Count;

            foreach (CardInfo card in gameState.AvailableCards)
            {
                if (consideredCards >= _options.CardCandidateCount)
                {
                    break;
                }

                if (card.RemainingUses <= 0)
                {
                    continue;
                }

                consideredCards++;
                trace.ConsideredCardCandidateCount++;
                CardPlanDecisionResult decision = _cardTargetingModule.DecideBestPlan(
                    gameState,
                    card,
                    gameState.BoardState.SideToMove,
                    targetingOptions,
                    engineTopMoves);

                if (!decision.HasSelection)
                {
                    trace.CardTargetingSkipCount++;
                    candidates.Add(TurnPlanCandidate.Skipped(
                        MapCardPlanSkipCode(decision.SkipCode),
                        CreateCardTargetingSkipReason(card, decision),
                        originalIndex));
                    originalIndex++;
                    continue;
                }

                CardPlanCandidate cardCandidate = decision.SelectedCandidate!;
                CardEffectPlanningResult planningResult = _cardEffectPlanningProbe.Probe(
                    gameState,
                    card,
                    cardCandidate.Plan);

                if (CanAnalyzePostCardMoves(planningResult))
                {
                    if (!CanSpendEngineCall(trace))
                    {
                        trace.EngineCallLimitSkipCount++;
                        trace.CardEffectSkipCount++;
                        candidates.Add(TurnPlanCandidate.SkippedCardEffect(
                            planningResult,
                            TurnPlanSkipCode.EngineCallLimitExceeded,
                            "Post-card move analysis skipped because the engine call limit was reached.",
                            originalIndex));
                        originalIndex++;
                        continue;
                    }

                    originalIndex = AddPostCardMoveCandidates(
                        planningResult,
                        cardCandidate.Score,
                        originStateFingerprint,
                        candidates,
                        originalIndex,
                        trace);
                    continue;
                }

                trace.CardEffectSkipCount++;
                candidates.Add(TurnPlanCandidate.SkippedCardEffect(
                    planningResult,
                    _options.AllowCoarseCardEffects,
                    originalIndex));
                originalIndex++;
            }
        }

        private int AddPostCardMoveCandidates(
            CardEffectPlanningResult planningResult,
            CardPlanScore cardScore,
            string originStateFingerprint,
            IList<TurnPlanCandidate> candidates,
            int originalIndex,
            TurnPlannerTraceBuilder trace)
        {
            MoveFilterResult postCardMoves = _moveFilter.GetFilteredMoves(
                planningResult.ResultingState!,
                _options.PostCardMoveCandidateCount);
            trace.EngineCallCount++;
            trace.PostCardMoveCandidateCount += postCardMoves.Recommendations.Count;

            if (!postCardMoves.HasRecommendations)
            {
                candidates.Add(TurnPlanCandidate.Skipped(
                    TurnPlanSkipCode.MoveFilterRejected,
                    "No legal post-card move recommendation was available.",
                    originalIndex));
                return originalIndex + 1;
            }

            foreach (MoveRecommendation recommendation in postCardMoves.Recommendations)
            {
                MovePlan movePlan = MovePlan.FromRecommendation(recommendation);
                TurnPlan plan = new TurnPlan(
                    planningResult.Plan.Actor,
                    originStateFingerprint,
                    CreateCardMoveScore(cardScore, recommendation),
                    CreateCardMoveRankKey(planningResult.Plan, movePlan),
                    planningResult.Status,
                    planningResult.Code,
                    planningResult.Plan,
                    movePlan);

                candidates.Add(TurnPlanCandidate.Selected(plan, originalIndex));
                originalIndex++;
            }

            return originalIndex;
        }

        private TurnPlannerTraceSummary CreateTraceSummary(
            IReadOnlyList<TurnPlanCandidate> candidates,
            TurnPlannerTraceBuilder trace)
        {
            int selectedCandidateCount = 0;
            int skippedCandidateCount = 0;

            foreach (TurnPlanCandidate candidate in candidates)
            {
                if (candidate.HasPlan)
                {
                    selectedCandidateCount++;
                }
                else
                {
                    skippedCandidateCount++;
                }
            }

            return new TurnPlannerTraceSummary(
                _options.NoCardMoveCandidateCount,
                _options.CardCandidateCount,
                _options.TargetCandidateCount,
                _options.PostCardMoveCandidateCount,
                _options.OpponentReplyCandidateCount,
                _options.BeamWidth,
                CalculateDeterministicCandidateCap(_options),
                trace.RootNoCardMoveCandidateCount,
                trace.ConsideredCardCandidateCount,
                trace.CardTargetingSkipCount,
                trace.CardEffectSkipCount,
                trace.PostCardMoveCandidateCount,
                selectedCandidateCount,
                skippedCandidateCount,
                trace.EngineCallCount,
                _options.MaximumEngineCallCount,
                trace.EngineCallLimitSkipCount,
                trace.OpponentReplyDeferredCandidateCount,
                trace.BeamPrunedCandidateCount);
        }

        private void ApplyBeamPruning(
            IList<TurnPlanCandidate> candidates,
            TurnPlannerTraceBuilder trace)
        {
            var selectedCandidates = new List<TurnPlanCandidate>();

            foreach (TurnPlanCandidate candidate in candidates)
            {
                if (candidate.HasPlan)
                {
                    selectedCandidates.Add(candidate);
                }
            }

            if (selectedCandidates.Count <= _options.BeamWidth)
            {
                return;
            }

            selectedCandidates.Sort(TurnPlanCandidate.CompareByRank);
            var keptCandidates = new HashSet<TurnPlanCandidate>();

            for (int i = 0; i < _options.BeamWidth; i++)
            {
                keptCandidates.Add(selectedCandidates[i]);
            }

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                TurnPlanCandidate candidate = candidates[i];

                if (!candidate.HasPlan || keptCandidates.Contains(candidate))
                {
                    continue;
                }

                candidates.RemoveAt(i);
                trace.BeamPrunedCandidateCount++;
            }
        }

        private void RecordOpponentReplyDeferral(
            IEnumerable<TurnPlanCandidate> candidates,
            TurnPlannerTraceBuilder trace)
        {
            if (_options.OpponentReplyCandidateCount == 0)
            {
                return;
            }

            foreach (TurnPlanCandidate candidate in candidates)
            {
                if (candidate.HasPlan)
                {
                    trace.OpponentReplyDeferredCandidateCount++;
                }
            }
        }

        private static int CalculateDeterministicCandidateCap(TurnPlannerOptions options)
        {
            return checked(
                options.NoCardMoveCandidateCount +
                (options.CardCandidateCount * Math.Max(1, options.PostCardMoveCandidateCount)));
        }

        private static IReadOnlyList<MoveCandidate> CopyEngineTopMoves(
            IEnumerable<MoveRecommendation> recommendations)
        {
            var moves = new List<MoveCandidate>();

            foreach (MoveRecommendation recommendation in recommendations)
            {
                moves.Add(recommendation.Candidate);
            }

            return moves.AsReadOnly();
        }

        private static TurnPlanScore CreateMoveScore(MoveRecommendation recommendation)
        {
            var components = new[]
            {
                new TurnPlanScoreComponent(
                    "move.engine",
                    recommendation.EngineScore,
                    "Normalized engine score."),
                new TurnPlanScoreComponent(
                    "move.adjustment",
                    recommendation.AdjustmentScore,
                    "Move filter adjustment score."),
                new TurnPlanScoreComponent(
                    "move.total",
                    recommendation.AdjustedScore - recommendation.EngineScore - recommendation.AdjustmentScore,
                    "Move total clamp delta.")
            };

            return new TurnPlanScore(recommendation.AdjustedScore, components);
        }

        private static TurnPlanScore CreateCardMoveScore(
            CardPlanScore cardScore,
            MoveRecommendation recommendation)
        {
            var components = new[]
            {
                new TurnPlanScoreComponent(
                    "card.targeting",
                    cardScore.Total,
                    "Card targeting score."),
                new TurnPlanScoreComponent(
                    "move.engine",
                    recommendation.EngineScore,
                    "Post-card normalized engine score."),
                new TurnPlanScoreComponent(
                    "move.adjustment",
                    recommendation.AdjustmentScore,
                    "Post-card move filter adjustment score."),
                new TurnPlanScoreComponent(
                    "move.total",
                    recommendation.AdjustedScore - recommendation.EngineScore - recommendation.AdjustmentScore,
                    "Post-card move total clamp delta.")
            };

            return new TurnPlanScore(
                cardScore.Total + recommendation.AdjustedScore,
                components);
        }

        private static string CreateNoCardRankKey(MovePlan movePlan)
        {
            return string.Concat(
                "no-card|",
                movePlan.UciMove.ToLowerInvariant());
        }

        private static string CreateCardMoveRankKey(
            CardUsePlan cardPlan,
            MovePlan movePlan)
        {
            return string.Concat(
                "card|",
                cardPlan.CardId.ToLowerInvariant(),
                "|",
                CreateTargetRankKey(cardPlan.Target),
                "|",
                movePlan.UciMove.ToLowerInvariant());
        }

        private static string CreateTargetRankKey(CardTargetSelection target)
        {
            var builder = new StringBuilder();
            builder.Append(target.Kind);

            if (target.Piece != null)
            {
                builder.Append(':');
                builder.Append(target.Piece.Square);
                builder.Append(':');
                builder.Append(target.Piece.ExpectedColor);
                builder.Append(':');
                builder.Append(target.Piece.ExpectedKind);
            }

            for (int i = 0; i < target.Squares.Count; i++)
            {
                builder.Append(i == 0 ? ':' : ',');
                builder.Append(target.Squares[i]);
            }

            return builder.ToString();
        }

        private bool CanAnalyzePostCardMoves(CardEffectPlanningResult planningResult)
        {
            if (!planningResult.HasResultingState)
            {
                return false;
            }

            if (planningResult.Status == CardEffectApplicationStatus.Exact)
            {
                return true;
            }

            return planningResult.Status == CardEffectApplicationStatus.Coarse &&
                _options.AllowCoarseCardEffects;
        }

        private bool CanSpendEngineCall(TurnPlannerTraceBuilder trace)
        {
            return trace.EngineCallCount < _options.MaximumEngineCallCount;
        }

        private static TurnPlanSkipCode MapCardPlanSkipCode(CardPlanSkipCode skipCode)
        {
            switch (skipCode)
            {
                case CardPlanSkipCode.UnsupportedCard:
                case CardPlanSkipCode.MissingStrategy:
                case CardPlanSkipCode.NoLegalCandidate:
                case CardPlanSkipCode.NoBenefit:
                case CardPlanSkipCode.InvalidActor:
                    return TurnPlanSkipCode.UnsupportedCardEffect;

                case CardPlanSkipCode.EngineObservationUnavailable:
                    return TurnPlanSkipCode.EngineObservationUnavailable;

                default:
                    return TurnPlanSkipCode.UnsupportedCardEffect;
            }
        }

        private static string CreateCardTargetingSkipReason(
            CardInfo card,
            CardPlanDecisionResult decision)
        {
            return string.Concat(
                "Card targeting skipped for '",
                card.Id,
                "': ",
                decision.Reason);
        }

        private static string CreateStateFingerprint(GameState gameState)
        {
            BoardState board = gameState.BoardState;
            var builder = new StringBuilder();

            builder.Append("stm=");
            builder.Append(board.SideToMove);
            builder.Append("|castle=");
            builder.Append(board.CastlingRights);
            builder.Append("|ep=");
            builder.Append(board.EnPassantTarget.HasValue
                ? board.EnPassantTarget.Value.ToString()
                : "-");
            builder.Append("|half=");
            builder.Append(board.HalfmoveClock.ToString(CultureInfo.InvariantCulture));
            builder.Append("|full=");
            builder.Append(board.FullmoveNumber.ToString(CultureInfo.InvariantCulture));
            builder.Append("|pieces=");

            var pieces = new List<PieceInfo>(board.Pieces);
            pieces.Sort(ComparePieces);

            for (int i = 0; i < pieces.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                PieceInfo piece = pieces[i];
                builder.Append(piece.Color);
                builder.Append(':');
                builder.Append(piece.Kind);
                builder.Append(':');
                builder.Append(piece.Square);
            }

            builder.Append("|cards=");
            builder.Append(gameState.AvailableCards.Count.ToString(CultureInfo.InvariantCulture));
            builder.Append("|effects=");
            builder.Append(gameState.TileEffects.Count.ToString(CultureInfo.InvariantCulture));

            return builder.ToString();
        }

        private static int ComparePieces(PieceInfo left, PieceInfo right)
        {
            int squareComparison = string.Compare(
                left.Square.ToString(),
                right.Square.ToString(),
                StringComparison.Ordinal);

            if (squareComparison != 0)
            {
                return squareComparison;
            }

            int colorComparison = left.Color.CompareTo(right.Color);

            if (colorComparison != 0)
            {
                return colorComparison;
            }

            return left.Kind.CompareTo(right.Kind);
        }

        private sealed class TurnPlannerTraceBuilder
        {
            public int RootNoCardMoveCandidateCount { get; set; }

            public int ConsideredCardCandidateCount { get; set; }

            public int CardTargetingSkipCount { get; set; }

            public int CardEffectSkipCount { get; set; }

            public int PostCardMoveCandidateCount { get; set; }

            public int EngineCallCount { get; set; }

            public int EngineCallLimitSkipCount { get; set; }

            public int BeamPrunedCandidateCount { get; set; }

            public int OpponentReplyDeferredCandidateCount { get; set; }
        }
    }
}
