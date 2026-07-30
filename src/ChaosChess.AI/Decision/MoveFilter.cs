using System;
using System.Collections.Generic;
using ChaosChess.AI.Abstractions;
using ChaosChess.AI.Domain;

namespace ChaosChess.AI.Decision
{
    public sealed class MoveFilter
    {
        private const int PredictedMateScore = 90;
        private const int MaximumNonMateBoardScore = PredictedMateScore - 1;
        private const int MaximumNonTerminalTotalScore = 99;

        private readonly IChessEngine _chessEngine;
        private readonly MoveFilterOptions _options;

        public MoveFilter(
            IChessEngine chessEngine,
            MoveFilterOptions? options = null)
        {
            _chessEngine = chessEngine ?? throw new ArgumentNullException(nameof(chessEngine));
            _options = options ?? new MoveFilterOptions();
        }

        public MoveFilterResult GetFilteredMoves(GameState gameState, int variationCount)
        {
            if (gameState == null)
            {
                throw new ArgumentNullException(nameof(gameState));
            }

            if (variationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(variationCount), variationCount, "Variation count must be positive.");
            }

            IReadOnlyList<MoveCandidate> candidates = _chessEngine.GetTopMoves(
                gameState.BoardState,
                variationCount);

            if (candidates == null)
            {
                throw new InvalidOperationException("Chess engine returned no move candidates.");
            }

            var recommendations = new List<MoveRecommendation>();
            var filteredMoves = new List<FilteredMoveCandidate>();

            for (int i = 0; i < candidates.Count; i++)
            {
                MoveCandidate candidate = candidates[i];

                if (candidate == null)
                {
                    filteredMoves.Add(new FilteredMoveCandidate(
                        null,
                        i,
                        "Engine returned a null move candidate."));
                    continue;
                }

                if (!TryParseUciMove(candidate.UciMove, out ParsedMove parsedMove))
                {
                    filteredMoves.Add(new FilteredMoveCandidate(
                        candidate,
                        i,
                        "Invalid UCI move."));
                    continue;
                }

                PieceInfo? movingPiece = gameState.BoardState.FindPiece(parsedMove.From);

                if (movingPiece == null)
                {
                    filteredMoves.Add(new FilteredMoveCandidate(
                        candidate,
                        i,
                        "No piece exists on the move source square."));
                    continue;
                }

                if (movingPiece.Color != gameState.BoardState.SideToMove)
                {
                    filteredMoves.Add(new FilteredMoveCandidate(
                        candidate,
                        i,
                        "Move source piece does not match the side to move."));
                    continue;
                }

                PieceInfo? targetPiece = gameState.BoardState.FindPiece(parsedMove.To);

                if (IsPeaceCaptureBlocked(gameState, parsedMove.To, targetPiece))
                {
                    filteredMoves.Add(new FilteredMoveCandidate(
                        candidate,
                        i,
                        "Peace tile cancels capture on the destination square."));
                    continue;
                }

                var reasons = new List<string>();
                int engineScore = NormalizeEngineMoveScore(candidate);
                int adjustmentScore = CalculateAdjustment(
                    gameState,
                    parsedMove,
                    movingPiece,
                    targetPiece,
                    reasons);
                int adjustedScore = Clamp(
                    engineScore + adjustmentScore,
                    -MaximumNonTerminalTotalScore,
                    MaximumNonTerminalTotalScore);

                recommendations.Add(new MoveRecommendation(
                    candidate,
                    i,
                    engineScore,
                    adjustmentScore,
                    adjustedScore,
                    reasons));
            }

            recommendations.Sort(CompareRecommendations);

            return new MoveFilterResult(recommendations, filteredMoves);
        }

        private int CalculateAdjustment(
            GameState gameState,
            ParsedMove parsedMove,
            PieceInfo movingPiece,
            PieceInfo? targetPiece,
            IList<string> reasons)
        {
            int adjustment = 0;

            adjustment += CalculateMineAdjustment(
                gameState,
                parsedMove,
                movingPiece,
                targetPiece,
                reasons);
            adjustment += CalculateDestinationAdjustment(
                gameState,
                parsedMove,
                movingPiece,
                targetPiece,
                reasons);

            return adjustment;
        }

        private int CalculateMineAdjustment(
            GameState gameState,
            ParsedMove parsedMove,
            PieceInfo movingPiece,
            PieceInfo? targetPiece,
            IList<string> reasons)
        {
            if (!CanTriggerMine(movingPiece.Kind))
            {
                return 0;
            }

            var mineSquares = new List<Square>();

            foreach (Square pathSquare in EnumeratePath(parsedMove.From, parsedMove.To))
            {
                foreach (TileEffectInfo effect in gameState.TileEffects)
                {
                    if (IsEffect(effect, "Mine") && effect.Square == pathSquare)
                    {
                        mineSquares.Add(effect.Square);
                    }
                }
            }

            if (mineSquares.Count == 0)
            {
                return 0;
            }

            int materialBalance = 0;

            foreach (PieceInfo piece in EnumeratePostMovePieces(
                gameState.BoardState,
                movingPiece,
                targetPiece,
                parsedMove.To))
            {
                if (!IsInAnyMineBlast(piece.Square, mineSquares))
                {
                    continue;
                }

                int value = GetPieceValue(piece.Kind);
                materialBalance += piece.Color == gameState.BoardState.SideToMove ? -value : value;
            }

            int adjustment = NormalizeCentipawnScore(materialBalance);

            if (adjustment != 0)
            {
                reasons.Add("Mine path explosion adjusted material balance.");
            }

            return adjustment;
        }

        private int CalculateDestinationAdjustment(
            GameState gameState,
            ParsedMove parsedMove,
            PieceInfo movingPiece,
            PieceInfo? targetPiece,
            IList<string> reasons)
        {
            int adjustment = 0;

            foreach (TileEffectInfo effect in gameState.TileEffects)
            {
                if (effect.Square != parsedMove.To)
                {
                    continue;
                }

                if (IsEffect(effect, "Fire"))
                {
                    int fireAdjustment = -NormalizeCentipawnScore(
                        GetPieceValue(movingPiece.Kind) * _options.FireRiskWeight);
                    adjustment += fireAdjustment;

                    if (fireAdjustment != 0)
                    {
                        reasons.Add("Fire tile entry risk applied.");
                    }
                }
                else if (IsEffect(effect, "Blessing"))
                {
                    int blessingAdjustment = NormalizeCentipawnScore(
                        GetPromotionGain(movingPiece.Kind));
                    adjustment += blessingAdjustment;

                    if (blessingAdjustment != 0)
                    {
                        reasons.Add("Blessing tile promotion gain applied.");
                    }
                }
                else if (IsEffect(effect, "Peace") && targetPiece == null)
                {
                    adjustment += _options.PeaceEntryBonus;
                    reasons.Add("Peace tile entry bonus applied.");
                }
                else if (IsEffect(effect, "Portal"))
                {
                    adjustment += CalculatePortalAdjustment(
                        gameState,
                        effect,
                        movingPiece,
                        reasons);
                }
            }

            return adjustment;
        }

        private int CalculatePortalAdjustment(
            GameState gameState,
            TileEffectInfo effect,
            PieceInfo movingPiece,
            IList<string> reasons)
        {
            if (effect.Owner != movingPiece.Color ||
                !effect.DestinationSquare.HasValue ||
                !effect.SharedRemainingUses.HasValue ||
                effect.SharedRemainingUses.Value <= 0)
            {
                return 0;
            }

            int adjustment = _options.PortalEntryBonus;
            PieceInfo? destinationPiece = gameState.BoardState.FindPiece(effect.DestinationSquare.Value);

            if (destinationPiece != null)
            {
                int destinationValue = GetPieceValue(destinationPiece.Kind);
                adjustment += destinationPiece.Color == movingPiece.Color
                    ? -NormalizeCentipawnScore(destinationValue)
                    : NormalizeCentipawnScore(destinationValue);
            }

            reasons.Add("Portal destination adjustment applied.");
            return adjustment;
        }

        private static bool IsPeaceCaptureBlocked(
            GameState gameState,
            Square destination,
            PieceInfo? targetPiece)
        {
            if (targetPiece == null)
            {
                return false;
            }

            foreach (TileEffectInfo effect in gameState.TileEffects)
            {
                if (IsEffect(effect, "Peace") && effect.Square == destination)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<PieceInfo> EnumeratePostMovePieces(
            BoardState boardState,
            PieceInfo movingPiece,
            PieceInfo? targetPiece,
            Square destination)
        {
            foreach (PieceInfo piece in boardState.Pieces)
            {
                if (ReferenceEquals(piece, movingPiece) ||
                    (targetPiece != null && ReferenceEquals(piece, targetPiece)))
                {
                    continue;
                }

                yield return piece;
            }

            yield return new PieceInfo(
                movingPiece.Kind,
                movingPiece.Color,
                destination,
                movingPiece.FenCode);
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

        private static bool IsInAnyMineBlast(Square square, IEnumerable<Square> mineSquares)
        {
            foreach (Square mineSquare in mineSquares)
            {
                if (ChebyshevDistance(square, mineSquare) <= 1)
                {
                    return true;
                }
            }

            return false;
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

            if (uciMove.Length == 5 && !IsAsciiLetter(uciMove[4]))
            {
                return false;
            }

            parsedMove = new ParsedMove(from, to);
            return true;
        }

        private int NormalizeEngineMoveScore(MoveCandidate candidate)
        {
            if (candidate.ScoreCentipawns.HasValue)
            {
                return Clamp(
                    NormalizeCentipawnScore(candidate.ScoreCentipawns.Value),
                    -MaximumNonMateBoardScore,
                    MaximumNonMateBoardScore);
            }

            return candidate.MateIn!.Value > 0
                ? PredictedMateScore
                : -PredictedMateScore;
        }

        private int NormalizeCentipawnScore(double score)
        {
            int rounded = (int)Math.Round(
                score / _options.ScoreNormalizationDivisor,
                MidpointRounding.AwayFromZero);
            return Clamp(rounded, -100, 100);
        }

        private static int GetPromotionGain(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return GetPieceValue(PieceKind.Knight) - GetPieceValue(PieceKind.Pawn);
                case PieceKind.Knight:
                    return GetPieceValue(PieceKind.Rook) - GetPieceValue(PieceKind.Knight);
                case PieceKind.Bishop:
                    return GetPieceValue(PieceKind.Rook) - GetPieceValue(PieceKind.Bishop);
                case PieceKind.Rook:
                    return GetPieceValue(PieceKind.Queen) - GetPieceValue(PieceKind.Rook);
                default:
                    return 0;
            }
        }

        private static int GetPieceValue(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn:
                    return 100;
                case PieceKind.Knight:
                    return 320;
                case PieceKind.Bishop:
                    return 330;
                case PieceKind.Rook:
                    return 500;
                case PieceKind.Queen:
                    return 900;
                case PieceKind.Amazon:
                    return 1300;
                case PieceKind.Chancellor:
                    return 900;
                case PieceKind.KnightRider:
                    return 700;
                default:
                    return 0;
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

        private static bool IsEffect(TileEffectInfo effect, string effectType)
        {
            return string.Equals(
                effect.EffectType,
                effectType,
                StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareRecommendations(
            MoveRecommendation left,
            MoveRecommendation right)
        {
            int scoreComparison = right.AdjustedScore.CompareTo(left.AdjustedScore);

            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            return left.OriginalIndex.CompareTo(right.OriginalIndex);
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

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'a' && value <= 'z') ||
                (value >= 'A' && value <= 'Z');
        }

        private readonly struct ParsedMove
        {
            public ParsedMove(Square from, Square to)
            {
                From = from;
                To = to;
            }

            public Square From { get; }

            public Square To { get; }
        }
    }
}
