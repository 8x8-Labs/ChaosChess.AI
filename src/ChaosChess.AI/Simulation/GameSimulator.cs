using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Decision;
using ChaosChess.AI.Decision.CardTargeting;
using ChaosChess.AI.Domain;
using ChaosChess.AI.Evaluation;

namespace ChaosChess.AI.Simulation
{
    public sealed class GameSimulator
    {
        private const int WhiteHomeRank = 0;
        private const int BlackHomeRank = 7;

        private readonly IChessEngine _chessEngine;
        private readonly GameStateEvaluator _evaluator;
        private readonly CardDecisionModule _cardDecisionModule;
        private readonly CardTargetingModule? _cardTargetingModule;
        private readonly MoveFilter _moveFilter;
        private readonly IRandom? _random;

        public GameSimulator(
            IChessEngine chessEngine,
            GameStateEvaluator evaluator,
            CardDecisionModule cardDecisionModule,
            MoveFilter moveFilter,
            IRandom? random = null,
            CardTargetingModule? cardTargetingModule = null)
        {
            _chessEngine = chessEngine ?? throw new ArgumentNullException(nameof(chessEngine));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _cardDecisionModule = cardDecisionModule ?? throw new ArgumentNullException(nameof(cardDecisionModule));
            _moveFilter = moveFilter ?? throw new ArgumentNullException(nameof(moveFilter));
            _random = random;
            _cardTargetingModule = cardTargetingModule;
        }

        public SimulationResult SimulateFuture(
            GameState initialState,
            PieceColor perspective,
            SimulationOptions? options = null)
        {
            if (initialState == null)
            {
                throw new ArgumentNullException(nameof(initialState));
            }

            EnsureValidColor(perspective);

            SimulationOptions effectiveOptions = options ?? new SimulationOptions();
            IRandom? tieBreakRandom = effectiveOptions.UseRandomTieBreak
                ? _random ?? new SeededSimulationRandom(effectiveOptions.Seed ?? 0)
                : null;
            int? resultSeed = effectiveOptions.UseRandomTieBreak
                ? effectiveOptions.Seed ?? 0
                : effectiveOptions.Seed;

            var steps = new List<SimulationStep>();
            var warnings = new List<string>();
            GameState currentState = initialState;
            SimulationTerminationReason terminationReason = SimulationTerminationReason.HorizonReached;

            for (int ply = 0; ply < effectiveOptions.HorizonPly; ply++)
            {
                var stepWarnings = new List<string>();
                PieceColor sideToMove = currentState.BoardState.SideToMove;
                EvaluationResult evaluation = _evaluator.Evaluate(currentState, perspective);
                EvaluationResult actorEvaluation = sideToMove == perspective
                    ? evaluation
                    : _evaluator.Evaluate(currentState, sideToMove);
                MoveFilterResult moveFilterResult = _moveFilter.GetFilteredMoves(
                    currentState,
                    effectiveOptions.VariationCount);
                CardDecisionResult cardDecision = DecideCards(
                    currentState,
                    actorEvaluation,
                    sideToMove,
                    moveFilterResult);

                if (!moveFilterResult.HasRecommendations)
                {
                    GameState stateBeforeNoRecommendation = currentState;
                    GameState stateAfterNoRecommendation = currentState;

                    if (TryApplyFilteredPeaceBlock(currentState, moveFilterResult, out GameState? blockedState))
                    {
                        GameState confirmedBlockedState = blockedState ?? throw new InvalidOperationException("Peace block produced no state.");
                        stateAfterNoRecommendation = confirmedBlockedState;
                        currentState = confirmedBlockedState;
                        terminationReason = SimulationTerminationReason.MoveBlocked;
                    }
                    else
                    {
                        terminationReason = ClassifyNoMoveTermination(currentState, moveFilterResult);
                    }

                    steps.Add(new SimulationStep(
                        ply,
                        sideToMove,
                        stateBeforeNoRecommendation,
                        evaluation,
                        cardDecision,
                        moveFilterResult,
                        selectedMove: null,
                        stateAfterNoRecommendation,
                        terminationReason,
                        stepWarnings));
                    break;
                }

                MoveRecommendation selectedMove = SelectMove(moveFilterResult.Recommendations, tieBreakRandom);

                if (!TryParseUciMove(selectedMove.UciMove, out ParsedMove parsedMove))
                {
                    terminationReason = SimulationTerminationReason.NoMoveRecommendations;
                    stepWarnings.Add("Selected move has invalid UCI notation.");
                    warnings.AddRange(stepWarnings);
                    steps.Add(new SimulationStep(
                        ply,
                        sideToMove,
                        currentState,
                        evaluation,
                        cardDecision,
                        moveFilterResult,
                        selectedMove,
                        currentState,
                        terminationReason,
                        stepWarnings));
                    break;
                }

                MoveApplicationResult applied = ApplyMove(currentState, parsedMove);
                stepWarnings.AddRange(applied.Warnings);
                warnings.AddRange(applied.Warnings);
                currentState = applied.State;

                steps.Add(new SimulationStep(
                    ply,
                    sideToMove,
                    applied.StateBefore,
                    evaluation,
                    cardDecision,
                    moveFilterResult,
                    selectedMove,
                    currentState,
                    applied.TerminationReason,
                    stepWarnings));

                if (applied.TerminationReason.HasValue)
                {
                    terminationReason = applied.TerminationReason.Value;
                    break;
                }
            }

            return new SimulationResult(
                initialState,
                currentState,
                resultSeed,
                effectiveOptions.HorizonPly,
                steps,
                terminationReason,
                warnings);
        }

        private MoveRecommendation SelectMove(
            IReadOnlyList<MoveRecommendation> recommendations,
            IRandom? tieBreakRandom)
        {
            if (tieBreakRandom == null || recommendations.Count == 1)
            {
                return recommendations[0];
            }

            int bestScore = recommendations[0].AdjustedScore;
            int tiedCount = 1;

            for (int i = 1; i < recommendations.Count; i++)
            {
                if (recommendations[i].AdjustedScore != bestScore)
                {
                    break;
                }

                tiedCount++;
            }

            if (tiedCount == 1)
            {
                return recommendations[0];
            }

            return recommendations[tieBreakRandom.NextInt(0, tiedCount)];
        }

        private CardDecisionResult DecideCards(
            GameState currentState,
            EvaluationResult actorEvaluation,
            PieceColor sideToMove,
            MoveFilterResult moveFilterResult)
        {
            if (_cardTargetingModule == null)
            {
                return _cardDecisionModule.Decide(
                    currentState,
                    actorEvaluation,
                    sideToMove);
            }

            return _cardDecisionModule.Decide(
                currentState,
                actorEvaluation,
                sideToMove,
                _cardTargetingModule,
                targetingOptions: null,
                engineTopMoves: ToMoveCandidates(moveFilterResult.Recommendations));
        }

        private static IReadOnlyList<MoveCandidate> ToMoveCandidates(
            IEnumerable<MoveRecommendation> recommendations)
        {
            var candidates = new List<MoveCandidate>();

            foreach (MoveRecommendation recommendation in recommendations)
            {
                candidates.Add(recommendation.Candidate);
            }

            return candidates;
        }

        private SimulationTerminationReason ClassifyNoMoveTermination(
            GameState state,
            MoveFilterResult moveFilterResult)
        {
            if (moveFilterResult.FilteredMoves.Count > 0)
            {
                return SimulationTerminationReason.NoMoveRecommendations;
            }

            return _chessEngine.IsInCheck(state.BoardState)
                ? SimulationTerminationReason.Checkmate
                : SimulationTerminationReason.Stalemate;
        }

        private static bool TryApplyFilteredPeaceBlock(
            GameState state,
            MoveFilterResult moveFilterResult,
            out GameState? blockedState)
        {
            blockedState = null;

            foreach (FilteredMoveCandidate filteredMove in moveFilterResult.FilteredMoves)
            {
                if (filteredMove.Candidate == null ||
                    !string.Equals(
                        filteredMove.Reason,
                        "Peace tile cancels capture on the destination square.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TryParseUciMove(filteredMove.Candidate.UciMove, out ParsedMove parsedMove))
                {
                    continue;
                }

                var effects = new List<TileEffectInfo>(state.TileEffects);

                if (!TryConsumePeace(effects, parsedMove.To, state.BoardState.FindPiece(parsedMove.To)))
                {
                    continue;
                }

                BoardState nextBoard = new BoardState(
                    state.BoardState.Pieces,
                    Opposite(state.BoardState.SideToMove),
                    state.BoardState.CastlingRights,
                    state.BoardState.EnPassantTarget,
                    state.BoardState.HalfmoveClock,
                    state.BoardState.FullmoveNumber);
                blockedState = new GameState(
                    nextBoard,
                    state.AvailableCards,
                    TickTileEffects(effects));
                return true;
            }

            return false;
        }

        private MoveApplicationResult ApplyMove(GameState state, ParsedMove move)
        {
            var warnings = new List<string>();
            BoardState board = state.BoardState;
            PieceInfo? movingPiece = board.FindPiece(move.From);

            if (movingPiece == null || movingPiece.Color != board.SideToMove)
            {
                return new MoveApplicationResult(
                    state,
                    state,
                    SimulationTerminationReason.NoMoveRecommendations,
                    warnings);
            }

            PieceInfo? targetPiece = board.FindPiece(move.To);
            var tileEffects = new List<TileEffectInfo>(state.TileEffects);
            var pieces = CopyPiecesExcept(board.Pieces, movingPiece);

            PieceInfo? enPassantCapturedPiece = FindEnPassantCapturedPiece(board, movingPiece, move);
            PieceInfo? capturedPiece = targetPiece ?? enPassantCapturedPiece;

            if (capturedPiece != null)
            {
                RemovePieceAt(pieces, capturedPiece.Square);
            }

            MoveRookForCastling(pieces, movingPiece, move);

            PieceInfo movedPiece = CreateMovedPiece(movingPiece, move);
            pieces.Add(movedPiece);

            ApplyMineEffects(pieces, tileEffects, movingPiece, move);

            PieceInfo? survivingMovedPiece = FindPieceBySquare(pieces, move.To);
            if (survivingMovedPiece != null)
            {
                AddUnsupportedEffectWarnings(tileEffects, survivingMovedPiece.Square, warnings);
                ApplyPortalEffect(pieces, tileEffects, survivingMovedPiece);
            }

            IReadOnlyList<TileEffectInfo> tickedEffects = TickTileEffects(tileEffects);
            BoardState nextBoard = CreateNextBoard(
                board,
                pieces,
                movingPiece,
                capturedPiece,
                move);
            GameState nextState = new GameState(
                nextBoard,
                state.AvailableCards,
                tickedEffects);

            SimulationTerminationReason? termination = null;

            if (!HasBothKings(nextBoard))
            {
                termination = SimulationTerminationReason.KingRemoved;
            }

            if (warnings.Count > 0 && termination == null)
            {
                termination = SimulationTerminationReason.UnsupportedEffectEncountered;
            }

            return new MoveApplicationResult(
                state,
                nextState,
                termination,
                warnings);
        }

        private static List<PieceInfo> CopyPiecesExcept(
            IEnumerable<PieceInfo> source,
            PieceInfo excluded)
        {
            var pieces = new List<PieceInfo>();

            foreach (PieceInfo piece in source)
            {
                if (ReferenceEquals(piece, excluded))
                {
                    continue;
                }

                pieces.Add(piece);
            }

            return pieces;
        }

        private static bool TryConsumePeace(
            IList<TileEffectInfo> effects,
            Square destination,
            PieceInfo? targetPiece)
        {
            if (targetPiece == null)
            {
                return false;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (IsEffect(effects[i], "Peace") && effects[i].Square == destination)
                {
                    effects.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private static PieceInfo? FindEnPassantCapturedPiece(
            BoardState board,
            PieceInfo movingPiece,
            ParsedMove move)
        {
            if (movingPiece.Kind != PieceKind.Pawn ||
                !board.EnPassantTarget.HasValue ||
                board.EnPassantTarget.Value != move.To ||
                board.FindPiece(move.To) != null)
            {
                return null;
            }

            int capturedRank = movingPiece.Color == PieceColor.White
                ? move.To.Rank - 1
                : move.To.Rank + 1;

            if (capturedRank < 0 || capturedRank >= Square.BoardSize)
            {
                return null;
            }

            PieceInfo? captured = board.FindPiece(new Square(move.To.File, capturedRank));
            return captured != null && captured.Kind == PieceKind.Pawn && captured.Color != movingPiece.Color
                ? captured
                : null;
        }

        private static void MoveRookForCastling(
            IList<PieceInfo> pieces,
            PieceInfo movingPiece,
            ParsedMove move)
        {
            if (movingPiece.Kind != PieceKind.King ||
                Math.Abs(move.To.File - move.From.File) != 2)
            {
                return;
            }

            int rank = move.From.Rank;
            bool kingSide = move.To.File > move.From.File;
            Square rookFrom = new Square(kingSide ? 7 : 0, rank);
            Square rookTo = new Square(kingSide ? 5 : 3, rank);

            PieceInfo? rook = FindPieceBySquare(pieces, rookFrom);
            if (rook == null)
            {
                return;
            }

            pieces.Remove(rook);
            pieces.Add(new PieceInfo(
                rook.Kind,
                rook.Color,
                rookTo,
                rook.FenCode));
        }

        private static PieceInfo CreateMovedPiece(PieceInfo movingPiece, ParsedMove move)
        {
            if (movingPiece.Kind != PieceKind.Pawn || !move.Promotion.HasValue)
            {
                return new PieceInfo(
                    movingPiece.Kind,
                    movingPiece.Color,
                    move.To,
                    movingPiece.FenCode);
            }

            string fenCode = char.ToLowerInvariant(move.Promotion.Value).ToString();
            return new PieceInfo(
                PieceInfo.InferKind(fenCode),
                movingPiece.Color,
                move.To,
                fenCode);
        }

        private static void ApplyMineEffects(
            IList<PieceInfo> pieces,
            IList<TileEffectInfo> effects,
            PieceInfo movingPiece,
            ParsedMove move)
        {
            if (!CanTriggerMine(movingPiece.Kind))
            {
                return;
            }

            var triggeredMines = new List<TileEffectInfo>();

            foreach (Square pathSquare in EnumeratePath(move.From, move.To))
            {
                foreach (TileEffectInfo effect in effects)
                {
                    if (IsEffect(effect, "Mine") && effect.Square == pathSquare)
                    {
                        triggeredMines.Add(effect);
                    }
                }
            }

            foreach (TileEffectInfo mine in triggeredMines)
            {
                for (int i = pieces.Count - 1; i >= 0; i--)
                {
                    if (ChebyshevDistance(pieces[i].Square, mine.Square) <= 1)
                    {
                        pieces.RemoveAt(i);
                    }
                }

                effects.Remove(mine);
            }
        }

        private static void ApplyPortalEffect(
            IList<PieceInfo> pieces,
            IList<TileEffectInfo> effects,
            PieceInfo movedPiece)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                TileEffectInfo effect = effects[i];

                if (!IsEffect(effect, "Portal") ||
                    effect.Square != movedPiece.Square ||
                    effect.Owner != movedPiece.Color ||
                    !effect.DestinationSquare.HasValue ||
                    !effect.SharedRemainingUses.HasValue ||
                    effect.SharedRemainingUses.Value <= 0)
                {
                    continue;
                }

                RemovePieceAt(pieces, effect.DestinationSquare.Value);
                RemovePieceAt(pieces, movedPiece.Square);
                pieces.Add(new PieceInfo(
                    movedPiece.Kind,
                    movedPiece.Color,
                    effect.DestinationSquare.Value,
                    movedPiece.FenCode));

                int remainingUses = effect.SharedRemainingUses.Value - 1;

                if (remainingUses <= 0)
                {
                    RemovePortalPair(effects, effect);
                }
                else
                {
                    UpdatePortalPairUses(effects, effect, remainingUses);
                }

                return;
            }
        }

        private static void AddUnsupportedEffectWarnings(
            IEnumerable<TileEffectInfo> effects,
            Square movedSquare,
            IList<string> warnings)
        {
            foreach (TileEffectInfo effect in effects)
            {
                if (effect.Square != movedSquare)
                {
                    continue;
                }

                if (IsEffect(effect, "Fire"))
                {
                    warnings.Add("Fire effect is not applied because the current DTO has no delayed removal target.");
                }
                else if (IsEffect(effect, "Blessing"))
                {
                    warnings.Add("Blessing effect is not applied because the current DTO has no residency state.");
                }
            }
        }

        private static IReadOnlyList<TileEffectInfo> TickTileEffects(IEnumerable<TileEffectInfo> effects)
        {
            var ticked = new List<TileEffectInfo>();

            foreach (TileEffectInfo effect in effects)
            {
                int remainingTurns = effect.RemainingTurns <= 0
                    ? effect.RemainingTurns
                    : effect.RemainingTurns - 1;

                if (remainingTurns <= 0)
                {
                    continue;
                }

                ticked.Add(new TileEffectInfo(
                    effect.Id,
                    effect.EffectType,
                    effect.Square,
                    effect.Owner,
                    remainingTurns,
                    effect.DestinationSquare,
                    effect.SharedRemainingUses));
            }

            return ticked;
        }

        private static BoardState CreateNextBoard(
            BoardState previousBoard,
            IEnumerable<PieceInfo> pieces,
            PieceInfo movingPiece,
            PieceInfo? capturedPiece,
            ParsedMove move)
        {
            return new BoardState(
                pieces,
                Opposite(previousBoard.SideToMove),
                UpdateCastlingRights(previousBoard.CastlingRights, movingPiece, capturedPiece, move),
                CreateEnPassantTarget(movingPiece, move),
                movingPiece.Kind == PieceKind.Pawn || capturedPiece != null
                    ? 0
                    : previousBoard.HalfmoveClock + 1,
                previousBoard.SideToMove == PieceColor.Black
                    ? previousBoard.FullmoveNumber + 1
                    : previousBoard.FullmoveNumber);
        }

        private static CastlingRights UpdateCastlingRights(
            CastlingRights rights,
            PieceInfo movingPiece,
            PieceInfo? capturedPiece,
            ParsedMove move)
        {
            CastlingRights updated = rights;

            if (movingPiece.Kind == PieceKind.King)
            {
                updated &= movingPiece.Color == PieceColor.White
                    ? ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide)
                    : ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);
            }

            if (movingPiece.Kind == PieceKind.Rook)
            {
                updated = ClearRookCastlingRight(updated, movingPiece.Color, move.From);
            }

            if (capturedPiece != null && capturedPiece.Kind == PieceKind.Rook)
            {
                updated = ClearRookCastlingRight(updated, capturedPiece.Color, capturedPiece.Square);
            }

            return updated;
        }

        private static CastlingRights ClearRookCastlingRight(
            CastlingRights rights,
            PieceColor color,
            Square rookSquare)
        {
            if (color == PieceColor.White && rookSquare.Rank == WhiteHomeRank)
            {
                if (rookSquare.File == 0)
                {
                    return rights & ~CastlingRights.WhiteQueenSide;
                }

                if (rookSquare.File == 7)
                {
                    return rights & ~CastlingRights.WhiteKingSide;
                }
            }

            if (color == PieceColor.Black && rookSquare.Rank == BlackHomeRank)
            {
                if (rookSquare.File == 0)
                {
                    return rights & ~CastlingRights.BlackQueenSide;
                }

                if (rookSquare.File == 7)
                {
                    return rights & ~CastlingRights.BlackKingSide;
                }
            }

            return rights;
        }

        private static Square? CreateEnPassantTarget(PieceInfo movingPiece, ParsedMove move)
        {
            if (movingPiece.Kind != PieceKind.Pawn ||
                Math.Abs(move.To.Rank - move.From.Rank) != 2)
            {
                return null;
            }

            return new Square(
                move.From.File,
                (move.From.Rank + move.To.Rank) / 2);
        }

        private static void UpdatePortalPairUses(
            IList<TileEffectInfo> effects,
            TileEffectInfo triggeredEffect,
            int remainingUses)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                TileEffectInfo effect = effects[i];

                if (!IsPortalPairMember(effect, triggeredEffect))
                {
                    continue;
                }

                effects[i] = new TileEffectInfo(
                    effect.Id,
                    effect.EffectType,
                    effect.Square,
                    effect.Owner,
                    effect.RemainingTurns,
                    effect.DestinationSquare,
                    remainingUses);
            }
        }

        private static void RemovePortalPair(
            IList<TileEffectInfo> effects,
            TileEffectInfo triggeredEffect)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (IsPortalPairMember(effects[i], triggeredEffect))
                {
                    effects.RemoveAt(i);
                }
            }
        }

        private static bool IsPortalPairMember(
            TileEffectInfo effect,
            TileEffectInfo triggeredEffect)
        {
            return IsEffect(effect, "Portal") &&
                (ReferenceEquals(effect, triggeredEffect) ||
                    effect.Square == triggeredEffect.DestinationSquare ||
                    effect.DestinationSquare == triggeredEffect.Square);
        }

        private static PieceInfo? FindPieceBySquare(IEnumerable<PieceInfo> pieces, Square square)
        {
            foreach (PieceInfo piece in pieces)
            {
                if (piece.Square == square)
                {
                    return piece;
                }
            }

            return null;
        }

        private static void RemovePieceAt(IList<PieceInfo> pieces, Square square)
        {
            for (int i = pieces.Count - 1; i >= 0; i--)
            {
                if (pieces[i].Square == square)
                {
                    pieces.RemoveAt(i);
                }
            }
        }

        private static bool HasBothKings(BoardState board)
        {
            bool hasWhiteKing = false;
            bool hasBlackKing = false;

            foreach (PieceInfo piece in board.Pieces)
            {
                if (piece.Kind == PieceKind.King)
                {
                    if (piece.Color == PieceColor.White)
                    {
                        hasWhiteKing = true;
                    }
                    else
                    {
                        hasBlackKing = true;
                    }
                }
            }

            return hasWhiteKing && hasBlackKing;
        }

        private static IEnumerable<Square> EnumeratePath(Square from, Square to)
        {
            int dx = to.File - from.File;
            int dy = to.Rank - from.Rank;
            int divisor = GreatestCommonDivisor(Math.Abs(dx), Math.Abs(dy));

            if (divisor == 0)
            {
                yield break;
            }

            int stepFile = dx / divisor;
            int stepRank = dy / divisor;

            for (int i = 1; i <= divisor; i++)
            {
                yield return new Square(
                    from.File + (stepFile * i),
                    from.Rank + (stepRank * i));
            }
        }

        private static bool CanTriggerMine(PieceKind kind)
        {
            return kind == PieceKind.Rook ||
                kind == PieceKind.Queen ||
                kind == PieceKind.Amazon ||
                kind == PieceKind.Chancellor ||
                kind == PieceKind.KnightRider;
        }

        private static bool TryParseUciMove(string uciMove, out ParsedMove parsedMove)
        {
            parsedMove = default;

            if (uciMove == null || (uciMove.Length != 4 && uciMove.Length != 5))
            {
                return false;
            }

            if (!Square.TryParse(uciMove.Substring(0, 2), out Square from) ||
                !Square.TryParse(uciMove.Substring(2, 2), out Square to))
            {
                return false;
            }

            char? promotion = null;

            if (uciMove.Length == 5)
            {
                if (!IsAsciiLetter(uciMove[4]))
                {
                    return false;
                }

                promotion = uciMove[4];
            }

            parsedMove = new ParsedMove(from, to, promotion);
            return true;
        }

        private static bool IsEffect(TileEffectInfo effect, string effectType)
        {
            return string.Equals(
                effect.EffectType,
                effectType,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                int remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }

        private static int ChebyshevDistance(Square left, Square right)
        {
            return Math.Max(
                Math.Abs(left.File - right.File),
                Math.Abs(left.Rank - right.Rank));
        }

        private static PieceColor Opposite(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private static void EnsureValidColor(PieceColor color)
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                throw new ArgumentOutOfRangeException(nameof(color), color, "Unknown piece color.");
            }
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'a' && value <= 'z') ||
                (value >= 'A' && value <= 'Z');
        }

        private readonly struct ParsedMove
        {
            public ParsedMove(Square from, Square to, char? promotion)
            {
                From = from;
                To = to;
                Promotion = promotion;
            }

            public Square From { get; }

            public Square To { get; }

            public char? Promotion { get; }
        }

        private sealed class MoveApplicationResult
        {
            public MoveApplicationResult(
                GameState stateBefore,
                GameState state,
                SimulationTerminationReason? terminationReason,
                IEnumerable<string> warnings)
            {
                StateBefore = stateBefore ?? throw new ArgumentNullException(nameof(stateBefore));
                State = state ?? throw new ArgumentNullException(nameof(state));
                TerminationReason = terminationReason;
                Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
            }

            public GameState StateBefore { get; }

            public GameState State { get; }

            public SimulationTerminationReason? TerminationReason { get; }

            public IEnumerable<string> Warnings { get; }
        }

        private sealed class SeededSimulationRandom : IRandom
        {
            private uint _state;

            public SeededSimulationRandom(int seed)
            {
                _state = unchecked((uint)seed);
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                if (minInclusive >= maxExclusive)
                {
                    throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Maximum must be greater than minimum.");
                }

                uint range = (uint)(maxExclusive - minInclusive);
                return minInclusive + (int)(NextUInt32() % range);
            }

            public double NextDouble()
            {
                return NextUInt32() / ((double)uint.MaxValue + 1.0);
            }

            private uint NextUInt32()
            {
                _state = unchecked((_state * 1664525u) + 1013904223u);
                return _state;
            }
        }
    }
}
